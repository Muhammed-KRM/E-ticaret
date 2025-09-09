using System.Collections.Generic;
using System.Linq;
using System;

namespace KeremProject1backend.Models.DBs
{
    public class ConfigDef
    {
        // Mevcut ConfigDef özellikleri (varsa)

        // PayTR Ayarları
        public string PayTrMerchantId { get; set; } = null!;
        public string PayTrMerchantKey { get; set; } = null!;
        public string PayTrMerchantSalt { get; set; } = null!;

        // Yeni: Kargo API Ayarları (Placeholder)
        public string? ShippingApiUrl { get; set; } // Kargo API'sinin temel adresi
        public string? ShippingApiKey { get; set; } // Kargo API anahtarı (gerekliyse)
        // public string? ShippingApiUser { get; set; } // Bazı API'ler ek kimlik bilgisi isteyebilir
        // public string? ShippingApiPassword { get; set; }

         // OrderStatus Enum (Aktif hale getirildi ve düzeltildi)
         // public static EnumUIItem OrderStatusEnum => new EnumUIItem
         // {
         //    Name = "OrderStatus", // Frontend'de kullanılacak isim
         //    Pairs = Enum.GetValues(typeof(OrderStatus)) // OrderModels.cs içindeki enum
         //               .Cast<OrderStatus>()
         //               .Select(e => new EnumUIItemParam { Key = e.ToString(), Value = (int)e }).ToList()
         // };
    }

    public enum UserRoleinAuthorization : byte
    {
        AdminAdmin = 1,
        Admin = 2,
        User = 3,
       
    }
}
