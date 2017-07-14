using System;

namespace CTestAdapter.Events
{
    public interface ISolutionEventsListener
    {
        /// <summary>
        /// Fires an event when a project is opened/closed/loaded/unloaded
        /// </summary>
        event EventHandler<SolutionEventsListenerEventArgs> SolutionProjectChanged;

        event EventHandler SolutionUnloaded;

        void StartListeningForChanges();
        void StopListeningForChanges();
    }
}