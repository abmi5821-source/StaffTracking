using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaffTracking.Data;
using StaffTracking.Models;

namespace StaffTracking.Controllers;

[Authorize]
public class BranchesController : Controller
{
    private readonly AppDbContext _db;
    public BranchesController(AppDbContext db) { _db = db; }

    // مدير المندوبية مقيّد بمندوبيته
    private int? MyRegion => User.IsInRole("RegionManager") ? int.Parse(User.FindFirstValue("RegionId")) : (int?)null;
    private void Fill() => ViewBag.Regions = _db.Regions.Where(r => MyRegion == null || r.RegionId == MyRegion).OrderBy(r => r.RegionId).ToList();

    public IActionResult Index(int? regionId)
    {
        Fill();
        var q = _db.Branches.AsQueryable();
        if (MyRegion != null) q = q.Where(b => b.RegionId == MyRegion);
        else if (regionId != null) q = q.Where(b => b.RegionId == regionId);
        ViewBag.Selected = regionId;
        ViewBag.Names = _db.Regions.ToDictionary(r => r.RegionId, r => r.Name);
        return View(q.OrderBy(b => b.RegionId).ThenBy(b => b.Name).ToList());
    }

    public IActionResult Create() { Fill(); return View("Form", new Branch { IsActive = true, RegionId = MyRegion ?? 0 }); }

    [HttpPost]
    public IActionResult Create(Branch m)
    {
        if (MyRegion != null) m.RegionId = MyRegion.Value;
        if (string.IsNullOrWhiteSpace(m.Name)) ModelState.AddModelError("", "اسم الفرع مطلوب");
        if (!_db.Regions.Any(r => r.RegionId == m.RegionId)) ModelState.AddModelError("", "اختر المندوبية");
        if (!ModelState.IsValid) { Fill(); return View("Form", m); }
        m.BranchId = 0; _db.Branches.Add(m); _db.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var b = _db.Branches.Find(id);
        if (b == null || (MyRegion != null && b.RegionId != MyRegion)) return NotFound();
        Fill(); return View("Form", b);
    }

    [HttpPost]
    public IActionResult Edit(int id, Branch m)
    {
        var b = _db.Branches.Find(id);
        if (b == null || (MyRegion != null && b.RegionId != MyRegion)) return NotFound();
        if (string.IsNullOrWhiteSpace(m.Name)) ModelState.AddModelError("", "اسم الفرع مطلوب");
        if (!ModelState.IsValid) { Fill(); m.BranchId = id; return View("Form", m); }
        b.Name = m.Name; b.Code = m.Code; b.IsActive = m.IsActive;
        if (MyRegion == null) b.RegionId = m.RegionId;
        _db.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Bulk() { Fill(); return View(); }

    // إضافة عدة فروع دفعة واحدة: اسم فرع في كل سطر
    [HttpPost]
    public IActionResult Bulk(int regionId, string names)
    {
        if (MyRegion != null) regionId = MyRegion.Value;
        var existing = _db.Branches.Where(b => b.RegionId == regionId).Select(b => b.Name).ToHashSet();
        var list = (names ?? "").Split('\n').Select(x => x.Trim()).Where(x => x != "").Distinct().Where(x => !existing.Contains(x));
        foreach (var n in list) _db.Branches.Add(new Branch { RegionId = regionId, Name = n, IsActive = true });
        _db.SaveChanges();
        return RedirectToAction("Index", new { regionId });
    }
}
