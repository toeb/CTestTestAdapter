using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ImplemenationCTestTestAdapter.Events;

namespace ImplemenationCTestTestAdapter
{
    [Export(typeof (ITestContainerDiscoverer))]
    public class CTestContainerDiscoverer : CTestContainerDiscovererBase
    {
        public string TestFileExtension => ".cmake";
        public string TestFileName => "CTestTestfile";

        private readonly CTestLogWindow _log;
        private readonly BuildConfiguration _buildConfiguration;
        private readonly CMakeCache _cmakeCache;
        private readonly CTestTestCollector _testCollector;
        private readonly CTestInfo _testInfo;

        [ImportingConstructor]
        public CTestContainerDiscoverer(
            [Import(typeof (SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(serviceProvider, new CTestTestfileAddRemoveListener(), CTestExecutor.ExecutorUriString)
        {
            _log = new CTestLogWindow
            {
                Enabled = true,
                AutoRaise = false
            };
            _buildConfiguration = new BuildConfiguration(serviceProvider);
            _cmakeCache = new CMakeCache();
            _testCollector = new CTestTestCollector();
            _testInfo = new CTestInfo();
            _cmakeCache.CMakeCacheDir = _buildConfiguration.SolutionDir;
            _cmakeCache.CacheChanged += OnCMakeCacheChanged;
            _cmakeCache.StartWatching();
            _testCollector.CTestWorkingDir = _cmakeCache.CMakeCacheDir;
            _testCollector.CTestExecutable = _cmakeCache.CTestExecutable;
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
            _log.OutputLine($"-- working dir:{_testCollector.CTestWorkingDir}");
            _log.OutputLine($"-- ctest:      {_testCollector.CTestExecutable}");
            _log.OutputLine($"-- config:     {_testCollector.CurrentActiveConfig}");
            _log.OutputLine($"-- args:       {_testCollector.CTestArguments}");
            _testCollector.CollectTestCases(_testInfo);
            _log.OutputLine($"=> {_testInfo.Tests.Count}");
            _testInfo.WriteTestInfoFile(Path.Combine(_buildConfiguration.SolutionDir, CTestInfo.CTestInfoFileName));
            // TODO only change _validTests if they really changed!!!
            foreach (var test in _testInfo.Tests)
            {
                _log.OutputLine($"valid test: {test.Number} := {test.Name}");
            }
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
#if false
            _log.OutputLine($"CTestContainerDiscoverer.CollectCTestTestfiles({currentDir})");
#endif
            var file = new FileInfo(Path.Combine(currentDir, TestFileName + TestFileExtension));
            if (!file.Exists)
            {
#if false
                _log.OutputLine("CTestContainerDiscoverer.CollectCTestTestfiles(no file)");
#endif
                return Enumerable.Empty<string>();
            }
            var content = file.OpenText().ReadToEnd();
            var matches = Regex.Matches(content, @".*[sS][uB][bB][dD][iI][rR][sS]\s*\((?<subdir>.*)\)");
            var subdirs = (from Match match in matches select match.Groups["subdir"].Value).ToList();
            if (content.Contains("add_test"))
            {
#if false
                _log.OutputLine($"CTestContainerDiscoverer.CollectCTestTestfiles: {file.DirectoryName}");
#endif
                if (subdirs.Count == 0)
                {
                    return Enumerable.Repeat(file.FullName, 1);
                }
                if (subdirs.Count > 0)
                {
#if false
                    _log.OutputLine("CTestContainerDiscoverer.CollectCTestTestfiles: recurse");
#endif
                    return subdirs
                        .SelectMany(d => CollectCTestTestfiles(Path.Combine(currentDir, d)))
                        .Concat(Enumerable.Repeat(file.FullName, 1));
                }
            }
#if false
            _log.OutputLine("CTestContainerDiscoverer.CollectCTestTestfiles: NO tests found");
            _log.OutputLine("CTestContainerDiscoverer.CollectCTestTestfiles: recurse");
#endif
            return subdirs
                .SelectMany(d => CollectCTestTestfiles(Path.Combine(currentDir, d)));
        }

        protected override IEnumerable<string> FindTestFiles()
        {
#if false
            _log.OutputLine("CTestContainerDiscoverer.FindTestFiles START");
#endif
            var res = CollectCTestTestfiles(_cmakeCache.CMakeCacheDir);
#if false
            _log.OutputLine("CTestContainerDiscoverer.FindTestFiles END");
#endif
            return res;
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
#if false
            _log.OutputLine($"CTestContainerDiscoverer.GetNewTestContainer: (try) \"{s}\"");
#endif
            return new CTestContainer(this, s);
        }

        protected override void TestContainersAboutToBeUpdated()
        {
            UpdateListOfValidTests();
        }

#endregion
    }
}