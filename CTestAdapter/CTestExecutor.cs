using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace CTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class CTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://CTestExecutor/v1";

        private const string RegexFieldOutput = "output";
        private const string RegexFieldDuration = "duration";

        private static readonly Regex RegexOutput =
            new Regex(@"Output:\r\n-+\r\n(?<" + RegexFieldOutput + @">.*)\r\n<end of output>\r\n",
                RegexOptions.Singleline);

        private static readonly Regex RegexDuration =
            new Regex(@"<end of output>\r\nTest time =\s+(?<" + RegexFieldDuration + @">[\d\.]+) sec\r\n",
                RegexOptions.Singleline);

        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public bool EnableLogging { get; set; }

        private bool _runningFromSources;

        private bool _cancelled;
        private string _solutionDir;
        private readonly CMakeCache _cmakeCache;
        private readonly BuildConfiguration _buildConfiguration;
        private readonly CTestInfo _ctestInfo;

        public CTestExecutor()
        {
            EnableLogging = true;
            _runningFromSources = false;
            _buildConfiguration = new BuildConfiguration();
            _solutionDir = _buildConfiguration.SolutionDir;
            _cmakeCache = new CMakeCache
            {
                CMakeCacheDir = _solutionDir
            };
            _ctestInfo = new CTestInfo();
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        private void TryUpdateCacheDir(string source, IMessageLogger log)
        {
            if (_cmakeCache.CMakeCacheDir.Length != 0)
            {
                return;
            }
            var info = new FileInfo(source);
            if (info.Exists)
            {
                _cmakeCache.CMakeCacheDir =
                    FindRootOfCTestTestfile(info.DirectoryName, log);
            }
        }

        private string FindRootOfCTestTestfile(string directory, IMessageLogger log)
        {
            while (true)
            {
                var info = new DirectoryInfo(directory);
                if (File.Exists(info.FullName + "\\" + _cmakeCache.CMakeCacheFile))
                {
                    _solutionDir = info.FullName;
                    return info.FullName;
                }
                if (!info.Exists)
                {
                    return string.Empty;
                }
                if (info.Parent == null)
                {
                    return string.Empty;
                }
                directory = info.Parent.FullName;
            }
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var enumerable = sources as IList<string> ?? sources.ToList();
            TryUpdateCacheDir(enumerable.First(), frameworkHandle);
            _runningFromSources = true;
            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                "CTestExecutor.RunTests(src)");
            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                "CTestExecutor.RunTests: ctest (" + _cmakeCache.CTestExecutable + ")");
            var logFileDir = _cmakeCache.CMakeCacheDir + "\\Testing\\Temporary";
            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                "CTestExecutor.RunTests: logs are written to (file://" + logFileDir + ")");
            var testInfoFilename = Path.Combine(_solutionDir, CTestInfo.CTestInfoFileName);
            if (!File.Exists(testInfoFilename))
            {
                /*
                frameworkHandle.SendMessage(TestMessageLevel.Warning,
                    "CTestExecutor.RunTests: didn't find info file:" + testInfoFilename + "");
                */
            }
            _ctestInfo.ReadTestInfoFile(testInfoFilename);
            foreach (var s in enumerable)
            {
                var cases = CTestDiscoverer.ParseTestContainerFile(s, frameworkHandle, EnableLogging, _ctestInfo);
                RunTests(cases.Values, runContext, frameworkHandle);
            }
            _runningFromSources = false;
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var testCases = tests as IList<TestCase> ?? tests.ToList();
            if (!testCases.Any())
            {
                return;
            }
            TryUpdateCacheDir(testCases.First().Source, frameworkHandle);
            var buildConfiguration = _buildConfiguration.ConfigurationName;
            if (!buildConfiguration.Any())
            {
                // get configuration name from cmake cache, pick first found
                var typeString = _cmakeCache["CMAKE_CONFIGURATION_TYPES"];
                var types = typeString.Split(';');
                if (types.Any())
                {
                    buildConfiguration = types.First();
                    frameworkHandle.SendMessage(TestMessageLevel.Warning,
                        "CTestExecutor.RunTests: configuration fallback to: " + buildConfiguration);
                }
            }
            if (!buildConfiguration.Any())
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning,
                    "CTestExecutor.RunTests: no build configuration found");
            }
            if (!_buildConfiguration.HasDte && _ctestInfo.FileRead)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning,
                    "CTestExecutor.RunTests: DTE object not found, maybe having a problem here.");
                //return;
            }
            if (!File.Exists(_cmakeCache.CTestExecutable))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error,
                    "CTestExecutor.RunTests: ctest not found: \"" + _cmakeCache.CTestExecutable + "\"");
                return;
            }
            if (!Directory.Exists(_cmakeCache.CMakeCacheDir))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error,
                    "CTestExecutor.RunTests: working directory not found: \"" + _cmakeCache.CMakeCacheDir + "\"");
                return;
            }
            if (EnableLogging)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    "CTestExecutor.RunTests: working directory is \"" + _cmakeCache.CMakeCacheDir + "\"");
            }
            var logFileDir = _cmakeCache.CMakeCacheDir + "\\Testing\\Temporary";
            if (!_runningFromSources)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    "CTestExecutor.RunTests: ctest (" + _cmakeCache.CTestExecutable + ")");
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    "CTestExecutor.RunTests: logs are written to (file://" + logFileDir + ")");
            }
            // run test cases
            foreach (var test in testCases)
            {
                if (_cancelled)
                {
                    break;
                }
                // verify we have a run directory and a ctest executable
                var args = "-R \"^" + test.FullyQualifiedName + "$\"";
                if (buildConfiguration.Any())
                {
                    args += " -C \"" + buildConfiguration + "\"";
                }
                var startInfo = new ProcessStartInfo
                {
                    Arguments = args,
                    FileName = _cmakeCache.CTestExecutable,
                    WorkingDirectory = _cmakeCache.CMakeCacheDir,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var process = new Process
                {
                    StartInfo = startInfo
                };
                var logFileName = logFileDir + "\\LastTest.log";
                if (File.Exists(logFileName))
                {
                    File.Delete(logFileName);
                }
                var logMsg = "CTestExecutor.RunTests: ctest " + test.FullyQualifiedName;
                if (buildConfiguration.Any())
                {
                    logMsg += " -C " + buildConfiguration;
                }
                frameworkHandle.SendMessage(TestMessageLevel.Informational, logMsg);
                if (runContext.IsBeingDebugged)
                {
                    process.Start();
#if false
                    var vars = new System.Collections.Generic.Dictionary<string, string>();
                    frameworkHandle.LaunchProcessWithDebuggerAttached(
                        _cmakeCache.CTestExecutable,
                        _cmakeCache.CMakeCacheDir,
                        args,
                        vars);
#endif
                }
                else
                {
                    process.Start();
                }
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                if (!File.Exists(logFileName))
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Warning, "logfile not found: "
                                                                          + logFileName);
                }
                var content = File.ReadAllText(logFileName);
                var logFileBackup = logFileDir + "\\" + test.FullyQualifiedName + ".log";
                File.Copy(logFileName, logFileBackup, true);
                var matchesDuration = RegexDuration.Match(content);
                var timeSpan = new TimeSpan();
                if (matchesDuration.Success)
                {
                    timeSpan = TimeSpan.FromSeconds(
                        double.Parse(matchesDuration.Groups[RegexFieldDuration].Value,
                            System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
                }
                else
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Warning,
                        "CTestExecutor.RunTests: could not get runtime of test " + test.FullyQualifiedName);
                }
                var testResult = new TestResult(test)
                {
                    ComputerName = Environment.MachineName,
                    Duration = timeSpan,
                    Outcome = process.ExitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed
                };
                if (process.ExitCode != 0)
                {
                    var matchesOutput = RegexOutput.Match(content);
                    testResult.ErrorMessage = matchesOutput.Groups[RegexFieldOutput].Value;
                    frameworkHandle.SendMessage(TestMessageLevel.Error,
                        "CTestExecutor.RunTests: ERROR IN TEST " + test.FullyQualifiedName + ":");
                    frameworkHandle.SendMessage(TestMessageLevel.Error, output);
                    frameworkHandle.SendMessage(TestMessageLevel.Error,
                        "CTestExecutor.RunTests: END OF TEST OUTPUT FROM " + test.FullyQualifiedName);
                }
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    "CTestExecutor.RunTests: Log saved to file://" + logFileBackup);
                frameworkHandle.RecordResult(testResult);
            }
        }
    }
}