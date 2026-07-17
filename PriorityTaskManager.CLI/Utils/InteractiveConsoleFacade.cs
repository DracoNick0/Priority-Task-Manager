using PriorityTaskManager.Services;
using System.Runtime.Versioning;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Default console facade for interactive CLI command flows.
    /// </summary>
    public sealed class InteractiveConsoleFacade : IInteractiveConsoleFacade
    {
        /// <inheritdoc/>
        [SupportedOSPlatform("windows")]
        public bool CursorVisible
        {
            get => Console.CursorVisible;
            set => Console.CursorVisible = value;
        }

        /// <inheritdoc/>
        public int CursorTop => Console.CursorTop;

        /// <inheritdoc/>
        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }

        /// <inheritdoc/>
        public string? ReadLine()
        {
            return Console.ReadLine();
        }

        /// <inheritdoc/>
        public void Write(string text)
        {
            Console.Write(text);
        }

        /// <inheritdoc/>
        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        /// <inheritdoc/>
        public void ClearAndRenderDashboard(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            ConsoleHelper.ClearAndRenderDashboard(snapshotProvider, taskMetricsService);
        }

        /// <inheritdoc/>
        public void DrawMenuItems(IReadOnlyList<string> items, int selectedIndex, int startLine)
        {
            ConsoleMenuHelper.DrawMenuItems(items, selectedIndex, startLine);
        }

        /// <inheritdoc/>
        public void UpdateMenuSelection(IReadOnlyList<string> items, int previousIndex, int selectedIndex, int startLine)
        {
            ConsoleMenuHelper.UpdateMenuSelection(items, previousIndex, selectedIndex, startLine);
        }
    }
}