using Lift.Models;
using Lift.Observer;

namespace Lift.Subject
{
    /// <summary>Subject: one lift, many observers (one-to-many relationship).</summary>
    public interface IElevatorSubject
    {
        void Attach(IElevatorObserver observer);
        void Detach(IElevatorObserver observer);
        void Notify(ElevatorEvent e);
    }
}
