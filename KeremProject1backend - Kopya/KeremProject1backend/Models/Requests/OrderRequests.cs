using KeremProject1backend.Models.DBs;
using System;

namespace KeremProject1backend.Models.Requests
{
    public class CancelOrderRequest
    {
        public string Reason { get; set; } = "Müşteri talebi üzerine iptal edildi.";
    }

    public class ReturnRequest
    {
        public string Reason { get; set; } = "Müşteri iade talebinde bulundu.";
    }

    public class UpdateShippingInfoRequest
    {
        public string TrackingNumber { get; set; } = null!;
        public string ShippingCarrier { get; set; } = null!;
    }

    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
    }

    // Admin için teslim edilmemiş siparişleri getirme isteği
    public class GetNonDeliveredOrdersRequest
    {
        // Filtreleme seçenekleri
        public OrderStatus? OrderStatus { get; set; } // Belirli bir durum için filtreleme (opsiyonel)
        public DateTime? FromDate { get; set; } // Başlangıç tarihi (opsiyonel)
        public DateTime? ToDate { get; set; } // Bitiş tarihi (opsiyonel)
        public int? UserId { get; set; } // Belirli bir kullanıcının siparişleri (opsiyonel)
        
        // Sıralama seçenekleri
        public bool SortByDateAsc { get; set; } = false; // false: Yeniden eskiye, true: Eskiden yeniye
        
        // Sayfalama
        public int PageNumber { get; set; } = 1; // Sayfa numarası
        public int PageSize { get; set; } = 20; // Sayfadaki öğe sayısı
    }

    // Admin tarafından kargo takip numarası gönderme isteği
    public class SendTrackingNumberRequest
    {
        public int OrderId { get; set; }
        public string TrackingNumber { get; set; } = null!;
        public string ShippingCarrier { get; set; } = null!;
    }
} 