using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Add this for logging purposes

namespace testsubject.Models
{
    public class EmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = "smtp.gmail.com";
            var smtpPort = 587; // Port for TLS encryption
            var smtpUsername = "";
            var smtpPassword = "";
            var fromEmail = "";

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                client.EnableSsl = true; // TLS enabled

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                try
                {
                    await client.SendMailAsync(mailMessage);
                    Debug.WriteLine("Email sent successfully to " + toEmail);
                }
                catch (SmtpException smtpEx)
                {
                    Debug.WriteLine("SMTP Exception: " + smtpEx.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception: " + ex.Message);
                }
            }
        }
    }

public class EmailSender : IEmailSender
    {
        private readonly EmailService _emailService;

        public EmailSender(EmailService emailService)
        {
            _emailService = emailService;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }

}
