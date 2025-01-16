using System;
using System.ComponentModel.DataAnnotations;
using testsubject.Models;

namespace testsubject
{
    public class CarWashBooking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string UserEmail { get; set; }

        public string ServiceName { get; set; } // Name of the service

        [Required]
        public string CarModel { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime BookingTime { get; set; }

        // Navigation property

        public Campus Campus { get; set; } // Campus for the booking

        // New properties
        public string CarPlate { get; set; } // Car plate from parking booking
        public CarType CarType { get; set; } // E.g., Sedan, SUV, Truck


        public decimal CalculateCost()
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

            return costByCarType.TryGetValue(CarType, out var cost) ? cost : 0;
        }

    }
    
    
    
  
    public enum CarType
    {
        Sedan,
        SUV,
        Hatchback,
        Coupe,
        Convertible,
        Wagon,
        Van,
        Truck
    }
    public enum Campus
    {
        Steve,
        Ml,
        Ritson,
        City
    }

    public enum Status
    {
        Available, Not_Available
    }
    public class CarWashService
    {
        [Key]
        public int Id { get; set; } // Primary Key

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        public string Username { get;set; } 

       [StringLength(50, ErrorMessage = "City cannot be longer than 50 characters.")]
        public string City { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "State/Province cannot be longer than 50 characters.")]
        public string State { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "Postal code cannot be longer than 10 characters.")]
        public string PostalCode { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Country cannot be longer than 50 characters.")]
        public string Country { get; set; } = string.Empty;

     
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }


        [Required]
        public string ServiceName { get; set; } // Name of the service

        [Required]
        public string Description { get; set; } // Brief description of the service

        // New fields related to service status and submission forms
        [Required]
        public DateTime? SubmissionDate { get; set; } // Date of service submission


        public Status Status { get; set; } = Status.Available;  // Status of the service (Available, Not Available, or Free)

        // Link to the Profile table        
        [Required]
        [EmailAddress]
        public string StudentEmail { get; set; } // Email of the student (from Profile)

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } // Student's phone number from Profile (if needed)

        [Required]
        public string Address { get; set; } // Address from the Profile table


        public RegAccept RegAccept { get; set; } = RegAccept.Awaiting;

    }

   public enum RegAccept
    {
        Awaiting,Accept,Rejected
    }

    public class CarWashServiceProfile
    {
    public CarWashService CarWashService { get; set; }
        public Profile Profile { get; set; }

   
    }



    public class Credit
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; } // Email for identifying the user

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Available credit must be a positive number.")]
        public decimal AvailableCredit { get; set; } // Total credit available

        [Required]
        [StringLength(16, MinimumLength = 16)]
        public string AccountNumber { get; set; } // Unique 16-digit account number
    }


    public class Transaction
    {
        public int TransactionId { get; set; }
        public string UserEmail { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
    }


}