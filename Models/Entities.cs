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

public class RegionStat { public string Name { get; set; } public int Branches { get; set; } public int Employees { get; set; } }
