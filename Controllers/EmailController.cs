using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp; // Make sure this is the desired namespace for SmtpClient
using System;

namespace testsubject.Controllers
{
    public class EmailController : Controller
    {
        private readonly IConfiguration _configuration;

        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SendEmail(string toEmail)
        {
            var smtpSettings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Test Project", smtpSettings["FromAddress"]));
            message.To.Add(new MailboxAddress("Recipient", toEmail));
            message.Subject = "Hi, this is demo email";
            message.Body = new TextPart("plain")
            {
                Text = "Hello, My First Demo Mail it is. Thanks",
            };

            try
            {
                using (var client = new MailKit.Net.Smtp.SmtpClient()) // Fully qualified name
                {
                    // Port 465 is typically used with SSL; Port 587 is used with STARTTLS
                    client.Connect(smtpSettings["SmtpServer"], int.Parse(smtpSettings["SmtpPort"]), true);
                    client.Authenticate(smtpSettings["SmtpUsername"], smtpSettings["SmtpPassword"]);
                    client.Send(message);
                    client.Disconnect(true);
                }
                ViewData["Message"] = "Email sent successfully.";
            }
            catch (Exception ex)
            {
                ViewData["Message"] = $"Error sending email: {ex.Message}";
            }

            return View("Index");
        }
    }
}
