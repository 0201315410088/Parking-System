using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using testsubject.Data;
using testsubject.Models;

namespace testsubject.Controllers
{
    public class CarWashServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public CarWashServicesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserEmail = User.Identity?.Name;

            if (currentUserEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var latestCarWashService = await _context.CarWashServices
                .Where(c => c.StudentEmail == currentUserEmail)
                .OrderByDescending(c => c.SubmissionDate)
                .FirstOrDefaultAsync();

            ViewBag.RegAcceptStatus = "Awaiting";

            if (latestCarWashService != null)
            {
                if (latestCarWashService.RegAccept == RegAccept.Accept)
                {
                    ViewBag.RegAcceptStatus = "Accepted";
                }
                else if (latestCarWashService.RegAccept == RegAccept.Rejected)
                {
                    ViewBag.RegAcceptStatus = "Rejected";
                }
            }

            return View(latestCarWashService);
        }

        public async Task<IActionResult> AdminIndex()
        {
            var awaitingServices = await _context.CarWashServices
                .Where(s => s.RegAccept == RegAccept.Awaiting)
                .ToListAsync();

            return View(awaitingServices);
        }

        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            var service = await _context.CarWashServices.FindAsync(id);
            if (service != null)
            {
                service.RegAccept = RegAccept.Accept;
                service.Status = Status.Available;
                _context.CarWashServices.Update(service);
                await _context.SaveChangesAsync();

                // Send professional email for acceptance
                string subject = "Car Wash Service Accepted";
                string message = $@"
                    We are pleased to inform you that your car wash service '{service.ServiceName}' has been accepted! 
                    You can now start receiving bookings from customers.
                    <br/><br/>
                    Service Details:
                    <ul>
                        <li><strong>Service Name:</strong> {service.ServiceName}</li>
                        <li><strong>Status:</strong> Available</li>
                    </ul>
                    <br/>
                    Thank you for your submission and we wish you success with your car wash service.";

                await SendEmailAsync(service.StudentEmail, subject, message);
            }

            return RedirectToAction(nameof(AdminIndex));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var service = await _context.CarWashServices.FindAsync(id);
            if (service != null)
            {
                service.RegAccept = RegAccept.Rejected;
                service.Status = Status.Not_Available;
                _context.CarWashServices.Update(service);
                await _context.SaveChangesAsync();

                // Send professional email for rejection
                string subject = "Car Wash Service Rejected";
                string message = $@"
                    We regret to inform you that your car wash service '{service.ServiceName}' has been rejected at this time.
                    Please feel free to make adjustments and submit again for consideration.
                    <br/><br/>
                    Service Details:
                    <ul>
                        <li><strong>Service Name:</strong> {service.ServiceName}</li>
                        <li><strong>Status:</strong> Not Available</li>
                    </ul>
                    <br/>
                    Thank you for your submission.";

                await SendEmailAsync(service.StudentEmail, subject, message);
            }

            return RedirectToAction(nameof(AdminIndex));
        }

        private async Task SendEmailAsync(string email, string subject, string messageContent)
        {
            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("Car Wash Services", _configuration["EmailSettings:FromAddress"]));
                emailMessage.To.Add(new MailboxAddress("", email));
                emailMessage.Subject = subject;

                // Professional email template in HTML
                var messageBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f8f8; padding: 20px;'>
                            <tr>
                                <td align='center'>
                                    <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border: 1px solid #e0e0e0; padding: 20px;'>
                                        <tr>
                                            <td style='font-size: 20px; font-weight: bold; text-align: center; color: #4CAF50;'>
                                                Car Wash Services
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 10px 0 20px 0; font-size: 16px;'>
                                                <p>Dear {email},</p>
                                                <p>{messageContent}</p>
                                                <p style='margin-top: 20px;'>Best regards,<br/>Car Wash Services Team</p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='text-align: center; padding: 20px 0;'>
                                                <small style='color: #888;'>This is an automated message. Please do not reply.</small>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </body>
                </html>";

                emailMessage.Body = new TextPart("html")
                {
                    Text = messageBody
                };

                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"];
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                    var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                    var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                    var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);

                    await client.ConnectAsync(smtpServer, smtpPort, enableSsl);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carWashService = await _context.CarWashServices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (carWashService == null)
            {
                return NotFound();
            }

            return View(carWashService);
        }

        public async Task<IActionResult> Create()
        {
            var userEmail = User.Identity.Name;
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (profile != null)
            {
                var carWashService = new CarWashService
                {
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    PhoneNumber = profile.PhoneNumber,
                    City = profile.City,
                    State = profile.State,
                    Username = profile.Username,
                    PostalCode = profile.PostalCode,
                    Country = profile.Country,
                    Address = profile.Address,
                    DateOfBirth = profile.DateOfBirth,
                    SubmissionDate = DateTime.Now,
                    StudentEmail = userEmail
                };

                return View(carWashService);
            }

            return View(new CarWashService());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentEmail,FirstName,Username,DateOfBirth,LastName,PhoneNumber,City,State,PostalCode,Country,Address,ServiceName,Description,SubmissionDate")] CarWashService carWashService)
        {
            if (ModelState.IsValid)
            {
                carWashService.SubmissionDate = DateTime.Now;
                carWashService.Status = Status.Available;

                _context.Add(carWashService);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(carWashService);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carWashService = await _context.CarWashServices.FindAsync(id);
            if (carWashService == null)
            {
                return NotFound();
            }
            return View(carWashService);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Username,City,State,PostalCode,Country,DateOfBirth,ServiceName,Description,SubmissionDate,Status,StudentEmail,PhoneNumber,Address")] CarWashService carWashService)
        {
            if (id != carWashService.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(carWashService);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarWashServiceExists(carWashService.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(carWashService);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carWashService = await _context.CarWashServices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (carWashService == null)
            {
                return NotFound();
            }

            return View(carWashService);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carWashService = await _context.CarWashServices.FindAsync(id);
            _context.CarWashServices.Remove(carWashService);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarWashServiceExists(int id)
        {
            return _context.CarWashServices.Any(e => e.Id == id);
        }
    }
}
