using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace CTestAdapter.Events
{
    public class SolutionEventListener : IVsSolutionEvents, ISolutionEventsListener
    {
        private IVsSolution _solution;
        private uint _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;

        public event EventHandler SolutionUnloaded;
        public event EventHandler<SolutionEventsListenerEventArgs> SolutionProjectChanged;

        public SolutionEventListener([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            ValidateArg.NotNull(serviceProvider, "serviceProvider");
            _solution = (IVsSolution) serviceProvider.GetService(typeof(IVsSolution));

            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
            }
        }

        public void StartListeningForChanges()
        {
            if (_solution != null)
            {
                var hr = _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
                ErrorHandler.ThrowOnFailure(hr); // do nothing if this fails
            }
        }

        public void StopListeningForChanges()
        {
            if (_solutionEventsCookie != VSConstants.VSCOOKIE_NIL && _solution != null)
            {
                int hr = _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                ErrorHandler.Succeeded(hr); // do nothing if this fails

                _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;
            }
        }

        public void OnSolutionProjectUpdated(IVsProject project, SolutionChangedReason reason)
        {
            if (project == null)
            {
                return;
            }
            if (null != SolutionProjectChanged)
            {
                SolutionProjectChanged(this, new SolutionEventsListenerEventArgs(project, reason));
            }
        }

        public void OnSolutionUnloaded()
        {
            if (null != SolutionUnloaded)
            {
                SolutionUnloaded(this, new EventArgs());
            }
        }

        #region IVsSolutionEvents Members

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            var project = pRealHierarchy as IVsProject;
            OnSolutionProjectUpdated(project, SolutionChangedReason.Load);
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            var project = pRealHierarchy as IVsProject;
            OnSolutionProjectUpdated(project, SolutionChangedReason.Unload);
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            OnSolutionUnloaded();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_solution == null || _solutionEventsCookie == 0)
            {
                return;
            }
            GC.SuppressFinalize(this);
            SolutionProjectChanged = null;
            SolutionUnloaded = null;
            _solution.UnadviseSolutionEvents(_solutionEventsCookie);
            _solutionEventsCookie = 0;
            _solution = null;
        }

        #endregion
    }
}