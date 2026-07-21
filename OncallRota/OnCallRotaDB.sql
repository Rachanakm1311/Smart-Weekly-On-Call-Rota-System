-- ============================================================
--  OnCallRotaDB  --  Complete Database Script
--  Generated from: SQL Server (Server=.)
--  Database: OnCallRotaDB
--  Application: Smart Weekly On-Call Rota Management System
--  Generated on: 2026-07-17 18:58:39
-- ============================================================

USE master;
GO

-- ------------------------------------------------------------
-- 1. CREATE DATABASE
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N''OnCallRotaDB'')
BEGIN
    CREATE DATABASE OnCallRotaDB;
    PRINT ''Database OnCallRotaDB created.'';
END
ELSE
    PRINT ''Database OnCallRotaDB already exists — skipping CREATE.'';
GO

USE OnCallRotaDB;
GO

-- ============================================================
-- 2. CREATE TABLES  (dependency order: no FK violations)
-- ============================================================

-- ------------------------------------------------------------
-- 2.1 Roles
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.Roles'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RoleId      INT          IDENTITY(1,1)  NOT NULL,
        RoleName    VARCHAR(50)                 NOT NULL,
        Description VARCHAR(200)                    NULL,
        Status      VARCHAR(20)  DEFAULT (''Active'')   NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (RoleId)
    );
    PRINT ''Table Roles created.'';
END
ELSE
    PRINT ''Table Roles already exists — skipping.'';
GO

-- ------------------------------------------------------------
-- 2.2 Teams
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.Teams'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Teams (
        TeamId      INT           IDENTITY(1,1)   NOT NULL,
        TeamName    VARCHAR(100)                  NOT NULL,
        ManagerName VARCHAR(100)                      NULL,
        Status      VARCHAR(20)   DEFAULT (''Active'')  NULL,
        TeamDL      VARCHAR(200)                      NULL,
        TeamEmailId VARCHAR(150)                      NULL,
        CONSTRAINT PK_Teams PRIMARY KEY (TeamId)
    );
    PRINT ''Table Teams created.'';
END
ELSE
    PRINT ''Table Teams already exists — skipping.'';
GO

-- ------------------------------------------------------------
-- 2.3 Employees
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.Employees'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Employees (
        EmployeeId     INT          IDENTITY(1,1)              NOT NULL,
        FirstName      VARCHAR(50)                             NOT NULL,
        LastName       VARCHAR(50)                             NOT NULL,
        EmployeeName   VARCHAR(100)                            NOT NULL,
        Email          VARCHAR(150)                                NULL,
        Phone          VARCHAR(20)                                 NULL,
        Region         VARCHAR(50)                                 NULL,
        TeamId         INT                                     NOT NULL,
        RoleId         INT                                     NOT NULL,
        Status         VARCHAR(20)  DEFAULT (''Active'')            NULL,
        Password       VARCHAR(200) DEFAULT (''Password123!'')  NOT NULL,
        ProfilePicture VARCHAR(200)                                NULL,
        CONSTRAINT PK_Employees PRIMARY KEY (EmployeeId),
        CONSTRAINT FK_Employees_Teams FOREIGN KEY (TeamId)
            REFERENCES dbo.Teams (TeamId),
        CONSTRAINT FK_Employees_Roles FOREIGN KEY (RoleId)
            REFERENCES dbo.Roles (RoleId)
    );
    PRINT ''Table Employees created.'';
END
ELSE
BEGIN
    -- Add FirstName / LastName to an existing database
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''dbo.Employees'') AND name = ''FirstName'')
        ALTER TABLE dbo.Employees ADD FirstName VARCHAR(50) NOT NULL DEFAULT '''';
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''dbo.Employees'') AND name = ''LastName'')
        ALTER TABLE dbo.Employees ADD LastName  VARCHAR(50) NOT NULL DEFAULT '''';
    -- Add Region column to existing databases
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''dbo.Employees'') AND name = ''Region'')
        ALTER TABLE dbo.Employees ADD Region VARCHAR(50) NULL;
    PRINT ''Table Employees already exists — columns checked/added.'';
END
GO

-- ------------------------------------------------------------
-- 2.4 Applications
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.Applications'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Applications (
        ApplicationId   INT          IDENTITY(1,1)              NOT NULL,
        ApplicationName VARCHAR(100)                            NOT NULL,
        TeamId          INT                                     NOT NULL,
        Status          VARCHAR(20)  DEFAULT (''Active'')            NULL,
        CONSTRAINT PK_Applications PRIMARY KEY (ApplicationId),
        CONSTRAINT FK_Applications_Teams FOREIGN KEY (TeamId)
            REFERENCES dbo.Teams (TeamId)
    );
    PRINT ''Table Applications created.'';
