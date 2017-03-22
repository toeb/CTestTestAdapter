using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CTestAdapter.Events;

namespace CTestAdapter
{
    [Export(typeof(ITestContainerDiscoverer))]
    public class CTestContainerDiscoverer : CTestContainerDiscovererBase
    {
        public string TestFileExtension
        {
            get { return ".cmake"; }
        }

        public string TestFileName
        {
            get { return "CTestTestfile"; }
        }

        private readonly CTestLogWindow _log;
        private readonly BuildConfiguration _buildConfiguration;
        private readonly CMakeCache _cmakeCache;
        private readonly CTestTestCollector _testCollector;
        private readonly CTestInfo _testInfo;

        [ImportingConstructor]
        public CTestContainerDiscoverer(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider
            )
            : base(serviceProvider, new CTestTestfileAddRemoveListener(), CTestExecutor.ExecutorUriString)
        {
            _buildConfiguration = new BuildConfiguration(serviceProvider);
            _cmakeCache = new CMakeCache();
            _testCollector = new CTestTestCollector();
            _testInfo = new CTestInfo();
            _cmakeCache.CMakeCacheDir = _buildConfiguration.SolutionDir;
            _cmakeCache.CacheChanged += OnCMakeCacheChanged;
            _cmakeCache.StartWatching();
            _testCollector.CTestWorkingDir = _cmakeCache.CMakeCacheDir;
            _testCollector.CTestExecutable = _cmakeCache.CTestExecutable;
            _log = new CTestLogWindow
            {
                Enabled = true,
                AutoRaise = false
            };
        }

        private void OnCMakeCacheChanged()
        {
            _testCollector.CTestExecutable = _cmakeCache.CTestExecutable;
            _testCollector.CTestWorkingDir = _cmakeCache.CMakeCacheDir;
            ResetTestContainers();
        }

        private void UpdateListOfValidTests()
        {
            _testCollector.CurrentActiveConfig = _buildConfiguration.ConfigurationName;
            _log.OutputLine("CTestContainerDiscoverer.UpdateListOfValidTests");
            _log.OutputLine("-- working dir:" + _testCollector.CTestWorkingDir);
            _log.OutputLine("-- ctest:      " + _testCollector.CTestExecutable);
            _log.OutputLine("-- config:     " + _testCollector.CurrentActiveConfig);
            _log.OutputLine("-- args:       " + _testCollector.CTestArguments);
            _testCollector.CollectTestCases(_testInfo);
            _log.OutputLine("Number of Tests found by ctest -N: " + _testInfo.Tests.Count);
            _testInfo.WriteTestInfoFile(Path.Combine(_buildConfiguration.SolutionDir, CTestInfo.CTestInfoFileName));
            // TODO only change _validTests if they really changed!!!
            foreach (var test in _testInfo.Tests)
            {
                _log.OutputLine("valid test: " + test.Number + " := " + test.Name);
            }
            _log.OutputLine(System.DateTime.Now.ToLongTimeString());
        }

        #region CTestContainerDiscovererBase

        protected override bool IsTestContainerFile(string file)
        {
            try
            {
                return TestFileExtension.Equals(
                    Path.GetExtension(file),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception /*e*/)
            {
                // TODO do some messaging here or so ...
            }
            return false;
        }

        private IEnumerable<string> CollectCTestTestfiles(string currentDir)
        {
            var file = new FileInfo(Path.Combine(currentDir, TestFileName + TestFileExtension));
            if (!file.Exists)
            {
                return Enumerable.Empty<string>();
            }
            var res = new List<string>();
            var content = file.OpenText().ReadToEnd();
            var matches = Regex.Matches(content, @".*[sS][uB][bB][dD][iI][rR][sS]\s*\((?<subdir>.*)\)");
            var subdirs = (from Match match in matches select match.Groups["subdir"].Value).ToList();
            if (content.Contains("add_test"))
            {
                res.Add(file.FullName);
            }
            foreach (var dir in subdirs)
            {
                var subpath = dir.Trim('\"');
                subpath = Path.Combine(currentDir, subpath);
                res.AddRange(CollectCTestTestfiles(subpath));
            }
            return res;
        }

        protected override IEnumerable<string> FindTestFiles()
        {
            return CollectCTestTestfiles(_cmakeCache.CMakeCacheDir);
        }

        protected override IEnumerable<string> FindTestFiles(IVsProject project)
        {
            // we don't want to react to loading/unloading of projects 
            // within the solution. Test discovery is only done using
            // ctest
            return new List<string>();
        }

        protected override ITestContainer GetNewTestContainer(string s)
        {
            return new CTestContainer(this, s);
        }

        protected override void TestContainersAboutToBeUpdated()
        {
            UpdateListOfValidTests();
        }

        #endregion
    }
}