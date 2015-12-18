using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Data;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    /// The CTestContainerDiscoverer connects with visual studios TestExplorer window
    /// it provides the filenames for CTestDiscoverer through the TestContainers enumerable
    /// </summary>
    [Export(typeof (ITestContainerDiscoverer))]
    public class CTestContainerDiscoverer : ITestContainerDiscoverer
    {
        [Import(typeof (SVsServiceProvider))]
        public IServiceProvider ServiceProvider { get; set; }

        [Import]
        public IUnitTestStorage Storage { get; set; }

        [ImportingConstructor]
        public CTestContainerDiscoverer()
        {
            //CTestLogger.Instance.LogMessage("initializing CTestContainerDiscoverer");
            _solutionWatcher = new CTestSolutionEventListener();
            _cmakeCache = new CMakeCache();
            _ctestFilesWatcher = new CTestTestFilesWatcher();
            _testContainers = new List<CTestContainer>();
            _testCollector = new CTestTestCollector();

            _solutionWatcher.AfterSolutionLoaded += OnSolutionOpened;
            _solutionWatcher.AfterSolutionClosed += OnSolutionClosed;
            _solutionWatcher.AfterProjectOpened += OnSolutionOpened;

            _cmakeCache.CacheChanged += UpdateTestContainers;
            _ctestFilesWatcher.CTestFileChanged += UpdateTestContainers;

            //CTestLogger.Instance.LogMessage("initializing CTestContainerDiscoverer done");
        }

        public Uri ExecutorUri => CTestExecutor.ExecutorUri;

        public IEnumerable<ITestContainer> TestContainers => _testContainers;

        private readonly CTestSolutionEventListener _solutionWatcher;
        private readonly CMakeCache _cmakeCache;
        private readonly CTestTestFilesWatcher _ctestFilesWatcher;
        private readonly List<CTestContainer> _testContainers;
        private readonly CTestTestCollector _testCollector;

        private void OnSolutionOpened()
        {
            //CTestLogger.Instance.LogMessage("CTestContainerDiscoverer:OnSolutionOpened");
            _cmakeCache.CMakeCacheDir = _solutionWatcher.SolutionDir;
            _ctestFilesWatcher.CTestBaseDirectory = _solutionWatcher.SolutionDir;
            if (string.IsNullOrEmpty(_cmakeCache.CTestExecutable))
            {
                CTestLogger.Instance.LogMessage("no ctest command found in cache, testing not enabled");
            }
            UpdateTestContainers();
        }

        private void OnSolutionClosed()
        {
            //CTestLogger.Instance.LogMessage("CTestContainerDiscoverer:OnSolutionClosed");
            _cmakeCache.StopWatching();
            _ctestFilesWatcher.StopWatching();
        }

        private void UpdateTestContainers()
        {
            CTestLogger.Instance.LogMessage("CTestContainerDiscoverer:UpdateTestContainers");
            _solutionWatcher.UpdateCurrentConfigurationName();
            _testCollector.CTestExecutable = _cmakeCache.CTestExecutable;
            _testCollector.CTestWorkingDir = _cmakeCache.CMakeCacheDir;
            _testCollector.CurrentActiveConfig = _solutionWatcher.CurrentConfigurationName;
            _testCollector.CollectTestCases();
            _testContainers.Clear();
            var containerFileName =
                Path.Combine(_solutionWatcher.SolutionDir, "CTestAdapter", "container.ctest");
            var container = new CTestContainer(this, containerFileName)
            {
                CTestList = _testCollector.CTestNames,
                CTestExecutable = _cmakeCache.CTestExecutable,
                CTestWorkingDirectory = _cmakeCache.CMakeCacheDir
            };
            if (!container.SaveContainer())
            {
                CTestLogger.Instance.LogMessage("error writing ctest container to disk: " +
                                                container.Source);
                return;
            }
            _testContainers.Add(container);
            TestContainersUpdated?.Invoke(this, null);
            CTestLogger.Instance.LogMessage("CTestContainerDiscoverer:UpdateTestContainers done");
        }

        public event EventHandler TestContainersUpdated;
    }
}