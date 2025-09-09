namespace KeremProject1backend.Models.Requests
{
    public class LogsRequest
    {
    }

    public class GetSystemLogsRequest
    {
        public int? Id { get; set; }
        public string? TableName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Action { get; set; }
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public int? ModUser { get; set; }


    }

    public class SendReportRequest
    {
        public string Title { get; set; } = string.Empty; // Başlık
        public string Content { get; set; } = string.Empty; // İçerik
    }

    public class GetReportsRequest
    {
        public int? Id { get; set; } // Belirli bir rapor ID'sini sorgulamak için (isteğe bağlı)
    }

    public class UpdateContactRequest
    {
        public string Address { get; set; } = string.Empty; // Adres bilgisi
        public string Phone { get; set; } = string.Empty; // Telefon numarası
        public string Email { get; set; } = string.Empty; // E-posta adresi
        public string WorkingHours { get; set; } = string.Empty; // Çalışma saatleri
        public string Description { get; set; } = string.Empty; // Açıklama
    }
}
