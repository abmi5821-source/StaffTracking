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
        int? my = User.IsInRole("RegionManager") ? int.Parse(User.FindFirstValue("RegionId")) : (int?)null;
        var regions = _db.Regions.Where(r => my == null || r.RegionId == my).OrderBy(r => r.RegionId).ToList();
        var branches = _db.Branches.Where(b => b.IsActive).ToList();
        var regionOfBranch = branches.ToDictionary(b => b.BranchId, b => b.RegionId);
        var regionOfEmp = _db.Employees.Where(e => e.IsActive).ToList()
            .Where(e => regionOfBranch.ContainsKey(e.BranchId)).ToDictionary(e => e.EmployeeId, e => regionOfBranch[e.BranchId]);
        var yStart = new DateTime(DateTime.Today.Year, 1, 1);
        var leaves = _db.Leaves.Where(l => l.StartDate >= yStart).Select(l => l.EmployeeId).ToList();
        var disc = _db.Disciplinary.Select(d => new { d.EmployeeId, d.Kind }).ToList();
        var crs = _db.Courses.Select(c => new { c.EmployeeId, c.Scope }).ToList();

        var rows = regions.Select(r => new RegionStat
        {
            Name = r.Name,
            Branches = branches.Count(b => b.RegionId == r.RegionId),
            Employees = regionOfEmp.Count(x => x.Value == r.RegionId),
            LeavesYear = leaves.Count(e => regionOfEmp.GetValueOrDefault(e) == r.RegionId),
            Warnings = disc.Count(d => d.Kind == "Warning" && regionOfEmp.GetValueOrDefault(d.EmployeeId) == r.RegionId),
            Penalties = disc.Count(d => d.Kind == "Penalty" && regionOfEmp.GetValueOrDefault(d.EmployeeId) == r.RegionId),
            LocalCourses = crs.Count(c => c.Scope == "Local" && regionOfEmp.GetValueOrDefault(c.EmployeeId) == r.RegionId),
            IntlCourses = crs.Count(c => c.Scope == "International" && regionOfEmp.GetValueOrDefault(c.EmployeeId) == r.RegionId)
        }).ToList();
        return View(rows);
    }
}
