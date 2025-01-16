// Models/Slot.cs
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using testsubject.Models;
using System.Linq;
using testsubject.Data;
using Microsoft.EntityFrameworkCore;

public class RitsonNewSlot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SlotId { get; set; }

    public bool IsTaken { get; set; } = false;
    public ICollection<RitsonNewBooking> Bookings { get; set; } = new List<RitsonNewBooking>();

    // Color property based on the IsTaken status
    public string Color => IsTaken ? "red" : "green";

    // New property to determine if the slot can be deleted
    public bool IsDeletable { get; set; } = true;
}

// Models/NewBooking.cs

public enum RitsonCarType
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


public class RitsonNewBooking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BookingId { get; set; }

    [Required]
    [StringLength(15, ErrorMessage = "Car Plate Cannot Be More Than 15 Characters!")]
    [UniqueCarPlate] // Custom validation attribute for uniqueness
    public string CarPlate { get; set; }

    [Required]
    public RitsonCarType RitsonCarType { get; set; }

    public string UserEmail { get; set; } // Foreign key to Profile (Email)

    public int SlotId { get; set; }
    public DateTime BookingTime { get; set; } = DateTime.UtcNow.AddHours(2);

    // Navigation property
    public RitsonNewSlot Slot { get; set; } // References the Slot

    [ForeignKey("UserEmail")]
    public Profile Profile { get; set; }

    // Encapsulated property for cost
    public double Cost { get; private set; }

    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(2); // New property for end time

    // Calculate duration based on booking start and end time
    public TimeSpan BookingDuration => EndTime - BookingTime;

    // Method to calculate cost based on duration
    public void CalculateCost(double ratePerHour)
    {
        double ratePerMinute = ratePerHour / 60; // Calculate rate per minute
        double totalMinutes = BookingDuration.TotalMinutes;
        Cost = Math.Ceiling(totalMinutes) * ratePerMinute; // Round up to the nearest minute
    }

    // Static method to check overlapping bookings
    public static bool IsOverlapping(DateTime startTime, DateTime endTime, List<RitsonNewBooking> existingBookings) =>
        existingBookings.Any(booking => startTime < booking.EndTime && endTime > booking.BookingTime);


}








// Models/BookingViewModel.cs

public class RitsonBookingViewModel
{
    public int BookingId { get; set; }
    public int SlotId { get; set; }
    public DateTime BookingTime { get; set; } = DateTime.UtcNow.AddHours(2);
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(2);
    public TimeSpan BookingDuration { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public string Status { get; set; }
    public double Cost { get; set; } // Added Cost property
    public RitsonCarType RitsonCarType { get; set; } // Added CarType property
    public string CarPlate { get; set; } // Added UserType property
    [Required]
    public byte[] CarPicture { get; set; } = new byte[0]; // Default initialization to an empty byte array.



    // Method to set the profile picture from an uploaded file
    public void SetCarPicture(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                CarPicture = memoryStream.ToArray();
            }
        }
    }

}



// Models/TransactionHistory.cs

public class RitsonTransactionHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TransactionId { get; set; }

    public int BookingId { get; set; }
    public string UserEmail { get; set; }
    public string Username { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow.AddHours(4);
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(4);
    public double Cost { get; set; } // Total cost including overtime
    public double OvertimeCost { get; set; } // Separate field for overtime cost
    // Optionally, navigation properties could be added if you want to relate back to bookings or profiles
    // public NewBooking Booking { get; set; }
    // public Profile User { get; set; }
}

public class RitsonProfileTransactionViewModel
{
    public Profile Profile { get; set; }
    public RitsonTransactionHistory RitsonTransactionHistory { get; set; }

}

public class RitsonCarImageBookingViewModel
{
    public RitsonBookingViewModel RitsonBookingViewModel { get; set; }
    public CarImageModel CarImageModel { get; set; }
}
