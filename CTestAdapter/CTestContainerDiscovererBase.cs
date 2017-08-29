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
      get { return this._executorUriString; }
      set
      {
        this._executorUriString = value;
        this.ExecutorUri = new Uri(value);
      }
    }

    public IEnumerable<ITestContainer> TestContainers
    {
      get
      {
        if (!this._initialContainerSearch)
        {
          return this._cachedContainers;
        }
        var files = this.FindTestFiles();
        this.UpdateFileWatcher(files, true);
        this._initialContainerSearch = false;
        return this._cachedContainers;
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
      this._log = new CTestLogWindow()
      {
        Enabled = true,
        AutoRaise = false
      };
      this.ExecutorUriString = executorUri;
      this._initialContainerSearch = true;
      this.ServiceProvider = serviceProvider;
      this._cachedContainers = new List<ITestContainer>();
      this._solutionListener = new SolutionEventListener(ServiceProvider);
      this._testFilesUpdateWatcher = new TestFilesWatcher();
      if (addRemoveListener == null)
      {
        addRemoveListener = new ProjectItemAddRemoveListener(serviceProvider);
      }
      this._testFilesAddRemoveListener = addRemoveListener;
      this._testFilesAddRemoveListener.TestFileChanged += this.OnTestContainerFileChanged;
      this._testFilesAddRemoveListener.StartListeningForTestFileChanges();
      this._solutionListener.SolutionUnloaded += this.OnSolutionUnloaded;
      this._solutionListener.SolutionProjectChanged += this.OnSolutionProjectChanged;
      this._solutionListener.StartListeningForChanges();
      this._testFilesUpdateWatcher.FileChangedEvent += this.OnTestContainerFileChanged;
    }

    #region TestContainerHandling

    private void UpdateFileWatcher(IEnumerable<string> files, bool isAdd)
    {
      var enumerable = files as IList<string> ?? files.ToList();
      this.TestContainersAboutToBeUpdated();
      foreach (var file in enumerable)
      {
        if (isAdd)
        {
          this._testFilesUpdateWatcher.AddWatch(file);
          this.AddTestContainerIfTestFile(file);
        }
        else
        {
          this._testFilesUpdateWatcher.RemoveWatch(file);
          this.RemoveTestContainer(file);
        }
      }
    }

    private void AddTestContainerIfTestFile(string file)
    {
      var isTestFile = this.IsTestContainerFile(file);
      this.RemoveTestContainer(file);
      if (!isTestFile)
      {
        this._log.OutputLine("CTestContainerDiscovererBase.AddTestContainerIfTestFile: not a test file: " + file);
        return;
      }
      var container = this.GetNewTestContainer(file);
      this._cachedContainers.Add(container);
    }

    private void RemoveTestContainer(string file)
    {
      var index = this._cachedContainers.FindIndex(x => x.Source.Equals(file, StringComparison.OrdinalIgnoreCase));
      if (index >= 0)
      {
        this._log.OutputLine("CTestContainerDiscovererBase.RemoveTestContainer: removing " + file);
        this._cachedContainers.RemoveAt(index);
      }
    }

    protected void ResetTestContainers()
    {
      this._log.OutputLine("CTestContainerDiscovererBase.ResetTestContainers");
      this._initialContainerSearch = true;
      this._log.OutputLine(
          "CTestContainerDiscovererBase.ResetTestContainers => CTestContainerDiscovererBase.TestContainersAboutToBeUpdated");
      this.TestContainersAboutToBeUpdated();
    }

    #endregion

    #region ListenerEvents

    private void OnSolutionUnloaded(object sender, EventArgs eventArgs)
    {
      this._log.OutputLine("CTestContainerDiscovererBase.OnSolutionUnloaded");
      this._initialContainerSearch = true;
    }

    private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
    {
      this._log.OutputLine(
          "CTestContainerDiscovererBase.OnSolutionProjectChanged (SHOULD NOT BE CALLED IN CTEST ADAPTER)");
      // this does not apply to ctest tests, as the CTestTestfile.cmake files
      // are not part of the projects.
      if (e == null)
      {
        return;
      }
      var files = this.FindTestFiles(e.Project);
      switch (e.ChangedReason)
      {
        case SolutionChangedReason.Load:
          this._log.OutputLine(
              "CTestContainerDiscovererBase.OnSolutionProjectChanged => CTestContainerDiscovererBase.UpdateFileWatcher(true)");
          this.UpdateFileWatcher(files, true);
          break;
        case SolutionChangedReason.Unload:
          this._log.OutputLine(
              "CTestContainerDiscovererBase.OnSolutionProjectChanged => CTestContainerDiscovererBase.UpdateFileWatcher(false)");
          this.UpdateFileWatcher(files, false);
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
      this._log.OutputLine("CTestContainerDiscovererBase.OnTestContainerFileChanged");
      if (e == null)
      {
        return;
      }
      // Don't do anything for files we are sure can't be test files
      if (!this.IsTestContainerFile(e.File))
      {
        return;
      }
      this._log.OutputLine("OnTestContainerFileChanged => TestContainersAboutToBeUpdated");
      this.TestContainersAboutToBeUpdated();
      switch (e.ChangedReason)
      {
        case TestFileChangedReason.Added:
          this._testFilesUpdateWatcher.AddWatch(e.File);
          this.AddTestContainerIfTestFile(e.File);
          break;
        case TestFileChangedReason.Removed:
          this._testFilesUpdateWatcher.RemoveWatch(e.File);
          this.RemoveTestContainer(e.File);
          break;
        case TestFileChangedReason.Changed:
          AddTestContainerIfTestFile(e.File);
          break;
        case TestFileChangedReason.None:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      if (this._initialContainerSearch)
      {
        return;
      }
      if (null != this.TestContainersUpdated)
      {
        this.TestContainersUpdated.Invoke(this, EventArgs.Empty);
      }
    }

    #endregion

    #region Destructors

    public void Dispose()
    {
      this.Dispose(true);
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
      if (this._testFilesUpdateWatcher != null)
      {
        this._testFilesUpdateWatcher.FileChangedEvent -= this.OnTestContainerFileChanged;
        ((IDisposable)this._testFilesUpdateWatcher).Dispose();
      }
      if (this._testFilesAddRemoveListener != null)
      {
        this._testFilesAddRemoveListener.TestFileChanged -= this.OnTestContainerFileChanged;
        this._testFilesAddRemoveListener.StopListeningForTestFileChanges();
      }
      if (this._solutionListener != null)
      {
        this._solutionListener.SolutionUnloaded -= this.OnSolutionUnloaded;
        this._solutionListener.SolutionProjectChanged -= this.OnSolutionProjectChanged;
        this._solutionListener.StopListeningForChanges();
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
