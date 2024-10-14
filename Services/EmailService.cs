using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace BookMateHub.Api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"] ?? "")
            {
                Port = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"),
                Credentials = new NetworkCredential(
                    _configuration["EmailSettings:SenderEmail"] ?? "",
                    _configuration["EmailSettings:SenderPassword"] ?? ""),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["EmailSettings:SenderEmail"] ?? ""),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);
            smtpClient.Send(mailMessage);
        }
    }
}
