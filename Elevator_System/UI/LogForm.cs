using System.Data;
using Lift.Data;

namespace Lift.UI
{
    /// <summary>Shows the stored log. All database work runs through DbBackgroundRunner (BackgroundWorker).</summary>
    public sealed class LogForm : Form
    {
        private static readonly (string Column, string Header, int Width)[] Columns =
        {
            ("LogId", "ID", 60), ("LogDate", "Date", 85), ("LogClock", "Time", 75),
            ("ElevatorName", "Lift", 70), ("EventType", "Event", 140),
            ("CurrentFloor", "Floor", 100), ("FromFloor", "From", 100), ("ToFloor", "To", 100),
            ("Action", "Action", 100)
        };

        private readonly IElevatorLogRepository _repository;
        private readonly BackgroundLogWriter _writer;
        private readonly DataGridView _grid = new();
        private readonly Label _info = new();
        private bool _busy;
        private bool _reloadAgain;

        public LogForm(IElevatorLogRepository repository, BackgroundLogWriter writer)
        {
            _repository = repository;
            _writer = writer;

            Text = "Elevator operation log";
            ClientSize = new Size(1000, 560);
            StartPosition = FormStartPosition.CenterParent;

            _grid.Location = new Point(12, 12);
            _grid.Size = new Size(976, 450);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoGenerateColumns = true;
            Controls.Add(_grid);

            Button refresh = MakeButton("Refresh", 12, (s, e) => Reload());
            Button clear = MakeButton("Clear Log", 142, (s, e) => ClearLog());
            Button close = MakeButton("Close", 858, (s, e) => Close());
            close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            _info.Location = new Point(280, 482);
            _info.Size = new Size(560, 24);
            _info.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(_info);

            _writer.BatchSaved += OnBatchSaved;          // live update while this window is open
            FormClosed += (s, e) => _writer.BatchSaved -= OnBatchSaved;
            Shown += (s, e) => Reload();
        }

        private Button MakeButton(string text, int x, EventHandler onClick)
        {
            var button = new Button { Text = text, Location = new Point(x, 472), Size = new Size(120, 40) };
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button.Click += onClick;
            Controls.Add(button);
            return button;
        }

        private void OnBatchSaved(object? sender, EventArgs e) => Reload();

        private void Reload()
        {
            if (_busy) { _reloadAgain = true; return; }
            SetBusy(true, "Loading log...");
            DbBackgroundRunner.Run(() => _repository.GetLogs(), ShowTable, ShowError);
        }

        private void ClearLog()
        {
            var answer = MessageBox.Show("Delete ALL log records?", "Clear Log",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes || _busy) return;

            SetBusy(true, "Clearing log...");
            DbBackgroundRunner.Run(() => { _repository.ClearLogs(); return true; },
                _ => { SetBusy(false, ""); Reload(); },
                ShowError);
        }

        private void ShowTable(DataTable table)
        {
            if (IsDisposed) return;
            _grid.DataSource = table;

            foreach (var (column, header, width) in Columns)
            {
                if (!_grid.Columns.Contains(column)) continue;
                _grid.Columns[column].HeaderText = header;
                if (column == "Action") _grid.Columns[column].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                else _grid.Columns[column].Width = width;
            }

            SetBusy(false, $"{table.Rows.Count} records");
            if (_reloadAgain) { _reloadAgain = false; Reload(); }
        }

        private void ShowError(Exception ex)
        {
            if (IsDisposed) return;
            SetBusy(false, "Database error: " + ex.Message);
        }

        private void SetBusy(bool busy, string message)
        {
            _busy = busy;
            _info.Text = message;
            UseWaitCursor = busy;
        }
    }
}
