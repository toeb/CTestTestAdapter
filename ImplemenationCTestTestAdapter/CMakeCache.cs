using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ImplemenationCTestTestAdapter
{
    public class CMakeCache
    {
        private enum CMakeCacheEntryType
        {
            // ReSharper disable InconsistentNaming
            // ReSharper disable UnusedMember.Local
            INTERNAL,
            STATIC,
            STRING,
            BOOL,
            PATH,
            FILEPATH
        }

        private class CMakeCacheEntry
        {
            public string Name;
            public string Value;
            //public CMakeCacheEntryType Type;
        }

        public event Action CacheChanged;

        private readonly Dictionary<string, CMakeCacheEntry> _cacheEntries;
        private FileSystemWatcher _cacheWatcher;

        private static readonly Regex CacheEntryRegex = new Regex(@"^([\w-\.]+):([^=]+)=(.*)$");

        private string _cmakeCacheFile = "CMakeCache.txt";
        private string _cmakeCacheDir;
        private CMakeCacheEntry _tmpEntry;

        public string CMakeCacheFile
        {
            get { return _cmakeCacheFile; }
            set
            {
                _cmakeCacheFile = value;
                StartWatching();
            }
        }

        public string CMakeCacheDir
        {
            get { return _cmakeCacheDir; }
            set
            {
                _cmakeCacheDir = value;
                StartWatching();
            }
        }

        public string CTestExecutable
            => this["CMAKE_CTEST_COMMAND"];

        public string this[string name]
            => _cacheEntries.TryGetValue(name, out _tmpEntry) ? _tmpEntry.Value : string.Empty;

        public void StartWatching()
        {
            if (!Directory.Exists(_cmakeCacheDir))
            {
                StopWatching();
                return;
            }
            if (_cacheWatcher == null)
            {
                _cacheWatcher = new FileSystemWatcher(_cmakeCacheDir)
                {
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                    Filter = _cmakeCacheFile
                };
                _cacheWatcher.Changed += OnCMakeCacheChanged;
                _cacheWatcher.Created += OnCMakeCacheChanged;
            }
            else
            {
                _cacheWatcher.Path = _cmakeCacheDir;
                _cacheWatcher.Filter = _cmakeCacheFile;
            }
            ReloadCMakeCache();
        }

        public void StopWatching()
        {
            if (null == _cacheWatcher)
            {
                return;
            }
            _cacheWatcher.Changed -= OnCMakeCacheChanged;
            _cacheWatcher.Created -= OnCMakeCacheChanged;
            _cacheWatcher.Dispose();
            _cacheWatcher = null;
        }

        public CMakeCache()
        {
            _cacheEntries = new Dictionary<string, CMakeCacheEntry>();
        }

        private void OnCMakeCacheChanged(object source, FileSystemEventArgs e)
        {
            ReloadCMakeCache();
        }

        public void ReloadCMakeCache()
        {
            _cacheEntries.Clear();
            string cMakeCacheFileName = Path.Combine(CMakeCacheDir, CMakeCacheFile);
            if (!File.Exists(cMakeCacheFileName))
            {
                CTestLogger.Instance.LogMessage("cache not found at: " + cMakeCacheFileName);
                return;
            }
            var stream = new FileStream(cMakeCacheFileName, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite);
            var r = new StreamReader(stream);
            while (!r.EndOfStream)
            {
                var line = r.ReadLine();
                if (null == line)
                {
                    continue;
                }
                line = line.TrimStart(' ');
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//"))
                {
                    continue;
                }
                var c = CacheEntryRegex.Split(line);
                if (c.Length != 5)
                {
                    CTestLogger.Instance.LogMessage("cache load: element count != 5: (" + c.Length + ")" + line);
                    int count = 0;
                    foreach (var asdf in c)
                    {
                        CTestLogger.Instance.LogMessage("v" + count + ": " + asdf);
                        count++;
                    }
                    continue;
                }
                CMakeCacheEntryType myType;
                if (!Enum.TryParse(c[2], out myType))
                {
                    CTestLogger.Instance.LogMessage("cache load: error parsing enum Type: " + c[2]);
                    continue;
                }
                var entry = new CMakeCacheEntry()
                {
                    Name = c[1],
                    //Type = myType,
                    Value = c[3]
                };
                _cacheEntries.Add(entry.Name, entry);
            }
            r.Close();
            stream.Close();
            r.Dispose();
            stream.Dispose();
            CacheChanged?.Invoke();
        }
    }
}