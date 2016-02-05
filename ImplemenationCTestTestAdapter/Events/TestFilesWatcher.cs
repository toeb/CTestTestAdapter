using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace ImplemenationCTestTestAdapter.Events
{
    [Export(typeof (ITestFilesUpdateWatcher))]
    class TestFilesWatcher : IDisposable, ITestFilesUpdateWatcher
    {
        private class FileWatcherInfo
        {
            public FileWatcherInfo(FileSystemWatcher watcher)
            {
                Watcher = watcher;
                LastEventTime = DateTime.MinValue;
            }

            public FileSystemWatcher Watcher { get; set; }
            public DateTime LastEventTime { get; set; }
        }

        private IDictionary<string, FileWatcherInfo> _fileWatchers;
        public event EventHandler<TestFileChangedEventArgs> FileChangedEvent;

        public TestFilesWatcher()
        {
            _fileWatchers = new Dictionary<string, FileWatcherInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddWatch(string path)
        {
            ValidateArg.NotNullOrEmpty(path, "path");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            var directoryName = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            FileWatcherInfo watcherInfo;
            if (_fileWatchers.TryGetValue(path, out watcherInfo))
            {
                return;
            }
            Debug.Assert(directoryName != null, "directoryName != null");
            watcherInfo = new FileWatcherInfo(new FileSystemWatcher(directoryName, fileName));
            _fileWatchers.Add(path, watcherInfo);
            watcherInfo.Watcher.Changed += OnChanged;
            watcherInfo.Watcher.EnableRaisingEvents = true;
        }

        public void RemoveWatch(string path)
        {
            ValidateArg.NotNullOrEmpty(path, "path");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            FileWatcherInfo watcherInfo;
            if (!_fileWatchers.TryGetValue(path, out watcherInfo))
            {
                return;
            }
            watcherInfo.Watcher.EnableRaisingEvents = false;
            _fileWatchers.Remove(path);
            watcherInfo.Watcher.Changed -= OnChanged;
            watcherInfo.Watcher.Dispose();
            watcherInfo.Watcher = null;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            FileWatcherInfo watcherInfo;
            if (FileChangedEvent == null || !_fileWatchers.TryGetValue(e.FullPath, out watcherInfo))
            {
                return;
            }
            var writeTime = File.GetLastWriteTime(e.FullPath);
            // Only fire update if enough time has passed since last update to prevent duplicate events
            if (!(writeTime.Subtract(watcherInfo.LastEventTime).TotalMilliseconds > 500))
            {
                return;
            }
            watcherInfo.LastEventTime = writeTime;
            FileChangedEvent(sender, new TestFileChangedEventArgs(e.FullPath, TestFileChangedReason.Changed));
        }

        public void Dispose()
        {
            Dispose(true);
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _fileWatchers == null)
            {
                return;
            }
            foreach (var fileWatcher in _fileWatchers.Values)
            {
                if (fileWatcher?.Watcher == null)
                {
                    continue;
                }
                fileWatcher.Watcher.Changed -= OnChanged;
                fileWatcher.Watcher.Dispose();
            }
            _fileWatchers.Clear();
            _fileWatchers = null;
        }
    }
}