using Lift.Models;
using Lift.Subject;

namespace Lift.Observer
{
    /// <summary>
    /// The floor status display of ONE floor. It is attached to both lifts and shows where each lift is,
    /// e.g.  "A: G ▲   B: 1".
    /// </summary>
    public sealed class FloorDisplayObserver : IElevatorObserver
    {
        private readonly IReadOnlyList<Elevator> _lifts;
        private readonly Label _label;

        public FloorDisplayObserver(Label label, IReadOnlyList<Elevator> lifts)
        {
            _label = label;
            _lifts = lifts;
            Refresh();
        }

        public void Update(ElevatorEvent e) => Refresh();

        private void Refresh()
        {
            _label.Text = string.Join("   ", _lifts.Select(l =>
                $"{l.Name.Replace("Lift ", "")}: {FloorInfo.Label(l.NearestFloor)}{l.Direction.Arrow()}"));
        }
    }
}
