using System.ComponentModel;

namespace Lift.Data
{
    /// <summary>
    /// The ONE place where a database job is moved off the GUI thread (BackgroundWorker).
    /// DoWork runs on a worker thread; the two callbacks run back on the thread that called Run
    /// (the GUI thread), so they may touch controls.
    /// </summary>
    public static class DbBackgroundRunner
    {
        public static void Run<T>(Func<T> work, Action<T> onSuccess, Action<Exception> onError)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) => e.Result = work();
            worker.RunWorkerCompleted += (s, e) =>
            {
                try
                {
                    if (e.Error != null) onError(e.Error);
                    else onSuccess((T)e.Result!);
                }
                finally
                {
                    worker.Dispose();
                }
            };
            worker.RunWorkerAsync();
        }
    }
}
