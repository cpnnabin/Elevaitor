using Lift.Models;
using Lift.Subject;

namespace Lift.Observer
{
    /// <summary>The display window, state text, pending-stops text and SOS button of ONE lift's control panel.</summary>
    public sealed class ControlPanelObserver : IElevatorObserver
    {
        private readonly Elevator _lift;
        private readonly Label _floorDisplay;
        private readonly Label _stateDisplay;
        private readonly Label _pendingDisplay;
        private readonly Button _sosButton;

        public ControlPanelObserver(Elevator lift, Label floorDisplay, Label stateDisplay,
            Label pendingDisplay, Button sosButton)
        {
            _lift = lift;
            _floorDisplay = floorDisplay;
            _stateDisplay = stateDisplay;
            _pendingDisplay = pendingDisplay;
            _sosButton = sosButton;
            Refresh();
        }

        public void Update(ElevatorEvent e) => Refresh();

        private void Refresh()
        {
            _floorDisplay.Text = $"{FloorInfo.Label(_lift.NearestFloor)} {_lift.Direction.Arrow()}";
            _stateDisplay.Text = _lift.StateName.ToUpper();
            _pendingDisplay.Text = _lift.Stops.Count == 0
                ? "Pending: none"
                : "Pending: " + string.Join(", ", _lift.Stops.Select(FloorInfo.Label));
            _sosButton.Text = _lift.EmergencyActive ? "CANCEL SOS" : "SOS";
        }
    }
}
