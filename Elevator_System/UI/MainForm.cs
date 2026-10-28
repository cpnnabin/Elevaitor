using Lift.Controller;
using Lift.Data;
using Lift.Models;
using Lift.Observer;
using Lift.Subject;

namespace Lift.UI
{
    /// <summary>
    /// The window. It only builds controls, forwards button clicks, and runs the timer.
    /// All lift behaviour lives in Subject/, State/ and Controller/.
    /// </summary>
    public sealed class MainForm : Form
    {
        // Must match BuildingView: padding 10, two floors, so each floor band is (620 - 20) / 2 = 300 high.
        private const int ViewX = 20, ViewY = 20, ViewW = 440, ViewH = 620;
        private const int FloorBandHeight = 300;

        private readonly Elevator[] _lifts = { new Elevator(1, "Lift A"), new Elevator(2, "Lift B") };
        private readonly ElevatorDispatcher _dispatcher;
        private readonly SqlElevatorLogRepository _repository = new();
        private readonly BackgroundLogWriter _logWriter;
        private readonly SirenObserver _siren = new();
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = SimulationSettings.TickIntervalMs };
        private readonly BuildingView _view;
        private readonly Label _status = new();
        private LogForm? _logForm;

        public MainForm()
        {
            Text = "Elevator - two lifts";
            ClientSize = new Size(1100, 700);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            _dispatcher = new ElevatorDispatcher(_lifts);
            _dispatcher.RequestDispatched += (s, e) => _status.Text = e.Message;

            _logWriter = new BackgroundLogWriter(_repository);
            _logWriter.WriteFailed += (s, message) => _status.Text = "Log database problem: " + message;

            _view = new BuildingView(_lifts)
            {
                Location = new Point(ViewX, ViewY),
                Size = new Size(ViewW, ViewH)
            };
            Controls.Add(_view);

            // Observers that every lift shares
            var logObserver = new LogObserver(_logWriter);
            foreach (Elevator lift in _lifts)
            {
                lift.Attach(logObserver);
                lift.Attach(_siren);
            }

            BuildHallControls();
            for (int i = 0; i < _lifts.Length; i++) BuildLiftPanel(_lifts[i], 640 + i * 220);
            BuildBottomControls();

            _status.Location = new Point(20, 650);
            _status.Size = new Size(1060, 40);
            _status.Font = new Font("Segoe UI", 10f);
            _status.Text = "Ready";
            Controls.Add(_status);

            _timer.Tick += OnTick;
            Shown += (s, e) =>
            {
                _logWriter.Write(new ElevatorLogEntry(DateTime.Now, null, "System",
                    "Application started - Lift A and Lift B at Ground Floor", FloorInfo.Ground, null, null));
                _timer.Start();
            };
            FormClosing += OnFormClosing;
        }

        // ---------- building the screen ----------