END
ELSE
    PRINT ''Table Applications already exists — skipping.'';
GO

-- ------------------------------------------------------------
-- 2.5 HolidayCalendar
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.HolidayCalendar'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.HolidayCalendar (
        HolidayId   INT          IDENTITY(1,1)             NOT NULL,
        HolidayName VARCHAR(100)                           NOT NULL,
        HolidayDate DATE                                   NOT NULL,
        Location    VARCHAR(50)  DEFAULT ('''')             NOT NULL,
        Description VARCHAR(200)                               NULL,
        Status      VARCHAR(20)  DEFAULT (''Active'')           NULL,
        CONSTRAINT PK_HolidayCalendar PRIMARY KEY (HolidayId)
    );
    PRINT ''Table HolidayCalendar created.'';
END
ELSE
    PRINT ''Table HolidayCalendar already exists — skipping.'';
GO

-- ------------------------------------------------------------
-- 2.6 LeaveRequests
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.LeaveRequests'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequests (
        LeaveId       INT          IDENTITY(1,1)                  NOT NULL,
        EmployeeId    INT                                         NOT NULL,
        LeaveType     VARCHAR(30)  DEFAULT (''Annual Leave'')          NULL,
        StartDate     DATE                                            NULL,
        EndDate       DATE                                            NULL,
        Reason        VARCHAR(250)                                    NULL,
        Status        VARCHAR(20)  DEFAULT (''Pending'')               NULL,
        ApprovedBy    VARCHAR(100)                                    NULL,   -- manager name string, NOT a FK
        ApprovedDate  DATE                                            NULL,
        WorkOnHoliday BIT          DEFAULT ((0))               NOT NULL,
        CompOffDate   DATE                                            NULL,
        CONSTRAINT PK_LeaveRequests PRIMARY KEY (LeaveId),
        CONSTRAINT FK_LeaveRequests_Employees FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees (EmployeeId)
    );
    PRINT ''Table LeaveRequests created.'';
END
ELSE
BEGIN
    -- Add WorkOnHoliday / CompOffDate to existing databases
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''dbo.LeaveRequests'') AND name = ''WorkOnHoliday'')
        ALTER TABLE dbo.LeaveRequests ADD WorkOnHoliday BIT NOT NULL DEFAULT ((0));
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''dbo.LeaveRequests'') AND name = ''CompOffDate'')
        ALTER TABLE dbo.LeaveRequests ADD CompOffDate DATE NULL;
    PRINT ''Table LeaveRequests already exists — columns checked/added.'';
END
GO

-- ------------------------------------------------------------
-- 2.7 OnCallSchedule
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.OnCallSchedule'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.OnCallSchedule (
        ScheduleId        INT         IDENTITY(1,1)             NOT NULL,
        TeamId            INT                                   NOT NULL,
        WeekStartDate     DATE                                      NULL,
        WeekEndDate       DATE                                      NULL,
        PrimaryEmployeeId INT                                       NULL,
        BackupEmployeeId  INT                                       NULL,
        Status            VARCHAR(20) DEFAULT (''Active'')           NULL,
        CONSTRAINT PK_OnCallSchedule PRIMARY KEY (ScheduleId),
        CONSTRAINT FK_OnCallSchedule_Teams
            FOREIGN KEY (TeamId)       REFERENCES dbo.Teams (TeamId),
        CONSTRAINT FK_OnCallSchedule_PrimaryEmployee
            FOREIGN KEY (PrimaryEmployeeId) REFERENCES dbo.Employees (EmployeeId),
        CONSTRAINT FK_OnCallSchedule_BackupEmployee
            FOREIGN KEY (BackupEmployeeId)  REFERENCES dbo.Employees (EmployeeId)
    );
    PRINT ''Table OnCallSchedule created.'';
END
ELSE
    PRINT ''Table OnCallSchedule already exists — skipping.'';
GO

-- ------------------------------------------------------------
-- 2.8 RotationQueue
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.RotationQueue'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.RotationQueue (
        QueueId       INT   IDENTITY(1,1)  NOT NULL,
        TeamId        INT                  NOT NULL,
        EmployeeId    INT                  NOT NULL,
        QueuePosition INT   DEFAULT ((1))  NOT NULL,
        IsActive      BIT   DEFAULT ((1))      NULL,
        CONSTRAINT PK_RotationQueue PRIMARY KEY (QueueId),
        CONSTRAINT FK_RotationQueue_Teams FOREIGN KEY (TeamId)
            REFERENCES dbo.Teams (TeamId),
        CONSTRAINT FK_RotationQueue_Employees FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees (EmployeeId)
    );
    PRINT ''Table RotationQueue created.'';
END
ELSE
    PRINT ''Table RotationQueue already exists — skipping.'';
GO

-- ------------------------------------------------------------
-- 2.9 ShiftSwapRequests
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.ShiftSwapRequests'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.ShiftSwapRequests (
        SwapId                INT          IDENTITY(1,1)           NOT NULL,
        ScheduleId            INT                                  NOT NULL,
        RequestedByEmployeeId INT                                  NOT NULL,
        SwapWithEmployeeId    INT                                  NOT NULL,
        Reason                VARCHAR(250)                             NULL,
        RequestDate           DATETIME     DEFAULT (GETDATE())         NULL,
        Status                VARCHAR(20)  DEFAULT (''Pending'')        NULL,
        ApprovedBy            VARCHAR(100)                             NULL,   -- manager name string, NOT a FK
        ApprovedDate          DATETIME                                 NULL,
        CONSTRAINT PK_ShiftSwapRequests PRIMARY KEY (SwapId),
        CONSTRAINT FK_ShiftSwapRequests_OnCallSchedule
            FOREIGN KEY (ScheduleId) REFERENCES dbo.OnCallSchedule (ScheduleId)
            ON DELETE CASCADE,                                               -- deleting schedule removes swap requests
        CONSTRAINT FK_ShiftSwapRequests_RequestedBy
            FOREIGN KEY (RequestedByEmployeeId) REFERENCES dbo.Employees (EmployeeId),
        CONSTRAINT FK_ShiftSwapRequests_SwapWith
            FOREIGN KEY (SwapWithEmployeeId)    REFERENCES dbo.Employees (EmployeeId)
    );
    PRINT ''Table ShiftSwapRequests created.'';
END
ELSE
    PRINT ''Table ShiftSwapRequests already exists — skipping.'';
GO

-- ------------------------------------------------------------
-- 2.10 Notifications
-- ------------------------------------------------------------
IF OBJECT_ID(''dbo.Notifications'', ''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        NotificationId   INT          IDENTITY(1,1)            NOT NULL,
        EmployeeId       INT                                   NOT NULL,
        NotificationType VARCHAR(50)                               NULL,
        Message          VARCHAR(500) DEFAULT ('''')            NOT NULL,
        SentDate         DATETIME     DEFAULT (GETDATE())          NULL,
        Status           VARCHAR(20)  DEFAULT (''Unread'')          NULL,
        CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
        CONSTRAINT FK_Notifications_Employees
            FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees (EmployeeId)
            ON DELETE CASCADE   -- deleting employee cleans up their notifications
    );
    PRINT ''Table Notifications created.'';
