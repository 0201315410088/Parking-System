//using System.Diagnostics;
//using System.Security.Claims;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using Stripe;
//using Stripe.Checkout;
//using testsubject.Data;
//using testsubject.Models;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using System.IO;
//using System;

//namespace YourNamespace.Controllers
//{
//    [Authorize]
//    public class CarWashPaymentController : Controller
//    {
//        private readonly StripeSettings _stripeSettings;
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<IdentityUser> _userManager;
//        private readonly EmailService _emailService; // Inject EmailService

//        public CarWashPaymentController(IOptions<StripeSettings> stripeSettings, ApplicationDbContext context, UserManager<IdentityUser> userManager, EmailService emailService)
//        {
//            _stripeSettings = stripeSettings.Value;
//            _context = context;
//            _userManager = userManager;
//            _emailService = emailService;
//        }

//        [HttpGet]
//        public async Task<IActionResult> CreateCheckoutSession(int carWashId)
//        {
//            if (carWashId <= 0)
//            {
//                ModelState.AddModelError(string.Empty, "Invalid car wash selected.");
//                return View("Index", await _context.CarWashs.ToListAsync());
//            }

//            var carWash = await _context.CarWashs.FindAsync(carWashId);
//            if (carWash == null)
//            {
//                ModelState.AddModelError(string.Empty, "Car wash booking not found.");
//                return View("Index", await _context.CarWashs.ToListAsync());
//            }

//            var user = await _userManager.GetUserAsync(User);
//            if (user == null || carWash.Email != user.Email)
//            {
//                return Forbid();
//            }

//            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

//            try
//            {
//                var options = new SessionCreateOptions
//                {
//                    PaymentMethodTypes = new List<string> { "card" },
//                    LineItems = new List<SessionLineItemOptions>
//                    {
//                        new SessionLineItemOptions
//                        {
//                            PriceData = new SessionLineItemPriceDataOptions
//                            {
//                                Currency = "usd",
//                                ProductData = new SessionLineItemPriceDataProductDataOptions
//                                {
//                                    Name = "Car Wash Payment",
//                                    Description = "Payment for car wash service",
//                                },
//                                UnitAmount = Convert.ToInt32(carWash.Price * 100),
//                            },
//                            Quantity = 1,
//                        },
//                    },
//                    Mode = "payment",
//                    SuccessUrl = Url.Action("Success", "CarWashPayment", null, Request.Scheme),
//                    CancelUrl = Url.Action("Index", "CarWash", null, Request.Scheme),
//                    Metadata = new Dictionary<string, string>
//                    {
//                        { "CarWashId", carWashId.ToString() }
//                    }
//                };

//                var service = new SessionService();
//                var session = service.Create(options);

//                return Redirect(session.Url);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error creating Stripe Checkout session: {ex.Message}");
//                return StatusCode(500, "Internal server error");
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> CheckoutSessionCompleted()
//        {
//            try
//            {
//                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
//                var stripeEvent = EventUtility.ParseEvent(json);

//                if (stripeEvent.Type == Events.CheckoutSessionCompleted)
//                {
//                    var session = stripeEvent.Data.Object as Session;
//                    if (session != null)
//                    {
//                        var carWashId = Convert.ToInt32(session.Metadata["CarWashId"]);
//                        var carWash = await _context.CarWashs.FindAsync(carWashId);

//                        if (carWash != null)
//                        {
//                            // Send an email notification after successful payment
//                            var user = await _userManager.GetUserAsync(User);
//                            if (user != null)
//                            {
//                                var emailBody = $"Your payment for car wash service (ID: {carWashId}) was successful. Thank you!";
//                                await _emailService.SendEmailAsync(user.Email, "Payment Confirmation", emailBody);
//                            }

//                            // Update car wash booking status or perform other relevant actions
//                            // Example: Mark as paid or update any specific fields related to payment
//                            TempData["AlertMessage"] = $"Car wash ID {carWashId} successfully paid.";
//                        }
//                        else
//                        {
//                            TempData["AlertMessage"] = $"Car wash ID {carWashId} not found.";
//                        }
//                    }
//                    else
//                    {
//                        Debug.WriteLine("Session object is null.");
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"Received event of type {stripeEvent.Type} instead of {Events.CheckoutSessionCompleted}.");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Exception in CheckoutSessionCompleted: {ex.Message}");
//                return BadRequest();
//            }

//            return Ok();
//        }

//        public IActionResult Success()
//        {
//            return View();
//        }
//    }
//}
