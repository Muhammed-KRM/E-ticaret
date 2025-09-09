using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using KeremProject1backend.Services; // SessionService için
using System;

namespace KeremProject1backend.Operations
{
    public static class CartOperations
    {
        // Helper method: GuestCartId oluşturma
        private static string GenerateGuestCartId()
        {
            return "guest_" + DateTime.UtcNow.Ticks + "_" + Guid.NewGuid().ToString("N")[..8];
        }

        public static async Task<BaseResponse> AddToCartOperation(string? token, AddToCartRequest request, GeneralContext dbContext, UsersContext usersContext)
        {
            var response = new BaseResponse();
            try
            {
                int? userIdInt = null;
                string? guestId = null;

                // Token varsa üye kullanıcı, yoksa üye olmayan kullanıcı
                if (!string.IsNullOrEmpty(token))
                {
                    var currentSession = SessionService.TestToken(token, usersContext);
                    if (currentSession == null)
                    {
                        response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                        return response;
                    }
                    userIdInt = currentSession._user.Id;
                }
                else
                {
                    // Üye olmayan kullanıcı için GuestId kontrol et veya oluştur
                    if (string.IsNullOrEmpty(request.GuestCartId))
                    {
                        // GuestCartId yoksa yeni bir tane oluştur
                        guestId = GenerateGuestCartId();
                    }
                    else
                    {
                        guestId = request.GuestCartId;
                    }
                }

                // 1. Ürünü veritabanından bul ve kontrol et
                var product = await dbContext.Products.FindAsync(request.ProductId);

                if (product == null || !product.IsActive)
                {
                    response.GenerateError(4004, "Ürün bulunamadı veya aktif değil.");
                    return response;
                }

                // 2. Kullanıcının sepetini bul veya oluştur
                ShoppingCart? cart = null;
                
                if (userIdInt.HasValue)
                {
                    // Üye kullanıcının sepeti
                    cart = await dbContext.ShoppingCarts
                                          .Include(c => c.Items)
                                          .FirstOrDefaultAsync(c => c.UserId == userIdInt);

                    if (cart == null)
                    {
                        cart = new ShoppingCart { UserId = userIdInt };
                        dbContext.ShoppingCarts.Add(cart);
                    }
                }
                else
                {
                    // Üye olmayan kullanıcının sepeti
                    cart = await dbContext.ShoppingCarts
                                          .Include(c => c.Items)
                                          .FirstOrDefaultAsync(c => c.GuestId == guestId);
                    
                    if (cart == null)
                    {
                        cart = new ShoppingCart { GuestId = guestId };
                        dbContext.ShoppingCarts.Add(cart);
                    }
                }

                // 3. Sepetteki mevcut ürünü bul
                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                var oldCartItem = cartItem != null ? new CartItem 
                {
                    Id = cartItem.Id,
                    ShoppingCartId = cartItem.ShoppingCartId,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    UnitPrice = cartItem.UnitPrice,
                    Quantity = cartItem.Quantity
                } : null;

                // 4. Stok kontrolü
                int quantityAlreadyInCart = cartItem?.Quantity ?? 0;
                int requestedTotalQuantity = quantityAlreadyInCart + request.Quantity;

                if (product.StockQuantity < requestedTotalQuantity)
                {
                    response.GenerateError(4005, $"Yetersiz stok. Maksimum {product.StockQuantity} adet eklenebilir.");
                    return response;
                }

                // 5. Sepet öğesini ekle veya güncelle
                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        UnitPrice = product.Price, // Gerçek fiyat
                        ProductName = product.Name   // Gerçek isim
                    };
                    cart.Items.Add(cartItem);
                }
                else
                {
                    cartItem.Quantity = requestedTotalQuantity; // Miktarı güncelle
                }

                // 6. Değişiklikleri kaydet
                await dbContext.SaveChangesAsync();