END
ELSE
    PRINT ''Table Notifications already exists — skipping.'';
GO


-- ============================================================
-- 3. SEED DATA  (SET IDENTITY_INSERT so IDs match the app)
-- ============================================================

-- ------------------------------------------------------------
-- 3.1 Roles
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Roles ON;
MERGE dbo.Roles AS target
USING (VALUES
    (1, ''Admin'',    ''System Administrator'',          ''Active''),
    (2, ''Employee'', ''Application Support Employee'',  ''Active'')
) AS src (RoleId, RoleName, Description, Status)
ON target.RoleId = src.RoleId
WHEN NOT MATCHED THEN
    INSERT (RoleId, RoleName, Description, Status)
    VALUES (src.RoleId, src.RoleName, src.Description, src.Status);
SET IDENTITY_INSERT dbo.Roles OFF;
PRINT ''Roles seeded.'';
GO

-- ------------------------------------------------------------
-- 3.2 Teams
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Teams ON;
MERGE dbo.Teams AS target
USING (VALUES
    (101, ''MS-COTS'',      ''Sarah'',   ''Active'', NULL, ''cots.team@nexorainfotech.com''),
    (102, ''Java Web'',     ''John'',    ''Active'', NULL, ''java.team@nexorainfotech.com''),
    (103, ''SAP Logistics'',''David'',   ''Active'', NULL, ''sap.team@nexorainfotech.com''),
    (104, ''SQL DBA'',      ''Michael'', ''Active'', NULL, ''database.team@nexorainfotech.com''),
    (108, ''Oracle DBA'',   ''Edward'',  ''Active'', NULL, ''oracle.team@nexorainfotech.com'')
) AS src (TeamId, TeamName, ManagerName, Status, TeamDL, TeamEmailId)
ON target.TeamId = src.TeamId
WHEN NOT MATCHED THEN
    INSERT (TeamId, TeamName, ManagerName, Status, TeamDL, TeamEmailId)
    VALUES (src.TeamId, src.TeamName, src.ManagerName, src.Status, src.TeamDL, src.TeamEmailId);
