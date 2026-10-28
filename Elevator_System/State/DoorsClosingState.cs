using Lift.Models;
using Lift.Subject;

namespace Lift.State
{
    public sealed class DoorsClosingState : ElevatorStateBase
    {
        public override string Name => "Doors closing";

        public override void Tick(Elevator e)
        {
            if (e.ObstructionDetected)        // object near the door: stop closing and re-open
            {
                e.Raise(ElevatorEventType.ObstructionDetected,
                    $"{e.Name}: object in the doorway at {FloorInfo.Name(e.NearestFloor)} - doors re-opening");
                e.SetState(new DoorsOpeningState());
                return;
            }

            e.DoorOpenness = Math.Max(0.0, e.DoorOpenness - SimulationSettings.DoorSpeedPerTick);
            if (e.DoorOpenness <= SimulationSettings.Epsilon)
            {
                e.DoorOpenness = 0;
                e.Raise(ElevatorEventType.DoorsClosed, $"{e.Name} doors closed at {FloorInfo.Name(e.NearestFloor)}");
                e.SetState(new IdleState());
            }
        }

        public override void OnOpenPressed(Elevator e) => e.SetState(new DoorsOpeningState());

        public override void OnStopAdded(Elevator e, int floor)
        {
            if (floor == e.AtFloor)
            {
                e.RemoveStop(floor);
                e.SetState(new DoorsOpeningState());
            }
        }

        public override double RemainingBusyTicks(Elevator e) =>
            e.DoorOpenness / SimulationSettings.DoorSpeedPerTick;
    }
}
