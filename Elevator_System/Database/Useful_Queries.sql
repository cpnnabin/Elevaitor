/* Handy queries for testing and for the viva. Run them in Visual Studio (SQL Server Object Explorer)
   or SSMS connected to (localdb)\MSSQLLocalDB, database Elevator. */
USE [Elevator];
GO

-- 1. Everything, newest first (this is what the Log window shows)
SELECT * FROM dbo.vw_ElevatorLogDetail ORDER BY LogId DESC;

-- 2. Last 20 operations
SELECT TOP (20) * FROM dbo.vw_ElevatorLogDetail ORDER BY LogId DESC;

-- 3. One lift only
SELECT * FROM dbo.vw_ElevatorLogDetail WHERE ElevatorName = N'Lift A' ORDER BY LogId DESC;

-- 4. Only emergency (SOS) records
SELECT * FROM dbo.vw_ElevatorLogDetail WHERE EventType LIKE N'Emergency%' ORDER BY LogId DESC;

-- 5. Only door-sensor records (object in the doorway)
SELECT * FROM dbo.vw_ElevatorLogDetail WHERE EventType = N'ObstructionDetected' ORDER BY LogId DESC;

-- 6. Every trip: where from, where to
SELECT LogDate, LogClock, ElevatorName, FromFloor, ToFloor
FROM dbo.vw_ElevatorLogDetail WHERE EventType = N'FloorArrived' ORDER BY LogId DESC;

-- 7. How many of each event, per lift
SELECT ElevatorName, EventType, COUNT(*) AS Total
FROM dbo.vw_ElevatorLogDetail
GROUP BY ElevatorName, EventType
ORDER BY ElevatorName, EventType;

-- 8. How busy is each floor (arrivals per floor and lift)
SELECT ElevatorName, CurrentFloor, COUNT(*) AS Arrivals
FROM dbo.vw_ElevatorLogDetail WHERE EventType = N'FloorArrived'
GROUP BY ElevatorName, CurrentFloor;

-- 9. Today only
SELECT * FROM dbo.ElevatorLogs WHERE CAST(LogTime AS date) = CAST(GETDATE() AS date) ORDER BY LogId DESC;

-- 10. Floor and lift details
SELECT * FROM dbo.Floors;
SELECT * FROM dbo.Elevators;

-- 11. Delete all log rows (the Clear Log button does the same through the DataAdapter)
-- DELETE FROM dbo.ElevatorLogs;

-- 12. Start again from nothing
-- DROP VIEW  dbo.vw_ElevatorLogDetail;
-- DROP TABLE dbo.ElevatorLogs;
-- DROP TABLE dbo.Elevators;
-- DROP TABLE dbo.Floors;
