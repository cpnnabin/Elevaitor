using Lift.Models;
using Lift.Subject;

namespace Lift.State
{
    public sealed class DoorsOpeningState : ElevatorStateBase
    {
        public override string Name => "Doors opening";

        public override void Tick(Elevator e)
        {
            e.DoorOpenness = Math.Min(1.0, e.DoorOpenness + SimulationSettings.DoorSpeedPerTick);
            if (e.DoorOpenness >= 1.0 - SimulationSettings.Epsilon)
            {
                e.DoorOpenness = 1.0;
                e.SetState(new DoorsOpenState());
            }
        }

        public override void OnStopAdded(Elevator e, int floor)
        {
            if (floor == e.AtFloor) e.RemoveStop(floor);          // already here
        }

        public override void OnClosePressed(Elevator e) => e.SetState(new DoorsClosingState());

        public override double RemainingBusyTicks(Elevator e) =>
            (1 - e.DoorOpenness) / SimulationSettings.DoorSpeedPerTick
            + SimulationSettings.DoorHoldTicks
            + 1 / SimulationSettings.DoorSpeedPerTick;
    }
}
