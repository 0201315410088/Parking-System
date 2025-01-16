using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using Stripe.Checkout;
using Stripe;
using System.Diagnostics;
using System.Security.Policy;
using testsubject.Data;
using testsubject.Models;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly StripeSettings _stripeSettings;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public HomeController(IOptions<StripeSettings> stripeSettings, ApplicationDbContext context, UserManager<IdentityUser> userManager, IConfiguration configuration)
    {
        _stripeSettings = stripeSettings.Value;
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    // GET: Home/Index
    //public async Task<IActionResult> Index()
    //{
    //    var user = await _userManager.GetUserAsync(User);
    //    var userEmail = user?.Email;

    //    var bookings = await _context.Bookings
    //        .Where(b => b.CustomerEmail == userEmail && b.BookingStatus != BookingStatus.PAID)
    //        .ToListAsync();

    //    return View(bookings);
    //}

    //[HttpPost]
    //public async Task<IActionResult> CreateCheckoutSession(int bookingId)
    //{
    //    if (bookingId <= 0)
    //    {
    //        ModelState.AddModelError(string.Empty, "Invalid booking selected.");
    //        return View("Index", await _context.Bookings.ToListAsync());
    //    }

    //    var booking = await _context.Bookings.FindAsync(bookingId);
    //    if (booking == null)
    //    {
    //        ModelState.AddModelError(string.Empty, "Booking not found.");
    //        return View("Index", await _context.Bookings.ToListAsync());
    //    }

    //    var user = await _userManager.GetUserAsync(User);
    //    if (user == null || booking.CustomerEmail != user.Email)
    //    {
    //        return Forbid();
    //    }

    //    StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

    //    try
    //    {
    //        var options = new SessionCreateOptions
    //        {
    //            PaymentMethodTypes = new List<string> { "card" },
    //            LineItems = new List<SessionLineItemOptions>
    //            {
    //                new SessionLineItemOptions
    //                {
    //                    PriceData = new SessionLineItemPriceDataOptions
    //                    {
    //                        Currency = "usd",
    //                        ProductData = new SessionLineItemPriceDataProductDataOptions
    //                        {
    //                            Name = "Booking Payment",
    //                            Description = "Payment for booking",
    //                        },
    //                        UnitAmount = (long)(Convert.ToDecimal(booking.Cost) * 100m),
    //                    },
    //                    Quantity = 1,
    //                },
    //            },
    //            Mode = "payment",
    //            SuccessUrl = Url.Action("Success", "Home", new { bookingId = bookingId }, Request.Scheme),
    //            CancelUrl = Url.Action("Index", "Home", null, Request.Scheme),
    //            Metadata = new Dictionary<string, string>
    //            {
    //                { "BookingId", bookingId.ToString() }
    //            }
    //        };

    //        var service = new SessionService();
    //        var session = service.Create(options);

    //        // Update booking status after payment is successful
    //        booking.BookingStatus = BookingStatus.PAID;
    //        booking.CountdownStartTime = DateTime.UtcNow;
    //        _context.Update(booking);
    //        await _context.SaveChangesAsync();

    //        TempData["AlertMessage"] = $"Booking ID {bookingId} status successfully changed to PAID.";
    //        return Redirect(session.Url);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Error creating Stripe Checkout session: {ex.Message}");
    //        return StatusCode(500, "Internal server error");
    //    }
    //}

    //public async Task<IActionResult> Success(int bookingId)
    //{
    //    var booking = await _context.Bookings.FindAsync(bookingId);
    //    if (booking == null)
    //    {
    //        return NotFound();
    //    }

    //    var user = await _userManager.GetUserAsync(User);
    //    if (user == null)
    //    {
    //        return Forbid();
    //    }

    //    // Send payment confirmation email
    //    try
    //    {
    //        var smtpSettings = _configuration.GetSection("EmailSettings");
    //        string host = smtpSettings["SmtpServer"];
    //        int port = int.Parse(smtpSettings["SmtpPort"]);
    //        bool enableSsl = smtpSettings["EnableSsl"] == "true";

    //        var message = new MimeMessage();
    //        message.From.Add(new MailboxAddress("Your Company", smtpSettings["FromAddress"]));
    //        message.To.Add(new MailboxAddress("Customer", user.Email));
    //        message.Subject = "Payment Confirmation";

    //        // Format amount as USD
    //        decimal amountPaid = Convert.ToDecimal(booking.Cost);
    //        string formattedAmount = amountPaid.ToString("C", new System.Globalization.CultureInfo("en-US"));

    //        message.Body = new TextPart("html") // Using HTML format for a professional look
    //        {
    //            Text = $@"
    //    <html>
    //    <body>
    //        <h2>Payment Confirmation</h2>
    //        <p>Dear Customer,</p>
    //        <p>Thank you for your payment.</p>
    //        <p><strong>Details:</strong></p>
    //        <ul>
    //            <li><strong>Product Name:</strong> Booking Payment</li>
    //            <li><strong>Description:</strong> Payment for booking</li>
    //            <li><strong>Amount Paid:</strong> {formattedAmount}</li>
    //        </ul>
    //        <p>Best regards,<br>Your Company</p>
    //    </body>
    //    </html>"
    //        };

    //        using (var client = new MailKit.Net.Smtp.SmtpClient()) // Fully qualified name
    //        {
    //            client.Connect(host, port, enableSsl);
    //            client.Authenticate(smtpSettings["SmtpUsername"], smtpSettings["SmtpPassword"]);
    //            await client.SendAsync(message);
    //            client.Disconnect(true);
    //        }

    //        Debug.WriteLine("Payment confirmation email sent successfully.");


   
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Error sending email: {ex.Message}");
    //    }

    //    return View(booking);
    //}

    public IActionResult Cancel()
    {
        return View("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Services()
    {
        return View();
    }
    public IActionResult ManageSlots()
    {
        return View();
    }
    public IActionResult Contact()
    {
        return View();
    }
    public IActionResult Blog()
    {
        return View();
    }
    public IActionResult Home()
    {
        return View();
    }

    //public async Task<IActionResult> BookSpot()
    //{
    //    var user = await _userManager.GetUserAsync(User);
    //    var userEmail = user?.Email;

    //    var bookings = await _context.Bookings
    //        .Where(b => b.CustomerEmail == userEmail && b.BookingStatus != BookingStatus.PAID)
    //        .ToListAsync();

    //    return View(bookings);
    //}
}
