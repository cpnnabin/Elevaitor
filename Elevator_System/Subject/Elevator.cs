using Lift.Models;
using Lift.Observer;
using Lift.State;

namespace Lift.Subject
{
    /// <summary>
    /// One lift. It is BOTH:
    ///  - the Subject of the Observer pattern (it notifies observers), and
    ///  - the Context of the State pattern (it hands every action to its current state).
    /// It contains no GUI and no database code.
    /// </summary>
    public class Elevator : IElevatorSubject
    {
        private readonly List<IElevatorObserver> _observers = new();
        private readonly SortedSet<int> _stops = new();
        private readonly HashSet<int> _panelStops = new();   // requested from inside this lift
        private readonly HashSet<int> _hallStops = new();    // given by the dispatcher
        private IElevatorState _state;

        public Elevator(int id, string name, int startFloor = FloorInfo.MinFloor)
        {
            if (!FloorInfo.IsValid(startFloor)) throw new ArgumentOutOfRangeException(nameof(startFloor));
            Id = id;
            Name = name;
            Position = startFloor;
            LegStartFloor = startFloor;
            _state = new IdleState();
            _state.Enter(this);
        }

        // ---------- read-only status (what observers and the GUI look at) ----------
        public int Id { get; }
        public string Name { get; }
        public double Position { get; internal set; }          // 1.0 = Ground, 2.0 = First, 1.5 = half way
        public double DoorOpenness { get; internal set; }      // 0 = closed, 1 = fully open
        public Direction Direction { get; internal set; } = Direction.None;
        public int HoldTicksLeft { get; internal set; }
        public int LegStartFloor { get; internal set; }
        public bool ObstructionDetected { get; private set; }
        public bool EmergencyActive => _state is EmergencyState;
        public string StateName => _state.Name;
        public IReadOnlyCollection<int> Stops => _stops;

        public int NearestFloor =>
            Math.Clamp((int)Math.Round(Position), FloorInfo.MinFloor, FloorInfo.MaxFloor);

        /// <summary>The floor number when the car is exactly at a floor, otherwise null (between floors).</summary>
        public int? AtFloor =>
            Math.Abs(Position - Math.Round(Position)) < SimulationSettings.Epsilon ? (int)Math.Round(Position) : null;

        // ---------- Subject (Observer pattern) ----------
        public void Attach(IElevatorObserver observer)
        {
            if (!_observers.Contains(observer)) _observers.Add(observer);
        }

        public void Detach(IElevatorObserver observer) => _observers.Remove(observer);

        public void Notify(ElevatorEvent e)
        {
            foreach (IElevatorObserver observer in _observers.ToArray())
            {
                try { observer.Update(e); }
                catch (Exception ex)   // one broken observer must never stop the lift or the other observers
                {
                    System.Diagnostics.Debug.WriteLine($"Observer {observer.GetType().Name} failed: {ex.Message}");
                }
            }
        }

        internal void Raise(ElevatorEventType type, string message, int? from = null, int? to = null) =>
            Notify(new ElevatorEvent(Id, Name, type, NearestFloor, from, to, message, DateTime.Now));

        // ---------- Actions (all are handed to the current State) ----------
        public void Tick() => _state.Tick(this);

        /// <summary>Button pressed INSIDE this lift. Handled only by this lift.</summary>
        public bool RequestFloorFromPanel(int floor) =>
            AddStop(floor, fromHall: false, ElevatorEventType.PanelRequest,
                $"Panel button {FloorInfo.Label(floor)} pressed inside {Name}");

        /// <summary>Request from OUTSIDE. Only the dispatcher calls this.</summary>
        public bool AcceptHallCall(int floor, Direction direction) =>
            AddStop(floor, fromHall: true, ElevatorEventType.RequestAssigned,
                $"Call from {FloorInfo.Name(floor)} ({direction}) assigned to {Name}");

        public void PressOpen() => _state.OnOpenPressed(this);
        public void PressClose() => _state.OnClosePressed(this);

        public void PressEmergency()
        {
            if (!EmergencyActive) _state.OnEmergency(this);
        }

        public void ResetEmergency()
        {
            if (EmergencyActive) _state.OnReset(this);
        }

        /// <summary>Door sensor: true = an object is in the doorway.</summary>
        public void SetObstruction(bool present) => ObstructionDetected = present;

        /// <summary>Remove calls that came from outside (used when this lift breaks down).</summary>
        public void CancelHallCalls()
        {
            foreach (int floor in _hallStops.Where(f => !_panelStops.Contains(f)).ToList())
                _stops.Remove(floor);
            _hallStops.Clear();
        }

