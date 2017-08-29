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
      this._buildConfiguration = new BuildConfiguration(serviceProvider);
      this._cmakeCache = new CMakeCache();
      this._testCollector = new CTestTestCollector();
      this._testInfo = new CTestInfo();
      this._cmakeCache.CMakeCacheDir = this._buildConfiguration.SolutionDir;
      this._cmakeCache.CacheChanged += this.OnCMakeCacheChanged;
      this._cmakeCache.StartWatching();
      this._testCollector.CTestWorkingDir = this._cmakeCache.CMakeCacheDir;
      this._testCollector.CTestExecutable = this._cmakeCache.CTestExecutable;
      this._log = new CTestLogWindow
      {
        Enabled = true,
        AutoRaise = false
      };
    }

    private void OnCMakeCacheChanged()
    {
      this._testCollector.CTestExecutable = this._cmakeCache.CTestExecutable;
      this._testCollector.CTestWorkingDir = this._cmakeCache.CMakeCacheDir;
      this.ResetTestContainers();
    }

    private void UpdateListOfValidTests()
    {
      this._testCollector.CurrentActiveConfig = this._buildConfiguration.ConfigurationName;
      this._log.OutputLine("CTestContainerDiscoverer.UpdateListOfValidTests");
      this._log.OutputLine("-- working dir:" + this._testCollector.CTestWorkingDir);
      this._log.OutputLine("-- ctest:      " + this._testCollector.CTestExecutable);
      this._log.OutputLine("-- config:     " + this._testCollector.CurrentActiveConfig);
      this._log.OutputLine("-- args:       " + this._testCollector.CTestArguments);
      this._testCollector.CollectTestCases(this._testInfo);
      this._log.OutputLine("Number of Tests found by ctest -N: " + this._testInfo.Tests.Count);
      this._testInfo.WriteTestInfoFile(Path.Combine(
        this._buildConfiguration.SolutionDir, CTestInfo.CTestInfoFileName));
      // TODO only change _validTests if they really changed!!!
      foreach (var test in _testInfo.Tests)
      {
        this._log.OutputLine("valid test: " + test.Number + " := " + test.Name);
      }
      this._log.OutputLine(System.DateTime.Now.ToLongTimeString());
    }

    #region CTestContainerDiscovererBase

    protected override bool IsTestContainerFile(string file)
    {
      try
      {
        return this.TestFileExtension.Equals(
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
      var file = new FileInfo(Path.Combine(currentDir, this.TestFileName + this.TestFileExtension));
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
        res.AddRange(this.CollectCTestTestfiles(subpath));
      }
      return res;
    }

    protected override IEnumerable<string> FindTestFiles()
    {
      return this.CollectCTestTestfiles(this._cmakeCache.CMakeCacheDir);
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
      this.UpdateListOfValidTests();
    }

    #endregion
  }
}
