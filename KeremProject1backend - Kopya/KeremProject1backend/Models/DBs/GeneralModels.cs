namespace KeremProject1backend.Models.DBs
{
    public class GeneralModels
    {
    }

    public class Report
    {
        public int Id { get; set; } // Benzersiz ID
        public int UserId { get; set; } // Raporu gönderen kullanıcının ID'si
        public string UserName { get; set; } = string.Empty; // Raporu gönderen kullanıcının adı
        public string Title { get; set; } = string.Empty; // Rapor başlığı
        public string Content { get; set; } = string.Empty; // Rapor içeriği
        public DateTime CreatedAt { get; set; } // Oluşturulma tarihi
        public bool IsReviewed { get; set; } // Admin tarafından incelendi mi
    }

    public class Contact
    {
        public int Id { get; set; } // Benzersiz ID
        public string Address { get; set; } = string.Empty; // Adres bilgisi
        public string Phone { get; set; } = string.Empty; // Telefon numarası
        public string Email { get; set; } = string.Empty; // E-posta adresi
        public string WorkingHours { get; set; } = string.Empty; // Çalışma saatleri
        public string Description { get; set; } = string.Empty; // Açıklama
        public DateTime UpdatedAt { get; set; } // Son güncelleme tarihi
        public int UpdatedBy { get; set; } // Güncelleyen kullanıcı ID
    }
}
