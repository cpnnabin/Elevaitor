using Lift.Models;

namespace Lift.Observer
{
    /// <summary>Observer: anything that wants to be told when a lift does something.</summary>
    public interface IElevatorObserver
    {
        void Update(ElevatorEvent e);
    }
}
