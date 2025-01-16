using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using testsubject.Models;

namespace testsubject.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<CarWashBooking> CarWashBookings { get; set; }
        public DbSet<CarWashService> CarWashServices { get; set; }
        public DbSet<Transaction> Transactions { get; set; }


        public DbSet<Credit> Credits { get; set; }



        public DbSet<ImageModel> Images { get; set; }
        public DbSet<Profile> Profiles { get; set; }
 


        public DbSet<CarImageModel> CarImages { get; set; }
        public DbSet<NewSlot> Slots { get; set; }
        public DbSet<NewBooking> NewBookings { get; set; }
        public DbSet<TransactionHistory> TransactionHistory { get; set; }


        public DbSet<SteveNewSlot> SteveNewSlots { get; set; }
        public DbSet<SteveNewBooking> SteveNewBookings { get; set; }
        public DbSet<SteveTransactionHistory> SteveTransactionHistorys { get; set; }

        public DbSet<CityNewSlot> CityNewSlots { get; set; }
        public DbSet<CityNewBooking> CityNewBookings { get; set; }
        public DbSet<CityTransactionHistory> CityTransactionHistorys { get; set; }

        public DbSet<RitsonNewSlot> RitsonNewSlots { get; set; }
        public DbSet<RitsonNewBooking> RitsonNewBookings { get; set; }
        public DbSet<RitsonTransactionHistory> RitsonTransactionHistorys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        
        }



    }

}


