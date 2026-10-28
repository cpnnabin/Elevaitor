using Lift.Models;
using Lift.Subject;

namespace Lift.State
{
    /// <summary>Between floors. One object per direction (Up / Down).</summary>
    public sealed class MovingState : ElevatorStateBase
    {
        private readonly Direction _direction;

        public MovingState(Direction direction) => _direction = direction;

        public override string Name => _direction == Direction.Up ? "Moving up" : "Moving down";

        public override void Enter(Elevator e)
        {
            e.Direction = _direction;
            if (e.AtFloor.HasValue) e.LegStartFloor = e.AtFloor.Value;

            e.Raise(ElevatorEventType.MovementStarted,
                $"{e.Name} moving {(_direction == Direction.Up ? "up" : "down")} from {FloorInfo.Name(e.LegStartFloor)}",
                e.LegStartFloor, e.NextStopInDirection(_direction));
        }

        public override void Tick(Elevator e)
        {
            double eps = SimulationSettings.Epsilon;
            double step = SimulationSettings.SpeedPerTick;
            double nextFloorPos = _direction == Direction.Up
                ? Math.Floor(e.Position + eps) + 1
                : Math.Ceiling(e.Position - eps) - 1;

            int before = e.NearestFloor;
            bool atFloor = Math.Abs(nextFloorPos - e.Position) <= step + eps;
            e.Position = atFloor ? nextFloorPos : e.Position + (_direction == Direction.Up ? step : -step);

            if (e.NearestFloor != before)
                e.Raise(ElevatorEventType.FloorChanged, $"{e.Name} display now {FloorInfo.Label(e.NearestFloor)}");

            if (!atFloor) return;

            int floor = (int)Math.Round(e.Position);
            if (!FloorInfo.IsValid(floor))                       // safety net - should never happen
            {
                e.Position = Math.Clamp(e.Position, FloorInfo.MinFloor, FloorInfo.MaxFloor);
                e.SetState(new IdleState());
                return;
            }

            if (e.RemoveStop(floor))
            {
                e.Raise(ElevatorEventType.FloorArrived,
                    $"{e.Name} arrived at {FloorInfo.Name(floor)}", e.LegStartFloor, floor);
                e.SetState(new DoorsOpeningState());
            }
            else if (e.HasStopsAhead(_direction))
            {
                // keep going - nothing to do on this floor
            }
            else if (e.Stops.Count > 0)
            {
                e.SetState(new MovingState(_direction.Opposite()));
            }
            else
            {
                e.SetState(new IdleState());
            }
        }
    }
}