SET IDENTITY_INSERT dbo.Teams OFF;
PRINT ''Teams seeded.'';
GO

-- ------------------------------------------------------------
-- 3.3 Employees  (passwords are BCrypt hashes of ''Password123!'')
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Employees ON;
MERGE dbo.Employees AS target
USING (VALUES
    (5001, ''Rachana'',      ''K M'',           ''Rachana K M'',           ''rachana.nair@nexorainfotech.com'',      ''9876543210'', ''Bangalore'',  101, 2, ''Active''),
    (5002, ''Amiya'',        ''Saaho'',          ''Amiya Saaho'',           ''amiya.sahoo@nexorainfotech.com'',       ''9876543211'', ''Kolkata'',    101, 2, ''Active''),
    (5003, ''Ankusarao'',    ''Swathi'',         ''Ankusarao Swathi'',      ''swathi.krishna@nexorainfotech.com'',    ''9876543212'', ''Hyderabad'',  101, 2, ''Active''),
    (5004, ''Hari'',         ''Kumar V'',        ''Hari Kumar V'',          ''hari.prasad@nexorainfotech.com'',       ''9876543213'', ''Chennai'',    101, 2, ''Active''),
    (5005, ''Prakash'',      ''Raj'',            ''Prakash Raj'',           ''prakash.kumar@nexorainfotech.com'',     ''9876543214'', ''Bangalore'',  102, 2, ''Active''),
    (5006, ''Ajit'',         ''Mishra'',         ''Ajit Mishra'',           ''ajit.sharma@nexorainfotech.com'',       ''9876543215'', ''Pune'',       102, 2, ''Active''),
    (5007, ''Chandra'',      ''Munnamgi'',       ''Chandra Munnamgi'',      ''chandra.sekhar@nexorainfotech.com'',    ''9876543216'', ''Hyderabad'',  102, 2, ''Active''),
    (5008, ''Bharathiraja'', ''M'',              ''Bharathiraja'',          ''bharathiraja.m@nexorainfotech.com'',    ''9876543217'', ''Chennai'',    103, 2, ''Active''),
    (5009, ''Jaishree'',     ''Mahato'',         ''Jaishree Mahato'',       ''jaishree.venkat@nexorainfotech.com'',   ''9876543218'', ''Kolkata'',    103, 2, ''Active''),
    (5010, ''Akhila'',       ''Dangeti'',        ''Akhila Dangeti'',        ''akhila.reddy@nexorainfotech.com'',      ''9876543219'', ''Hyderabad'',  103, 2, ''Active''),
    (5011, ''Kuntalika'',    ''Dey Choudary'',   ''Kuntalika Dey Choudary'',''kuntalika.das@nexorainfotech.com'',     ''9876543220'', ''Kolkata'',    103, 2, ''Active''),
    (5012, ''Rahul'',        ''K J'',            ''Rahul K J'',             ''rahul.mehta@nexorainfotech.com'',       ''9876543221'', ''Bangalore'',  104, 2, ''Active''),
    (5013, ''Sneha'',        ''S'',              ''Sneha S'',               ''sneha.joshi@nexorainfotech.com'',       ''9876543222'', ''Pune'',       104, 2, ''Active''),
    (5014, ''Vinay'',        ''H'',              ''Vinay H'',               ''vinay.patil@nexorainfotech.com'',       ''9876543223'', ''Bangalore'',  104, 2, ''Active''),
    (5015, ''Admin'',        ''User'',           ''Admin User'',            ''admin.user@nexorainfotech.com'',        ''9100000001'', ''Bangalore'',  101, 1, ''Active''),
    (5016, ''John'',         ''David'',          ''John David'',            ''john.mathew@nexorainfotech.com'',       ''9100000002'', ''Chennai'',    101, 2, ''Active''),
    (5017, ''David'',        ''Jack'',           ''David Jack'',            ''david.wilson@nexorainfotech.com'',      ''9100000003'', ''Delhi'',      102, 2, ''Active''),
    (5018, ''Emily'',        ''Carter'',         ''Emily Carter'',          ''emily.carter@nexorainfotech.com'',      ''9100000004'', ''Pune'',       101, 2, ''Active'')
) AS src (EmployeeId, FirstName, LastName, EmployeeName, Email, Phone, Region, TeamId, RoleId, Status)
ON target.EmployeeId = src.EmployeeId
WHEN NOT MATCHED THEN
    INSERT (EmployeeId, FirstName, LastName, EmployeeName, Email, Phone, Region, TeamId, RoleId, Status, Password)
    VALUES (src.EmployeeId, src.FirstName, src.LastName, src.EmployeeName, src.Email, src.Phone,
            src.Region, src.TeamId, src.RoleId, src.Status, ''Password123!'');
