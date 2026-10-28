using Lift.Models;
using Lift.Observer;
using Lift.Subject;

namespace Lift.Controller
{
    public sealed class DispatchResultEventArgs : EventArgs
    {
        public DispatchResultEventArgs(int floor, Direction direction, Elevator? lift, string message)
        {
            Floor = floor;
            Direction = direction;
            Lift = lift;
            Message = message;
        }

        public int Floor { get; }
        public Direction Direction { get; }
        public Elevator? Lift { get; }      // null = nobody could take the call
        public string Message { get; }
    }

    /// <summary>
    /// Handles the buttons OUTSIDE the lifts. It gives each call to the lift with the shortest
    /// estimated response time. It never moves or re-directs a lift - the chosen lift just adds
    /// the floor to its own list, so work already in progress is not disturbed.
    /// The dispatcher is also an Observer of every lift, so it knows when a call has been answered.
    /// </summary>
    public sealed class ElevatorDispatcher : IElevatorObserver
    {
        private const double Tolerance = 1e-9;

        private readonly List<Elevator> _lifts;
        private readonly Dictionary<(int Floor, Direction Direction), Elevator> _pendingCalls = new();

        public event EventHandler<DispatchResultEventArgs>? RequestDispatched;

        public ElevatorDispatcher(IEnumerable<Elevator> lifts)
        {
            _lifts = lifts.ToList();
            foreach (Elevator lift in _lifts) lift.Attach(this);
        }

        public IReadOnlyList<Elevator> Lifts => _lifts;

        public void Tick()
        {
            foreach (Elevator lift in _lifts) lift.Tick();
        }

        public Elevator? HandleHallRequest(int floor, Direction direction)
        {
            if (!FloorInfo.IsValid(floor))
            {
                Publish(floor, direction, null, $"Invalid floor {floor} - call ignored");
                return null;
            }

            if (_pendingCalls.TryGetValue((floor, direction), out Elevator? alreadyAssigned))
            {
                Publish(floor, direction, alreadyAssigned,
                    $"{FloorInfo.Name(floor)} call already assigned to {alreadyAssigned.Name}");
                return alreadyAssigned;
            }

            Elevator? best = null;
            double bestEta = double.PositiveInfinity;
            foreach (Elevator lift in _lifts)
            {
                double eta = lift.EstimateResponseTicks(floor);
                if (double.IsInfinity(eta)) continue;                      // lift is in emergency

                bool faster = eta < bestEta - Tolerance;
                bool sameButLessBusy = Math.Abs(eta - bestEta) <= Tolerance && best != null
                                       && lift.Stops.Count < best.Stops.Count;
                if (best == null || faster || sameButLessBusy)
                {
                    best = lift;
                    bestEta = eta;
                }
            }

            if (best == null || !best.AcceptHallCall(floor, direction))
            {
                Publish(floor, direction, null, $"No lift available for {FloorInfo.Name(floor)} right now");
                return null;
            }

            _pendingCalls[(floor, direction)] = best;
            double seconds = SimulationSettings.TicksToSeconds(bestEta);
            Publish(floor, direction, best,
                $"{FloorInfo.Name(floor)} call ({direction}) -> {best.Name}, about {seconds:0.0} s");
            return best;
        }

        // Observer: the dispatcher listens to every lift.
        public void Update(ElevatorEvent e)
        {
            if (e.Type == ElevatorEventType.DoorsOpened || e.Type == ElevatorEventType.FloorArrived)
            {
                foreach (var key in _pendingCalls
                    .Where(p => p.Key.Floor == e.CurrentFloor && p.Value.Id == e.ElevatorId)
                    .Select(p => p.Key).ToList())
                {
                    _pendingCalls.Remove(key);
                }
            }
            else if (e.Type == ElevatorEventType.EmergencyActivated)
            {
                ReassignCallsOf(e.ElevatorId);
            }
        }

        // A lift broke down: its outside calls go to the other lift.
        private void ReassignCallsOf(int elevatorId)
        {
            var orphaned = _pendingCalls.Where(p => p.Value.Id == elevatorId).Select(p => p.Key).ToList();
            if (orphaned.Count == 0) return;

            _pendingCalls[orphaned[0]].CancelHallCalls();
            foreach (var key in orphaned) _pendingCalls.Remove(key);
            foreach (var key in orphaned) HandleHallRequest(key.Floor, key.Direction);
        }

        private void Publish(int floor, Direction direction, Elevator? lift, string message) =>
            RequestDispatched?.Invoke(this, new DispatchResultEventArgs(floor, direction, lift, message));
    }
}
