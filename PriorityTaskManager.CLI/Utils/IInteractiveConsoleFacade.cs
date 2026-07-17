using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Defines console and menu operations used by interactive CLI command flows.
    /// </summary>
    public interface IInteractiveConsoleFacade
    {
        /// <summary>
        /// Gets or sets whether the cursor is visible.
        /// </summary>
        bool CursorVisible { get; set; }

        /// <summary>
        /// Gets the current cursor top position.
        /// </summary>
        int CursorTop { get; }

        /// <summary>
        /// Reads a key from console input.
        /// </summary>
        ConsoleKeyInfo ReadKey(bool intercept);

        /// <summary>
        /// Reads a line from console input.
        /// </summary>
        string? ReadLine();

        /// <summary>
        /// Writes text to the output.
        /// </summary>
        void Write(string text);

        /// <summary>
        /// Writes a line of text to the output.
        /// </summary>
        void WriteLine(string text);

        /// <summary>
        /// Clears the terminal and renders the dashboard.
        /// </summary>
        void ClearAndRenderDashboard(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService);

        /// <summary>
        /// Draws menu items for a selectable list.
        /// </summary>
        void DrawMenuItems(IReadOnlyList<string> items, int selectedIndex, int startLine);

        /// <summary>
        /// Updates menu line highlighting after selection movement.
        /// </summary>
        void UpdateMenuSelection(IReadOnlyList<string> items, int previousIndex, int selectedIndex, int startLine);

        /// <summary>
        /// Prompts for inline text input on a specific row and supports confirm or cancel.
        /// </summary>
        bool TryPromptInlineInput(int row, string prefix, string initialValue, out string value);

        /// <summary>
        /// Runs a toggle selection menu for multi-select values.
        /// </summary>
        bool RunToggleSelectionMenu<T>(string title, string instructions, IList<T> selectedItems, IReadOnlyList<T> allItems, Func<T, string> labelSelector);

        /// <summary>
        /// Runs an adjustable value menu.
        /// </summary>
        void RunAdjustableValueMenu(string title, string instructions, List<ConsoleMenuHelper.AdjustableMenuOption> options);
    }
}