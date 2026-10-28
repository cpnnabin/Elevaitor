using Lift.Models;
using Lift.Subject;

namespace Lift.State
{
    /// <summary>Standing at a floor with the doors closed.</summary>
    public sealed class IdleState : ElevatorStateBase
    {
        public override string Name => "Ready";

        public override void Enter(Elevator e) => e.Direction = Direction.None;

        public override void Tick(Elevator e)
        {
            int? floor = e.AtFloor;
            if (floor.HasValue && e.RemoveStop(floor.Value))      // somebody wants THIS floor
            {
                e.SetState(new DoorsOpeningState());
                return;
            }

            Direction next = e.NextDirection();
            if (next != Direction.None) e.SetState(new MovingState(next));
        }

        public override void OnOpenPressed(Elevator e) => e.SetState(new DoorsOpeningState());
    }
}
