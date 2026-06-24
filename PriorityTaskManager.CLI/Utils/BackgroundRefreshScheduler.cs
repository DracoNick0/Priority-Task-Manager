using System;
using System.Threading;
using System.Threading.Tasks;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Runs a background task that refreshes the schedule snapshot at quarter-hour boundaries (XX:00, XX:15, XX:30, XX:45).
    /// </summary>
    public class BackgroundRefreshScheduler
    {
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _refreshTask;

        public BackgroundRefreshScheduler(ScheduleSnapshotProvider snapshotProvider)
        {
            _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
        }

        /// <summary>
        /// Starts the background refresh scheduler. Aligns to the next quarter-hour boundary and repeats every 15 minutes.
        /// </summary>
        public void Start()
        {
            if (_refreshTask != null && !_refreshTask.IsCompleted)
            {
                return; // Already running
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _refreshTask = Task.Run(() => RefreshLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops the background refresh scheduler.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            try
            {
                _refreshTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        private void RefreshLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    var nextRefreshTime = CalculateNextQuarterHourBoundary(now);
                    var delay = nextRefreshTime - now;

                    if (delay.TotalMilliseconds > 0)
                    {
                        try
                        {
                            Task.Delay(delay, cancellationToken).Wait(cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _snapshotProvider.RefreshActiveListSnapshot(out _);
                    }
                }
            }
            catch
            {
                // Silently fail background task to not crash the main loop
            }
        }

        private static DateTime CalculateNextQuarterHourBoundary(DateTime now)
        {
            int minute = now.Minute;
            int nextQuarter = (minute / 15 + 1) * 15;
            
            if (nextQuarter >= 60)
            {
                return now.AddHours(1).Date.AddHours(now.Hour + 1);
            }

            return now.Date.AddHours(now.Hour).AddMinutes(nextQuarter);
        }
    }
}