WHEN MATCHED THEN
    UPDATE SET
        FirstName    = src.FirstName,
        LastName     = src.LastName,
        EmployeeName = src.EmployeeName,
        Region       = src.Region;
SET IDENTITY_INSERT dbo.Employees OFF;
PRINT ''Employees seeded.'';
GO

-- ------------------------------------------------------------
-- 3.4 Applications
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Applications ON;
MERGE dbo.Applications AS target
USING (VALUES
    (2001, ''EFAB'',        101, ''Active''),
    (2002, ''EGAMME'',      101, ''Active''),
    (2003, ''PickPull'',    101, ''Active''),
    (2004, ''BNET'',        101, ''Active''),
    (2005, ''N1WCTC'',      101, ''Active''),
    (2006, ''Java Billing'',102, ''Active''),
    (2007, ''Bluebeam'',    102, ''Active''),
    (2008, ''SAP Portal'',  103, ''Active''),
    (2009, ''SAP Reports'', 103, ''Active''),
    (2010, ''SQL Server'',  104, ''Active''),
    (2011, ''Oracle'',      104, ''Active''),
    (2012, ''BFlex'',       101, ''Active'')
) AS src (ApplicationId, ApplicationName, TeamId, Status)
ON target.ApplicationId = src.ApplicationId
WHEN NOT MATCHED THEN
    INSERT (ApplicationId, ApplicationName, TeamId, Status)
    VALUES (src.ApplicationId, src.ApplicationName, src.TeamId, src.Status);
SET IDENTITY_INSERT dbo.Applications OFF;
PRINT ''Applications seeded.'';
GO

-- ------------------------------------------------------------
-- 3.5 HolidayCalendar
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.HolidayCalendar ON;
MERGE dbo.HolidayCalendar AS target
USING (VALUES
    (4001, ''Republic Day'', ''2026-01-26'', ''Bangalore'', ''National Holiday'',  ''Active''),
    (4002, ''Republic Day'', ''2026-01-26'', ''Pune'',      ''National Holiday'',  ''Active''),
    (4003, ''Ugadi'',        ''2026-03-20'', ''Bangalore'', ''Regional Holiday'',  ''Active''),
    (4004, ''Gudi Padwa'',   ''2026-03-20'', ''Pune'',      ''Regional Holiday'',  ''Active''),
    (4005, ''Bonalu'',       ''2026-08-15'', ''Hyderabad'', ''Regional Holiday'',  ''Active''),
    (4006, ''Durga Puja'',   ''2026-10-21'', ''Kolkata'',   ''Regional Holiday'',  ''Active'')
) AS src (HolidayId, HolidayName, HolidayDate, Location, Description, Status)
ON target.HolidayId = src.HolidayId
WHEN NOT MATCHED THEN
    INSERT (HolidayId, HolidayName, HolidayDate, Location, Description, Status)
    VALUES (src.HolidayId, src.HolidayName, src.HolidayDate,
            src.Location, src.Description, src.Status);
SET IDENTITY_INSERT dbo.HolidayCalendar OFF;
PRINT ''HolidayCalendar seeded.'';
GO