        // Outside the lifts: one display and one request button per floor.
        private void BuildHallControls()
        {
            for (int floor = FloorInfo.MaxFloor; floor >= FloorInfo.MinFloor; floor--)
            {
                int hallFloor = floor;
                Direction direction = floor == FloorInfo.Ground ? Direction.Up : Direction.Down;
                int bandTop = ViewY + 10 + (FloorInfo.MaxFloor - floor) * FloorBandHeight;

                var caption = new Label
                {
                    Text = FloorInfo.Name(floor).ToUpper() + " STATUS",
                    Location = new Point(470, bandTop + 90),
                    Size = new Size(160, 22),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                };

                var display = new Label
                {
                    Location = new Point(470, bandTop + 115),
                    Size = new Size(160, 44),
                    BackColor = Color.Black,
                    ForeColor = Color.Red,
                    Font = new Font("Consolas", 14f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                Button request = MakeButton(direction == Direction.Up ? "▲" : "▼", 525, bandTop + 170, 50, 50, 18f);
                request.Click += (s, e) => Safe(() => _dispatcher.HandleHallRequest(hallFloor, direction));

                Controls.Add(caption);
                Controls.Add(display);
                Controls.Add(request);

                var floorDisplay = new FloorDisplayObserver(display, _lifts);
                foreach (Elevator lift in _lifts) lift.Attach(floorDisplay);
            }
        }

        // Inside one lift: display window, floor buttons, door buttons, SOS and the door-sensor switch.
        private void BuildLiftPanel(Elevator lift, int x)
        {
            var box = new GroupBox
            {
                Text = lift.Name + " control panel",
                Location = new Point(x, 20),
                Size = new Size(200, 620),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            var screen = new Panel { BackColor = Color.Black, Location = new Point(10, 25), Size = new Size(180, 120) };
            var floorText = new Label
            {
                Dock = DockStyle.Top, Height = 75, ForeColor = Color.Red,
                Font = new Font("Consolas", 34f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
            };
            var stateText = new Label
            {
                Dock = DockStyle.Fill, ForeColor = Color.Red,
                Font = new Font("Consolas", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
            };
            screen.Controls.Add(stateText);      // Fill first, then Top
            screen.Controls.Add(floorText);

            Button floor1 = MakeButton("1", 65, 160, 70, 50, 16f);
            Button floorG = MakeButton("G", 65, 220, 70, 50, 16f);
            Button open = MakeButton("Open", 15, 285, 80, 45, 10f);
            Button close = MakeButton("Close", 105, 285, 80, 45, 10f);
            Button sos = MakeButton("SOS", 15, 345, 170, 60, 16f);
            sos.BackColor = Color.Firebrick;
            sos.ForeColor = Color.White;

            floor1.Click += (s, e) => Safe(() => lift.RequestFloorFromPanel(FloorInfo.First));
            floorG.Click += (s, e) => Safe(() => lift.RequestFloorFromPanel(FloorInfo.Ground));
            open.Click += (s, e) => Safe(lift.PressOpen);
            close.Click += (s, e) => Safe(lift.PressClose);
            sos.Click += (s, e) => Safe(() =>
            {
                if (lift.EmergencyActive) lift.ResetEmergency();
                else lift.PressEmergency();
            });

            var sensor = new CheckBox
            {
                Text = "Object in doorway", Location = new Point(15, 425), Size = new Size(175, 24),
                Font = new Font("Segoe UI", 9f)
            };
            sensor.CheckedChanged += (s, e) => lift.SetObstruction(sensor.Checked);

            var pending = new Label
            {
                Location = new Point(15, 465), Size = new Size(175, 40), Font = new Font("Segoe UI", 9f)
            };

            box.Controls.AddRange(new Control[] { screen, floor1, floorG, open, close, sos, sensor, pending });
            Controls.Add(box);

            lift.Attach(new ControlPanelObserver(lift, floorText, stateText, pending, sos));
        }

        private void BuildBottomControls()
        {
            Button log = MakeButton("LOG", 470, 552, 160, 44, 12f);
            log.Click += (s, e) => ShowLog();

            Button exit = MakeButton("Exit", 470, 602, 160, 40, 12f);
            exit.Click += (s, e) => Close();

            Controls.Add(log);
            Controls.Add(exit);
        }

        private static Button MakeButton(string text, int x, int y, int w, int h, float fontSize) => new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            Font = new Font("Segoe UI", fontSize, FontStyle.Bold)
        };

        // ---------- behaviour ----------

        private void OnTick(object? sender, EventArgs e)
        {
            try
            {
                _dispatcher.Tick();     // moves cars and doors one step
                _view.Invalidate();     // redraw
            }
            catch (Exception ex)
            {
                _status.Text = "Simulation error: " + ex.Message;
            }
        }

        private void ShowLog()
        {
            if (_logForm == null || _logForm.IsDisposed)
            {
                _logForm = new LogForm(_repository, _logWriter);
                _logForm.Show(this);
            }
            else
            {
                _logForm.Activate();
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _timer.Stop();
            _siren.Dispose();
            _logWriter.Write(new ElevatorLogEntry(DateTime.Now, null, "System", "Application closed",
                null, null, null));
            _logWriter.Stop();           // saves what is still queued
        }

        // One error handler for every button.
        private void Safe(Action action)
        {
            try { action(); }
            catch (Exception ex) { _status.Text = "Error: " + ex.Message; }
        }
    }
}
