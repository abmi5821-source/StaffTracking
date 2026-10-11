-- قاعدة بيانات منصة متابعة موظفي الفروع
-- SQL Server
CREATE DATABASE BankStaffTracking;
GO
USE BankStaffTracking;
GO

-- المندوبيات
CREATE TABLE Regions (
    RegionId     INT IDENTITY PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO Regions (Name) VALUES
(N'مندوبية الشمال'), (N'مندوبية الوسط'), (N'مندوبية الجنوب'),
(N'مندوبية ديالى'), (N'مندوبية الأنبار'), (N'بغداد - الإدارة العامة');

-- الفروع
CREATE TABLE Branches (
    BranchId     INT IDENTITY PRIMARY KEY,
    RegionId     INT NOT NULL REFERENCES Regions(RegionId),
    Name         NVARCHAR(150) NOT NULL,
    Code         NVARCHAR(30) NULL,
    IsActive     BIT NOT NULL DEFAULT 1
);
CREATE INDEX IX_Branches_Region ON Branches(RegionId);

-- المستخدمون (مدير القسم + مدراء المندوبيات)
-- PasswordHash يُنتج عبر ASP.NET Identity PasswordHasher
CREATE TABLE AppUsers (
    UserId        INT IDENTITY PRIMARY KEY,
    UserName      NVARCHAR(60) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(500) NOT NULL,
    FullName      NVARCHAR(150) NOT NULL,
    Role          NVARCHAR(20) NOT NULL CHECK (Role IN ('DeptManager','RegionManager')),
    RegionId      INT NULL REFERENCES Regions(RegionId), -- NULL لمدير القسم
    IsActive      BIT NOT NULL DEFAULT 1
);

-- الموظفون: أصيل وبديل لكل فرع
CREATE TABLE Employees (
    EmployeeId      INT IDENTITY PRIMARY KEY,
    BranchId        INT NOT NULL REFERENCES Branches(BranchId),
    FullName        NVARCHAR(150) NOT NULL,
    EmployeeNumber  NVARCHAR(30) NULL,
    Phone           NVARCHAR(30) NULL,
    EmpType         NVARCHAR(10) NOT NULL CHECK (EmpType IN ('Primary','Substitute')),
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Employees_Branch ON Employees(BranchId);
-- موظف أصيل واحد فعّال وبديل واحد فعّال لكل فرع
CREATE UNIQUE INDEX UX_Branch_EmpType ON Employees(BranchId, EmpType) WHERE IsActive = 1;

-- الإجازات
CREATE TABLE Leaves (
    LeaveId      INT IDENTITY PRIMARY KEY,
    EmployeeId   INT NOT NULL REFERENCES Employees(EmployeeId),
    StartDate    DATE NOT NULL,
    EndDate      DATE NOT NULL,
    LeaveType    NVARCHAR(50) NULL,   -- اعتيادية، مرضية، زمنية ...
    Notes        NVARCHAR(500) NULL,
    EnteredBy    INT NOT NULL REFERENCES AppUsers(UserId),
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CHECK (EndDate >= StartDate)
);
CREATE INDEX IX_Leaves_Emp_Date ON Leaves(EmployeeId, StartDate);

-- العقوبات والتنبيهات
CREATE TABLE Disciplinary (
    DisciplinaryId INT IDENTITY PRIMARY KEY,
    EmployeeId     INT NOT NULL REFERENCES Employees(EmployeeId),
    Kind           NVARCHAR(10) NOT NULL CHECK (Kind IN ('Warning','Penalty')), -- تنبيه / عقوبة
    IssueDate      DATE NOT NULL,
    Reason         NVARCHAR(500) NOT NULL,
    BookNumber     NVARCHAR(50) NULL,  -- رقم الكتاب
    EnteredBy      INT NOT NULL REFERENCES AppUsers(UserId),
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- الدورات
CREATE TABLE Courses (
    CourseId     INT IDENTITY PRIMARY KEY,
    EmployeeId   INT NOT NULL REFERENCES Employees(EmployeeId),
    Title        NVARCHAR(200) NOT NULL,
    Scope        NVARCHAR(15) NOT NULL CHECK (Scope IN ('Local','International')),
    Country      NVARCHAR(80) NULL,
    StartDate    DATE NULL,
    EndDate      DATE NULL,
    EnteredBy    INT NOT NULL REFERENCES AppUsers(UserId),
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- سجل التعديلات (مفيد للمشرف)
CREATE TABLE AuditLog (
    AuditId     BIGINT IDENTITY PRIMARY KEY,
    UserId      INT NULL,
    Action      NVARCHAR(50) NOT NULL,
    TableName   NVARCHAR(50) NOT NULL,
    RecordId    INT NULL,
    Details     NVARCHAR(1000) NULL,
    At          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- تقرير: ملخص الإجازات لكل موظف (أسبوع / شهر / سنة)
CREATE VIEW vw_LeaveSummary AS
SELECT  e.EmployeeId, e.FullName, e.EmpType, b.Name AS Branch, r.Name AS Region,
  (SELECT COUNT(*) FROM Leaves l WHERE l.EmployeeId = e.EmployeeId
      AND l.StartDate >= DATEADD(DAY, -7, CAST(GETDATE() AS DATE)))  AS LeavesLast7Days,
  (SELECT COUNT(*) FROM Leaves l WHERE l.EmployeeId = e.EmployeeId
      AND YEAR(l.StartDate) = YEAR(GETDATE()) AND MONTH(l.StartDate) = MONTH(GETDATE())) AS LeavesThisMonth,
  (SELECT COUNT(*) FROM Leaves l WHERE l.EmployeeId = e.EmployeeId
      AND YEAR(l.StartDate) = YEAR(GETDATE())) AS LeavesThisYear,
  (SELECT COUNT(*) FROM Disciplinary d WHERE d.EmployeeId = e.EmployeeId AND d.Kind = 'Warning') AS Warnings,
  (SELECT COUNT(*) FROM Disciplinary d WHERE d.EmployeeId = e.EmployeeId AND d.Kind = 'Penalty') AS Penalties,
  (SELECT COUNT(*) FROM Courses c WHERE c.EmployeeId = e.EmployeeId AND c.Scope = 'Local') AS LocalCourses,
  (SELECT COUNT(*) FROM Courses c WHERE c.EmployeeId = e.EmployeeId AND c.Scope = 'International') AS InternationalCourses
FROM Employees e
JOIN Branches b ON b.BranchId = e.BranchId
JOIN Regions  r ON r.RegionId = b.RegionId
WHERE e.IsActive = 1;
GO
