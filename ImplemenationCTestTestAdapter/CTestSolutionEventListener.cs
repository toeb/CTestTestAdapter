using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;

namespace ImplemenationCTestTestAdapter
{
    public class CTestSolutionEventListener : IVsSolutionEvents, IDisposable
    {
        private DTE2 _dte;
        private IVsSolution _solution;
        private uint _solutionEventsCookie;
        private string _currentActiveConfigurationName = "Debug";

        public event Action AfterSolutionLoaded;
        public event Action AfterProjectOpened;

        public event Action BeforeSolutionClosed;
        public event Action AfterSolutionClosed;

        public string SolutionFile => null != _dte ? _dte.Solution.FileName : string.Empty;

        public string SolutionDir => null != _dte ? Path.GetDirectoryName(_dte.Solution.FileName) : string.Empty;

        public string CurrentConfigurationName => _currentActiveConfigurationName;

        public CTestSolutionEventListener()
        {
            _solution = (IVsSolution) ServiceProvider.GlobalProvider.GetService(typeof (IVsSolution));
            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
            }
            _dte = (DTE2) ServiceProvider.GlobalProvider.GetService(typeof (DTE));
            if (_dte == null)
            {
                CTestLogger.Instance.LogMessage("CTestSolutionEventListener: DTE object not found!");
            }
        }

        public void UpdateCurrentConfigurationName()
        {
            _currentActiveConfigurationName = BuildConfiguration.GetCurrentActiveConfiguration();
        }

        #region IVsSolutionEvents Members

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            AfterSolutionClosed?.Invoke();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            AfterProjectOpened?.Invoke();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            UpdateCurrentConfigurationName();
            AfterSolutionLoaded?.Invoke();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            BeforeSolutionClosed?.Invoke();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
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
            if (_solution != null && _solutionEventsCookie != 0)
            {
                GC.SuppressFinalize(this);
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                AfterSolutionLoaded = null;
                BeforeSolutionClosed = null;
                AfterSolutionClosed = null;
                AfterProjectOpened = null;
                _solutionEventsCookie = 0;
                _solution = null;
                _dte = null;
            }
        }

        #endregion
    }
}