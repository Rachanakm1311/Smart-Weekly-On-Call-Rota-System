using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OncallRota.Interfaces;
using OncallRota.Models;

namespace OncallRota.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings     _cfg;
        private readonly ILogger<EmailService> _log;

        public EmailService(IOptions<EmailSettings> cfg, ILogger<EmailService> log)
        {
            _cfg = cfg.Value;
            _log = log;
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            // Guard: skip if credentials are still placeholder
            if (string.IsNullOrWhiteSpace(_cfg.Username) ||
                _cfg.Username.Contains("your-sender") ||
                string.IsNullOrWhiteSpace(_cfg.Password) ||
                _cfg.Password.Contains("your-app-password"))
            {
                _log.LogWarning("[EMAIL SKIPPED] SMTP credentials not configured. To:{Email} Sub:{Subject}", toEmail, subject);
                return;
            }

            _log.LogInformation("[EMAIL] Sending to:{To} From:{From} Host:{Host}:{Port} Sub:{Sub}",
                toEmail, _cfg.SenderEmail, _cfg.SmtpHost, _cfg.SmtpPort, subject);

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_cfg.SenderName, _cfg.SenderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body   = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient();

                // Always log SMTP protocol messages in debug
                client.MessageSent += (_, args) =>
                    _log.LogDebug("[EMAIL SMTP] Response: {Response}", args.Response);

                var secOpt = _cfg.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : (_cfg.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);

                _log.LogInformation("[EMAIL] Connecting to {Host}:{Port} TLS={Tls}", _cfg.SmtpHost, _cfg.SmtpPort, secOpt);
                await client.ConnectAsync(_cfg.SmtpHost, _cfg.SmtpPort, secOpt);

                _log.LogInformation("[EMAIL] Authenticating as {User}", _cfg.Username);
                await client.AuthenticateAsync(_cfg.Username, _cfg.Password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _log.LogInformation("[EMAIL] SUCCESS – delivered to {Email}", toEmail);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _log.LogError("[EMAIL AUTH FAILED] Wrong Gmail password or App Password not enabled. Detail: {Msg}", ex.Message);
            }
            catch (MailKit.Net.Smtp.SmtpCommandException ex)
            {
                _log.LogError("[EMAIL SMTP ERROR] Status:{Status} Code:{Code} Msg:{Msg}", ex.StatusCode, ex.ErrorCode, ex.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[EMAIL ERROR] Failed to send to {Email}", toEmail);
            }
        }
    }
}