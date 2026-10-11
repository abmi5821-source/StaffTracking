using System.ComponentModel.DataAnnotations;
namespace StaffTracking.Models;

public class Region { public int RegionId { get; set; } public string Name { get; set; } }

public class Branch
{
    public int BranchId { get; set; }
    public int RegionId { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public bool IsActive { get; set; }
}

public class AppUser
{
    [Key] public int UserId { get; set; }
    public string UserName { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }          // DeptManager | RegionManager
    public int? RegionId { get; set; }
    public bool IsActive { get; set; }
}

public class Employee
{
    public int EmployeeId { get; set; }
    public int BranchId { get; set; }
    public string FullName { get; set; }
    public string EmployeeNumber { get; set; }
    public string Phone { get; set; }
    public string EmpType { get; set; }       // Primary | Substitute
    public bool IsActive { get; set; }
}


public class Leave
{
    public int LeaveId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string LeaveType { get; set; }
    public string Notes { get; set; }
    public int EnteredBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class Disciplinary
{
    public int DisciplinaryId { get; set; }
    public int EmployeeId { get; set; }
    public string Kind { get; set; }          // Warning | Penalty
    public DateTime IssueDate { get; set; }
    public string Reason { get; set; }
    public string BookNumber { get; set; }
    public int EnteredBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class Course
{
    public int CourseId { get; set; }
    public int EmployeeId { get; set; }
    public string Title { get; set; }
    public string Scope { get; set; }         // Local | International
    public string Country { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int EnteredBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RegionStat { public string Name { get; set; } public int Branches { get; set; } public int Employees { get; set; }
    public int LeavesYear { get; set; } public int Warnings { get; set; } public int Penalties { get; set; }
    public int LocalCourses { get; set; } public int IntlCourses { get; set; } }

public class EmpRow { public int EmployeeId { get; set; } public string FullName { get; set; } public string EmpType { get; set; }
    public bool IsActive { get; set; } public string Branch { get; set; } public string Region { get; set; }
    public int LeavesYear { get; set; } public int Warnings { get; set; } public int Penalties { get; set; }
    public int LocalCourses { get; set; } public int IntlCourses { get; set; } }

public class TimelineItem { public DateTime Date { get; set; } public string Kind { get; set; } public int Id { get; set; }
    public string Type { get; set; } public string Color { get; set; } public string Title { get; set; } public string Detail { get; set; } }

public class EmpDetailsVm
{
    public Employee Emp { get; set; } public string Branch { get; set; } public string Region { get; set; }
    public int Week { get; set; } public int Month { get; set; } public int Year { get; set; } public int ChartYear { get; set; }
    public int[] MonthlyLeaves { get; set; } public int[] MonthlyDays { get; set; }
    public int Warnings { get; set; } public int Penalties { get; set; } public int Local { get; set; } public int Intl { get; set; }
    public List<TimelineItem> Timeline { get; set; }
}
