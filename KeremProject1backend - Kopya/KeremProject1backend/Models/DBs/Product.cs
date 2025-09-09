using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // ICollection için

namespace KeremProject1backend.Models.DBs
{
    [Table("Products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; } // Görünen fiyat (indirimli veya normal)

        // İndirim özelliği için yeni alanlar
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? OriginalPrice { get; set; } // Orijinal fiyat (indirim varsa)

        public bool IsDiscounted { get; set; } = false; // İndirimde mi?

        [Required]
        public int StockQuantity { get; set; }

        public string? ImageUrl { get; set; }

        // Yeni eklenen alanlar:
        public int? CategoryId { get; set; } // Nullable olabilir, kategori zorunlu değilse

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; } // Navigation property


        // Mevcut IsActive alanı
        public bool IsActive { get; set; } = true;

        // Bu ürüne ait yorumlar için navigation property (varsa)
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

        // Bu ürünün bulunduğu sepet öğeleri (varsa)
        // public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

         // Bu ürünün bulunduğu sipariş öğeleri (varsa)
        // public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    // Gerekirse Kategori Modeli
    // public class Category
    // {
    //     public int Id { get; set; }
    //     [Required] public string Name { get; set; }
    //     public virtual ICollection<Product> Products { get; set; }
    // }
} 