using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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

        public CampusEmailService(ILogger<CampusEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendVerificationOtpAsync(string toEmail, string fullName, string otpCode)
        {
            _logger.LogInformation("=================================================");
            _logger.LogInformation("🏛️ MARWADI UNIVERSITY STUDENT MAIL GATEWAY");
            _logger.LogInformation("To: {ToEmail}", toEmail);
            _logger.LogInformation("Subject: UniVerse Account Verification Code: {Code}", otpCode);
            _logger.LogInformation("Dear {FullName}, your UniVerse verification code is: {Code}. Valid for 15 minutes.", fullName, otpCode);
            _logger.LogInformation("=================================================");
            return Task.CompletedTask;
        }
    }
}
