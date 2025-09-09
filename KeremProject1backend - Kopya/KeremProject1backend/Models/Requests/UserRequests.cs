using KeremProject1backend.Models.DBs;
using Microsoft.AspNetCore.Mvc; // [FromForm] için
using System.ComponentModel.DataAnnotations;

namespace KeremProject1backend.Models.Requests
{
    public class UserRequests
    {
    }

    public class UserDeleteRequests
    {
        public int Id { get; set; }
    }
    public class GetAllUserRequests
    {
    }

    public class UpdateUserRequests
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public UserRoleinAuthorization UserRoleinAuthorization { get; set; }
        public string PasswordHash { get; set; } = null!;
        public bool State { get; set; }
        public string UserImageLink { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public DateTime ModTime { get; set; }
        public int ModUser { get; set; }

    }

    public class UpdateMyProfileRequest
    {
        public string? UserName { get; set; }
        public string? UserImageLink { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }
}

// --- E-Ticaret İstek Modelleri ---

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    // Üye olmayan kullanıcılar için geçici sepet ID'si
    public string? GuestCartId { get; set; }
}

public class UpdateCartRequest

{
    public int CartItemId { get; set; } // Eklendi  
    public int NewQuantity { get; set; } // Eklendi 
    // Üye olmayan kullanıcılar için geçici sepet ID'si
    public string? GuestCartId { get; set; }
}

public class InitiatePaymentRequest
{
    [Required(ErrorMessage = "Sipariş ID'si gereklidir.")]
    public int OrderId { get; set; }
}

public class AddReviewRequest
{
    public int ProductId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
}

public class CancelOrderRequest
{
    public string? Reason { get; set; }
}

public class ReturnRequest
{
    public string? Reason { get; set; }
}

public class UpdateShippingInfoRequest // Admin için
{
    public string TrackingNumber { get; set; } = null!;
    public string ShippingCarrier { get; set; } = null!;
}

// --- PayTR Callback Modeli (Ayrı veya mevcut dosya içinde olabilir) ---
// PayTR callback isteği POST form data olarak gelir.
// Bu yüzden property isimleri PayTR'ın gönderdiği field isimleriyle eşleşmeli.
public class PayTrCallbackRequest
{
    public string merchant_oid { get; set; } = null!;
    public string status { get; set; } = null!;
    public string total_amount { get; set; } = null!;
    public string hash { get; set; } = null!;
    public string? failed_reason_code { get; set; }
    public string? failed_reason_msg { get; set; }
    public string? test_mode { get; set; }
    public string? payment_type { get; set; }
    public string? currency { get; set; }
    public string? payment_amount { get; set; }
}

// --- Ödeme/İade İstek Modelleri ---

// Yeni İade İsteği Modeli
public class RefundRequest
{
    [Required(ErrorMessage = "İade edilecek siparişin MerchantOid değeri gereklidir.")]
    public string MerchantOid { get; set; } = null!;

    [Required(ErrorMessage = "İade tutarı gereklidir.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "İade tutarı sıfırdan büyük olmalıdır.")]
    public decimal RefundAmount { get; set; }

    // İsteğe bağlı: İade sebebi
    public string? Reason { get; set; }
}

// --- Ürün Yönetimi İstek Modelleri ---
/*
public class CreateProductRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    [Required]
    public decimal Price { get; set; }
    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; } // Kategori ID'si eklendi (opsiyonel)
}

public class UpdateProductRequest : CreateProductRequest // Güncelleme için Create'den miras alıyor
{
    [Required]
    public int Id { get; set; } // Güncellenecek ürünün ID'si
    public bool? IsActive { get; set; } // Aktiflik durumunu güncellemek için (isteğe bağlı)
    // CategoryId CreateProductRequest'ten miras alındı.
}
*/
// Ürün listeleme için filtreleme/sayfalama request'i
public class GetProductsRequest
{
    public string? SearchTerm { get; set; } // İsim veya açıklamada arama
    public int? CategoryId { get; set; } // Belirli bir kategoriye göre filtrele
    public bool IncludeInactive { get; set; } = false; // Admin için pasif ürünleri de getir
    public int PageNumber { get; set; } = 1;   // Sayfa numarası
    public int PageSize { get; set; } = 10;    // Sayfa başına ürün sayısı
    public string SortBy { get; set; } = "Name"; // Sıralama alanı (Name, Price vb.)
    public bool SortAscending { get; set; } = true; // Artan sıralama mı?
}

public class GetAllProductsRequest
{
}

// --- Kategori Yönetimi İstek Modelleri (Admin) ---

public class CreateCategoryRequest
{
    [Required(ErrorMessage = "Kategori adı gereklidir.")]
    [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest
{
    [Required]
    public int Id { get; set; } // Güncellenecek kategorinin ID'si
}

// --- İade Yönetimi İstek Modelleri (Admin) ---

public class ProcessReturnRequest
{
    // Onay veya Red için ortak model
    public string? AdminNotes { get; set; } // İsteğe bağlı admin notu

    // Sadece onay durumunda gerekebilir:
    // İade edilecek tutar (kısmi iade için)
    // Eğer tam iade yapılacaksa, siparişin kendi tutarı kullanılır.
    public decimal? RefundAmount { get; set; }
}

// --- Sipariş Yönetimi İstek Modelleri (Admin) ---

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Yeni sipariş durumu gereklidir.")]
    public OrderStatus Status { get; set; } // OrderModels.cs içindeki enum
}

// --- Kullanıcı Rapor Gönderme İsteği ---
public class SubmitReportRequest
{
    [Required(ErrorMessage = "Rapor başlığı gereklidir.")]
    [MaxLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Rapor içeriği gereklidir.")]
    public string Content { get; set; } = null!;
}

// --- Sipariş Oluşturma İstek Modeli ---
public class CreateOrderRequest
{
    [Required(ErrorMessage = "Teslimat adresi gereklidir.")]
    public string ShippingAddress { get; set; } = null!;
    
    public string? BillingAddress { get; set; } // Opsiyonel, yoksa ShippingAddress kullanılır
    
    // Üye olmayan kullanıcılar için zorunlu bilgiler
    public string? CustomerName { get; set; }
    
    [EmailAddress]
    public string? CustomerEmail { get; set; }
    
    public string? CustomerPhone { get; set; }
    
    // Sepet ID'si
    public string? GuestCartId { get; set; }
}
