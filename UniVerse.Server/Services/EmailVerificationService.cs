using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UniVerse.Server.Services
{
    public interface IEmailVerificationService
    {
        string GenerateAndStoreCode(string email, string fullName);
        bool ValidateCode(string email, string code);
        bool IsEmailVerified(string email);
        void MarkVerified(string email);
        void Remove(string email);
        string? GetLatestCode(string email);
    }

    public class VerificationRecord
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiryUtc { get; set; }
        public bool IsVerified { get; set; }
    }

    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly ConcurrentDictionary<string, VerificationRecord> _store = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<EmailVerificationService> _logger;

        public EmailVerificationService(ILogger<EmailVerificationService> logger)
        {
            _logger = logger;
        }

        public string GenerateAndStoreCode(string email, string fullName)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var code = new Random().Next(100000, 999999).ToString();
            var record = new VerificationRecord
            {
                Email = normalizedEmail,
                FullName = fullName.Trim(),
                Code = code,
                ExpiryUtc = DateTime.UtcNow.AddMinutes(15),
                IsVerified = false
            };

            _store[normalizedEmail] = record;
            _logger.LogInformation("🏛️ [MU Mail Gateway] Generated verification code {Code} for student {Email}", code, normalizedEmail);
            return code;
        }

        public bool ValidateCode(string email, string code)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (_store.TryGetValue(normalizedEmail, out var record))
            {
                if (DateTime.UtcNow <= record.ExpiryUtc && string.Equals(record.Code.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    record.IsVerified = true;
                    return true;
                }
            }
            return false;
        }

        public bool IsEmailVerified(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return _store.TryGetValue(normalizedEmail, out var record) && record.IsVerified;
        }

        public void MarkVerified(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (_store.TryGetValue(normalizedEmail, out var record))
            {
                record.IsVerified = true;
            }
        }

        public void Remove(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            _store.TryRemove(normalizedEmail, out _);
        }

        public string? GetLatestCode(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (_store.TryGetValue(normalizedEmail, out var record))
            {
                return record.Code;
            }
            return null;
        }
    }
    public interface ICampusEmailService
    {
        Task SendVerificationOtpAsync(string toEmail, string fullName, string otpCode);
    }

    public class CampusEmailService : ICampusEmailService
    {
        private readonly ILogger<CampusEmailService> _logger;
        private readonly IConfiguration _configuration;

        public CampusEmailService(ILogger<CampusEmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendVerificationOtpAsync(string toEmail, string fullName, string otpCode)
        {
            var senderEmail = _configuration["SmtpSettings:SenderEmail"];
            var senderPassword = _configuration["SmtpSettings:SenderPassword"];
            var host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var portStr = _configuration["SmtpSettings:Port"] ?? "587";
            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            // 1. Send via real SMTP if credentials are configured
            if (!string.IsNullOrWhiteSpace(senderEmail) && !string.IsNullOrWhiteSpace(senderPassword))
            {
                try
                {
                    using var client = new SmtpClient(host, port)
                    {
                        EnableSsl = true,
                        Credentials = new NetworkCredential(senderEmail, senderPassword)
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, "UniVerse — Marwadi University"),
                        Subject = $"🔐 UniVerse Account Verification Code: {otpCode}",
                        Body = $"Hello {fullName},\n\nYour 6-digit Marwadi University verification code to create your UniVerse account is:\n\n👉  {otpCode}  👈\n\nThis security code is valid for 15 minutes. Please do not share this code with anyone.\n\n— The UniVerse Team (Marwadi University)",
                        IsBodyHtml = false
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("SMTP verification email successfully dispatched to {ToEmail}", toEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SMTP dispatch error for {ToEmail}", toEmail);
                }
            }

            // 2. Dispatch via Web3Forms Real Campus Email Gateway (as in Next.js codebase)
            try
            {
                using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                var payload = new
                {
                    access_key = "0e0d37e6-99cf-41c3-8eb1-4dc7950c268c",
                    from_name = "UniVerse — Marwadi University",
                    subject = $"🔐 UniVerse Verification Code: {otpCode}",
                    email = toEmail,
                    message = $"Hello {fullName},\n\nYour 6-digit Marwadi University verification code to create your UniVerse account is:\n\n👉  {otpCode}  👈\n\nThis security code is valid for 15 minutes. Please enter this code on the registration page to verify your student email.\n\n— The UniVerse Team (Marwadi University)"
                };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://api.web3forms.com/submit", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Web3Forms email successfully dispatched to {ToEmail}", toEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Web3Forms email gateway dispatch notice for {ToEmail}", toEmail);
            }

            // 3. Also log to logs/sent_emails.log
            try
            {
                var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, "sent_emails.log");
                var logLine = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] RECIPIENT: {toEmail} | CODE: {otpCode} | NAME: {fullName}{Environment.NewLine}";
                await File.AppendAllTextAsync(logPath, logLine);
            }
            catch { }

            _logger.LogInformation("🏛️ [MU Mail Gateway] Dispatched OTP code to student {ToEmail}", toEmail);
        }
    }
}
