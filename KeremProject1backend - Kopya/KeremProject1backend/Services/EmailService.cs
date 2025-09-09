using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace KeremProject1backend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;
        
        // Sabit e-posta adresleri (istenildiği gibi)
        private readonly string _fromEmail = "Zeytin@gmail.com";
        private readonly string _adminEmail = "ZeytinAdmin@gmail.com";

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // SMTP ayarlarını yapılandırmadan oku (varsayılan değerlerle)
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            if (!int.TryParse(_configuration["EmailSettings:SmtpPort"], out _smtpPort))
                _smtpPort = 587; // Varsayılan SMTP port
            
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? _fromEmail;
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "YourAppPasswordHere";
            
            if (!bool.TryParse(_configuration["EmailSettings:EnableSsl"], out _enableSsl))
                _enableSsl = true; // Varsayılan olarak SSL aktif
        }

        public async Task<bool> SendEmailAsync(string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                message.To.Add(new MailAddress(_adminEmail));

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await client.SendMailAsync(message);
                _logger.LogInformation($"E-posta gönderildi: Konu={subject}, Alıcı={_adminEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"E-posta gönderimi sırasında hata oluştu: Konu={subject}");
                return false;
            }
        }

        public async Task<bool> SendOrderNotificationAsync(int orderId, string merchantOid, string customerName, decimal totalAmount)
        {
            string subject = $"Yeni Sipariş Bildirimi - Sipariş #{orderId}";
            
            string body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                    <h2 style='color: #20c997;'>Yeni Bir Sipariş Alındı!</h2>
                    <p>Sayın Yönetici,</p>
                    <p>Yeni bir sipariş ödemesi tamamlandı. Sipariş detayları aşağıdaki gibidir:</p>
                    <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                        <tr style='background-color: #e9ecef;'>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>Sipariş No</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{orderId}</td>
                        </tr>
                        <tr>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>İşlem Numarası</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{merchantOid}</td>
                        </tr>
                        <tr style='background-color: #e9ecef;'>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>Müşteri</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{customerName}</td>
                        </tr>
                        <tr>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>Toplam Tutar</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{totalAmount:C2} TL</td>
                        </tr>
                    </table>
                    <p style='margin-top: 20px;'>
                        Siparişi yönetim panelinden inceleyebilir ve işleme alabilirsiniz.
                    </p>
                </div>
            </body>
            </html>";

            return await SendEmailAsync(subject, body);
        }

        public async Task<bool> SendOrderConfirmationToUserAsync(string? recipientEmail, string? customerName, int orderId, decimal totalAmount, List<string> productNames, string? shippingAddress)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail)) 
            {
                _logger.LogWarning($"SendOrderConfirmationToUserAsync: Alıcı e-posta adresi boş olduğu için e-posta gönderilemedi. OrderID: {orderId}");
                return false;
            }
            string actualCustomerName = !string.IsNullOrWhiteSpace(customerName) ? customerName : "Değerli Müşterimiz";
            string actualShippingAddress = !string.IsNullOrWhiteSpace(shippingAddress) ? shippingAddress : "Belirtilmemiş";

            string subject = $"Siparişiniz Alındı - Sipariş #{orderId}";
            string productListHtml = string.Join("", productNames.Select(p => $"<li>{p}</li>"));

            string body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                    <h2 style='color: #007bff;'>Siparişiniz Alındı!</h2>
                    <p>Merhaba {actualCustomerName},</p>
                    <p>#{orderId} numaralı siparişiniz başarıyla alınmıştır. Sipariş detaylarınız aşağıdaki gibidir:</p>
                    <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                        <tr style='background-color: #e9ecef;'>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>Sipariş No</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{orderId}</td>
                        </tr>
                        <tr>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>Teslimat Adresi</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{actualShippingAddress}</td>
                        </tr>
                        <tr style='background-color: #e9ecef;'>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>Ürünler</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'><ul>{productListHtml}</ul></td>
                        </tr>
                        <tr>
                            <th style='padding: 10px; text-align: left; border: 1px solid #dee2e6;'>Toplam Tutar</th>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{totalAmount:C2} TL</td>
                        </tr>
                    </table>
                    <p style='margin-top: 20px;'>
                        Siparişinizi hazırlayıp en kısa sürede kargoya vereceğiz.
                    </p>
                    <p>Teşekkür ederiz!</p>
                </div>
            </body>
            </html>";

            return await SendEmailAsyncToUser(recipientEmail, subject, body);
        }

        public async Task<bool> SendOrderStatusUpdateToUserAsync(string? recipientEmail, string? customerName, int orderId, string newStatus, string? trackingNumber = null, string? shippingCarrier = null, string? orderDetailsUrl = null)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogWarning($"SendOrderStatusUpdateToUserAsync: Alıcı e-posta adresi boş olduğu için e-posta gönderilemedi. OrderID: {orderId}");
                return false;
            }
            string actualCustomerName = !string.IsNullOrWhiteSpace(customerName) ? customerName : "Değerli Müşterimiz";

            string subject = $"Sipariş Durumunuz Güncellendi - Sipariş #{orderId}";
            string statusMessage;
            string trackingInfoHtml = "";

            switch (newStatus)
            {
                case "Pending":
                    statusMessage = "Ödeme onayı bekleniyor.";
                    break;
                case "Processing":
                    statusMessage = "Hazırlanıyor.";
                    break;
                case "Paid":
                    statusMessage = "Ödemesi tamamlandı ve hazırlanmaya başlandı.";
                    break;
                case "Shipped":
                    statusMessage = "Kargoya verildi.";
                    if (!string.IsNullOrEmpty(trackingNumber) && !string.IsNullOrEmpty(shippingCarrier))
                    {
                        trackingInfoHtml = $@"
                        <p>Kargo Takip Bilgileriniz:</p>
                        <ul style='list-style-type: none; padding: 0;'>
                            <li><strong>Kargo Firması:</strong> {shippingCarrier}</li>
                            <li><strong>Takip Numarası:</strong> {trackingNumber}</li>
                        </ul>";
                    }
                    else
                    {
                        trackingInfoHtml = "<p>Kargo takip bilgileriniz en kısa sürede eklenecektir.</p>";
                    }
                    break;
                case "Delivered":
                    statusMessage = "Teslim edildi.";
                    break;
                case "Cancelled":
                    statusMessage = "İptal edildi.";
                    break;
                case "Failed":
                    statusMessage = "Başarısız oldu. Lütfen bizimle iletişime geçin.";
                    break;
                default:
                    statusMessage = newStatus; // Diğer durumlar için doğrudan durumu yaz
                    break;
            }

            string orderLinkHtml = !string.IsNullOrEmpty(orderDetailsUrl) ? $"<p>Sipariş detaylarınızı <a href='{orderDetailsUrl}'>buradan</a> inceleyebilirsiniz.</p>" : "";

            string body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                    <h2 style='color: #17a2b8;'>Sipariş Durumunuz Güncellendi!</h2>
                    <p>Merhaba {actualCustomerName},</p>
                    <p>#{orderId} numaralı siparişinizin durumu güncellenmiştir.</p>
                    <p><strong>Yeni Durum:</strong> {statusMessage}</p>
                    {trackingInfoHtml}
                    {orderLinkHtml}
                    <p style='margin-top: 20px;'>
                        Bizi tercih ettiğiniz için teşekkür ederiz.
                    </p>
                </div>
            </body>
            </html>";

            return await SendEmailAsyncToUser(recipientEmail, subject, body);
        }

        // Kullanıcıya e-posta göndermek için özel metot (alıcıyı parametre olarak alır)
        private async Task<bool> SendEmailAsyncToUser(string? recipientEmail, string? subject, string body, bool isHtml = true)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail) || string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogWarning("SendEmailAsyncToUser: Alıcı e-posta adresi veya konu boş olduğu için e-posta gönderilemedi.");
                _logger.LogWarning($"Alıcı: {recipientEmail}, Konu: {subject}"); // Daha fazla detay logla
                return false;
            }

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail), // Gönderen yine sabit
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                message.To.Add(new MailAddress(recipientEmail)); // Alıcıyı parametreden al

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await client.SendMailAsync(message);
                _logger.LogInformation($"Kullanıcıya e-posta gönderildi: Konu={subject}, Alıcı={recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kullanıcıya e-posta gönderimi sırasında hata oluştu: Konu={subject}, Alıcı={recipientEmail}");
                return false;
            }
        }
    }
} 