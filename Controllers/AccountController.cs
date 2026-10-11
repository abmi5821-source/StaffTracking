using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StaffTracking.Data;
using StaffTracking.Models;

namespace StaffTracking.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) { _db = db; }

    [HttpGet] public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string userName, string password)
    {
        var u = _db.AppUsers.FirstOrDefault(x => x.UserName == userName && x.IsActive);
        if (u != null && !string.IsNullOrEmpty(password) &&
            new PasswordHasher<AppUser>().VerifyHashedPassword(u, u.PasswordHash, password) != PasswordVerificationResult.Failed)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, u.UserId.ToString()),
                new(ClaimTypes.Name, u.FullName),
                new(ClaimTypes.Role, u.Role),
                new("RegionId", u.RegionId?.ToString() ?? "")
            };
            await HttpContext.SignInAsync(new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return RedirectToAction("Index", "Home");
        }
        ViewBag.Error = "اسم المستخدم أو كلمة السر غير صحيحة";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }
}
