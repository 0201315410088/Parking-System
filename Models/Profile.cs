using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace testsubject.Models
{
    public class Profile
    {
        [Key]
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters.")]
        public string Username { get; private set; } = "User";

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "City cannot be longer than 50 characters.")]
        public string City { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "State/Province cannot be longer than 50 characters.")]
        public string State { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "Postal code cannot be longer than 10 characters.")]
        public string PostalCode { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Country cannot be longer than 50 characters.")]
        public string Country { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters.")]
        public string Address { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [MinimumAge(16, ErrorMessage = "Date of birth must be at least 16 years ago.")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(200, ErrorMessage = "Biography cannot be longer than 200 characters.")]
        public string Biography { get; set; } = string.Empty;


        [Required]
        public byte[] ProfilePicture { get; set; } = new byte[0]; // Default initialization to an empty byte array.

        public ICollection<NewBooking> Bookings { get; set; } = new List<NewBooking>();

        public UserType UserType { get; private set; } // Use a private setter

        public Profile()
        {
        }
        // Constructor accepting email
        public Profile(string email)
        {
            Email = email;
            Username = ExtractUsernameFromEmail(email); // Assign the result to Username
            UserType = GetUserType(Email); // Set the user type based on the email
        }

        private string ExtractUsernameFromEmail(string email)
        {
            if (!string.IsNullOrEmpty(email) && email.Contains('@'))
            {
                return email.Split('@')[0]; // Extract everything before the '@'
            }
            return string.Empty;
        }

        private UserType GetUserType(string email)
        {
            // Determine the user type based on the email domain
            if (!string.IsNullOrEmpty(email))
            {
                if (email.EndsWith("@dut4life.ac.za", StringComparison.OrdinalIgnoreCase))
                {
                    return UserType.Student;
                }
                else if (email.EndsWith("@dut.ac.za", StringComparison.OrdinalIgnoreCase))
                {
                    return UserType.Lecturer;
                }
            }

            return UserType.General_User;
        }

        // Method to set the profile picture from an uploaded file
        public void SetProfilePicture(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    ProfilePicture = memoryStream.ToArray();
                }
            }
        }

    }
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;

                // Adjust age if the birthday has not occurred yet this year
                if (dateOfBirth > today.AddYears(-age)) age--;

                if (age < _minimumAge)
                {
                    return new ValidationResult($"You must be at least {_minimumAge} years old.");
                }
            }

            return ValidationResult.Success;
        }
    }
    public enum UserType
    {
        Student,
        Lecturer,
        General_User
    }

    public class ImageModel
    {
        public int Id { get; set; }

        [Required]
        public string ImageName { get; set; }

        [Required]
        public byte[] ImageData { get; set; }

        [Required]
        public string UserEmail { get; set; } // This links to the Profile's Email
    }
}




