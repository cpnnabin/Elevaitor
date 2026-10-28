using Lift.Subject;

namespace Lift.State
{
    /// <summary>State pattern: every action a lift can receive is answered differently by each state.</summary>
    public interface IElevatorState
    {
        string Name { get; }
        void Enter(Elevator e);
        void Tick(Elevator e);
        void OnStopAdded(Elevator e, int floor);
        void OnOpenPressed(Elevator e);
        void OnClosePressed(Elevator e);
        void OnEmergency(Elevator e);
        void OnReset(Elevator e);
        double RemainingBusyTicks(Elevator e);
    }
}
