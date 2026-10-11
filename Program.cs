using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StaffTracking.Data;
using StaffTracking.Models;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// اختيار قاعدة البيانات: DATABASE_URL (Render/PostgreSQL) أو ConnectionStrings:Default (SQL Server محليًا)
string conn; bool pg;
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(dbUrl))
{
    var u = new Uri(dbUrl);
    var ui = u.UserInfo.Split(':', 2);
    conn = $"Host={u.Host};Port={(u.Port > 0 ? u.Port : 5432)};Database={u.AbsolutePath.TrimStart('/')};" +
           $"Username={Uri.UnescapeDataString(ui[0])};Password={Uri.UnescapeDataString(ui[1])};SSL Mode=Prefer;Trust Server Certificate=true";
    pg = true;
}
else
{
    conn = builder.Configuration.GetConnectionString("Default");
    pg = conn.StartsWith("Host=", StringComparison.OrdinalIgnoreCase);
}
builder.Services.AddDbContext<AppDbContext>(o => { if (pg) o.UseNpgsql(conn); else o.UseSqlServer(conn); });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Account/Login"; o.ExpireTimeSpan = TimeSpan.FromHours(8); });
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear(); o.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (pg)
    {
        db.Database.EnsureCreated();   // PostgreSQL: إنشاء الجداول تلقائيًا
        // جداول أضيفت لاحقًا (لقواعد أنشئت قبل هذا الإصدار)
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""Leaves"" (""LeaveId"" serial PRIMARY KEY, ""EmployeeId"" int NOT NULL, ""StartDate"" timestamp NOT NULL, ""EndDate"" timestamp NOT NULL, ""LeaveType"" text, ""Notes"" text, ""EnteredBy"" int NOT NULL, ""CreatedAt"" timestamp NOT NULL);
CREATE TABLE IF NOT EXISTS ""Disciplinary"" (""DisciplinaryId"" serial PRIMARY KEY, ""EmployeeId"" int NOT NULL, ""Kind"" text NOT NULL, ""IssueDate"" timestamp NOT NULL, ""Reason"" text, ""BookNumber"" text, ""EnteredBy"" int NOT NULL, ""CreatedAt"" timestamp NOT NULL);
CREATE TABLE IF NOT EXISTS ""Courses"" (""CourseId"" serial PRIMARY KEY, ""EmployeeId"" int NOT NULL, ""Title"" text NOT NULL, ""Scope"" text NOT NULL, ""Country"" text, ""StartDate"" timestamp, ""EndDate"" timestamp, ""EnteredBy"" int NOT NULL, ""CreatedAt"" timestamp NOT NULL);");
    }
    if (!db.Regions.Any())
    {
        db.Regions.AddRange(new[] { "مندوبية الشمال", "مندوبية الوسط", "مندوبية الجنوب",
            "مندوبية ديالى", "مندوبية الأنبار", "بغداد - الإدارة العامة" }.Select(n => new Region { Name = n }));
        db.SaveChanges();
    }
    if (!db.AppUsers.Any(u => u.Role == "DeptManager"))
    {
        var admin = new AppUser { UserName = "admin", FullName = "مدير القسم", Role = "DeptManager", IsActive = true };
        admin.PasswordHash = new PasswordHasher<AppUser>().HashPassword(admin, "Admin@12345");
        db.AppUsers.Add(admin);
        db.SaveChanges();
    }
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();
