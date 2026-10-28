using Lift.Models;
using Lift.Subject;

namespace Lift.State
{
    /// <summary>
    /// SOS pressed. The car freezes where it is. At a floor the doors open so people can leave;
    /// between floors the doors stay shut. All requests are refused until the SOS is cancelled.
    /// </summary>
    public sealed class EmergencyState : ElevatorStateBase
    {
        public override string Name => "EMERGENCY";

        public override void Enter(Elevator e)
        {
            string where = e.AtFloor.HasValue
                ? $"at {FloorInfo.Name(e.AtFloor.Value)}"
                : "between floors - car stopped";
            e.Raise(ElevatorEventType.EmergencyActivated,
                $"EMERGENCY button pressed on {e.Name} {where} - siren ON");
        }

        public override void Tick(Elevator e)
        {
            if (e.AtFloor.HasValue && e.DoorOpenness < 1.0)
                e.DoorOpenness = Math.Min(1.0, e.DoorOpenness + SimulationSettings.DoorSpeedPerTick);
        }

        public override void OnEmergency(Elevator e) { }   // already in emergency

        public override void OnReset(Elevator e)
        {
            e.Raise(ElevatorEventType.EmergencyReset, $"Emergency cancelled on {e.Name} - siren OFF");

            if (!e.AtFloor.HasValue)                                   // frozen between floors: carry on
            {
                if (!e.HasStopsAhead(e.Direction)) e.AddRecoveryStop();
                e.SetState(new MovingState(e.Direction == Direction.None ? Direction.Up : e.Direction));
            }
            else if (e.DoorOpenness > SimulationSettings.Epsilon)
            {
                e.SetState(new DoorsOpenState());                      // doors close after the normal hold time
            }
            else
            {
                e.SetState(new IdleState());
            }
        }
    }
}
