using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using testsubject.Data;


    public class NewSlotsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewSlotsController(ApplicationDbContext context)
        {
            _context = context;
        }

    // GET: NewSlots
    public async Task<IActionResult> Index()
    {
        var slots = await _context.Slots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .ToListAsync();

        // Adjust the logic to determine if the slot is deletable based on bookings
        foreach (var slot in slots)
        {
            var isSlotBooked = await _context.NewBookings
                .AnyAsync(b => b.SlotId == slot.SlotId &&
                               b.EndTime > DateTime.UtcNow); // Check for any active or future bookings

            // If there are active bookings, set a flag in the model
            slot.IsDeletable = !isSlotBooked;
        }

        return View(slots);
    }


    // GET: NewSlots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Include bookings to get detailed information
        var newSlot = await _context.Slots
            .Include(s => s.Bookings) // Include bookings to show details
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // GET: NewSlots/Create
    public IActionResult Create()
        {
            return View();
        }

    // POST: NewSlots/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int numberOfSlots)
    {
        if (numberOfSlots < 1)
        {
            return BadRequest("Invalid number of slots.");
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            var newSlot = new NewSlot
            {
                IsTaken = false // Default to not taken
            };

            _context.Slots.Add(newSlot);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }



    // GET: NewSlots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var newSlot = await _context.Slots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // POST: NewSlots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SlotId,IsTaken")] NewSlot newSlot)
    {
        if (id != newSlot.SlotId)
        {
            return NotFound();
        }

        // Check if the slot has bookings when trying to change its status
        var existingSlot = await _context.Slots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (existingSlot == null)
        {
            return NotFound();
        }

        // Prevent marking a slot as taken if it has active bookings
        if (newSlot.IsTaken && existingSlot.Bookings.Any(b => b.EndTime > DateTime.UtcNow))
        {
            ModelState.AddModelError("IsTaken", "Cannot mark this slot as taken because it has active bookings.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(newSlot);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NewSlotExists(newSlot.SlotId))
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

        return View(newSlot);
    }


    // GET: NewSlots/Delete/5
    public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newSlot = await _context.Slots
                .FirstOrDefaultAsync(m => m.SlotId == id);
            if (newSlot == null)
            {
                return NotFound();
            }

            return View(newSlot);
        }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var slot = await _context.Slots
            .Include(s => s.Bookings) // Include bookings for this slot
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (slot == null)
        {
            return NotFound();
        }

        // Check if the slot has any active or future bookings
        if (slot.Bookings != null && slot.Bookings.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete this slot because it has active or future bookings.";
            return RedirectToAction(nameof(Index));
        }

        _context.Slots.Remove(slot);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Slot deleted successfully.";
        return RedirectToAction(nameof(Index));
    }


    private bool NewSlotExists(int id)
        {
            return _context.Slots.Any(e => e.SlotId == id);
        }
    }

