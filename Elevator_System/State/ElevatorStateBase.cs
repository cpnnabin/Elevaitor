using Lift.Subject;

namespace Lift.State
{
    /// <summary>Default behaviour shared by all states. A state only overrides what it does differently.</summary>
    public abstract class ElevatorStateBase : IElevatorState
    {
        public abstract string Name { get; }

        public virtual void Enter(Elevator e) { }
        public virtual void Tick(Elevator e) { }
        public virtual void OnStopAdded(Elevator e, int floor) { }
        public virtual void OnOpenPressed(Elevator e) { }
        public virtual void OnClosePressed(Elevator e) { }
        public virtual void OnEmergency(Elevator e) => e.SetState(new EmergencyState());   // SOS works in every state
        public virtual void OnReset(Elevator e) { }
        public virtual double RemainingBusyTicks(Elevator e) => 0;
    }
}