        // ---------- Response-time estimate used by the dispatcher ----------
        public double EstimateResponseTicks(int floor)
        {
            if (EmergencyActive) return double.PositiveInfinity;

            // Doors already open (or opening/closing) at that floor: the call is answered at once.
            if (AtFloor == floor && (_state is DoorsOpenState || _state is DoorsOpeningState || _state is DoorsClosingState))
                return 0;

            double ticks = _state.RemainingBusyTicks(this);
            double pos = Position;
            var targets = new SortedSet<int>(_stops) { floor };

            foreach (int stop in OrderStops(targets, pos, Direction))
            {
                ticks += Math.Abs(stop - pos) / SimulationSettings.SpeedPerTick;
                pos = stop;
                if (stop == floor) return ticks;
                ticks += SimulationSettings.DoorCycleTicks;
            }
            return ticks;
        }

        // The lift keeps going the way it is going, serves everything ahead, then turns round.
        private static IEnumerable<int> OrderStops(IEnumerable<int> stops, double pos, Direction dir)
        {
            List<int> list = stops.ToList();
            double eps = SimulationSettings.Epsilon;

            if (dir == Direction.None)
            {
                int nearest = list.OrderBy(s => Math.Abs(s - pos)).First();
                dir = nearest >= pos - eps ? Direction.Up : Direction.Down;
            }

            return dir == Direction.Up
                ? list.Where(s => s >= pos - eps).OrderBy(s => s)
                      .Concat(list.Where(s => s < pos - eps).OrderByDescending(s => s))
                : list.Where(s => s <= pos + eps).OrderByDescending(s => s)
                      .Concat(list.Where(s => s > pos + eps).OrderBy(s => s));
        }

        // ---------- Helpers used by the State classes ----------
        internal void SetState(IElevatorState state)
        {
            _state = state;
            state.Enter(this);
            Raise(ElevatorEventType.StateChanged, state.Name);
        }

        internal bool RemoveStop(int floor)
        {
            _panelStops.Remove(floor);
            _hallStops.Remove(floor);
            return _stops.Remove(floor);
        }

        internal bool HasStopsAhead(Direction dir)
        {
            double eps = SimulationSettings.Epsilon;
            return dir == Direction.Up ? _stops.Any(s => s > Position + eps)
                 : dir == Direction.Down ? _stops.Any(s => s < Position - eps)
                 : false;
        }

        internal int? NextStopInDirection(Direction dir)
        {
            double eps = SimulationSettings.Epsilon;
            if (dir == Direction.Up) return _stops.Where(s => s > Position + eps).Select(s => (int?)s).FirstOrDefault();
            if (dir == Direction.Down) return _stops.Where(s => s < Position - eps).OrderByDescending(s => s).Select(s => (int?)s).FirstOrDefault();
            return null;
        }

        internal Direction NextDirection()
        {
            if (_stops.Count == 0) return Direction.None;
            double eps = SimulationSettings.Epsilon;
            if (Direction == Direction.Up && HasStopsAhead(Direction.Up)) return Direction.Up;
            if (Direction == Direction.Down && HasStopsAhead(Direction.Down)) return Direction.Down;

            int nearest = _stops.OrderBy(s => Math.Abs(s - Position)).First();
            return nearest > Position + eps ? Direction.Up
                 : nearest < Position - eps ? Direction.Down
                 : Direction.None;
        }

        /// <summary>After an emergency between floors: make sure the lift has somewhere to go.</summary>
        internal void AddRecoveryStop()
        {
            int floor = Direction == Direction.Down ? (int)Math.Floor(Position) : (int)Math.Ceiling(Position);
            floor = Math.Clamp(floor, FloorInfo.MinFloor, FloorInfo.MaxFloor);
            _stops.Add(floor);
            _panelStops.Add(floor);
        }

        private bool AddStop(int floor, bool fromHall, ElevatorEventType type, string message)
        {
            if (!FloorInfo.IsValid(floor))
            {
                Raise(ElevatorEventType.RequestRejected, $"Invalid floor {floor} requested - ignored");
                return false;
            }
            if (EmergencyActive)
            {
                Raise(ElevatorEventType.RequestRejected,
                    $"{Name} is in EMERGENCY - request for {FloorInfo.Name(floor)} ignored");
                return false;
            }

            Raise(type, message, NearestFloor, floor);
            _stops.Add(floor);
            (fromHall ? _hallStops : _panelStops).Add(floor);
            _state.OnStopAdded(this, floor);
            return true;
        }
    }
}
