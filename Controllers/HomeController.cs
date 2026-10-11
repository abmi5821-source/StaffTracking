using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaffTracking.Data;
using StaffTracking.Models;

namespace StaffTracking.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) { _db = db; }

    public IActionResult Index()
    {
        var q = _db.Regions.AsQueryable();
        if (User.IsInRole("RegionManager"))
        {
            var rid = int.Parse(User.FindFirstValue("RegionId"));
            q = q.Where(r => r.RegionId == rid);   // مدير المندوبية يرى مندوبيته فقط
        }
        var rows = q.Select(r => new RegionStat
        {
            Name = r.Name,
            Branches = _db.Branches.Count(b => b.RegionId == r.RegionId && b.IsActive),
            Employees = _db.Employees.Count(e => e.IsActive &&
                _db.Branches.Any(b => b.BranchId == e.BranchId && b.RegionId == r.RegionId))
        }).ToList();
        return View(rows);
    }
}
