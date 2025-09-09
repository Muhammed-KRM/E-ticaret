using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace KeremProject1backend.Operations
{
    public static class ProductOperations
    {
        // Admin yetkisi kontrolü için yardımcı metot
        private static bool IsAdmin(string token, UsersContext usersContext)
        {
            var session = SessionService.TestToken(token, usersContext);
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin || 
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }

        // Ürünü Resimle Birlikte Ekle (Yeni)
        public static async Task<BaseResponse> CreateProductWithImageOperation(
            string token, 
            CreateProductWithImageRequest request, 
            GeneralContext dbContext, 
            UsersContext usersContext, 
            FileService fileService, 
            ILogger logger,
            string baseUrl)
        {
            var response = new BaseResponse();
            try
            {
                // Oturum kontrolü
                var session = SessionService.TestToken(token, usersContext);
                if (session == null || session._user == null)
                {
                    response.GenerateError(1000, "Geçersiz oturum veya oturum süresi dolmuş.");
                    return response;
                }
                int userId = session._user.Id;

                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId))
                {
                    response.GenerateError(4004, "Belirtilen kategori bulunamadı.");
                    return response;
                }
                
                var newProduct = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    IsDiscounted = request.IsDiscounted,
                    OriginalPrice = request.IsDiscounted ? request.OriginalPrice : null,
                    StockQuantity = request.StockQuantity,
                    CategoryId = request.CategoryId,
                    IsActive = true
                };

                dbContext.Products.Add(newProduct);
                await dbContext.SaveChangesAsync();
                
                string? imageFileNameOnly = null;
                if (request.ProductImage != null && request.ProductImage.Length > 0)
                {
                    try
                    {
                        var fileUploadResult = await fileService.UploadFileAsync(
                            request.ProductImage, "product-images", newProduct.Id, $"{newProduct.Name} ürününe ait resim");
                            
                        var fileModel = new FileModel
                        {
                            FileName = fileUploadResult.FileName,
                            ContentType = fileUploadResult.ContentType,
                            FileSize = fileUploadResult.FileSize,
                            FilePath = fileUploadResult.FilePath,
                            FileType = "product-image",
                            RelatedEntityId = newProduct.Id,
                            Description = $"{newProduct.Name} ürün görseli",
                            UploadDate = DateTime.Now,
                            IsActive = true
                        };
                        
                        dbContext.Files.Add(fileModel);
                        await dbContext.SaveChangesAsync();
                        
                        logger?.LogInformation($"CreateProduct: fileModel.FileName DÖNEN DEĞER: '{fileModel.FileName}'");
                        imageFileNameOnly = fileModel.FileName;
                        logger?.LogInformation($"CreateProduct: imageFileNameOnly: '{imageFileNameOnly}'");
                        
                        // Resim yükleme logunu kaydet
                        await LogServices.AddLogAsync(
                            dbContext, 
                            "Files", 
                            'C', // Create
                            userId,
                            null, // Önceki değer yok
                            fileModel // Yeni eklenen resim
                        );
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, $"Ürün oluşturuldu (ID={newProduct.Id}) ancak resim yüklenirken hata oluştu.");
                    }
                }
                
                if (!string.IsNullOrEmpty(imageFileNameOnly))
                {
                    newProduct.ImageUrl = imageFileNameOnly;
                    dbContext.Entry(newProduct).State = EntityState.Modified;
                    await dbContext.SaveChangesAsync();
                }
                
                logger?.LogInformation($"Yeni ürün eklendi: ID={newProduct.Id}, Veritabanına kaydedilen ImageUrl: '{newProduct.ImageUrl}'");
                string? fullImageUrl = null;
                if (!string.IsNullOrEmpty(newProduct.ImageUrl))
                {
                    fullImageUrl = $"{baseUrl}/api/File/Download/{newProduct.ImageUrl}";
                }
                
                // Ana ürün oluşturma logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext, 
                    "Products", 
                    'C', // Create
                    userId, 
                    null, // Önceki değer yok
                    newProduct // Yeni eklenen ürün
                );
                
                response.Response = new { ProductId = newProduct.Id, ImageUrl = fullImageUrl };
                response.GenerateSuccess("Ürün başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "CreateProductWithImageOperation sırasında hata oluştu.");
                response.GenerateError(5200, $"Ürün eklenirken hata: {ex.Message}");
            }
            return response;
        }

        // Ürün Detayı Getirme - ImageUrl'i mutlak yap
        public static async Task<BaseResponse> GetProductByIdOperation(int productId, GeneralContext dbContext, ILogger logger, string baseUrl)
        {
             var response = new BaseResponse();
            try
            {
                var product = await dbContext.Products
                                        .Include(p => p.Category)
                                        .Where(p => p.Id == productId)
                                        .Select(p => new ProductDetailsResponse 
                                        {
                                            Id = p.Id,
                                            Name = p.Name,
                                            Description = p.Description,
                                            Price = p.Price,
                                            OriginalPrice = p.OriginalPrice,
                                            IsDiscounted = p.IsDiscounted,
                                            DiscountPercentage = p.IsDiscounted && p.OriginalPrice.HasValue && p.OriginalPrice > 0 
                                                ? Math.Round(((p.OriginalPrice.Value - p.Price) / p.OriginalPrice.Value) * 100, 2) 
                                                : null,
                                            StockQuantity = p.StockQuantity,
                                            ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? $"{baseUrl}/api/File/Download/{p.ImageUrl}" : null,
                                            IsActive = p.IsActive,
                                        })
                                        .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.GenerateError(4404, "Ürün bulunamadı.");
                    return response;
                }

                response.Response = product;
                response.GenerateSuccess("Ürün detayları başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"GetProductByIdOperation sırasında hata oluştu (ID: {productId}).");
                response.GenerateError(5201, $"Ürün getirilirken hata: {ex.Message}");
            }
            return response;
        }

        // Tüm Ürünleri Listeleme - ImageUrl'leri mutlak yap
        public static async Task<BaseResponse> GetAllProductsOperation(string? token, GetAllProductsRequest request, GeneralContext dbContext, UsersContext _usersContext, ILogger logger, string baseUrl)
        {
            var response = new BaseResponse();
            try
            {
                var productsQuery = dbContext.Products
                    .Include(p => p.Category) // Kategoriyi dahil et
                    .AsQueryable(); // Filtreleme ve sıralama için IQueryable kullan

                // İleride eklenecek filtreleme ve sıralama işlemleri burada olabilir
                // Örnek: if (!string.IsNullOrEmpty(request.CategoryName)) productsQuery = productsQuery.Where(p => p.Category.Name == request.CategoryName);

                var products = await productsQuery.Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        OriginalPrice = p.OriginalPrice,
                        IsDiscounted = p.IsDiscounted,
                        DiscountPercentage = p.IsDiscounted && p.OriginalPrice.HasValue && p.OriginalPrice > 0 
                            ? Math.Round(((p.OriginalPrice.Value - p.Price) / p.OriginalPrice.Value) * 100, 2) 
                            : (decimal?)null,
                        ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? $"{baseUrl}/api/File/Download/{p.ImageUrl}" : null,
                        IsActive = p.IsActive,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category != null ? new
                        {
                            CategoryId = p.Category.Id,
                            CategoryName = p.Category.Name
                        } : null
                    })
                    .ToListAsync();

                response.Response = products;
                response.GenerateSuccess("Tüm ürünler başarıyla listelendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetAllProductsOperation sırasında hata oluştu.");
                response.GenerateError(5202, $"Ürünler listelenirken hata: {ex.Message}");
            }
            return response;
        }

        // Sıralama için yardımcı metot - güncellendi
        private static IQueryable<Product> ApplySortingByOption(IQueryable<Product> query, ProductSortOption sortOption)
        {
            return sortOption switch
            {
                ProductSortOption.NameAsc => query.OrderBy(p => p.Name),
                ProductSortOption.NameDesc => query.OrderByDescending(p => p.Name),
                ProductSortOption.PriceAsc => query.OrderBy(p => p.Price),
                ProductSortOption.PriceDesc => query.OrderByDescending(p => p.Price),
                ProductSortOption.StockAsc => query.OrderBy(p => p.StockQuantity),
                ProductSortOption.StockDesc => query.OrderByDescending(p => p.StockQuantity),
                ProductSortOption.NewestFirst => query.OrderByDescending(p => p.Id), // ID'ye göre, varsayılan olarak
                _ => query.OrderBy(p => p.Name), // Varsayılan olarak isme göre sırala
            };
        }

        // Ürün Güncelleme (JSON ile, resim güncelleme olmadan)
        public static async Task<BaseResponse> UpdateProductOperation(string token, int productId, UpdateProductRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger, string baseUrl)
        {
            var response = new BaseResponse();
            try
            {
                // Oturum kontrolü
                var session = SessionService.TestToken(token, usersContext);
                if (session == null || session._user == null)
                {
                    response.GenerateError(1000, "Geçersiz oturum veya oturum süresi dolmuş.");
                    return response;
                }
                int userId = session._user.Id;

                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                if (productId != request.Id)
                {
                    response.GenerateError(4000, "URL'deki ürün ID'si ile istekteki ID uyuşmuyor.");
                    return response;
                }

                if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId))
                {
                    response.GenerateError(4004, "Belirtilen kategori bulunamadı.");
                    return response;
                }

                var product = await dbContext.Products.FindAsync(productId);

                if (product == null)
                {
                    response.GenerateError(4404, "Güncellenecek ürün bulunamadı.");
                    return response;
                }

                // İndirim mantığı kontrolü
                if (request.IsDiscounted && (request.OriginalPrice == null || request.OriginalPrice <= request.Price))
                {
                    response.GenerateError(4006, "İndirimli ürünlerde orijinal fiyat, indirimli fiyattan büyük olmalıdır ve boş bırakılamaz.");
                    return response;
                }

                // Değişiklik öncesi ürünün mevcut halini kaydet (log için)
                var oldProduct = new Product
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    IsDiscounted = product.IsDiscounted,
                    OriginalPrice = product.OriginalPrice,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    CategoryId = product.CategoryId,
                    IsActive = product.IsActive
                };

                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.IsDiscounted = request.IsDiscounted;
                product.OriginalPrice = request.IsDiscounted ? request.OriginalPrice : null;
                product.StockQuantity = request.StockQuantity;
                product.ImageUrl = request.ImageUrl; // ImageUrl güncelleniyor
                product.CategoryId = request.CategoryId;
                if (request.IsActive.HasValue)
                {
                    product.IsActive = request.IsActive.Value;
                }
                // ImageUrl güncellenmez (sadece mevcut değer korunur)

                dbContext.Entry(product).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Ürün güncellendi: ID={product.Id}, Ad={product.Name}");

                // Güncelleme logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Products",
                    'U', // Update
                    userId,
                    oldProduct, // Önceki değer
                    product // Yeni değer
                );

                // Güncellenmiş ürün bilgilerini response'a ekle
                string? fullImageUrl = null;
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    fullImageUrl = $"{baseUrl}/api/File/Download/{product.ImageUrl}";
                }

                response.Response = new { 
                    ProductId = product.Id, 
                    ImageUrl = fullImageUrl,
                    Name = product.Name,
                    Price = product.Price,
                    IsDiscounted = product.IsDiscounted,
                    OriginalPrice = product.OriginalPrice
                };
                response.GenerateSuccess("Ürün başarıyla güncellendi.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger?.LogWarning(ex, $"UpdateProductOperation - Concurrency hatası (ID: {productId}).");
                response.GenerateError(4202, "Ürün güncellenirken bir çakışma oluştu. Lütfen tekrar deneyin.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"UpdateProductOperation sırasında hata oluştu (ID: {productId}).");
                response.GenerateError(5203, $"Ürün güncellenirken hata: {ex.Message}");
            }
            return response;
        }

        // Ürün Silme (Pasifleştirme)
        public static async Task<BaseResponse> DeleteProductOperation(string token, int productId, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                // Oturum kontrolü
                var session = SessionService.TestToken(token, usersContext);
                if (session == null || session._user == null)
                {
                    response.GenerateError(1000, "Geçersiz oturum veya oturum süresi dolmuş.");
                    return response;
                }
                int userId = session._user.Id;

                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                var product = await dbContext.Products.FindAsync(productId);

                if (product == null)
                {
                    response.GenerateError(4404, "Silinecek ürün bulunamadı.");
                    return response;
                }

                // Değişiklik öncesi halini kaydet (log için)
                var oldProduct = new Product
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    CategoryId = product.CategoryId,
                    IsActive = product.IsActive
                };

                // Gerçek silme işlemi (Hard Delete)
                dbContext.Products.Remove(product);

                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Ürün tamamen silindi: ID={product.Id}, Ad={product.Name}");

                // Silme işlemi logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Products",
                    'D', // Delete
                    userId,
                    oldProduct, // Önceki değer
                    null // Yeni değer yok (silindi)
                );

                response.GenerateSuccess("Ürün başarıyla silindi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"DeleteProductOperation sırasında hata oluştu (ID: {productId}).");
                response.GenerateError(5204, $"Ürün silinirken hata: {ex.Message}");
            }
            return response;
        }

        // Ürün Arama Fonksiyonu
        public static async Task<BaseResponse> SearchProductsOperation(
            SearchProductsRequest request, 
            GeneralContext dbContext, 
            ILogger logger, 
            string baseUrl)
        {
            var response = new BaseResponse();
            try
            {
                // Request nesnesi null ise yeni bir tane oluştur
                request ??= new SearchProductsRequest();

                // Başlangıç sorgusu
                var query = dbContext.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                // İsim ile arama filtresi
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.Trim();
                    
                    // SQL LIKE karakter kaçışları
                    searchTerm = searchTerm.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                    
                    if (request.ExactMatch)
                    {
                        // Başlangıç eşleşmesi ("Ze" ile başlayan)
                        query = query.Where(p => EF.Functions.Like(p.Name, searchTerm + "%"));
                        logger?.LogInformation($"Kesin arama (başlangıç eşleşmesi): '{searchTerm}%'");
                    }
                    else
                    {
                        // Bulanık arama (içinde "Ze" geçen benzerleri)
                        query = query.Where(p => EF.Functions.Like(p.Name, "%" + searchTerm + "%"));
                        logger?.LogInformation($"Bulanık arama (içeren): '%{searchTerm}%'");
                    }
                    
                    // Debug için ürün isimlerini alalım
                    var productNames = await query.Select(p => p.Name).Take(20).ToListAsync();
                    logger?.LogInformation($"Filtrelenmiş ürün isimleri: {string.Join(", ", productNames)}");
                }
                
                // Kategori filtresi - 0 değerini kontrol et
                if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryId == request.CategoryId.Value);
                }
                
                // Fiyat filtreleri - 0 değerini kontrol et
                if (request.MinPrice.HasValue && request.MinPrice.Value > 0)
                {
                    query = query.Where(p => p.Price >= request.MinPrice.Value);
                }
                
                if (request.MaxPrice.HasValue && request.MaxPrice.Value > 0)
                {
                    query = query.Where(p => p.Price <= request.MaxPrice.Value);
                }
                
                // Aktiflik filtresi
                if (request.OnlyActive)
                {
                    query = query.Where(p => p.IsActive);
                }
                
                // Toplam sayı
                var totalCount = await query.CountAsync();
                
                // Sıralama - yeni sıralama metodu kullanıldı
                query = ApplySortingByOption(query, request.SortOption);
                
                // Sayfalama - 0 değerlerini kontrol et
                var pageNumber = Math.Max(1, request.PageNumber);
                var pageSize = Math.Max(10, Math.Min(50, request.PageSize)); // En az 10, en fazla 50
                
                // 0 olduğunda varsayılan değerler
                if (pageSize <= 0) pageSize = 20;
                if (pageNumber <= 0) pageNumber = 1;
                
                var skip = (pageNumber - 1) * pageSize;
                
                // Sorguyu çalıştır
                var products = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        OriginalPrice = p.OriginalPrice,
                        IsDiscounted = p.IsDiscounted,
                        DiscountPercentage = p.IsDiscounted && p.OriginalPrice.HasValue && p.OriginalPrice > 0 
                            ? Math.Round(((p.OriginalPrice.Value - p.Price) / p.OriginalPrice.Value) * 100, 2) 
                            : (decimal?)null,
                        ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? $"{baseUrl}/api/File/Download/{p.ImageUrl}" : null,
                        IsActive = p.IsActive,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category != null ? new
                        {
                            CategoryId = p.Category.Id,
                            CategoryName = p.Category.Name
                        } : null
                    })
                    .ToListAsync();
                
                // Sayfalama bilgisi ile birlikte sonucu döndür
                var result = new 
                {
                    Data = products,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };
                
                response.Response = result;
                if (totalCount > 0)
                {
                    response.GenerateSuccess($"{products.Count} ürün bulundu. Toplam {totalCount} ürün.");
                }
                else
                {
                    // Veri bulunamadığında daha anlamlı bir mesaj
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        response.GenerateSuccess($"'{request.SearchTerm}' aramanız için ürün bulunamadı.");
                    }
                    else
                    {
                        response.GenerateSuccess("Filtrelere uygun ürün bulunamadı.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SearchProductsOperation sırasında hata oluştu.");
                response.GenerateError(5207, $"Ürün arama sırasında hata: {ex.Message}");
            }
            
            return response;
        }

        // Yeni Ürün Ekleme
        public static async Task<BaseResponse> CreateProductOperation(string token, CreateProductRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                // Oturum kontrolü
                var session = SessionService.TestToken(token, usersContext);
                if (session == null || session._user == null)
                {
                    response.GenerateError(1000, "Geçersiz oturum veya oturum süresi dolmuş.");
                    return response;
                }
                int userId = session._user.Id;

                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId))
                {
                    response.GenerateError(4004, "Belirtilen kategori bulunamadı.");
                    return response;
                }

                // İndirim mantığı kontrolü
                if (request.IsDiscounted && (request.OriginalPrice == null || request.OriginalPrice <= request.Price))
                {
                    response.GenerateError(4006, "İndirimli ürünlerde orijinal fiyat, indirimli fiyattan büyük olmalıdır ve boş bırakılamaz.");
                    return response;
                }

                var newProduct = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price, // Güncel fiyat
                    IsDiscounted = request.IsDiscounted,
                    OriginalPrice = request.IsDiscounted ? request.OriginalPrice : null,
                    StockQuantity = request.StockQuantity,
                    ImageUrl = request.ImageUrl, // CreateProductRequest'te ImageUrl var
                    CategoryId = request.CategoryId,
                    IsActive = true
                };

                dbContext.Products.Add(newProduct);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Yeni ürün eklendi: ID={newProduct.Id}, Ad={newProduct.Name}");

                // Log kayıt işlemi
                await LogServices.AddLogAsync(
                    dbContext, 
                    "Products", 
                    'C', // Create
                    userId,
                    null, // Önceki değer yok
                    newProduct // Yeni eklenen ürün
                );

                response.Response = new { ProductId = newProduct.Id };
                response.GenerateSuccess("Ürün başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "CreateProductOperation sırasında hata oluştu.");
                response.GenerateError(5200, $"Ürün eklenirken hata: {ex.Message}");
            }
            return response;
        }
    }

    // Sayfalama için genel response modeli (BaseResponses.cs'e taşınabilir)
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
  }
} 