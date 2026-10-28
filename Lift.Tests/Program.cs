using System.Data;
using Lift.Controller;
using Lift.Data;
using Lift.Models;
using Lift.Observer;
using Lift.Subject;

int passed = 0, failed = 0;

void Check(string name, bool ok, string detail = "")
{
    if (ok) { passed++; Console.WriteLine($"  PASS  {name}"); }
    else { failed++; Console.WriteLine($"  FAIL  {name}  {detail}"); }
}

(Elevator a, Elevator b, ElevatorDispatcher d, Recorder rec) NewSystem()
{
    var a = new Elevator(1, "Lift A");
    var b = new Elevator(2, "Lift B");
    var d = new ElevatorDispatcher(new[] { a, b });
    var rec = new Recorder();
    a.Attach(rec); b.Attach(rec);
    return (a, b, d, rec);
}

Console.WriteLine("1. Request buttons outside the lifts (up / down)");
{
    var (a, b, d, rec) = NewSystem();
    var lift = d.HandleHallRequest(FloorInfo.Ground, Direction.Up);
    Check("Both lifts idle at Ground: call from Ground goes to Lift A (tie)", lift == a);
    Sim.Run(d, 200);
    Check("Doors open at Ground", rec.Has(a.Id, ElevatorEventType.DoorsOpened, 1));
}
{
    var (a, b, d, rec) = NewSystem();
    d.HandleHallRequest(FloorInfo.First, Direction.Down);
    Sim.Run(d, 600);
    Check("Down button on First floor: a lift arrives at First floor", a.AtFloor == 2 || b.AtFloor == 2);
    Check("FloorArrived event says From Ground To First",
        rec.Events.Any(e => e.Type == ElevatorEventType.FloorArrived && e.FromFloor == 1 && e.ToFloor == 2));
    Check("Doors open at First floor", rec.Events.Any(e => e.Type == ElevatorEventType.DoorsOpened && e.CurrentFloor == 2));
}

Console.WriteLine("2. Two lifts share the work");
{
    var (a, b, d, rec) = NewSystem();
    var first = d.HandleHallRequest(FloorInfo.First, Direction.Down);
    var second = d.HandleHallRequest(FloorInfo.Ground, Direction.Up);
    Check("First call goes to Lift A", first == a);
    Check("Busy Lift A is not disturbed: Ground call goes to Lift B", second == b);
}
{
    var (a, b, d, rec) = NewSystem();
    a.RequestFloorFromPanel(FloorInfo.First);
    Sim.Run(d, 40);                                            // A is now between floors going up
    var lift = d.HandleHallRequest(FloorInfo.First, Direction.Down);
    Check("A is already moving to First: call from First is given to A (shortest response)", lift == a);
    Sim.Run(d, 600);
    Check("A never left its trip: it reached First", a.AtFloor == 2);
}
{
    var (a, b, d, rec) = NewSystem();
    var l1 = d.HandleHallRequest(FloorInfo.First, Direction.Down);
    var l2 = d.HandleHallRequest(FloorInfo.First, Direction.Down);
    Check("Pressing the same call button twice does not create a second request",
        l1 == l2 && rec.Events.Count(e => e.Type == ElevatorEventType.RequestAssigned) == 1);
}

Console.WriteLine("3. Control panel inside a lift");
{
    var (a, b, d, rec) = NewSystem();
    bool ok = b.RequestFloorFromPanel(FloorInfo.First);
    Sim.Run(d, 600);
    Check("Panel button accepted", ok);
    Check("Only Lift B moved", b.AtFloor == 2 && a.AtFloor == 1);
    Check("Lift A has no movement events", !rec.Events.Any(e => e.ElevatorId == a.Id && e.Type == ElevatorEventType.MovementStarted));
    Check("Invalid floor is refused (no crash)", !a.RequestFloorFromPanel(7));
}

Console.WriteLine("4. Door sensor (object near the door)");
{
    var (a, b, d, rec) = NewSystem();
    a.RequestFloorFromPanel(FloorInfo.First);
    Sim.RunUntil(d, () => a.StateName == "Doors closing", 1500);
    a.SetObstruction(true);
    Sim.Run(d, 10);
    Check("Doors stop closing and re-open", rec.Events.Any(e => e.Type == ElevatorEventType.ObstructionDetected));
    Sim.Run(d, 400);                                           // far longer than the normal 3 s hold
    Check("Doors stay fully open while the object is there", a.DoorOpenness == 1.0 && a.StateName == "Doors open");
    a.SetObstruction(false);
    Sim.Run(d, 400);
    Check("Object removed: doors close and lift is ready", a.DoorOpenness == 0.0 && a.StateName == "Ready");
}