public class SteveNewSlotsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SteveNewSlotsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: NewSlots
    public async Task<IActionResult> Index()
    {
        var slots = await _context.SteveNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .ToListAsync();

        // Adjust the logic to determine if the slot is deletable based on bookings
        foreach (var slot in slots)
        {
            var isSlotBooked = await _context.SteveNewBookings
                .AnyAsync(b => b.SlotId == slot.SlotId &&
                               b.EndTime > DateTime.UtcNow); // Check for any active or future bookings

            // If there are active bookings, set a flag in the model
            slot.IsDeletable = !isSlotBooked;
        }

        return View(slots);
    }


    // GET: NewSlots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Include bookings to get detailed information
        var newSlot = await _context.SteveNewSlots
            .Include(s => s.Bookings) // Include bookings to show details
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // GET: NewSlots/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: NewSlots/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int numberOfSlots)
    {
        if (numberOfSlots < 1)
        {
            return BadRequest("Invalid number of slots.");
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            var newSlot = new SteveNewSlot
            {
                IsTaken = false // Default to not taken
            };

            _context.SteveNewSlots.Add(newSlot);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }



    // GET: NewSlots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var newSlot = await _context.SteveNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // POST: NewSlots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SlotId,IsTaken")] SteveNewSlot newSlot)
    {
        if (id != newSlot.SlotId)
        {
            return NotFound();
        }

        // Check if the slot has bookings when trying to change its status
        var existingSlot = await _context.SteveNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (existingSlot == null)
        {
            return NotFound();
        }

        // Prevent marking a slot as taken if it has active bookings
        if (newSlot.IsTaken && existingSlot.Bookings.Any(b => b.EndTime > DateTime.UtcNow))
        {
            ModelState.AddModelError("IsTaken", "Cannot mark this slot as taken because it has active bookings.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(newSlot);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NewSlotExists(newSlot.SlotId))
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

        return View(newSlot);
    }


    // GET: NewSlots/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var newSlot = await _context.SteveNewSlots
            .FirstOrDefaultAsync(m => m.SlotId == id);
        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var slot = await _context.SteveNewSlots
            .Include(s => s.Bookings) // Include bookings for this slot
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (slot == null)
        {
            return NotFound();
        }

        // Check if the slot has any active or future bookings
        if (slot.Bookings != null && slot.Bookings.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete this slot because it has active or future bookings.";
            return RedirectToAction(nameof(Index));
        }

        _context.SteveNewSlots.Remove(slot);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Slot deleted successfully.";
        return RedirectToAction(nameof(Index));
    }


    private bool NewSlotExists(int id)
    {
        return _context.SteveNewSlots.Any(e => e.SlotId == id);
    }
}


public class RitsonNewSlotsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RitsonNewSlotsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: NewSlots
    public async Task<IActionResult> Index()
    {
        var slots = await _context.RitsonNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .ToListAsync();

        // Adjust the logic to determine if the slot is deletable based on bookings
        foreach (var slot in slots)
        {
            var isSlotBooked = await _context.RitsonNewBookings
                .AnyAsync(b => b.SlotId == slot.SlotId &&
                               b.EndTime > DateTime.UtcNow); // Check for any active or future bookings

            // If there are active bookings, set a flag in the model
            slot.IsDeletable = !isSlotBooked;
        }

        return View(slots);
    }


    // GET: NewSlots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Include bookings to get detailed information
        var newSlot = await _context.RitsonNewSlots
            .Include(s => s.Bookings) // Include bookings to show details
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // GET: NewSlots/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: NewSlots/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int numberOfSlots)
    {
        if (numberOfSlots < 1)
        {
            return BadRequest("Invalid number of slots.");
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            var newSlot = new RitsonNewSlot
            {
                IsTaken = false // Default to not taken
            };

            _context.RitsonNewSlots.Add(newSlot);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }



    // GET: NewSlots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var newSlot = await _context.RitsonNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // POST: NewSlots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SlotId,IsTaken")] SteveNewSlot newSlot)
    {
        if (id != newSlot.SlotId)
        {
            return NotFound();
        }

        // Check if the slot has bookings when trying to change its status
        var existingSlot = await _context.RitsonNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (existingSlot == null)
        {
            return NotFound();
        }

        // Prevent marking a slot as taken if it has active bookings
        if (newSlot.IsTaken && existingSlot.Bookings.Any(b => b.EndTime > DateTime.UtcNow))
        {
            ModelState.AddModelError("IsTaken", "Cannot mark this slot as taken because it has active bookings.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(newSlot);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NewSlotExists(newSlot.SlotId))
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

        return View(newSlot);
    }


    // GET: NewSlots/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var newSlot = await _context.RitsonNewSlots
            .FirstOrDefaultAsync(m => m.SlotId == id);
        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var slot = await _context.RitsonNewSlots
            .Include(s => s.Bookings) // Include bookings for this slot
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (slot == null)
        {
            return NotFound();
        }

        // Check if the slot has any active or future bookings
        if (slot.Bookings != null && slot.Bookings.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete this slot because it has active or future bookings.";
            return RedirectToAction(nameof(Index));
        }

        _context.RitsonNewSlots.Remove(slot);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Slot deleted successfully.";
        return RedirectToAction(nameof(Index));
    }


    private bool NewSlotExists(int id)
    {
        return _context.RitsonNewSlots.Any(e => e.SlotId == id);
    }
}

public class CityNewSlotsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CityNewSlotsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: NewSlots
    public async Task<IActionResult> Index()
    {
        var slots = await _context.CityNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .ToListAsync();

        // Adjust the logic to determine if the slot is deletable based on bookings
        foreach (var slot in slots)
        {
            var isSlotBooked = await _context.CityNewBookings
                .AnyAsync(b => b.SlotId == slot.SlotId &&
                               b.EndTime > DateTime.UtcNow); // Check for any active or future bookings

            // If there are active bookings, set a flag in the model
            slot.IsDeletable = !isSlotBooked;
        }

        return View(slots);
    }


    // GET: NewSlots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Include bookings to get detailed information
        var newSlot = await _context.CityNewSlots
            .Include(s => s.Bookings) // Include bookings to show details
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // GET: NewSlots/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: NewSlots/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int numberOfSlots)
    {
        if (numberOfSlots < 1)
        {
            return BadRequest("Invalid number of slots.");
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            var newSlot = new CityNewSlot
            {
                IsTaken = false // Default to not taken
            };

            _context.CityNewSlots.Add(newSlot);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }



    // GET: NewSlots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var newSlot = await _context.CityNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    // POST: NewSlots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SlotId,IsTaken")] SteveNewSlot newSlot)
    {
        if (id != newSlot.SlotId)
        {
            return NotFound();
        }

        // Check if the slot has bookings when trying to change its status
        var existingSlot = await _context.CityNewSlots
            .Include(s => s.Bookings) // Include bookings to check if any exist
            .FirstOrDefaultAsync(s => s.SlotId == id);

        if (existingSlot == null)
        {
            return NotFound();
        }

        // Prevent marking a slot as taken if it has active bookings
        if (newSlot.IsTaken && existingSlot.Bookings.Any(b => b.EndTime > DateTime.UtcNow))
        {
            ModelState.AddModelError("IsTaken", "Cannot mark this slot as taken because it has active bookings.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(newSlot);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NewSlotExists(newSlot.SlotId))
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

        return View(newSlot);
    }


    // GET: NewSlots/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var newSlot = await _context.CityNewSlots
            .FirstOrDefaultAsync(m => m.SlotId == id);
        if (newSlot == null)
        {
            return NotFound();
        }

        return View(newSlot);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var slot = await _context.CityNewSlots
            .Include(s => s.Bookings) // Include bookings for this slot
            .FirstOrDefaultAsync(m => m.SlotId == id);

        if (slot == null)
        {
            return NotFound();
        }

        // Check if the slot has any active or future bookings
        if (slot.Bookings != null && slot.Bookings.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete this slot because it has active or future bookings.";
            return RedirectToAction(nameof(Index));
        }

        _context.CityNewSlots.Remove(slot);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Slot deleted successfully.";
        return RedirectToAction(nameof(Index));
    }


    private bool NewSlotExists(int id)
    {
        return _context.CityNewSlots.Any(e => e.SlotId == id);
    }
}
