using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeremProject1backend.Models.DBs
{
    public enum OrderStatus : byte
    {
        Pending = 0,        // Ödeme bekleniyor / Yeni sipariş
        Processing = 1,     // İşleme alındı
        Paid = 2,           // Ödeme tamamlandı
        Shipped = 3,        // Kargoya verildi
        Delivered = 4,      // Teslim edildi
        Cancelled = 5,      // İptal edildi
        Failed = 6,         // Başarısız oldu (Ödeme vb.)
        ReturnRequested = 7, // İade Talep Edildi (Admin onayı bekliyor)
        Refunded = 8,       // İade Edildi (Tam iade, Onaylandı)
        PartiallyRefunded = 9, // Kısmen İade Edildi (Onaylandı)
        ReturnRejected = 10 // İade Talebi Reddedildi
    }

    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MerchantOid { get; set; } = null!; // PayTR veya kendi ürettiğimiz benzersiz sipariş no

        // Üye olan kullanıcılar için (nullable - üye olmayan kullanıcılar için null olabilir)
        public int? UserId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Kargo Bilgileri
        public string? ShippingAddress { get; set; } // Adres ayrı bir tabloya da alınabilir
        public string? BillingAddress { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShippingCarrier { get; set; } // Kargo firması adı

        // Üye olmayan kullanıcılar için müşteri bilgileri
        [StringLength(100)]
        public string? CustomerName { get; set; }
        [StringLength(256)]
        public string? CustomerEmail { get; set; }
        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        // İptal/İade Bilgileri
        public string? CancellationReason { get; set; }
        public string? ReturnReason { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; } 


        // Navigation Properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        // public virtual AllUsersModel User { get; set; } // Kullanıcı modeline bağlantı
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; } // O anki birim fiyat

        [Required]
        public int Quantity { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
        // public virtual Product Product { get; set; } // Ürün modeline bağlantı
    }
} 