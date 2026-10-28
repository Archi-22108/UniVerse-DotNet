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
            var cleanCode = (code ?? string.Empty).Trim();

            if (_store.TryGetValue(normalizedEmail, out var record))
            {
                if (DateTime.UtcNow <= record.ExpiryUtc && string.Equals(record.Code.Trim(), cleanCode, StringComparison.OrdinalIgnoreCase))
                {
                    record.IsVerified = true;
                    _logger.LogInformation("🏛️ [MU Mail Gateway] Student {Email} verified successfully with code {Code}", normalizedEmail, cleanCode);
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
        Task<(bool Success, string Message)> SendVerificationOtpAsync(string toEmail, string fullName, string otpCode);
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

        public async Task<(bool Success, string Message)> SendVerificationOtpAsync(string toEmail, string fullName, string otpCode)
        {
            var senderEmail = _configuration["SmtpSettings:SenderEmail"]?.Trim();
            var senderPassword = _configuration["SmtpSettings:SenderPassword"]?.Trim();
            var host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var portStr = _configuration["SmtpSettings:Port"] ?? "587";
            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            // 1. Check if SMTP credentials are configured
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogWarning("⚠️ SMTP credentials not configured in appsettings.json. Real email cannot be dispatched to {ToEmail}.", toEmail);
                return (false, "Email dispatch unavailable: SmtpSettings:SenderEmail or SenderPassword is not configured. Please add your Gmail & App Password in appsettings.json so OTPs land directly in student inboxes.");
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, senderPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "UniVerse — Marwadi University"),
                    Subject = $"🔐 UniVerse Account Verification Code: {otpCode}",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                // Professional Marwadi University HTML Email Template
                mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>UniVerse Verification Code</title>
</head>
<body style=""margin:0;padding:24px;background-color:#0b0e14;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#e2e8f0;"">
  <div style=""max-width:560px;margin:0 auto;background:#131822;border:1px solid #1e293b;border-radius:16px;overflow:hidden;box-shadow:0 20px 40px rgba(0,0,0,0.5);"">
    <div style=""background:linear-gradient(135deg,#0052cc 0%,#00d2ff 100%);padding:28px 32px;text-align:center;"">
      <h1 style=""margin:0;font-size:24px;font-weight:800;letter-spacing:1px;color:#ffffff;"">UniVerse</h1>
      <p style=""margin:4px 0 0;font-size:12px;letter-spacing:2px;text-transform:uppercase;color:rgba(255,255,255,0.85);font-weight:600;"">Marwadi University Campus Portal</p>
    </div>
    <div style=""padding:32px;"">
      <h2 style=""margin:0 0 12px;font-size:18px;color:#f8fafc;font-weight:600;"">Student Email Verification</h2>
      <p style=""margin:0 0 16px;font-size:14px;line-height:1.6;color:#94a3b8;"">Hello <strong style=""color:#ffffff;"">{WebUtility.HtmlEncode(fullName)}</strong>,</p>
      <p style=""margin:0 0 24px;font-size:14px;line-height:1.6;color:#94a3b8;"">Use the verification code below to verify your student email (<strong style=""color:#00d2ff;"">{WebUtility.HtmlEncode(toEmail)}</strong>) and complete your UniVerse registration.</p>
      
      <div style=""background:#0d1117;border:1px dashed #00d2ff;border-radius:12px;padding:20px;text-align:center;margin:0 0 24px;"">
        <div style=""font-size:11px;letter-spacing:2px;text-transform:uppercase;color:#00d2ff;margin-bottom:8px;font-weight:700;"">Your 6-Digit Verification Code</div>
        <div style=""font-size:36px;font-weight:800;letter-spacing:8px;color:#ffffff;font-family:Consolas,monospace;text-shadow:0 0 15px rgba(0,210,255,0.4);"">{otpCode}</div>
      </div>

      <div style=""background:#1e293b;border-left:4px solid #00d2ff;padding:12px 16px;border-radius:6px;margin-bottom:24px;"">
        <p style=""margin:0;font-size:12px;color:#cbd5e1;line-height:1.5;"">⏱️ <strong>Valid for 15 minutes.</strong> Never share this code with anyone.</p>
      </div>

      <p style=""margin:0;font-size:12px;color:#64748b;line-height:1.5;"">If you did not request this verification code, please ignore this email. No account will be registered without this code.</p>
    </div>
    <div style=""background:#0d1117;border-top:1px solid #1e293b;padding:16px 32px;text-align:center;"">
      <p style=""margin:0;font-size:11px;color:#64748b;"">Marwadi University, Rajkot, Gujarat &bull; UniVerse Student Platform</p>
    </div>
  </div>
</body>
</html>";

                // Add plain text fallback
                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    $"UniVerse — Marwadi University\n\nHello {fullName},\n\nYour 6-digit Marwadi University verification code is: {otpCode}\n\nValid for 15 minutes. Please do not share this code with anyone.\n\n— The UniVerse Team",
                    null,
                    "text/plain"
                );
                mailMessage.AlternateViews.Add(plainTextView);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("✅ [MU Mail Gateway] Real email successfully delivered to student {ToEmail} via SMTP", toEmail);
                return (true, $"Verification code sent directly to {toEmail}. Please check your phone / Gmail inbox.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SMTP dispatch failed for student {ToEmail}", toEmail);
                return (false, $"Email dispatch failed ({ex.Message}). Please verify SMTP credentials in appsettings.json.");
            }
        }
    }
}
