using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class InitializationController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public InitializationController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> SeedData()
    {
        await InitializeData();
        return Content("Data has been initialized.");
    }

    private async Task InitializeData()
    {
        var adminRole = "Admin";
        if (!await _roleManager.RoleExistsAsync(adminRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        var adminEmail = "admin@gmail.com";
        var adminPassword = "Admin@123";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
            await _userManager.CreateAsync(adminUser, adminPassword);
        }

        if (!await _userManager.IsInRoleAsync(adminUser, adminRole))
        {
            await _userManager.AddToRoleAsync(adminUser, adminRole);
        }
    }
}