                // Ekleme işlemini logla
                await LogServices.AddLogAsync(
                    dbContext,
                    "CartItems",
                    oldCartItem == null ? 'C' : 'U', // Create veya Update
                    userIdInt ?? 0, // Üye olmayan kullanıcı için 0
                    oldCartItem, // Önceki değer (null ise yeni eklemedir)
                    new { // Yeni/Değişmiş değer
                        Id = cartItem.Id,
                        ShoppingCartId = cartItem.ShoppingCartId,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        UnitPrice = cartItem.UnitPrice,
                        Quantity = cartItem.Quantity
                    }
                );

                response.Response = new { GuestCartId = guestId }; // Üye olmayan kullanıcı için GuestCartId döndür
                response.GenerateSuccess("Ürün sepete eklendi.");
            }
            catch (System.Exception ex)
            {
                // TODO: Daha spesifik loglama eklenebilir
                response.GenerateError(5000, $"Sepete eklerken hata: {ex.Message}");
            }
            return response;
        }

        public static async Task<BaseResponse> RemoveFromCartOperation(string? token, int cartItemId, string? guestCartId, GeneralContext dbContext, UsersContext usersContext)
        {
            var response = new BaseResponse();
            try
            {
                int? userIdInt = null;
                
                // Token varsa üye kullanıcı kontrolü
                if (!string.IsNullOrEmpty(token))
                {
                    var currentSession = SessionService.TestToken(token, usersContext);
                    if (currentSession == null)
                    {
                        response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                        return response;
                    }
                    userIdInt = currentSession._user.Id;
                }
                else if (string.IsNullOrEmpty(guestCartId))
                {
                    response.GenerateError(4006, "Üye olmayan kullanıcılar için GuestCartId gereklidir.");
                    return response;
                }

                // CartItem'ı bul ve sahiplik kontrolü yap
                CartItem? cartItem = null;
                
                if (userIdInt.HasValue)
                {
                    // Üye kullanıcının sepet öğesi
                    cartItem = await dbContext.CartItems
                            .Include(i => i.ShoppingCart)
                            .FirstOrDefaultAsync(i => i.Id == cartItemId && i.ShoppingCart != null && i.ShoppingCart.UserId == userIdInt);
                }
                else
                {
                    // Üye olmayan kullanıcının sepet öğesi
                    cartItem = await dbContext.CartItems
                        .Include(i => i.ShoppingCart)
                        .FirstOrDefaultAsync(i => i.Id == cartItemId && i.ShoppingCart != null && i.ShoppingCart.GuestId == guestCartId);
                }

                if (cartItem == null)
                {
                    response.GenerateError(4003, "Sepetteki ürün bulunamadı veya size ait değil.");
                    return response;
                }

                // Silinecek sepet öğesinin bilgilerini kopyala (log için)
                var oldCartItem = new CartItem
                {
                    Id = cartItem.Id,
                    ShoppingCartId = cartItem.ShoppingCartId,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    UnitPrice = cartItem.UnitPrice,
                    Quantity = cartItem.Quantity
                };

                dbContext.CartItems.Remove(cartItem);
                await dbContext.SaveChangesAsync();

                // Silme işlemini logla
                await LogServices.AddLogAsync(
                    dbContext,
                    "CartItems",
                    'D', // Delete
                    userIdInt ?? 0, // Üye olmayan kullanıcı için 0
                    oldCartItem, // Silinen değer
                    null // Yeni değer yok
                );

                response.GenerateSuccess("Ürün sepetten kaldırıldı.");
            }
            catch (System.Exception ex)
            {
                response.GenerateError(5001, $"Sepetten kaldırırken hata: {ex.Message}");
            }
            return response;
        }

        public static async Task<BaseResponse> GetCartOperation(string? token, string? guestCartId, GeneralContext dbContext, UsersContext usersContext, string baseUrl)
        {
            var response = new BaseResponse();
            try
            {
                int? userIdInt = null;
                
                // Token varsa üye kullanıcı kontrolü
                if (!string.IsNullOrEmpty(token))
                {
                    var currentSession = SessionService.TestToken(token, usersContext);
                    if (currentSession == null)
                    {
                        response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                        return response;
                    }
                    userIdInt = currentSession._user.Id;
                }
                else if (string.IsNullOrEmpty(guestCartId))
                {
                    // GuestCartId yoksa boş sepet döndür
                    var emptyGuestCart = new KeremProject1backend.Models.Responses.CartResponse 
                    { 
                        UserId = null, 
                        GuestId = null,
                        Items = new List<KeremProject1backend.Models.Responses.CartItemResponse>(), 
                        TotalPrice = 0 
                    };
                    response.Response = emptyGuestCart;
                    response.GenerateSuccess("Sepet bilgisi başarıyla getirildi.");
                    return response;
                }

                KeremProject1backend.Models.Responses.CartResponse? cart = null;
                
                if (userIdInt.HasValue)
                {
                    // Üye kullanıcının sepeti
                    cart = await dbContext.ShoppingCarts
                                          .Include(c => c.Items)
                                            .ThenInclude(i => i.Product)
                                          .Where(c => c.UserId == userIdInt)
                                          .Select(c => new KeremProject1backend.Models.Responses.CartResponse
                                          {
                                              UserId = c.UserId,
                                              GuestId = null,
                                              Items = c.Items.Select(i => new KeremProject1backend.Models.Responses.CartItemResponse
                                              {
                                                  ItemId = i.Id,
                                                  ProductId = i.ProductId,
                                                  ProductName = i.ProductName,
                                                  Quantity = i.Quantity,
                                                  UnitPrice = i.UnitPrice,
                                                  ImageUrl = i.Product != null && !string.IsNullOrEmpty(i.Product.ImageUrl) ? $"{baseUrl}/api/File/Download/{i.Product.ImageUrl}" : null
                                              }).ToList()
                                          })
                                          .FirstOrDefaultAsync();
                }
                else
                {
                    // Üye olmayan kullanıcının sepeti
                    cart = await dbContext.ShoppingCarts
                                          .Include(c => c.Items)
                                            .ThenInclude(i => i.Product)
                                          .Where(c => c.GuestId == guestCartId)
                                          .Select(c => new KeremProject1backend.Models.Responses.CartResponse
                                          {
                                              UserId = null,
                                              GuestId = c.GuestId,
                                              Items = c.Items.Select(i => new KeremProject1backend.Models.Responses.CartItemResponse
                                              {
                                                  ItemId = i.Id,
                                                  ProductId = i.ProductId,
                                                  ProductName = i.ProductName,
                                                  Quantity = i.Quantity,
                                                  UnitPrice = i.UnitPrice,
                                                  ImageUrl = i.Product != null && !string.IsNullOrEmpty(i.Product.ImageUrl) ? $"{baseUrl}/api/File/Download/{i.Product.ImageUrl}" : null
                                              }).ToList()
                                          })
                                          .FirstOrDefaultAsync();
                }

                if (cart == null)
                {
                    var emptyCart = new KeremProject1backend.Models.Responses.CartResponse 
                    { 
                        UserId = userIdInt, 
                        GuestId = guestCartId,
                        Items = new List<KeremProject1backend.Models.Responses.CartItemResponse>(), 
                        TotalPrice = 0 
                    };
                    response.Response = emptyCart;
                    response.GenerateSuccess("Sepet bilgisi başarıyla getirildi.");
                    return response;
                }

                cart.TotalPrice = cart.Items.Sum(i => i.TotalPrice);
                response.Response = cart;
                response.GenerateSuccess("Sepet bilgisi başarıyla getirildi.");
            }
            catch (System.Exception ex)
            {
                response.GenerateError(5002, $"Sepet getirilirken hata: {ex.Message}");
            }
            return response;
        }
        public static async Task<BaseResponse> UpdateCartOperation(string? token, int cartItemId, int newQuantity, string? guestCartId, GeneralContext dbContext, UsersContext usersContext)
        {
            var response = new BaseResponse();
            try
            {
                int? userIdInt = null;
                
                // Token varsa üye kullanıcı kontrolü
                if (!string.IsNullOrEmpty(token))
                {
                    var currentSession = SessionService.TestToken(token, usersContext);
                    if (currentSession == null)
                    {
                        response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                        return response;
                    }
                    userIdInt = currentSession._user.Id;
                }
                else if (string.IsNullOrEmpty(guestCartId))
                {
                    response.GenerateError(4006, "Üye olmayan kullanıcılar için GuestCartId gereklidir.");
                    return response;
                }

                // 1. Sepet öğesini bul ve kontrol et
                CartItem? cartItem = null;
                
                if (userIdInt.HasValue)
                {
                    // Üye kullanıcının sepet öğesi
                    cartItem = await dbContext.CartItems
                    .Include(i => i.ShoppingCart)
                    .FirstOrDefaultAsync(i => i.Id == cartItemId && i.ShoppingCart != null && i.ShoppingCart.UserId == userIdInt);
                }
                else
                {
                    // Üye olmayan kullanıcının sepet öğesi
                    cartItem = await dbContext.CartItems
                        .Include(i => i.ShoppingCart)
                        .FirstOrDefaultAsync(i => i.Id == cartItemId && i.ShoppingCart != null && i.ShoppingCart.GuestId == guestCartId);
                }

                if (cartItem == null)
                {
                    response.GenerateError(4003, "Sepet öğesi bulunamadı veya size ait değil.");
                    return response;
                }

                // Değişiklik öncesi sepet öğesi bilgilerini kopyala (log için)
                var oldCartItem = new CartItem
                {
                    Id = cartItem.Id,
                    ShoppingCartId = cartItem.ShoppingCartId,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    UnitPrice = cartItem.UnitPrice,
                    Quantity = cartItem.Quantity
                };

                // 2. Yeni miktar sıfır veya negatifse ürünü sepetten kaldır
                if (newQuantity <= 0)
                {
                    dbContext.CartItems.Remove(cartItem);
                    await dbContext.SaveChangesAsync();

                    // Silme işlemini logla
                    await LogServices.AddLogAsync(
                        dbContext,
                        "CartItems",
                        'D', // Delete
                        userIdInt ?? 0, // Üye olmayan kullanıcı için 0
                        oldCartItem, // Silinen değer
                        null // Yeni değer yok
                    );

                    response.GenerateSuccess("Ürün sepetten kaldırıldı.");
                    return response;
                }

                // 3. Ürünü veritabanından bul ve stok kontrolü yap
                var product = await dbContext.Products.FindAsync(cartItem.ProductId);
                if (product == null || !product.IsActive)
                {
                    response.GenerateError(4004, "Ürün bulunamadı veya aktif değil.");
                    return response;
                }

                if (product.StockQuantity < newQuantity)
                {
                    response.GenerateError(4005, $"Yetersiz stok. Maksimum {product.StockQuantity} adet eklenebilir.");
                    return response;
                }

                // 4. Sepet öğesini güncelle
                cartItem.Quantity = newQuantity;
                dbContext.Entry(cartItem).State = EntityState.Modified;

                // 5. Değişiklikleri kaydet
                await dbContext.SaveChangesAsync();

                // Güncelleme işlemini logla
                await LogServices.AddLogAsync(
                    dbContext,
                    "CartItems",
                    'U', // Update
                    userIdInt ?? 0, // Üye olmayan kullanıcı için 0
                    oldCartItem, // Değişmeden önceki değer
                    new { // Değişmiş değer
                        Id = cartItem.Id,
                        ShoppingCartId = cartItem.ShoppingCartId,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        UnitPrice = cartItem.UnitPrice,
                        Quantity = cartItem.Quantity
                    }
                );

                response.GenerateSuccess("Sepet güncellendi.");
            }
            catch (System.Exception ex)
            {
                response.GenerateError(5001, $"Sepet güncellenirken hata: {ex.Message}");
            }
            return response;
        }
    }
} 