using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using testsubject;
using testsubject.Data;
using testsubject.Models;

namespace testsubject.Controllers
{
    public class CarWashBookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripeSettings;

        public CarWashBookingsController(ApplicationDbContext context, IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _stripeSettings = stripeSettings.Value; // Get the value from IOptions
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey; // Set the Stripe secret key
        }

        // GET: CarWashBookings
        public async Task<IActionResult> Index()
        {
            return View(await _context.CarWashBookings.ToListAsync());
        }

        // GET: CarWashBookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carWashBooking = await _context.CarWashBookings.FirstOrDefaultAsync(m => m.Id == id);
            if (carWashBooking == null)
            {
                return NotFound();
            }

            return View(carWashBooking);
        }

        public IActionResult Create()
        {
            ViewData["Campus"] = Enum.GetValues(typeof(Campus)).Cast<Campus>()
                .Select(c => new SelectListItem
                {
                    Value = c.ToString(),
                    Text = c.ToString()
                }).ToList();

            ViewData["CarType"] = Enum.GetValues(typeof(CarType)).Cast<CarType>()
                .Select(c => new SelectListItem
                {
                    Value = c.ToString(),
                    Text = c.ToString()
                }).ToList();

            ViewData["ServiceName"] = new SelectList(
                _context.CarWashServices
                    .Where(s => s.Status == Status.Available && s.RegAccept == RegAccept.Accept)
                    .ToList(),
                "ServiceName",  // This is the field in the CarWashService entity you want to display
                "ServiceName"   // This is the value field that will be submitted
            );

            // Retrieve existing bookings and pass them to the view
            var bookedTimes = _context.CarWashBookings
                .Where(b => b.BookingTime >= DateTime.Now)
                .Select(b => new { b.ServiceName, b.BookingTime })
                .ToList();

            ViewBag.BookedTimes = bookedTimes;

            return View();
        }

        // POST: CarWashBookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerName,UserEmail,ServiceName,CarModel,BookingTime,Campus,CarPlate,CarType")] CarWashBooking carWashBooking)
        {
            if (ModelState.IsValid)
            {
                // Check if the selected service is available
                var service = await _context.CarWashServices
                    .FirstOrDefaultAsync(s => s.ServiceName == carWashBooking.ServiceName && s.Status == Status.Available);

                if (service == null)
                {
                    ModelState.AddModelError("ServiceName", "Selected service is not available.");
                    ViewData["ServiceName"] = new SelectList(_context.CarWashServices.Where(s => s.Status == Status.Available), "ServiceName", "ServiceName");
                    return View(carWashBooking);
                }

                // Check if the booking time is in the past
                if (carWashBooking.BookingTime < DateTime.Now)
                {
                    ModelState.AddModelError("BookingTime", "Booking time must be in the present or future.");
                    ViewData["ServiceName"] = new SelectList(_context.CarWashServices.Where(s => s.Status == Status.Available), "ServiceName", "ServiceName");
                    return View(carWashBooking);
                }

                // Check for existing bookings that overlap
                var bookings = await _context.CarWashBookings
                    .Where(b => b.ServiceName == carWashBooking.ServiceName &&
                                b.BookingTime < carWashBooking.BookingTime.AddMinutes(30) && // Booking ends
                                b.BookingTime.AddMinutes(30) > carWashBooking.BookingTime) // New booking starts
                    .ToListAsync();

                if (bookings.Any())
                {
                    ModelState.AddModelError("BookingTime", "This service is already booked during the selected time.");
                    ViewData["ServiceName"] = new SelectList(_context.CarWashServices.Where(s => s.Status == Status.Available), "ServiceName", "ServiceName");
                    return View(carWashBooking);
                }

                // Add the new booking if no conflicts
                _context.Add(carWashBooking);
                await _context.SaveChangesAsync();

                // Redirect to payment
                return RedirectToAction(nameof(Payment), new { bookingId = carWashBooking.Id });
            }
            ViewData["ServiceName"] = new SelectList(_context.CarWashServices.Where(s => s.Status == Status.Available), "ServiceName", "ServiceName");
            return View(carWashBooking);
        }

        // GET: CarWashBookings/Payment/5
        public async Task<IActionResult> Payment(int bookingId)
        {
            var booking = await _context.CarWashBookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            double cost = CalculateCarWashCost(booking); // Calculate cost based on car type
            var paymentUrl = GenerateStripePaymentUrl(bookingId, cost);
            return Redirect(paymentUrl); // Redirect to Stripe payment
        }

        private double CalculateCarWashCost(CarWashBooking booking)
        {
            var costByCarType = new Dictionary<CarType, decimal>
            {
                { CarType.Sedan, 10.00m },
                { CarType.SUV, 15.00m },
                { CarType.Hatchback, 12.00m },
                { CarType.Coupe, 14.00m },
                { CarType.Convertible, 20.00m },
                { CarType.Wagon, 18.00m },
                { CarType.Van, 22.00m },
                { CarType.Truck, 25.00m }
            };

            if (costByCarType.TryGetValue(booking.CarType, out var cost))
            {
                return (double)cost * 100; // Convert to cents for Stripe
            }

            return 10000; // Example default cost in cents (100.00 ZAR)
        }

        private string GenerateStripePaymentUrl(int bookingId, double cost)
        {
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Car Wash Service",
                                Description = "Full service car wash"
                            },
                            UnitAmount = (long)(cost), // Amount in cents
                            Currency = "zar"
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = Url.Action("Success", "CarWashBookings", new { bookingId }, Request.Scheme),
                CancelUrl = Url.Action("Cancel", "CarWashBookings", null, Request.Scheme)
            };

            var service = new SessionService();
            var session = service.Create(sessionOptions);
            return session.Url; // Return the payment URL
        }
        public IActionResult Success(int bookingId)
        {
            // Find the booking to get necessary details
            var booking = _context.CarWashBookings.Find(bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            // Get the service provider's email from the CarWashService table based on the booking's ServiceName
            var serviceProviderEmail = _context.CarWashServices
                .Where(s => s.ServiceName == booking.ServiceName)
                .Select(s => s.StudentEmail) // Assuming StudentEmail is the correct column
                .FirstOrDefault();

            if (string.IsNullOrEmpty(serviceProviderEmail))
            {
                return NotFound("Service provider email not found.");
            }

            // Check if credit record exists for the service provider
            var userCredit = _context.Credits.FirstOrDefault(c => c.UserEmail == serviceProviderEmail);

            // Calculate the amount to add to credit (your calculation logic can differ)
            decimal amountToAdd = (decimal)CalculateCarWashCost(booking) / 100; // Adjust based on your logic

            if (userCredit != null)
            {
                // Update existing credit record
                userCredit.AvailableCredit += amountToAdd; // Increment the credit
                _context.Update(userCredit);
            }
            else
            {
                // Create a new credit record for the service provider
                userCredit = new Credit
                {
                    UserEmail = serviceProviderEmail, // Use service provider's email
                    AvailableCredit = amountToAdd // Set the initial credit
                };
                _context.Credits.Add(userCredit);
            }

            // Save changes to the database
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                // Log the error (optional)
                Console.WriteLine(ex.InnerException?.Message); // Log or handle the exception appropriately
                return View("Error"); // Return an error view or handle it accordingly
            }

            // Return the success view or redirect to another action
            return View();
        }
        public IActionResult Cancel()
        {
            // Handle cancellation logic here
            return View();
        }

        // GET: CarWashBookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carWashBooking = await _context.CarWashBookings.FindAsync(id);
            if (carWashBooking == null)
            {
                return NotFound();
            }
            return View(carWashBooking);
        }

        // POST: CarWashBookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerName,UserEmail,ServiceName,CarModel,BookingTime,Campus,CarPlate,CarType")] CarWashBooking carWashBooking)
        {
            if (id != carWashBooking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(carWashBooking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarWashBookingExists(carWashBooking.Id))
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
            return View(carWashBooking);
        }

        // GET: CarWashBookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carWashBooking = await _context.CarWashBookings.FirstOrDefaultAsync(m => m.Id == id);
            if (carWashBooking == null)
            {
                return NotFound();
            }

            return View(carWashBooking);
        }

        // POST: CarWashBookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carWashBooking = await _context.CarWashBookings.FindAsync(id);
            _context.CarWashBookings.Remove(carWashBooking);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarWashBookingExists(int id)
        {
            return _context.CarWashBookings.Any(e => e.Id == id);
        }
    }
}
