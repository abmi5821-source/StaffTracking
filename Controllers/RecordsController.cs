using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaffTracking.Data;
using StaffTracking.Models;

namespace StaffTracking.Controllers;

[Authorize]
public class RecordsController : Controller
{
    private readonly AppDbContext _db;
    public RecordsController(AppDbContext db) { _db = db; }
    private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    private bool Can(int empId)
    {
        var e = _db.Employees.Find(empId); if (e == null) return false;
        int? my = User.IsInRole("RegionManager") ? int.Parse(User.FindFirstValue("RegionId")) : (int?)null;
        return my == null || _db.Branches.Any(b => b.BranchId == e.BranchId && b.RegionId == my);
    }
    private IActionResult Back(int id) => RedirectToAction("Details", "Employees", new { id });

    [HttpPost]
    public IActionResult AddLeave(int employeeId, DateTime startDate, DateTime endDate, string leaveType, string notes)
    {
        if (!Can(employeeId)) return NotFound();
        if (endDate < startDate) { TempData["Err"] = "تاريخ نهاية الإجازة قبل بدايتها"; return Back(employeeId); }
        _db.Leaves.Add(new Leave { EmployeeId = employeeId, StartDate = startDate, EndDate = endDate, LeaveType = leaveType, Notes = notes, EnteredBy = Me, CreatedAt = DateTime.Now });
        _db.SaveChanges(); return Back(employeeId);
    }

    [HttpPost]
    public IActionResult AddDisc(int employeeId, string kind, DateTime issueDate, string reason, string bookNumber)
    {
        if (!Can(employeeId)) return NotFound();
        if (string.IsNullOrWhiteSpace(reason) || (kind != "Warning" && kind != "Penalty")) { TempData["Err"] = "أدخل النوع والسبب"; return Back(employeeId); }
        _db.Disciplinary.Add(new Disciplinary { EmployeeId = employeeId, Kind = kind, IssueDate = issueDate, Reason = reason, BookNumber = bookNumber, EnteredBy = Me, CreatedAt = DateTime.Now });
        _db.SaveChanges(); return Back(employeeId);
    }

    [HttpPost]
    public IActionResult AddCourse(int employeeId, string title, string scope, string country, DateTime? startDate, DateTime? endDate)
    {
        if (!Can(employeeId)) return NotFound();
        if (string.IsNullOrWhiteSpace(title) || (scope != "Local" && scope != "International")) { TempData["Err"] = "أدخل اسم الدورة ونوعها"; return Back(employeeId); }
        _db.Courses.Add(new Course { EmployeeId = employeeId, Title = title, Scope = scope, Country = country, StartDate = startDate, EndDate = endDate, EnteredBy = Me, CreatedAt = DateTime.Now });
        _db.SaveChanges(); return Back(employeeId);
    }

    [HttpPost]
    public IActionResult Delete(string kind, int id)
    {
        int empId; 
        if (kind == "leave") { var x = _db.Leaves.Find(id); if (x == null || !Can(x.EmployeeId)) return NotFound(); empId = x.EmployeeId; _db.Leaves.Remove(x); }
        else if (kind == "disc") { var x = _db.Disciplinary.Find(id); if (x == null || !Can(x.EmployeeId)) return NotFound(); empId = x.EmployeeId; _db.Disciplinary.Remove(x); }
        else if (kind == "course") { var x = _db.Courses.Find(id); if (x == null || !Can(x.EmployeeId)) return NotFound(); empId = x.EmployeeId; _db.Courses.Remove(x); }
        else return NotFound();
        _db.SaveChanges(); return Back(empId);
    }
}
