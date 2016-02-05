using System;
using System.ComponentModel.Composition;
using System.IO;

namespace ImplemenationCTestTestAdapter.Events
{
    [Export(typeof (ITestFileAddRemoveListener))]
    public sealed class CTestTestfileAddRemoveListener : IDisposable,
        ITestFileAddRemoveListener
    {
        private readonly FileSystemWatcher _watcher;

        public string Path
        {
            get { return _watcher.Path; }
            set
            {
                if (_watcher.EnableRaisingEvents)
                {
                    _watcher.EnableRaisingEvents = Directory.Exists(value);
                }
                _watcher.Path = value;
            }
        }

        public bool Recurse
        {
            get { return _watcher.IncludeSubdirectories; }
            set { _watcher.IncludeSubdirectories = value; }
        }

        public string Filter
        {
            get { return _watcher.Filter; }
            set { _watcher.Filter = value; }
        }

        public event EventHandler<TestFileChangedEventArgs> TestFileChanged;

        [ImportingConstructor]
        public CTestTestfileAddRemoveListener()
        {
            _watcher = new FileSystemWatcher
            {
                EnableRaisingEvents = false,
                IncludeSubdirectories = true,
                Filter = "CTestTestfile.cmake",
            };
            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
        }

        public void StartListeningForTestFileChanges()
        {
            _watcher.EnableRaisingEvents = Directory.Exists(Path);
        }

        public void StopListeningForTestFileChanges()
        {
            _watcher.EnableRaisingEvents = false;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            TestFileChanged?.Invoke(this,
                new TestFileChangedEventArgs(
                    e.FullPath,
                    TestFileChangedReason.Added));
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            TestFileChanged?.Invoke(this,
                new TestFileChangedEventArgs(
                    e.FullPath,
                    TestFileChangedReason.Removed))
                ;
        }

        public void Dispose()
        {
            Dispose(true);
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            StopListeningForTestFileChanges();
            _watcher.Created -= OnCreated;
            _watcher.Deleted -= OnDeleted;
            _watcher.Dispose();
        }
    }
}