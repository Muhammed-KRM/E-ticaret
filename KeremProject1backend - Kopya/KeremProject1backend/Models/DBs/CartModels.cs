using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KeremProject1backend.Models.DBs
{
    public class ShoppingCart // Kullanıcıya özel kalıcı sepet için (isteğe bağlı)
    {
        [Key]
        public int Id { get; set; }
        
        // Üye olan kullanıcılar için (null olabilir üye olmayan kullanıcılar için)
        public int? UserId { get; set; }
        
        // Üye olmayan kullanıcılar için geçici sepet ID'si (GUID string)
        [StringLength(255)]
        public string? GuestId { get; set; }
        
        // Sepet oluşturulma ve son güncellenme tarihi
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; } = DateTime.UtcNow;
        
        // Üye olmayan kullanıcılar için geçici iletişim bilgileri
        [StringLength(100)]
        public string? GuestName { get; set; }
        [StringLength(256)]
        public string? GuestEmail { get; set; }
        [StringLength(20)]
        public string? GuestPhone { get; set; }
        
        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        [Key]
        public int Id { get; set; }
        public int ShoppingCartId { get; set; }
        public int ProductId { get; set; }
        [Required]
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; } // O anki fiyat
        public int Quantity { get; set; }

        // Navigation Properties
        public virtual ShoppingCart? ShoppingCart { get; set; } // Opsiyonel
        public virtual Product? Product { get; set; } // Product modeline bağlantı
    }
} 