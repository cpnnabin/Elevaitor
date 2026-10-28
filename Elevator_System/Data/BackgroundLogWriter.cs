using System.Collections.Concurrent;
using System.ComponentModel;

namespace Lift.Data
{
    /// <summary>
    /// Keeps the database away from the GUI. The lifts call Write() (instant - it only adds to a queue);
    /// one BackgroundWorker takes entries off the queue and saves them in batches.
    /// </summary>
    public sealed class BackgroundLogWriter : ILogWriter
    {
        private readonly IElevatorLogRepository _repository;
        private readonly BlockingCollection<ElevatorLogEntry> _queue = new();
        private readonly BackgroundWorker _worker = new() { WorkerReportsProgress = true };
        private readonly ManualResetEventSlim _finished = new(false);

        public event EventHandler? BatchSaved;               // raised on the GUI thread
        public event EventHandler<string>? WriteFailed;      // raised on the GUI thread

        public BackgroundLogWriter(IElevatorLogRepository repository)
        {
            _repository = repository;
            _worker.DoWork += OnDoWork;
            _worker.ProgressChanged += OnProgressChanged;
            _worker.RunWorkerAsync();
        }

        public void Write(ElevatorLogEntry entry)
        {
            try { _queue.Add(entry); }
            catch (InvalidOperationException) { /* already stopped - ignore */ }
        }

        /// <summary>Finish saving what is queued, then stop the worker.</summary>
        public void Stop(int timeoutMs = 5000)
        {
            _queue.CompleteAdding();
            _finished.Wait(timeoutMs);
        }

        private void OnDoWork(object? sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender!;
            try
            {
                foreach (ElevatorLogEntry first in _queue.GetConsumingEnumerable())
                {
                    var batch = new List<ElevatorLogEntry> { first };
                    while (batch.Count < 50 && _queue.TryTake(out ElevatorLogEntry? next)) batch.Add(next);

                    try
                    {
                        _repository.Save(batch);
                        worker.ReportProgress(0, batch.Count);       // int  = saved
                    }
                    catch (Exception ex)
                    {
                        worker.ReportProgress(0, ex.Message);        // text = failed
                    }
                }
            }
            finally
            {
                _finished.Set();
            }
        }

        private void OnProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string error) WriteFailed?.Invoke(this, error);
            else BatchSaved?.Invoke(this, EventArgs.Empty);
        }
    }
}
