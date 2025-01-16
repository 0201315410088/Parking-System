using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Linq;
using System.Threading.Tasks;
using testsubject.Data;
using testsubject.Models;

namespace testsubject.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> ViewProfile()
        {
            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get the user's profile details from the Profile table
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.Email == user.Email);
            if (userProfile == null)
            {
                return NotFound();
            }

            // Get the user's profile picture from the ImageModel table (if it exists)
            var profileImage = await _context.Images.FirstOrDefaultAsync(img => img.UserEmail == user.Email);

            // Get the user's credit balance from the Credit table
            var userCredit = await _context.Credits.FirstOrDefaultAsync(c => c.UserEmail == user.Email);
            decimal availableCredit = userCredit?.AvailableCredit ?? 0; // Default to 0 if no credit exists
            string accountNumber = userCredit?.AccountNumber; // Get account number

            // Prepare the view model with both profile and image data
            var model = new Profile(user.Email) // Pass the email to the Profile constructor
            {
                FirstName = userProfile.FirstName,
                LastName = userProfile.LastName,
                PhoneNumber = userProfile.PhoneNumber,
                Address = userProfile.Address,
                City = userProfile.City,
                State = userProfile.State,
                PostalCode = userProfile.PostalCode,
                Country = userProfile.Country,
                DateOfBirth = userProfile.DateOfBirth,
                Biography = userProfile.Biography,
                ProfilePicture = profileImage?.ImageData // Profile image can be null
            };

            // Pass the available credit and credit existence status to the view via ViewBag
            ViewBag.AvailableCredit = availableCredit;
            ViewBag.HasCreditAccount = userCredit != null;
            ViewBag.AccountNumber = accountNumber; // Correctly assign account number to ViewBag

            return View(model);
        }
    }





    public class ProfileCreateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileCreateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action to display the profile creation form
        // GET: Create profile
        public IActionResult Create()
        {
            // Get the logged-in user's email
            var userEmail = User.Identity.Name; // Assuming Identity is set up and email is the username

            // Pass the email to the view using the model
            var profile = new Profile
            {
                Email = userEmail // Pre-fill the email field with the logged-in user's email
            };

            return View(profile);
        }

        // Action to handle profile creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Profile profile)
        {
            if (ModelState.IsValid)
            {
                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();
                return RedirectToAction("Upload", "Image"); // Redirect after successful profile creation
            }

            return View(profile); // If invalid, return the same view with validation messages
        }


        // Action to display the list of profiles
        public IActionResult ProfileList()
        {
            var profiles = _context.Profiles.ToList();
            return View(profiles);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var profile = await _context.Profiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }
            return View(profile); // Returns the edit view with the profile model
        }

        // Action to handle profile edits (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Profile profile)
        {
            if (ModelState.IsValid)
            {
                _context.Update(profile); // Update the profile in the context
                await _context.SaveChangesAsync(); // Save changes to the database
                return RedirectToAction("ViewProfile","Profile"); // Redirect after successful update
            }
            return View(profile); // Return to the edit view with validation errors
        }
        // Action to delete a profile
        public async Task<IActionResult> Delete(int id)
        {
            var profile = await _context.Profiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }
            return View(profile);
        }

        // Action to confirm deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.Profiles.FindAsync(id);
            if (profile != null)
            {
                _context.Profiles.Remove(profile);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ProfileList));
        }
    }




    public class ImageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ImageController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please select an image to upload.");
                return View();
            }

            // Validate file type (optional)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("File", "Invalid file type. Only JPG, PNG, and GIF files are allowed.");
                return View();
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var image = new ImageModel
                    {
                        ImageName = file.FileName,
                        ImageData = memoryStream.ToArray(),
                        UserEmail = user.Email
                    };
                    _context.Images.Add(image);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("ViewProfile", "Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while uploading the image: {ex.Message}");
                return View();
            }
        }

        // Action to display the list of images
        public IActionResult ImageList()
        {
            var user = _userManager.GetUserAsync(User).Result;
            var images = _context.Images.Where(i => i.UserEmail == user.Email).ToList();
            return View(images);
        }





        // Action to display the withdrawal form
        public IActionResult Withdraw()
        {
            // Pass available credit to the view via ViewBag for display
            var credit = _context.Credits.FirstOrDefault(c => c.UserEmail == User.Identity.Name);
            ViewBag.AvailableCredit = credit?.AvailableCredit ?? 0;
            return View();
        }

        // POST: Credits/Withdraw
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(decimal amount)
        {
            var credit = await _context.Credits.FirstOrDefaultAsync(c => c.UserEmail == User.Identity.Name);

            if (credit == null || credit.AvailableCredit < amount)
            {
                ModelState.AddModelError("", "Insufficient credits available for withdrawal.");
                return View();
            }

            try
            {
                // Initiate Stripe transfer to the user's external account
                var payoutService = new PayoutService();
                var payoutOptions = new PayoutCreateOptions
                {
                    Amount = (long)(amount * 100), // Amount in cents
                    Currency = "zar",
                    Method = "instant" // or "standard"
                };

                var payout = payoutService.Create(payoutOptions);

                // Deduct the withdrawn amount from available credit
                credit.AvailableCredit -= amount;
                _context.Update(credit);
                await _context.SaveChangesAsync();

                return RedirectToAction("Success"); // Display success page
            }
            catch (StripeException ex)
            {
                // Handle Stripe errors
                ModelState.AddModelError("", $"Error processing withdrawal: {ex.Message}");
                return View();
            }
        }

    }
}


