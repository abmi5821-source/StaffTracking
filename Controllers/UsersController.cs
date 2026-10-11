using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StaffTracking.Data;
using StaffTracking.Models;

namespace StaffTracking.Controllers;

[Authorize(Roles = "DeptManager")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) { _db = db; }
    private void Fill() => ViewBag.Regions = _db.Regions.OrderBy(r => r.RegionId).ToList();

    public IActionResult Index()
    {
        ViewBag.Regions = _db.Regions.ToDictionary(r => r.RegionId, r => r.Name);
        return View(_db.AppUsers.OrderBy(u => u.Role).ThenBy(u => u.UserName).ToList());
    }

    public IActionResult Create() { Fill(); return View("Form", new AppUser { IsActive = true, Role = "RegionManager" }); }

    [HttpPost]
    public IActionResult Create(AppUser m, string password)
    {
        if (string.IsNullOrWhiteSpace(m.UserName)) ModelState.AddModelError("", "اسم المستخدم مطلوب");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) ModelState.AddModelError("", "كلمة السر يجب ألا تقل عن 6 أحرف");
        if (_db.AppUsers.Any(u => u.UserName == m.UserName)) ModelState.AddModelError("", "اسم المستخدم مستخدم مسبقًا");
        if (m.Role == "RegionManager" && m.RegionId == null) ModelState.AddModelError("", "اختر المندوبية لمدير المندوبية");
        if (!ModelState.IsValid) { Fill(); return View("Form", m); }
        if (m.Role == "DeptManager") m.RegionId = null;
        m.UserId = 0;
        m.PasswordHash = new PasswordHasher<AppUser>().HashPassword(m, password);
        _db.AppUsers.Add(m); _db.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var u = _db.AppUsers.Find(id); if (u == null) return NotFound();
        Fill(); return View("Form", u);
    }

    [HttpPost]
    public IActionResult Edit(int id, AppUser m, string password)
    {
        var u = _db.AppUsers.Find(id); if (u == null) return NotFound();
        if (string.IsNullOrWhiteSpace(m.UserName)) ModelState.AddModelError("", "اسم المستخدم مطلوب");
        if (_db.AppUsers.Any(x => x.UserName == m.UserName && x.UserId != id)) ModelState.AddModelError("", "اسم المستخدم مستخدم مسبقًا");
        if (m.Role == "RegionManager" && m.RegionId == null) ModelState.AddModelError("", "اختر المندوبية لمدير المندوبية");
        if (!string.IsNullOrWhiteSpace(password) && password.Length < 6) ModelState.AddModelError("", "كلمة السر يجب ألا تقل عن 6 أحرف");
        if (id.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier) && (!m.IsActive || m.Role != "DeptManager"))
            ModelState.AddModelError("", "لا يمكنك إيقاف حسابك أو تغيير صلاحيتك");
        if (!ModelState.IsValid) { Fill(); m.UserId = id; return View("Form", m); }
        u.UserName = m.UserName; u.FullName = m.FullName; u.Role = m.Role; u.IsActive = m.IsActive;
        u.RegionId = m.Role == "DeptManager" ? null : m.RegionId;
        if (!string.IsNullOrWhiteSpace(password)) u.PasswordHash = new PasswordHasher<AppUser>().HashPassword(u, password);
        _db.SaveChanges();
        return RedirectToAction("Index");
    }
}
