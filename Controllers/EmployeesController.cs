using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StaffTracking.Data;
using StaffTracking.Models;

namespace StaffTracking.Controllers;

[Authorize]
public class EmployeesController : Controller
{
    private readonly AppDbContext _db;
    public EmployeesController(AppDbContext db) { _db = db; }
    private int? MyRegion => User.IsInRole("RegionManager") ? int.Parse(User.FindFirstValue("RegionId")) : (int?)null;
    private bool CanBranch(int branchId) { var my = MyRegion; return _db.Branches.Any(b => b.BranchId == branchId && (my == null || b.RegionId == my)); }

    private void Fill()
    {
        var my = MyRegion;
        ViewBag.BranchItems = (from b in _db.Branches join r in _db.Regions on b.RegionId equals r.RegionId
                               where b.IsActive && (my == null || r.RegionId == my) orderby r.RegionId, b.Name
                               select new { b.BranchId, r.Name, B = b.Name }).ToList()
            .Select(x => new SelectListItem(x.Name + " - " + x.B, x.BranchId.ToString())).ToList();
    }

    public IActionResult Index(int? regionId, string q)
    {
        var my = MyRegion;
        var rows = (from e in _db.Employees join b in _db.Branches on e.BranchId equals b.BranchId
                    join r in _db.Regions on b.RegionId equals r.RegionId
                    where (my == null || r.RegionId == my) && (regionId == null || r.RegionId == regionId)
                    orderby r.RegionId, b.Name, e.EmpType
                    select new EmpRow { EmployeeId = e.EmployeeId, FullName = e.FullName, EmpType = e.EmpType, IsActive = e.IsActive, Branch = b.Name, Region = r.Name }).ToList();
        if (!string.IsNullOrWhiteSpace(q)) rows = rows.Where(x => x.FullName.Contains(q) || x.Branch.Contains(q)).ToList();
        var yStart = new DateTime(DateTime.Today.Year, 1, 1);
        var leaves = _db.Leaves.Where(l => l.StartDate >= yStart).Select(l => l.EmployeeId).ToList();
        var disc = _db.Disciplinary.Select(d => new { d.EmployeeId, d.Kind }).ToList();
        var crs = _db.Courses.Select(c => new { c.EmployeeId, c.Scope }).ToList();
        foreach (var r in rows)
        {
            r.LeavesYear = leaves.Count(x => x == r.EmployeeId);
            r.Warnings = disc.Count(d => d.EmployeeId == r.EmployeeId && d.Kind == "Warning");
            r.Penalties = disc.Count(d => d.EmployeeId == r.EmployeeId && d.Kind == "Penalty");
            r.LocalCourses = crs.Count(c => c.EmployeeId == r.EmployeeId && c.Scope == "Local");
            r.IntlCourses = crs.Count(c => c.EmployeeId == r.EmployeeId && c.Scope == "International");
        }
        ViewBag.Regions = _db.Regions.OrderBy(x => x.RegionId).ToList();
        ViewBag.SelRegion = regionId; ViewBag.Q = q;
        return View(rows);
    }

    public IActionResult Create() { Fill(); return View("Form", new Employee { IsActive = true, EmpType = "Primary" }); }

    [HttpPost]
    public IActionResult Create(Employee m)
    {
        Validate(m, 0);
        if (!ModelState.IsValid) { Fill(); return View("Form", m); }
        m.EmployeeId = 0; _db.Employees.Add(m); _db.SaveChanges();
        return RedirectToAction("Details", new { id = m.EmployeeId });
    }

    public IActionResult Edit(int id)
    {
        var e = _db.Employees.Find(id);
        if (e == null || !CanBranch(e.BranchId)) return NotFound();
        Fill(); return View("Form", e);
    }

    [HttpPost]
    public IActionResult Edit(int id, Employee m)
    {
        var e = _db.Employees.Find(id);
        if (e == null || !CanBranch(e.BranchId)) return NotFound();
        Validate(m, id);
        if (!ModelState.IsValid) { Fill(); m.EmployeeId = id; return View("Form", m); }
        e.BranchId = m.BranchId; e.FullName = m.FullName; e.EmployeeNumber = m.EmployeeNumber;
        e.Phone = m.Phone; e.EmpType = m.EmpType; e.IsActive = m.IsActive;
        _db.SaveChanges();
        return RedirectToAction("Details", new { id });
    }

