using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using testsubject.Data;
using testsubject.Models;



public class NewParkingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly StripeSettings _stripeSettings;

    public NewParkingController(ApplicationDbContext context, IOptions<StripeSettings> stripeSettings)
    {
        _context = context;
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }




    public async Task<IActionResult> CampusRecords()
    {
        // Fetch all bookings without filtering by user email
        var bookings = await _context.NewBookings
                                      .Include(b => b.Slot)
                                      .OrderByDescending(b => b.BookingTime)
                                      .ToListAsync();

        // Create a list of booking view models
        var bookingViewModels = bookings.Select(b => new CarImageBookingViewModel
        {
            BookingViewModel = new BookingViewModel
            {
                BookingId = b.BookingId,
                SlotId = b.SlotId,
                BookingTime = b.BookingTime,
                EndTime = b.EndTime,
                CarPlate = b.CarPlate,
                CarType = b.CarType,
                BookingDuration = b.EndTime - b.BookingTime,
                Status = b.EndTime.AddHours(-4) > DateTime.UtcNow ? "Active" : "Expired"
            },
            // Fetch associated car image based on the booking's user email
            CarImageModel = _context.CarImages.FirstOrDefault(c => c.UserEmail == b.UserEmail) // Use b.UserEmail to get the image related to the booking
        }).ToList();

        return View(bookingViewModels);
    }






    public async Task<IActionResult> MyBookings()
    {
        string userEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized("User email not found. Please log in.");
        }

        var bookings = await _context.NewBookings
                                     .Where(b => b.UserEmail == userEmail)
                                     .Include(b => b.Slot)
                                     .OrderByDescending(b => b.BookingTime)
                                     .ToListAsync();

        var bookingViewModels = bookings.Select(b => new CarImageBookingViewModel
        {
            BookingViewModel = new BookingViewModel
            {
                BookingId = b.BookingId,
                SlotId = b.SlotId,
                BookingTime = b.BookingTime,
                EndTime = b.EndTime,
                CarPlate = b.CarPlate,
                CarType = b.CarType,
                BookingDuration = b.EndTime - b.BookingTime,
                Status = b.EndTime.AddHours(-4) > DateTime.UtcNow ? "Active" : "Expired"
            },
            CarImageModel = _context.CarImages.FirstOrDefault(c => c.UserEmail == userEmail) // Fetching associated car image based on the user email
        }).ToList();

        return View(bookingViewModels);
    }



    public IActionResult Undefined()
    {
        return View();
    }



    public async Task<IActionResult> AvailableSlots()
    {
        var slots = await _context.Slots
            .Include(s => s.Bookings) // Include bookings
            .ToListAsync();

        return View(slots);
    }




    [HttpPost]
    public async Task<IActionResult> BookSlot(int slotId, DateTime bookingStartTime, DateTime bookingEndTime, CarType carType, string carPlate)
    {
        string userEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
        {
            return Json(new { success = false, message = "User email not found. Please log in." });
        }

        var slot = await _context.Slots.FirstOrDefaultAsync(s => s.SlotId == slotId);
        if (slot == null)
        {
            return Json(new { success = false, message = "Invalid slot." });
        }

        if (bookingEndTime <= bookingStartTime || bookingStartTime <= DateTime.UtcNow)
        {
            return Json(new { success = false, message = "Booking times must be valid and in the future." });
        }

        // Ensure booking duration is at least one hour
        if ((bookingEndTime - bookingStartTime).TotalHours < 1)
        {
            return Json(new { success = false, message = "Booking duration must be at least one hour." });
        }

        // Adjust booking times to match the database time (2 hours behind)
        var adjustedBookingStartTime = bookingStartTime.AddHours(0);
        var adjustedBookingEndTime = bookingEndTime.AddHours(0);

        // Check for existing active bookings for the user
        var existingBooking = await _context.NewBookings
            .FirstOrDefaultAsync(n => n.UserEmail == userEmail &&
                                       n.EndTime > DateTime.UtcNow.AddHours(0)); // Active booking check

        if (existingBooking != null)
        {
            return Json(new { success = false, message = "You already have an active booking. Please sign out before making a new one." });
        }

        // Check for overlapping bookings
        var isConflicted = await _context.NewBookings
            .AnyAsync(n => n.SlotId == slotId &&
                           n.BookingTime < adjustedBookingEndTime &&
                           n.EndTime > adjustedBookingStartTime);

        if (isConflicted)
        {
            return Json(new { success = false, message = "This time slot is already booked or time overlap!" });
        }

        // Check for duplicate car plate
        var duplicateCarPlate = await _context.NewBookings
            .AnyAsync(n => n.CarPlate == carPlate && n.EndTime > DateTime.UtcNow.AddHours(0));

        if (duplicateCarPlate)
        {
            return Json(new { success = false, message = "This car plate is already in use for an active booking." });
        }

        var booking = new NewBooking
        {
            SlotId = slotId,
            UserEmail = userEmail,
            CarType = carType,
            CarPlate = carPlate,
            BookingTime = adjustedBookingStartTime.AddHours(2).ToUniversalTime(),
            EndTime = adjustedBookingEndTime.AddHours(2).ToUniversalTime()
        };
        _context.NewBookings.Add(booking);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Booking successful!" });
    }



    [HttpPost]
    public async Task<IActionResult> SignOutFromParking(int bookingId)
    {
        try
        {
            var booking = await _context.NewBookings.FindAsync(bookingId);
            if (booking == null)
            {
                return Json(new { success = false, message = "Booking not found." });
            }

            // Check if the booking has not started
            if (DateTime.UtcNow < booking.BookingTime.AddHours(-4))
            {
                return Json(new { isNotStarted = true });
            }

            // Calculate duration and cost
            var now = DateTime.UtcNow;
            var duration = now - booking.BookingTime;
            double standardCost = CalculateCost(duration, booking.UserEmail);

            // Check if the user is in overtime
            bool isOvertime = now > booking.EndTime.AddHours(-4);
            double overtimeCost = 0;
            if (isOvertime)
            {
                overtimeCost = CalculateCost(now - booking.EndTime, booking.UserEmail);
                standardCost += overtimeCost; // Total cost includes overtime
            }

            // Log calculated costs for debugging
            Console.WriteLine($"Standard Cost: {standardCost - overtimeCost}, Overtime Cost: {overtimeCost}, Total Cost: {standardCost}");

            // Generate Stripe payment URL
            var paymentUrl = GenerateStripePaymentUrl(bookingId, standardCost);
            return Json(new { isOvertime, overtimeCost, paymentUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
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
                        Name = "Parking Payment"
                    },
                    UnitAmount = (long)(cost * 100), // Ensure this matches the calculated cost
                    Currency = "zar"
                },
                Quantity = 1
            }
        },
            Mode = "payment",
            SuccessUrl = Url.Action("Success", "NewParking", new { bookingId }, Request.Scheme),
            CancelUrl = Url.Action("Cancel", "NewParking", null, Request.Scheme)
        };

        var service = new SessionService();
        var session = service.Create(sessionOptions);
        return session.Url;
    }

    public async Task<IActionResult> Success(int bookingId)
    {
        var booking = await _context.NewBookings.FindAsync(bookingId);
        Profile obj = new Profile();

        if (booking != null)
        {
            // Calculate duration and cost
            var now = DateTime.UtcNow;

            var duration = now - booking.BookingTime;
            double standardCost = CalculateCost(duration, booking.UserEmail);

            // Calculate overtime cost if applicable
            bool isOvertime = now > booking.EndTime.AddHours(-4);
            double overtimeCost = 0;
            if (isOvertime)
            {
                overtimeCost = CalculateCost(now - booking.EndTime, booking.UserEmail);
                standardCost += overtimeCost; // Total cost includes overtime
            }

            // Log calculated costs for debugging
            Console.WriteLine($"Success: Standard Cost: {standardCost - overtimeCost}, Overtime Cost: {overtimeCost}, Total Cost: {standardCost}");

            // Create transaction record including overtime
            var transaction = new TransactionHistory
            {
                BookingId = booking.BookingId,
                UserEmail = booking.UserEmail,
                StartTime = booking.BookingTime,
                EndTime = now,
                Username = obj.Username,
                Cost = standardCost,            // Total cost including overtime
                OvertimeCost = overtimeCost      // Store overtime cost separately
            };

            // Add transaction record to history
            _context.TransactionHistory.Add(transaction);

            // Remove the booking from NewBookings after payment is confirmed
            _context.NewBookings.Remove(booking);

            // Save changes to the database
            await _context.SaveChangesAsync();
        }

        return View(); // Optionally pass transaction details to the view
    }


    public async Task<IActionResult> PaymentHistory()
    {
        // Fetch TransactionHistory along with the associated Profile data
        var transactionHistory = await _context.TransactionHistory
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();

        // Create a list of ProfileTransactionViewModel
        var profileTransactionViewModels = new List<ProfileTransactionViewModel>();

        foreach (var transaction in transactionHistory)
        {
            // Find the profile associated with the transaction (based on email)
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Email == transaction.UserEmail);

            // Add to the view model list
            profileTransactionViewModels.Add(new ProfileTransactionViewModel
            {
                Profile = profile,
                TransactionHistory = transaction
            });
        }

        // Pass the view model list to the view
        return View(profileTransactionViewModels);
    }



    private double CalculateCost(TimeSpan duration, string email)
    {
        Profile obj = new Profile(email); // Pass the required email parameter
        NewBooking objcar = new NewBooking(); // Pass the required email parameter

        double rate = 0;
        if (obj.UserType == UserType.Student)
        {
            if (objcar.CarType == CarType.Sedan)
            {
                rate = 13.17;
            }
            else if (objcar.CarType == CarType.SUV)
            {
                rate = 14.17;
            }
            else if (objcar.CarType == CarType.Hatchback)
            {
                rate = 13.50;
            }
            else if (objcar.CarType == CarType.Coupe)
            {
                rate = 14.00;
            }
            else if (objcar.CarType == CarType.Convertible)
            {
                rate = 14.50;
            }
            else if (objcar.CarType == CarType.Wagon)
            {
                rate = 13.75;
            }
            else if (objcar.CarType == CarType.Van)
            {
                rate = 14.25;
            }
            else if (objcar.CarType == CarType.Truck)
            {
                rate = 15.00;
            }
        }
        else if (obj.UserType == UserType.Lecturer)
        {
            if (objcar.CarType == CarType.Sedan)
            {
                rate = 15.12;
            }
            else if (objcar.CarType == CarType.SUV)
            {
                rate = 16.12;
            }
            else if (objcar.CarType == CarType.Hatchback)
            {
                rate = 15.50;
            }
            else if (objcar.CarType == CarType.Coupe)
            {
                rate = 16.00;
            }
            else if (objcar.CarType == CarType.Convertible)
            {
                rate = 16.50;
            }
            else if (objcar.CarType == CarType.Wagon)
            {
                rate = 15.75;
            }
            else if (objcar.CarType == CarType.Van)
            {
                rate = 16.25;
            }
            else if (objcar.CarType == CarType.Truck)
            {
                rate = 17.00;
            }
        }
        else if (obj.UserType == UserType.General_User)
        {
            if (objcar.CarType == CarType.Sedan)
            {
                rate = 18.19;
            }
            else if (objcar.CarType == CarType.SUV)
            {
                rate = 19.19;
            }
            else if (objcar.CarType == CarType.Hatchback)
            {
                rate = 18.50;
            }
            else if (objcar.CarType == CarType.Coupe)
            {
                rate = 19.00;
            }
            else if (objcar.CarType == CarType.Convertible)
            {
                rate = 19.50;
            }
            else if (objcar.CarType == CarType.Wagon)
            {
                rate = 18.75;
            }
            else if (objcar.CarType == CarType.Van)
            {
                rate = 19.25;
            }
            else if (objcar.CarType == CarType.Truck)
            {
                rate = 20.00;
            }
        }

        double ratePerHour = rate; // Example rate in ZAR
        double ratePerMinute = ratePerHour / 60; // Calculate the rate per minute

        // Calculate total cost based on the total minutes
        double totalMinutes = duration.TotalMinutes;
        return Math.Ceiling(totalMinutes) * ratePerMinute; // Round up to the nearest minute
    }


    [HttpPost]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        try
        {
            var booking = await _context.NewBookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null)
            {
                return BadRequest("Booking not found.");
            }

            _context.NewBookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Json(new { message = "Booking cancelled successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
        }
    }


    public IActionResult Cancel(int bookingId)
    {
        return View();
    }
}



public class CarImageController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public CarImageController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Action to display the image upload form
    public IActionResult Upload()
    {
        return View();
    }

    // Action to handle image upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            string userEmail = User.FindFirstValue(ClaimTypes.Email); // Assuming you're using claims for user identification

            // Check if a car image already exists for the user
            var existingCarImage = await _context.CarImages.FirstOrDefaultAsync(c => c.UserEmail == userEmail);

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                if (existingCarImage != null)
                {
                    // Update the existing image
                    existingCarImage.ImageData = imageData;
                    existingCarImage.ImageName = file.FileName; // Update name if needed
                    _context.CarImages.Update(existingCarImage);
                }
                else
                {
                    // If no image exists, create a new record
                    var carImage = new CarImageModel
                    {
                        UserEmail = userEmail,
                        ImageName = file.FileName,
                        ImageData = imageData
                    };
                    _context.CarImages.Add(carImage);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Home", "Home"); // Adjust to your success page or redirect
        }

        return View(); // Return the same view in case of error
    }

    // Action to display the list of images
    public IActionResult ImageList()
    {
        var user = _userManager.GetUserAsync(User).Result;
        var images = _context.Images.Where(i => i.UserEmail == user.Email).ToList();
        return View(images);
    }
}
