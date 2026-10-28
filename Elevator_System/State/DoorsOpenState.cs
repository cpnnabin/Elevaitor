using Lift.Models;
using Lift.Subject;

namespace Lift.State
{
    public sealed class DoorsOpenState : ElevatorStateBase
    {
        public override string Name => "Doors open";

        public override void Enter(Elevator e)
        {
            e.HoldTicksLeft = SimulationSettings.DoorHoldTicks;
            e.Raise(ElevatorEventType.DoorsOpened, $"{e.Name} doors opened at {FloorInfo.Name(e.NearestFloor)}");
        }

        public override void Tick(Elevator e)
        {
            if (e.ObstructionDetected)                            // never close on an object
            {
                e.HoldTicksLeft = SimulationSettings.DoorHoldTicks;
                return;
            }

            e.HoldTicksLeft--;
            if (e.HoldTicksLeft <= 0) e.SetState(new DoorsClosingState());
        }

        public override void OnStopAdded(Elevator e, int floor)
        {
            if (floor == e.AtFloor)
            {
                e.RemoveStop(floor);
                e.HoldTicksLeft = SimulationSettings.DoorHoldTicks;   // keep the doors open a bit longer
            }
        }

        public override void OnOpenPressed(Elevator e) => e.HoldTicksLeft = SimulationSettings.DoorHoldTicks;

        public override void OnClosePressed(Elevator e)
        {
            if (!e.ObstructionDetected) e.SetState(new DoorsClosingState());
        }

        public override double RemainingBusyTicks(Elevator e) =>
            e.HoldTicksLeft + 1 / SimulationSettings.DoorSpeedPerTick;
    }
}