Console.WriteLine("5. Emergency (SOS) button");
{
    var (a, b, d, rec) = NewSystem();
    a.RequestFloorFromPanel(FloorInfo.First);
    Sim.Run(d, 40);
    double frozen = a.Position;
    a.PressEmergency();
    Sim.Run(d, 200);
    Check("SOS between floors stops the car", a.EmergencyActive && a.Position == frozen);
    Check("Doors stay closed between floors", a.DoorOpenness == 0.0);
    Check("Requests inside the broken lift are refused", !a.RequestFloorFromPanel(FloorInfo.Ground));
    Check("Dispatcher gives new outside calls to the other lift", d.HandleHallRequest(FloorInfo.Ground, Direction.Up) == b);
    a.ResetEmergency();
    Sim.Run(d, 800);
    Check("Cancel SOS: lift carries on to its floor and opens", a.AtFloor == 2 && rec.Has(a.Id, ElevatorEventType.DoorsOpened, 2));
    Check("Siren ON / OFF events were sent",
        rec.Events.Any(e => e.Type == ElevatorEventType.EmergencyActivated && e.Message.Contains("siren ON")) &&
        rec.Events.Any(e => e.Type == ElevatorEventType.EmergencyReset && e.Message.Contains("siren OFF")));
}
{
    var (a, b, d, rec) = NewSystem();
    a.PressEmergency();
    Sim.Run(d, 60);
    Check("SOS at a floor opens the doors", a.EmergencyActive && a.DoorOpenness == 1.0);
    a.ResetEmergency();
    Sim.Run(d, 400);
    Check("After cancel the doors close again", !a.EmergencyActive && a.DoorOpenness == 0.0 && a.StateName == "Ready");
}
{
    var (a, b, d, rec) = NewSystem();
    d.HandleHallRequest(FloorInfo.First, Direction.Down);       // given to A
    a.PressEmergency();                                         // A breaks down at once
    Sim.Run(d, 600);
    Check("Call of a broken lift is re-assigned: Lift B serves it", b.AtFloor == 2 && a.AtFloor == 1 && a.Stops.Count == 0);
}

Console.WriteLine("6. Doors buttons");
{
    var (a, b, d, rec) = NewSystem();
    a.PressOpen();
    Sim.Run(d, 60);
    Check("Open button opens the doors", a.DoorOpenness == 1.0);
    a.PressClose();
    Sim.Run(d, 60);
    Check("Close button closes them", a.DoorOpenness == 0.0);
}

Console.WriteLine("7. Logging pipeline (Observer -> BackgroundWorker -> repository)");
{
    var repo = new FakeRepository();
    var writer = new BackgroundLogWriter(repo);
    var (a, b, d, rec) = NewSystem();
    var logObserver = new LogObserver(writer);
    a.Attach(logObserver); b.Attach(logObserver);

    d.HandleHallRequest(FloorInfo.First, Direction.Down);
    Sim.Run(d, 600);
    writer.Stop();

    Check("Rows were saved", repo.Saved.Count > 0);
    Check("Separate floor columns are filled (Arrived: current 2, from 1, to 2)",
        repo.Saved.Any(r => r.EventType == "FloorArrived" && r.CurrentFloorId == 2 && r.FromFloorId == 1 && r.ToFloorId == 2));
    Check("Each row names the lift", repo.Saved.All(r => r.ElevatorId is 1 or 2));
    Check("Display-only events are not logged", !repo.Saved.Any(r => r.EventType is "FloorChanged" or "StateChanged"));
}

Console.WriteLine();
Console.WriteLine($"Passed: {passed}   Failed: {failed}");
return failed == 0 ? 0 : 1;

// ---------- helpers ----------
static class Sim
{
    public static void Run(ElevatorDispatcher d, int ticks)
    {
        for (int i = 0; i < ticks; i++) d.Tick();
    }

    public static void RunUntil(ElevatorDispatcher d, Func<bool> condition, int maxTicks)
    {
        for (int i = 0; i < maxTicks && !condition(); i++) d.Tick();
    }
}

class Recorder : IElevatorObserver
{
    public List<ElevatorEvent> Events { get; } = new();
    public void Update(ElevatorEvent e) => Events.Add(e);

    public bool Has(int liftId, ElevatorEventType type, int floor) =>
        Events.Any(e => e.ElevatorId == liftId && e.Type == type && e.CurrentFloor == floor);
}

class FakeRepository : IElevatorLogRepository
{
    public List<ElevatorLogEntry> Saved { get; } = new();
    public void Save(IEnumerable<ElevatorLogEntry> entries) { lock (Saved) Saved.AddRange(entries); }
    public DataTable GetLogs() => new DataTable();
    public void ClearLogs() { lock (Saved) Saved.Clear(); }
}
