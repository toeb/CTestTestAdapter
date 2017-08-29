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
      get { return this._cmakeCacheFile; }
      set
      {
        this._cmakeCacheFile = value;
        this.StartWatching();
      }
    }

    public string CMakeCacheDir
    {
      get { return _cmakeCacheDir; }
      set
      {
        this._cmakeCacheDir = value;
        this.StartWatching();
      }
    }

    public string CTestExecutable
    {
      get { return this["CMAKE_CTEST_COMMAND"]; }
    }

    public string this[string name]
    {
      get { return this._cacheEntries.TryGetValue(name, out this._tmpEntry) ?
          this._tmpEntry.Value : string.Empty; }
    }

    public void StartWatching()
    {
      if (!Directory.Exists(this._cmakeCacheDir))
      {
        this.StopWatching();
        return;
      }
      if (this._cacheWatcher == null)
      {
        this._cacheWatcher = new FileSystemWatcher(this._cmakeCacheDir)
        {
          IncludeSubdirectories = false,
          EnableRaisingEvents = true,
          Filter = _cmakeCacheFile
        };
        this._cacheWatcher.Changed += this.OnCMakeCacheChanged;
        this._cacheWatcher.Created += this.OnCMakeCacheChanged;
      }
      else
      {
        this._cacheWatcher.Path = this._cmakeCacheDir;
        this._cacheWatcher.Filter = this._cmakeCacheFile;
      }
      this.ReloadCMakeCache();
    }

    public void StopWatching()
    {
      if (null == this._cacheWatcher)
      {
        return;
      }
      this._cacheWatcher.Changed -= this.OnCMakeCacheChanged;
      this._cacheWatcher.Created -= this.OnCMakeCacheChanged;
      this._cacheWatcher.Dispose();
      this._cacheWatcher = null;
    }

    public CMakeCache()
    {
      this._cacheEntries = new Dictionary<string, CMakeCacheEntry>();
    }

    private void OnCMakeCacheChanged(object source, FileSystemEventArgs e)
    {
      this.ReloadCMakeCache();
    }

    public void ReloadCMakeCache(IFrameworkHandle log = null)
    {
      var cMakeCacheFileName = Path.Combine(this.CMakeCacheDir, this.CMakeCacheFile);
      var newInfo = new FileInfo(cMakeCacheFileName);
      if (this._cmakeCacheInfo != null)
      {
        if (null != log)
        {
          log.SendMessage(TestMessageLevel.Informational,
              "CMakeCache.ReloadCMakeCache: comparing already loaded cache");
        }
        if (this._cmakeCacheInfo.FullName == newInfo.FullName &&
            this._cmakeCacheInfo.LastWriteTime == newInfo.LastWriteTime &&
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
      this._cmakeCacheInfo = newInfo;
      this._cacheEntries.Clear();
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
        var c = CMakeCache.CacheEntryRegex.Split(line);
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
        this._cacheEntries.Add(entry.Name, entry);
      }
      r.Close();
      stream.Close();
      r.Dispose();
      stream.Dispose();
      if (null != this.CacheChanged)
      {
        this.CacheChanged.Invoke();
      }
    }
  }
}
