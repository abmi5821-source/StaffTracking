using Microsoft.EntityFrameworkCore;
using StaffTracking.Models;

namespace StaffTracking.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Region> Regions { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Employee> Employees { get; set; }
}
