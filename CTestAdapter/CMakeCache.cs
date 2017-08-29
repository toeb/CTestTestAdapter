using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace CTestAdapter
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
        }

        public event Action CacheChanged;

        private readonly Dictionary<string, CMakeCacheEntry> _cacheEntries;
        private FileSystemWatcher _cacheWatcher;

        private static readonly Regex CacheEntryRegex = new Regex(@"^([\w-\.]+):([^=]+)=(.*)$");

        private string _cmakeCacheFile = "CMakeCache.txt";
        private string _cmakeCacheDir;
        private FileInfo _cmakeCacheInfo;
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
        {
            get { return this["CMAKE_CTEST_COMMAND"]; }
        }

        public string this[string name]
        {
            get { return _cacheEntries.TryGetValue(name, out _tmpEntry) ? _tmpEntry.Value : string.Empty; }
        }

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

        public void ReloadCMakeCache(IFrameworkHandle log = null)
        {
            var cMakeCacheFileName = Path.Combine(CMakeCacheDir, CMakeCacheFile);
            var newInfo = new FileInfo(cMakeCacheFileName);
            if (_cmakeCacheInfo != null)
            {
                if (null != log)
                {
                    log.SendMessage(TestMessageLevel.Informational,
                        "CMakeCache.ReloadCMakeCache: comparing already loaded cache");
                }
                if (_cmakeCacheInfo.FullName == newInfo.FullName &&
                    _cmakeCacheInfo.LastWriteTime == newInfo.LastWriteTime &&
                    newInfo.Exists)
                {
                    if (null != log)
                    {
                        log.SendMessage(TestMessageLevel.Informational,
                            "CMakeCache.ReloadCMakeCache: cache did not change, not reloading");
                    }
                    return;
                }
            }
            if (null != log)
            {
                log.SendMessage(TestMessageLevel.Informational,
                    "CMakeCache.ReloadCMakeCache: reloading cmake cache from \"" + cMakeCacheFileName + "\"");
            }
            _cmakeCacheInfo = newInfo;
            _cacheEntries.Clear();
            if (!File.Exists(cMakeCacheFileName))
            {
                if (null != log)
                {
                    log.SendMessage(TestMessageLevel.Error, "cache not found at:\"" + cMakeCacheFileName + "\"");
                }
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
                    if (null != log)
                    {
                        log.SendMessage(TestMessageLevel.Error,
                            "cache load: element count != 5: (" + c.Length + ")" + line);
                    }
                    var count = 0;
                    foreach (var asdf in c)
                    {
                        if (null != log)
                        {
                            log.SendMessage(TestMessageLevel.Error, "v" + count + ": " + asdf);
                        }
                        count++;
                    }
                    continue;
                }
                CMakeCacheEntryType myType;
                if (!Enum.TryParse(c[2], out myType))
                {
                    if (null != log)
                    {
                        log.SendMessage(TestMessageLevel.Error, "cache load: error parsing enum Type: " + c[2]);
                    }
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
            if (null != CacheChanged)
            {
                CacheChanged.Invoke();
            }
        }
    }
}
