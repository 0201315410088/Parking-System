using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.Identity;


using testsubject.Data;
using testsubject.Models;
using MimeKit; // For creating email messages
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization; // For sending emails

namespace testsubject.Controllers
{
    public class CreditsController : Controller
    {



        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripeSettings;
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public CreditsController(ApplicationDbContext context,
                                 IOptions<StripeSettings> stripeSettings,
                                 IConfiguration configuration,
                                 UserManager<IdentityUser> userManager)
        {
            _context = context;
            _stripeSettings = stripeSettings?.Value ?? throw new ArgumentNullException(nameof(stripeSettings));
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            _configuration = configuration; // For email configuration
            _userManager = userManager; // Injecting UserManager for identity operations
        }
        public async Task<IActionResult> Create(Credit obj)
        {
            return View(obj);
        }
            [HttpPost]
        public async Task<IActionResult> Create()
        {
            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the credit account already exists
            var existingCredit = await _context.Credits.FirstOrDefaultAsync(c => c.UserEmail == user.Email);
            if (existingCredit != null)
            {
                return BadRequest("Credit account already exists.");
            }

            // Create a new credit account
            var newCredit = new Credit
            {
                UserEmail = user.Email,
                AvailableCredit = 0, // Initialize with zero credits
                AccountNumber = GenerateUniqueAccountNumber() // Call your method to generate the account number
            };

            // Save to the database
            _context.Credits.Add(newCredit);
            await _context.SaveChangesAsync();

            return Ok("Credit account created successfully.");
        }

        private string GenerateUniqueAccountNumber()
        {
            Random random = new Random();
            string accountNumber;

            do
            {
                accountNumber = random.Next(100000000, 999999999).ToString() + random.Next(1000, 9999).ToString();
            } while (_context.Credits.Any(c => c.AccountNumber == accountNumber)); // Ensure uniqueness

            return accountNumber;
        }



        // GET: Credits
        public async Task<IActionResult> Index()
        {
            var credits = await _context.Credits.ToListAsync();
            return View(credits ?? new List<Credit>()); // Ensure model is initialized
        }

        // GET: Credits/Pay
        public IActionResult Pay()
        {
            var userEmail = User.Identity.Name;
            var credit = _context.Credits.FirstOrDefault(c => c.UserEmail == userEmail);

            if (credit == null)
            {
                return NotFound(); // Handle case where credit is not found
            }

            ViewBag.StripePublicKey = _stripeSettings.PublicKey;
            return View(credit);
        }

        // Action for successful payment
        public IActionResult Success()
        {
            return View();
        }

