using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic; // IValidatableObject için eklendi

namespace KeremProject1backend.Models.Requests
{
    // Ürün oluşturma isteği
    public class CreateProductRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [StringLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0.01, 1000000, ErrorMessage = "Fiyat 0.01 ile 1,000,000 arasında olmalıdır")]
        public decimal Price { get; set; }
        
        // İndirim özellikleri
        public bool IsDiscounted { get; set; } = false;
        
        // [Range] attribute'u kaldırıldı, custom validation ile kontrol edilecek
        public decimal? OriginalPrice { get; set; } // İndirim varsa zorunlu
        
        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, 1000000, ErrorMessage = "Stok miktarı 0 ile 1,000,000 arasında olmalıdır")]
        public int StockQuantity { get; set; }
        
        public string ImageUrl { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Kategori ID zorunludur")]
        public int CategoryId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsDiscounted)
            {
                if (!OriginalPrice.HasValue)
                {
                    yield return new ValidationResult("İndirimli ürünlerde orijinal fiyat zorunludur.", new[] { nameof(OriginalPrice) });
                }
                else if (OriginalPrice.Value < 0.01m || OriginalPrice.Value > 1000000m)
                {
                    yield return new ValidationResult("Orijinal fiyat 0.01 ile 1,000,000 arasında olmalıdır.", new[] { nameof(OriginalPrice) });
                }
                if (OriginalPrice.HasValue && Price >= OriginalPrice.Value)
                {
                    yield return new ValidationResult("İndirimli fiyat, orijinal fiyattan düşük olmalıdır.", new[] { nameof(Price) });
                }
            }
        }
    }
    
    // Resim yükleme desteği olan ürün oluşturma isteği
    public class CreateProductWithImageRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [StringLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0.01, 1000000, ErrorMessage = "Fiyat 0.01 ile 1,000,000 arasında olmalıdır")]
        public decimal Price { get; set; }
        
        // İndirim özellikleri
        public bool IsDiscounted { get; set; } = false;
        
        // [Range] attribute'u kaldırıldı, custom validation ile kontrol edilecek
        public decimal? OriginalPrice { get; set; } // İndirim varsa zorunlu
        
        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, 1000000, ErrorMessage = "Stok miktarı 0 ile 1,000,000 arasında olmalıdır")]
        public int StockQuantity { get; set; }
        
        // Form verisi olarak alınacak resim dosyası
        public IFormFile? ProductImage { get; set; } = null;
        
        [Required(ErrorMessage = "Kategori ID zorunludur")]
        public int CategoryId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsDiscounted)
            {
                if (!OriginalPrice.HasValue)
                {
                    yield return new ValidationResult("İndirimli ürünlerde orijinal fiyat zorunludur.", new[] { nameof(OriginalPrice) });
                }
                else if (OriginalPrice.Value < 0.01m || OriginalPrice.Value > 1000000m)
                {
                    yield return new ValidationResult("Orijinal fiyat 0.01 ile 1,000,000 arasında olmalıdır.", new[] { nameof(OriginalPrice) });
                }
                if (OriginalPrice.HasValue && Price >= OriginalPrice.Value)
                {
                    yield return new ValidationResult("İndirimli fiyat, orijinal fiyattan düşük olmalıdır.", new[] { nameof(Price) });
                }
            }
        }
    }
    
    // Ürün güncelleme isteği
    public class UpdateProductRequest : IValidatableObject
    {
        [Required(ErrorMessage = "ID zorunludur")]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [StringLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0.01, 1000000, ErrorMessage = "Fiyat 0.01 ile 1,000,000 arasında olmalıdır")]
        public decimal Price { get; set; }
        
        // İndirim özellikleri
        public bool IsDiscounted { get; set; } = false;
        
        // [Range] attribute'u kaldırıldı, custom validation ile kontrol edilecek
        public decimal? OriginalPrice { get; set; } // İndirim varsa zorunlu
        
        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, 1000000, ErrorMessage = "Stok miktarı 0 ile 1,000,000 arasında olmalıdır")]
        public int StockQuantity { get; set; }
        
        public string ImageUrl { get; set; } = string.Empty;
        
        public int? CategoryId { get; set; }
        
        public bool? IsActive { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsDiscounted)
            {
                if (!OriginalPrice.HasValue)
                {
                    yield return new ValidationResult("İndirimli ürünlerde orijinal fiyat zorunludur.", new[] { nameof(OriginalPrice) });
                }
                else if (OriginalPrice.Value < 0.01m || OriginalPrice.Value > 1000000m)
                {
                    yield return new ValidationResult("Orijinal fiyat 0.01 ile 1,000,000 arasında olmalıdır.", new[] { nameof(OriginalPrice) });
                }
                if (OriginalPrice.HasValue && Price >= OriginalPrice.Value)
                {
                    yield return new ValidationResult("İndirimli fiyat, orijinal fiyattan düşük olmalıdır.", new[] { nameof(Price) });
                }
            }
        }
    }
    
    // Resim yükleme desteği olan ürün güncelleme isteği
    public class UpdateProductWithImageRequest : IValidatableObject
    {
        [Required(ErrorMessage = "ID zorunludur")]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [StringLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0.01, 1000000, ErrorMessage = "Fiyat 0.01 ile 1,000,000 arasında olmalıdır")]
        public decimal Price { get; set; }
        
        // İndirim özellikleri
        public bool IsDiscounted { get; set; } = false;
        
        // [Range] attribute'u kaldırıldı, custom validation ile kontrol edilecek
        public decimal? OriginalPrice { get; set; } // İndirim varsa zorunlu
        
        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, 1000000, ErrorMessage = "Stok miktarı 0 ile 1,000,000 arasında olmalıdır")]
        public int StockQuantity { get; set; }
        
        // Form verisi olarak alınacak resim dosyası
        public IFormFile? ProductImage { get; set; } = null;
        
        [Required(ErrorMessage = "Kategori ID zorunludur")]
        public int? CategoryId { get; set; }
        
        public bool? IsActive { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsDiscounted)
            {
                if (!OriginalPrice.HasValue)
                {
                    yield return new ValidationResult("İndirimli ürünlerde orijinal fiyat zorunludur.", new[] { nameof(OriginalPrice) });
                }
                else if (OriginalPrice.Value < 0.01m || OriginalPrice.Value > 1000000m)
                {
                    yield return new ValidationResult("Orijinal fiyat 0.01 ile 1,000,000 arasında olmalıdır.", new[] { nameof(OriginalPrice) });
                }
                if (OriginalPrice.HasValue && Price >= OriginalPrice.Value)
                {
                    yield return new ValidationResult("İndirimli fiyat, orijinal fiyattan düşük olmalıdır.", new[] { nameof(Price) });
                }
            }
        }
    }
    
    // Filtreleme seçenekleri olan ürün listeleme isteği
    public class GetAllProductsRequest
    {
        public string? SearchTerm { get; set; } = null;
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? OnlyActive { get; set; } = true;
        public string SortBy { get; set; } = "name";
        public bool SortAscending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
    
    // Sıralama seçenekleri için enum
    public enum ProductSortOption
    {
        NameAsc = 0,        // İsme göre (A-Z)
        NameDesc = 1,       // İsme göre (Z-A)
        PriceAsc = 2,       // Fiyat (düşükten yükseğe)
        PriceDesc = 3,      // Fiyat (yüksekten düşüğe)
        NewestFirst = 4,    // En yeniler önce (ileride kullanılabilir)
        StockAsc = 5,       // Stok miktarı (azdan çoğa)
        StockDesc = 6       // Stok miktarı (çoktan aza)
    }
    
    // Ürün arama isteği
    public class SearchProductsRequest
    {
        public string? SearchTerm { get; set; } // Ze gibi bir arama terimi
        public bool ExactMatch { get; set; } = true; // true: Sadece başlangıçları eşleştir (Ze*), false: Benzerlerini de bul (*Ze*)
        public int? CategoryId { get; set; } // Kategori filtresi (opsiyonel)
        public decimal? MinPrice { get; set; } // Minimum fiyat filtresi (opsiyonel)
        public decimal? MaxPrice { get; set; } // Maksimum fiyat filtresi (opsiyonel)
        public bool OnlyActive { get; set; } = true; // Sadece aktif ürünleri göster (default: true)
        
        // Yeni daha kullanıcı dostu sıralama seçeneği
        public ProductSortOption SortOption { get; set; } = ProductSortOption.NameAsc;
        
        public int PageNumber { get; set; } = 1; // Sayfa numarası (default: 1)
        public int PageSize { get; set; } = 20; // Sayfa başına ürün sayısı (default: 20)
    }
} 