using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using CTestAdapter.Events;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace CTestAdapter
{
  public class CTestContainerDiscovererBase : ITestContainerDiscoverer
  {
    public event EventHandler TestContainersUpdated;

    public Uri ExecutorUri { get; private set; }

    public string ExecutorUriString
    {
      get { return _executorUriString; }
      set
      {
        _executorUriString = value;
        ExecutorUri = new Uri(value);
      }
    }

    public IEnumerable<ITestContainer> TestContainers
    {
      get
      {
        if (!_initialContainerSearch)
        {
          return _cachedContainers;
        }
        var files = FindTestFiles();
        UpdateFileWatcher(files, true);
        _initialContainerSearch = false;
        return _cachedContainers;
      }
    }

    #region Members

    protected readonly IServiceProvider ServiceProvider;
    private readonly CTestLogWindow _log;
    private readonly List<ITestContainer> _cachedContainers;
    private string _executorUriString;
    private bool _initialContainerSearch;
    private readonly ISolutionEventsListener _solutionListener;
    private readonly ITestFileAddRemoveListener _testFilesAddRemoveListener;
    private readonly ITestFilesUpdateWatcher _testFilesUpdateWatcher;

    #endregion

    [ImportingConstructor]
    public CTestContainerDiscovererBase(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        ITestFileAddRemoveListener addRemoveListener,
        string executorUri)
    {
      ValidateArg.NotNull(serviceProvider, "serviceProvider");
      _log = new CTestLogWindow()
      {
        Enabled = true,
        AutoRaise = false
      };
      ExecutorUriString = executorUri;
      _initialContainerSearch = true;
      ServiceProvider = serviceProvider;
      _cachedContainers = new List<ITestContainer>();
      _solutionListener = new SolutionEventListener(ServiceProvider);
      _testFilesUpdateWatcher = new TestFilesWatcher();
      if (addRemoveListener == null)
      {
        addRemoveListener = new ProjectItemAddRemoveListener(serviceProvider);
      }
      _testFilesAddRemoveListener = addRemoveListener;
      _testFilesAddRemoveListener.TestFileChanged += OnTestContainerFileChanged;
      _testFilesAddRemoveListener.StartListeningForTestFileChanges();
      _solutionListener.SolutionUnloaded += OnSolutionUnloaded;
      _solutionListener.SolutionProjectChanged += OnSolutionProjectChanged;
      _solutionListener.StartListeningForChanges();
      _testFilesUpdateWatcher.FileChangedEvent += OnTestContainerFileChanged;
    }

    #region TestContainerHandling

    private void UpdateFileWatcher(IEnumerable<string> files, bool isAdd)
    {
      var enumerable = files as IList<string> ?? files.ToList();
      TestContainersAboutToBeUpdated();
      foreach (var file in enumerable)
      {
        if (isAdd)
        {
          _testFilesUpdateWatcher.AddWatch(file);
          AddTestContainerIfTestFile(file);
        }
        else
        {
          _testFilesUpdateWatcher.RemoveWatch(file);
          RemoveTestContainer(file);
        }
      }
    }

    private void AddTestContainerIfTestFile(string file)
    {
      var isTestFile = IsTestContainerFile(file);
      RemoveTestContainer(file);
      if (!isTestFile)
      {
        _log.OutputLine("CTestContainerDiscovererBase.AddTestContainerIfTestFile: not a test file: " + file);
        return;
      }
      var container = GetNewTestContainer(file);
      _cachedContainers.Add(container);
    }

    private void RemoveTestContainer(string file)
    {
      var index = _cachedContainers.FindIndex(x => x.Source.Equals(file, StringComparison.OrdinalIgnoreCase));
      if (index >= 0)
      {
        _log.OutputLine("CTestContainerDiscovererBase.RemoveTestContainer: removing " + file);
        _cachedContainers.RemoveAt(index);
      }
    }

    protected void ResetTestContainers()
    {
      _log.OutputLine("CTestContainerDiscovererBase.ResetTestContainers");
      _initialContainerSearch = true;
      _log.OutputLine(
          "CTestContainerDiscovererBase.ResetTestContainers => CTestContainerDiscovererBase.TestContainersAboutToBeUpdated");
      TestContainersAboutToBeUpdated();
    }

    #endregion

    #region ListenerEvents

    private void OnSolutionUnloaded(object sender, EventArgs eventArgs)
    {
      _log.OutputLine("CTestContainerDiscovererBase.OnSolutionUnloaded");
      _initialContainerSearch = true;
    }

    private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
    {
      _log.OutputLine(
          "CTestContainerDiscovererBase.OnSolutionProjectChanged (SHOULD NOT BE CALLED IN CTEST ADAPTER)");
      // this does not apply to ctest tests, as the CTestTestfile.cmake files
      // are not part of the projects.
      if (e == null)
      {
        return;
      }
      var files = FindTestFiles(e.Project);
      switch (e.ChangedReason)
      {
        case SolutionChangedReason.Load:
          _log.OutputLine(
              "CTestContainerDiscovererBase.OnSolutionProjectChanged => CTestContainerDiscovererBase.UpdateFileWatcher(true)");
          UpdateFileWatcher(files, true);
          break;
        case SolutionChangedReason.Unload:
          _log.OutputLine(
              "CTestContainerDiscovererBase.OnSolutionProjectChanged => CTestContainerDiscovererBase.UpdateFileWatcher(false)");
          UpdateFileWatcher(files, false);
          break;
        case SolutionChangedReason.None:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      // Do not fire TestContainersUpdated event here.
      // This will cause us to fire this event too early before the UTE is ready to process containers and will result in an exception.
      // The UTE will query all the TestContainerDiscoverers once the solution is loaded.
    }

    private void OnTestContainerFileChanged(object sender, TestFileChangedEventArgs e)
    {
      _log.OutputLine("CTestContainerDiscovererBase.OnTestContainerFileChanged");
      if (e == null)
      {
        return;
      }
      // Don't do anything for files we are sure can't be test files
      if (!IsTestContainerFile(e.File))
      {
        return;
      }
      _log.OutputLine("OnTestContainerFileChanged => TestContainersAboutToBeUpdated");
      TestContainersAboutToBeUpdated();
      switch (e.ChangedReason)
      {
        case TestFileChangedReason.Added:
          _testFilesUpdateWatcher.AddWatch(e.File);
          AddTestContainerIfTestFile(e.File);
          break;
        case TestFileChangedReason.Removed:
          _testFilesUpdateWatcher.RemoveWatch(e.File);
          RemoveTestContainer(e.File);
          break;
        case TestFileChangedReason.Changed:
          AddTestContainerIfTestFile(e.File);
          break;
        case TestFileChangedReason.None:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      if (_initialContainerSearch)
      {
        return;
      }
      if (null != TestContainersUpdated)
      {
        TestContainersUpdated.Invoke(this, EventArgs.Empty);
      }
    }

    #endregion

    #region Destructors

    public void Dispose()
    {
      Dispose(true);
      // Use SupressFinalize in case a subclass
      // of this type implements a finalizer.
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing)
      {
        return;
      }
      if (_testFilesUpdateWatcher != null)
      {
        _testFilesUpdateWatcher.FileChangedEvent -= OnTestContainerFileChanged;
        ((IDisposable)_testFilesUpdateWatcher).Dispose();
      }
      if (_testFilesAddRemoveListener != null)
      {
        _testFilesAddRemoveListener.TestFileChanged -= OnTestContainerFileChanged;
        _testFilesAddRemoveListener.StopListeningForTestFileChanges();
      }
      if (_solutionListener != null)
      {
        _solutionListener.SolutionUnloaded -= OnSolutionUnloaded;
        _solutionListener.SolutionProjectChanged -= OnSolutionProjectChanged;
        _solutionListener.StopListeningForChanges();
      }
    }

    #endregion

    #region VirtualInterface

    protected virtual bool IsTestContainerFile(string file)
    {
      throw new Exception("IsTestContainerFile not implemented");
    }

    protected virtual IEnumerable<string> FindTestFiles()
    {
      throw new Exception("FindTestFiles not implemented");
    }

    protected virtual IEnumerable<string> FindTestFiles(IVsProject project)
    {
      throw new Exception("FindTestFiles(project) not implemented");
    }

    protected virtual ITestContainer GetNewTestContainer(string s)
    {
      throw new Exception("GetNewTestContainer not implemented");
    }

    protected virtual void TestContainersAboutToBeUpdated()
    {
    }

    #endregion
  }
}
