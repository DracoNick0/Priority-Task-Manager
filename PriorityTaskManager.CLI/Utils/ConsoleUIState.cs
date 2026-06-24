using System;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Tracks global console UI state to ensure interactive input flows are not interrupted by background operations.
    /// </summary>
    public class ConsoleUIState
    {
        private bool _isInputInProgress;
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Gets a value indicating whether the UI is currently processing interactive user input.
        /// </summary>
        public bool IsInputInProgress
        {
            get
            {
                lock (_syncRoot)
                {
                    return _isInputInProgress;
                }
            }
        }

        /// <summary>
        /// Marks that interactive input is beginning. Prevents concurrent UI operations.
        /// </summary>
        public void BeginInput()
        {
            lock (_syncRoot)
            {
                _isInputInProgress = true;
            }
        }

        /// <summary>
        /// Marks that interactive input has completed. Allows background operations to resume.
        /// </summary>
        public void EndInput()
        {
            lock (_syncRoot)
            {
                _isInputInProgress = false;
            }
        }

        /// <summary>
        /// Executes an action while input is marked as in progress, then clears the flag.
        /// </summary>
        public void ExecuteWithInputLock(Action action)
        {
            BeginInput();
            try
            {
                action();
            }
            finally
            {
                EndInput();
            }
        }
    }
}
