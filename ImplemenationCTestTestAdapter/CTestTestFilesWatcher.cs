using System;
using System.IO;

namespace ImplemenationCTestTestAdapter
{
    class CTestTestFilesWatcher
    {
        private FileSystemWatcher _watcher;
        private string _ctestFileName = "CTestTestfile.cmake";
        private string _ctestBaseDirectory;

        public event Action CTestFileChanged;

        public bool IsWatching => _watcher != null;

        public string CTestBaseDirectory
        {
            get { return _ctestBaseDirectory; }
            set
            {
                _ctestBaseDirectory = value;
                StartWatching();
            }
        }

        public string CTestFileName
        {
            get { return _ctestFileName; }
            set
            {
                _ctestFileName = value;
                StartWatching();
            }
        }

        public void StartWatching()
        {
            if (!Directory.Exists(_ctestBaseDirectory))
            {
                StopWatching();
                return;
            }
            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher(_ctestBaseDirectory)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    Filter = _ctestFileName
                };
                _watcher.Changed += OnChanged;
                _watcher.Renamed += OnRenamed;
            }
            else
            {
                _watcher.Path = _ctestBaseDirectory;
                _watcher.Filter = _ctestFileName;
            }
        }

        public void StopWatching()
        {
            if (_watcher == null)
            {
                return;
            }
            _watcher.Changed -= OnChanged;
            _watcher.Renamed -= OnRenamed;
            _watcher.Dispose();
            _watcher = null;
        }

        private void OnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            CTestLogger.Instance.LogMessage("CTestTestFilesWatcher: renamed " + renamedEventArgs.Name);
            CTestFileChanged?.Invoke();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            CTestLogger.Instance.LogMessage("CTestTestFilesWatcher: changed " + e.Name);
            CTestFileChanged?.Invoke();
        }
    }
}