-- ------------------------------------------------------------
-- 3.6 RotationQueue
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.RotationQueue ON;
MERGE dbo.RotationQueue AS target
USING (VALUES
    -- COTS Team (101)
    (3002, 101, 5002, 1, 1),  -- Amiya     pos 1
    (3001, 101, 5001, 2, 1),  -- Rachana   pos 2
    (3003, 101, 5003, 3, 1),  -- Swathi    pos 3
    (3004, 101, 5004, 4, 1),  -- Hari      pos 4
    (3016, 101, 5016, 5, 1),  -- John      pos 5
    -- Java Team (102)
    (3005, 102, 5005, 1, 1),  -- Prakash   pos 1
    (3006, 102, 5006, 2, 1),  -- Ajit      pos 2
    (3007, 102, 5007, 3, 1),  -- Chandra   pos 3
    (3023, 102, 5017, 4, 1),  -- David     pos 4
    -- SAP Team (103)
    (3009, 103, 5009, 1, 1),  -- Jaishree  pos 1
    (3008, 103, 5008, 2, 1),  -- Bharathiraja pos 2
    (3010, 103, 5010, 3, 1),  -- Akhila    pos 3
    (3011, 103, 5011, 4, 1),  -- Kuntalika pos 4
    -- Database Team (104)
    (3012, 104, 5012, 1, 1),  -- Rahul     pos 1
    (3013, 104, 5013, 2, 1),  -- Sneha     pos 2
    (3014, 104, 5014, 3, 1),  -- Vinay     pos 3
    -- TCS team (107)
    (3022, 107, 5018, 1, 1)   -- Emily     pos 1
) AS src (QueueId, TeamId, EmployeeId, QueuePosition, IsActive)
ON target.QueueId = src.QueueId
WHEN NOT MATCHED THEN
    INSERT (QueueId, TeamId, EmployeeId, QueuePosition, IsActive)
    VALUES (src.QueueId, src.TeamId, src.EmployeeId, src.QueuePosition, src.IsActive);
SET IDENTITY_INSERT dbo.RotationQueue OFF;
PRINT ''RotationQueue seeded.'';
GO


-- ============================================================
-- 4. USEFUL QUERIES (commented out — run as needed)
-- ============================================================

/*
-- View all employees with team and role
SELECT e.EmployeeId, e.EmployeeName, e.Email, e.Phone,
       t.TeamName, r.RoleName, e.Status
FROM   Employees e
JOIN   Teams t ON e.TeamId = t.TeamId
JOIN   Roles r ON e.RoleId = r.RoleId
ORDER  BY t.TeamName, e.EmployeeName;

-- View current on-call schedule
SELECT s.ScheduleId, t.TeamName,
       s.WeekStartDate, s.WeekEndDate,
       p.EmployeeName AS Primary_Engineer,
       b.EmployeeName AS Backup_Engineer,
       s.Status
FROM   OnCallSchedule s
JOIN   Teams     t ON s.TeamId            = t.TeamId
JOIN   Employees p ON s.PrimaryEmployeeId = p.EmployeeId
LEFT JOIN Employees b ON s.BackupEmployeeId = b.EmployeeId
ORDER  BY s.WeekStartDate DESC, t.TeamName;

-- View pending leave requests
SELECT lr.LeaveId, e.EmployeeName, lr.LeaveType,
       lr.StartDate, lr.EndDate, lr.Reason, lr.Status
FROM   LeaveRequests lr
JOIN   Employees e ON lr.EmployeeId = e.EmployeeId
WHERE  lr.Status = ''Pending''
ORDER  BY lr.StartDate;

-- View pending shift swap requests
SELECT ss.SwapId, req.EmployeeName AS Requested_By,
       swap.EmployeeName AS Swap_With,
       t.TeamName, s.WeekStartDate, ss.Reason, ss.Status
FROM   ShiftSwapRequests ss
JOIN   Employees  req  ON ss.RequestedByEmployeeId = req.EmployeeId
JOIN   Employees  swap ON ss.SwapWithEmployeeId    = swap.EmployeeId
JOIN   OnCallSchedule s ON ss.ScheduleId           = s.ScheduleId
JOIN   Teams      t    ON s.TeamId                 = t.TeamId
WHERE  ss.Status = ''Pending''
ORDER  BY ss.RequestDate DESC;

-- View rotation queue per team
SELECT t.TeamName, rq.QueuePosition,
       e.EmployeeName, rq.IsActive
FROM   RotationQueue rq
JOIN   Teams     t ON rq.TeamId     = t.TeamId
JOIN   Employees e ON rq.EmployeeId = e.EmployeeId
ORDER  BY t.TeamName, rq.QueuePosition;

-- Unread notifications
SELECT n.NotificationId, e.EmployeeName,
       n.NotificationType, n.Message, n.SentDate
FROM   Notifications n
JOIN   Employees e ON n.EmployeeId = e.EmployeeId
WHERE  n.Status = ''Unread''
ORDER  BY n.SentDate DESC;
*/

-- ============================================================
-- END OF SCRIPT
-- ============================================================
PRINT ''OnCallRotaDB script completed successfully.'';
GO