/* =====================================================================
   Elevator database - setup script
   Server   : (localdb)\MSSQLLocalDB
   Database : Elevator
   Safe to run many times. The application also runs this file by itself
   at start-up (it is copied next to the .exe and found with a relative path).
   ===================================================================== */

IF DB_ID(N'Elevator') IS NULL
    CREATE DATABASE [Elevator];
GO

USE [Elevator];
GO

/* The old single-lift table has different columns. Keep its data under another name. */
IF OBJECT_ID(N'dbo.ElevatorLogs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ElevatorLogs', N'ElevatorId') IS NULL
    EXEC sp_rename N'dbo.ElevatorLogs', N'ElevatorLogs_Old';
GO

/* ---------- Floor details (one row per floor) ---------- */
IF OBJECT_ID(N'dbo.Floors', N'U') IS NULL
    CREATE TABLE dbo.Floors
    (
        FloorId    INT          NOT NULL CONSTRAINT PK_Floors PRIMARY KEY,
        FloorName  NVARCHAR(30) NOT NULL,
        FloorLabel NVARCHAR(5)  NOT NULL
    );
GO

/* ---------- Lift details (one row per lift) ---------- */
IF OBJECT_ID(N'dbo.Elevators', N'U') IS NULL
    CREATE TABLE dbo.Elevators
    (
        ElevatorId   INT          NOT NULL CONSTRAINT PK_Elevators PRIMARY KEY,
        ElevatorName NVARCHAR(20) NOT NULL
    );
GO

/* ---------- Operation log ---------- */
IF OBJECT_ID(N'dbo.ElevatorLogs', N'U') IS NULL
    CREATE TABLE dbo.ElevatorLogs
    (
        LogId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ElevatorLogs_Lift PRIMARY KEY,
        LogTime        DATETIME2(0)      NOT NULL CONSTRAINT DF_ElevatorLogs_LogTime DEFAULT SYSDATETIME(),
        ElevatorId     INT               NULL CONSTRAINT FK_ElevatorLogs_Elevators REFERENCES dbo.Elevators (ElevatorId),
        EventType      NVARCHAR(30)      NOT NULL,
        Action         NVARCHAR(200)     NOT NULL,
        CurrentFloorId INT               NULL CONSTRAINT FK_ElevatorLogs_CurrentFloor REFERENCES dbo.Floors (FloorId),
        FromFloorId    INT               NULL CONSTRAINT FK_ElevatorLogs_FromFloor    REFERENCES dbo.Floors (FloorId),
        ToFloorId      INT               NULL CONSTRAINT FK_ElevatorLogs_ToFloor      REFERENCES dbo.Floors (FloorId)
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_ElevatorLogs_LogTime' AND object_id = OBJECT_ID(N'dbo.ElevatorLogs'))
    CREATE INDEX IX_ElevatorLogs_LogTime ON dbo.ElevatorLogs (LogTime);
GO

/* ---------- Start data ---------- */
MERGE dbo.Floors AS t
USING (VALUES (1, N'Ground Floor', N'G'), (2, N'First Floor', N'1')) AS s (FloorId, FloorName, FloorLabel)
   ON t.FloorId = s.FloorId
WHEN MATCHED THEN UPDATE SET FloorName = s.FloorName, FloorLabel = s.FloorLabel
WHEN NOT MATCHED THEN INSERT (FloorId, FloorName, FloorLabel) VALUES (s.FloorId, s.FloorName, s.FloorLabel);
GO

MERGE dbo.Elevators AS t
USING (VALUES (1, N'Lift A'), (2, N'Lift B')) AS s (ElevatorId, ElevatorName)
   ON t.ElevatorId = s.ElevatorId
WHEN MATCHED THEN UPDATE SET ElevatorName = s.ElevatorName
WHEN NOT MATCHED THEN INSERT (ElevatorId, ElevatorName) VALUES (s.ElevatorId, s.ElevatorName);
GO

/* ---------- Readable log: floor names and lift names in separate columns ---------- */
CREATE OR ALTER VIEW dbo.vw_ElevatorLogDetail
AS
SELECT  l.LogId,
        CONVERT(VARCHAR(10), l.LogTime, 103)            AS LogDate,       -- dd/mm/yyyy
        CONVERT(VARCHAR(8),  l.LogTime, 108)            AS LogClock,      -- hh:mm:ss
        COALESCE(e.ElevatorName, N'System')             AS ElevatorName,
        l.EventType,
        cf.FloorName                                    AS CurrentFloor,
        ff.FloorName                                    AS FromFloor,
        tf.FloorName                                    AS ToFloor,
        l.Action
FROM        dbo.ElevatorLogs l
LEFT JOIN   dbo.Elevators    e  ON e.ElevatorId = l.ElevatorId
LEFT JOIN   dbo.Floors       cf ON cf.FloorId   = l.CurrentFloorId
LEFT JOIN   dbo.Floors       ff ON ff.FloorId   = l.FromFloorId
LEFT JOIN   dbo.Floors       tf ON tf.FloorId   = l.ToFloorId;
GO