    private void Validate(Employee m, int selfId)
    {
        if (string.IsNullOrWhiteSpace(m.FullName)) ModelState.AddModelError("", "اسم الموظف مطلوب");
        if (!CanBranch(m.BranchId)) ModelState.AddModelError("", "اختر فرعًا من الفروع التابعة لك");
        else if (m.IsActive && _db.Employees.Any(x => x.BranchId == m.BranchId && x.EmpType == m.EmpType && x.IsActive && x.EmployeeId != selfId))
            ModelState.AddModelError("", "يوجد موظف " + (m.EmpType == "Primary" ? "أصيل" : "بديل") + " فعّال في هذا الفرع");
    }

    public IActionResult Details(int id, int? year)
    {
        var e = _db.Employees.Find(id);
        if (e == null || !CanBranch(e.BranchId)) return NotFound();
        var br = _db.Branches.Find(e.BranchId); var rg = _db.Regions.Find(br.RegionId);
        var leaves = _db.Leaves.Where(x => x.EmployeeId == id).ToList();
        var disc = _db.Disciplinary.Where(x => x.EmployeeId == id).ToList();
        var crs = _db.Courses.Where(x => x.EmployeeId == id).ToList();
        var today = DateTime.Today; var cy = year ?? today.Year;
        var vm = new EmpDetailsVm
        {
            Emp = e, Branch = br.Name, Region = rg.Name, ChartYear = cy,
            Week = leaves.Count(l => l.StartDate >= today.AddDays(-6)),
            Month = leaves.Count(l => l.StartDate.Year == today.Year && l.StartDate.Month == today.Month),
            Year = leaves.Count(l => l.StartDate.Year == today.Year),
            MonthlyLeaves = Enumerable.Range(1, 12).Select(m => leaves.Count(l => l.StartDate.Year == cy && l.StartDate.Month == m)).ToArray(),
            MonthlyDays = Enumerable.Range(1, 12).Select(m => leaves.Where(l => l.StartDate.Year == cy && l.StartDate.Month == m).Sum(l => (l.EndDate - l.StartDate).Days + 1)).ToArray(),
            Warnings = disc.Count(d => d.Kind == "Warning"), Penalties = disc.Count(d => d.Kind == "Penalty"),
            Local = crs.Count(c => c.Scope == "Local"), Intl = crs.Count(c => c.Scope == "International"),
            Timeline = new List<TimelineItem>()
        };
        vm.Timeline.AddRange(leaves.Select(l => new TimelineItem { Date = l.StartDate, Kind = "leave", Id = l.LeaveId, Type = "إجازة", Color = "primary",
            Title = (l.LeaveType ?? "إجازة") + " (" + ((l.EndDate - l.StartDate).Days + 1) + " يوم)", Detail = l.StartDate.ToString("yyyy-MM-dd") + " إلى " + l.EndDate.ToString("yyyy-MM-dd") + " " + l.Notes }));
        vm.Timeline.AddRange(disc.Select(d => new TimelineItem { Date = d.IssueDate, Kind = "disc", Id = d.DisciplinaryId,
            Type = d.Kind == "Warning" ? "تنبيه" : "عقوبة", Color = d.Kind == "Warning" ? "warning" : "danger", Title = d.Reason, Detail = string.IsNullOrEmpty(d.BookNumber) ? "" : "كتاب رقم " + d.BookNumber }));
        vm.Timeline.AddRange(crs.Select(c => new TimelineItem { Date = c.StartDate ?? c.CreatedAt, Kind = "course", Id = c.CourseId,
            Type = c.Scope == "Local" ? "دورة محلية" : "دورة دولية", Color = c.Scope == "Local" ? "success" : "info", Title = c.Title, Detail = c.Country }));
        vm.Timeline = vm.Timeline.OrderByDescending(t => t.Date).ToList();
        return View(vm);
    }
}