        // POST: Credits/Pay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(decimal amount)
        {
            const decimal minimumPayment = 13.00m; // Minimum payment amount in Rands
            var userEmail = User.Identity.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "User is not logged in." });
            }

            var credit = await _context.Credits.FirstOrDefaultAsync(c => c.UserEmail == userEmail);

            if (credit == null || credit.AvailableCredit < amount)
            {
                return Json(new { success = false, message = "Insufficient credits available for payment." });
            }

            // Check if the amount is less than the minimum payment
            if (amount < minimumPayment)
            {
                return Json(new { success = false, message = $"The minimum payment amount is {minimumPayment} Rands." });
            }

            try
            {
                // Create a new charge for the payment
                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert amount to cents
                    Currency = "zar", // Specify the currency
                    Source = Request.Form["stripeToken"].FirstOrDefault(), // Get the Stripe token from the form
                    Description = "Payment for car wash services"
                };

                var chargeService = new ChargeService();
                Charge charge = await chargeService.CreateAsync(chargeOptions);

                // Check if the charge was successful
                if (charge.Status == "succeeded")
                {
                    // Deduct the amount from the user's available credit
                    credit.AvailableCredit -= amount;
                    _context.Update(credit);

                    // Create a new transaction record
                    var transaction = new Transaction
                    {
                        UserEmail = userEmail,
                        Amount = amount, // Use the selected credit amount to be paid
                        TransactionDate = DateTime.UtcNow,
                        Description = "Payment for car wash services"
                    };
                    await _context.Transactions.AddAsync(transaction);
                    await _context.SaveChangesAsync();

                    // Send email notification
                    SendEmail(credit.UserEmail, amount);

                    // Redirect to Transactions page
                    return RedirectToAction("Transactions");
                }
                else
                {
                    return Json(new { success = false, message = "Payment failed. Please try again." });
                }
            }
            catch (StripeException ex)
            {
                return Json(new { success = false, message = $"Error processing payment: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        // GET: Credits/Transactions
        public async Task<IActionResult> Transactions(decimal amount)
        {
            var userEmail = User.Identity.Name;
            var transactions = await _context.Transactions
                                              .Where(t => t.UserEmail == userEmail)
                                              .OrderByDescending(t => t.TransactionDate)
                                              .ToListAsync();

            return View(transactions);
        }
        // GET: Credits/AdminTransactions
        [Authorize(Roles = "Admin")] // Restrict access to Admins
        public async Task<IActionResult> AdminTransactions(decimal amount)
        {
            var transactions = await _context.Transactions
                                              .OrderByDescending(t => t.TransactionDate)
                                              .ToListAsync();

            return View(transactions);
        }






        private void SendEmail(string toEmail, decimal amount)
        {
            var smtpSettings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("DUT Car Wash", smtpSettings["FromAddress"]));
            message.To.Add(new MailboxAddress("Recipient", toEmail));
            message.Subject = "Withdrawal Confirmation";

            // Create a formatted HTML message body
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    color: #333;
                    line-height: 1.6;
                    margin: 0;
                    padding: 0;
                    background-color: #f4f4f4;
                }}
                .receipt {{
                    max-width: 600px;
                    margin: 20px auto;
                    padding: 20px;
                    border: 1px solid #28a745; /* Changed border color to match the theme */
                    border-radius: 5px;
                    background-color: #fff;
                    box-shadow: 0 0 10px rgba(0,0,0,0.1);
                }}
                .header {{
                    text-align: center;
                    border-bottom: 2px solid #28a745;
                    padding-bottom: 10px;
                    margin-bottom: 20px;
                }}
                .header img {{
                    width: 150px;
                }}
                .title {{
                    font-size: 28px; /* Increased title size */
                    font-weight: bold;
                    color: #28a745;
                    margin: 10px 0;
                }}
                .content {{
                    margin-bottom: 20px;
                }}
                .item {{
                    display: flex;
                    justify-content: space-between;
                    padding: 10px 0;
                    border-bottom: 1px solid #eee;
                }}
                .total {{
                    font-weight: bold;
                    font-size: 20px; /* Increased total size */
                    color: #333;
                }}
                .footer {{
                    margin-top: 20px;
                    font-size: 12px;
                    color: #666;
                    text-align: center;
                }}
                .highlight {{
                    color: #007bff;
                    font-weight: bold;
                }}
                .service-partner {{
                    margin-top: 20px;
                    font-weight: bold;
                    font-size: 16px; /* Styled service partner information */
                }}
                .date {{
                    margin: 10px 0;
                    font-style: italic; /* Italicize the date */
                    color: #777;
                }}
            </style>
        </head>
        <body>
            <div class='receipt'>
                <div class='header'>
                    <img src='C:\Users\mthut\Desktop\testsubject\wwwroot\download.png' alt='DUT Logo' />
                    <div class='title'>Withdrawal Receipt</div>
                </div>
                <div class='content'>
                    <p>Hello,</p>
                    <p>Your withdrawal transaction has been processed successfully.</p>
                    <div class='item'>
                        <span>Amount Withdrawn:</span>
                        <span class='highlight'>R{amount}</span>
                    </div>
                    <div class='item'>
                        <span>Service Partner:</span>
                        <span class='service-partner'>{toEmail}</span>
                    </div>
                    <div class='item date'>
                        <span>Date:</span>
                        <span class='highlight'>{DateTime.Now.ToString("MMMM dd, yyyy")}</span> <!-- Dynamic date insertion -->
                    </div>
                    <div class='item total'>
                        <span>Total:</span>
                        <span class='highlight'>R{amount}</span>
                    </div>
                </div>
                <div class='footer'>
                    <p>Thank you for your business!</p>
                    <p>Best regards,</p>
                    <p>DUT Car Wash Team</p>
                </div>
            </div>
        </body>
        </html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(smtpSettings["SmtpServer"], int.Parse(smtpSettings["SmtpPort"]), true);
                    client.Authenticate(smtpSettings["SmtpUsername"], smtpSettings["SmtpPassword"]);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Handle email sending errors as needed (log them or notify admin)
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

        // Other actions...











            // POST: Credits/Transfer
            [HttpGet]
        public async Task<IActionResult> Transfer()
        {
            var userEmail = User.Identity?.Name;

            // Check if the user is logged in
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if not logged in
            }

            try
            {
                // Get the logged-in user's credit information
                var senderCredit = await _context.Credits.FirstOrDefaultAsync(c => c.UserEmail == userEmail);
                if (senderCredit == null)
                {
                    // If no credit record is found for the logged-in user
                    ViewBag.ErrorMessage = "Your credit record was not found.";
                    return View();
                }

                // Pass the user's available credit to the view
                ViewBag.AvailableCredit = senderCredit.AvailableCredit;

                return View();
            }
            catch (Exception ex)
            {
                // Log and show error if something goes wrong
                Console.WriteLine($"Error occurred while loading credit transfer page: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the transfer page. Please try again.";
                return View();
            }
        }

        // Transfer Credits POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(string recipientEmail, string recipientAccountNumber, decimal amount)
        {
            string resultMessage;

            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(recipientEmail) || string.IsNullOrEmpty(recipientAccountNumber) || amount <= 0)
                {
                    resultMessage = "Invalid input.";
                    ViewBag.ResultMessage = resultMessage;
                    return View(); // Consider returning a view that indicates an error instead
                }

                var senderEmail = User.Identity.Name; // Get the logged-in user's email
                var senderAccount = await _context.Credits.FirstOrDefaultAsync(c => c.UserEmail == senderEmail);

                // Check if the sender has sufficient credits
                if (senderAccount == null || senderAccount.AvailableCredit < amount)
                {
                    resultMessage = "Insufficient credits.";
                    ViewBag.ResultMessage = resultMessage;
                    return View(); // Consider returning a view that indicates an error instead
                }

                // Fetch recipient's account details
                var recipientAccount = await _context.Credits.FirstOrDefaultAsync(c => c.AccountNumber == recipientAccountNumber && c.UserEmail == recipientEmail);

                if (recipientAccount == null)
                {
                    resultMessage = "Recipient not found.";
                    ViewBag.ResultMessage = resultMessage;
                    return View(); // Consider returning a view that indicates an error instead
                }

                // Transfer credits
                senderAccount.AvailableCredit -= amount;
                recipientAccount.AvailableCredit += amount;

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Create transaction records for both sender and recipient
                var senderTransaction = new Transaction
                {
                    UserEmail = senderEmail,
                    Amount = -amount, // Negative for outgoing transaction
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Transferred R{amount} to {recipientEmail}"
                };

                var recipientTransaction = new Transaction
                {
                    UserEmail = recipientEmail,
                    Amount = amount, // Positive for incoming transaction
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Received R{amount} from {senderEmail}"
                };

                // Add both transactions to the context
                await _context.Transactions.AddAsync(senderTransaction);
                await _context.Transactions.AddAsync(recipientTransaction);

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Send notification emails to both parties
                SendEmail(senderEmail, amount, recipientEmail, "Credit Transfer Notification");
                SendEmail(recipientEmail, amount, senderEmail, "Credit Transfer Notification");

                // Redirect to the success page
                return RedirectToAction("Successs"); // Make sure "Success" is the correct name of your action method
            }
            catch (Exception ex)
            {
                // Log the error for further investigation
                Console.WriteLine($"Error during credit transfer: {ex.Message}");
                resultMessage = "An unexpected error occurred. Please try again.";
            }

            ViewBag.ResultMessage = resultMessage;
            return View(); // Consider returning an error view instead
        }

        // GET: Credits/Success
        public IActionResult Successs()
        {
            return View(); // This should return the view for the success page
        }


        // Reused EmailSend method to notify both sender and recipient
        private void EmailSend(string senderEmail, string recipientEmail, decimal amount)
        {
            var smtpSettings = _configuration.GetSection("EmailSettings");

            // Send email to sender
            SendEmail(senderEmail, amount, recipientEmail, "You have successfully transferred credits.");

            // Send email to recipient
            SendEmail(recipientEmail, amount, senderEmail, "You have received credits.");
        }

        private void SendEmail(string toEmail, decimal amount, string counterpartEmail, string subject)
        {
            var smtpSettings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("DUT Car Wash", smtpSettings["FromAddress"]));
            message.To.Add(new MailboxAddress("Recipient", toEmail));
            message.Subject = subject;

            // Create a formatted HTML message body that resembles a receipt
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    color: #333;
                    line-height: 1.6;
                    margin: 0;
                    padding: 0;
                    background-color: #f4f4f4;
                }}
                .receipt {{
                    max-width: 600px;
                    margin: 20px auto;
                    padding: 20px;
                    border: 1px solid #28a745; /* Changed border color to match the theme */
                    border-radius: 5px;
                    background-color: #fff;
                    box-shadow: 0 0 10px rgba(0,0,0,0.1);
                }}
                .header {{
                    text-align: center;
                    border-bottom: 2px solid #28a745;
                    padding-bottom: 10px;
                    margin-bottom: 20px;
                }}
                .header img {{
                    width: 150px;
                }}
                .title {{
                    font-size: 28px; /* Increased title size */
                    font-weight: bold;
                    color: #28a745;
                    margin: 10px 0;
                }}
                .content {{
                    margin-bottom: 20px;
                }}
                .item {{
                    display: flex;
                    justify-content: space-between;
                    padding: 10px 0;
                    border-bottom: 1px solid #eee;
                }}
                .total {{
                    font-weight: bold;
                    font-size: 20px; /* Increased total size */
                    color: #333;
                }}
                .footer {{
                    margin-top: 20px;
                    font-size: 12px;
                    color: #666;
                    text-align: center;
                }}
                .highlight {{
                    color: #007bff;
                    font-weight: bold;
                }}
                .service-partner {{
                    margin-top: 20px;
                    font-weight: bold;
                    font-size: 16px; /* Styled service partner information */
                }}
                .date {{
                    margin: 10px 0;
                    font-style: italic; /* Italicize the date */
                    color: #777;
                }}
            </style>
        </head>
        <body>
            <div class='receipt'>
                <div class='header'>
                    <img src='C:\Users\mthut\Desktop\testsubject\wwwroot\download.png' alt='DUT Logo' />
                    <div class='title'>Transfer Receipt</div>
                </div>
                <div class='content'>
                    <p>Hello,</p>
                    <p>Your Tranfer has been processed successfully.</p>
                    <div class='item'>
                        <span>Amount Transfered:</span>
                        <span class='highlight'>R{amount}</span>
                    </div>
                    <div class='item'>
                        <span>Service Partner:</span>
                        <span class='service-partner'>{counterpartEmail}</span>
                    </div>
                    <div class='item date'>
                        <span>Date:</span>
                        <span class='highlight'>{DateTime.Now.ToString("MMMM dd, yyyy")}</span> <!-- Dynamic date insertion -->
                    </div>
                    <div class='item total'>
                        <span>Total:</span>
                        <span class='highlight'>R{amount}</span>
                    </div>
                </div>
                <div class='footer'>
                    <p>Thank you for your business!</p>
                    <p>Best regards,</p>
                    <p>DUT Car Wash Team</p>
                </div>
            </div>
        </body>
        </html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(smtpSettings["SmtpServer"], int.Parse(smtpSettings["SmtpPort"]), true);
                    client.Authenticate(smtpSettings["SmtpUsername"], smtpSettings["SmtpPassword"]);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Handle email sending errors as needed (log them or notify admin)
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }


    }
}
