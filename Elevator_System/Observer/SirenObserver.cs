using System.Media;
using Lift.Models;

namespace Lift.Observer
{
    /// <summary>Plays a repeating alarm sound while any lift is in emergency.</summary>
    public sealed class SirenObserver : IElevatorObserver, IDisposable
    {
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 800 };
        private readonly HashSet<int> _liftsInEmergency = new();

        public SirenObserver() => _timer.Tick += (s, e) => SystemSounds.Hand.Play();

        public void Update(ElevatorEvent e)
        {
            if (e.Type == ElevatorEventType.EmergencyActivated) _liftsInEmergency.Add(e.ElevatorId);
            else if (e.Type == ElevatorEventType.EmergencyReset) _liftsInEmergency.Remove(e.ElevatorId);
            else return;

            if (_liftsInEmergency.Count > 0) { if (!_timer.Enabled) { SystemSounds.Hand.Play(); _timer.Start(); } }
            else _timer.Stop();
        }

        public void Dispose() => _timer.Dispose();
    }
}
