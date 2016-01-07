#define USE_LOGGING


#if USE_LOGGING
using System;
using System.Globalization;
using System.IO;

#endif

namespace ImplemenationCTestTestAdapter
{
    public sealed class CTestLogger
    {
#if USE_LOGGING
        private readonly string _logFileName;
        private StreamWriter _w;
#endif

        public static CTestLogger Instance { get; } = new CTestLogger();

        private CTestLogger()
        {
#if USE_LOGGING
            const string logDir = "C:\\tmp";
            const string baseName = "ctest_log_";
            var count = 0;
            var fileNameBase = Path.Combine(logDir, baseName);
            while (File.Exists(fileNameBase + count + ".log"))
            {
                count++;
            }
            _logFileName = fileNameBase + count + ".log";
#endif
        }

#if USE_LOGGING
        ~CTestLogger()
        {
            TryCloseFile();
        }
#endif

#if USE_LOGGING
        private void TryOpenFile(bool append = true)
        {
            if (null == _w)
            {
                _w = new StreamWriter(_logFileName, append);
                var friendlyName = AppDomain.CurrentDomain.FriendlyName;
                LogMessage($"assembly1: {friendlyName}");
                friendlyName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                LogMessage($"assembly2:{friendlyName}");
            }
        }

        private void TryCloseFile()
        {
            _w?.Close();
            _w = null;
        }
#endif

        public void LogMessage(string msg)
        {
#if USE_LOGGING
            TryOpenFile();
            _w.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ": " + msg);
            _w.Flush();
#endif
        }
    }
}