using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Runtime.InteropServices;
using ImplemenationCTestTestAdapter.Events;

namespace ImplemenationCTestTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class CTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://CTestExecutor/v1";

        private const string RegexFieldOutput = "output";
        private const string RegexFieldDuration = "duration";

        private static Regex RegexOutput = new Regex($@"Output:\r\n-+\r\n(?<{RegexFieldOutput}>.*)\r\n<end of output>\r\n", RegexOptions.Singleline);
        private static Regex RegexDuration = new Regex($@"<end of output>\r\nTest time =\s+(?<{RegexFieldDuration}>[\d\.]+) sec\r\n", RegexOptions.Singleline);

        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public bool EnableLogging { get; set; } = false;

        private bool _childWatcherEnabled = false;
        private ChildProcessWatcher _childWatcher;

        private bool _cancelled;
        private readonly CMakeCache _cmakeCache;
        private readonly BuildConfiguration _buildConfiguration;
        private readonly CTestInfo _ctestInfo;

        public CTestExecutor()
        {
            _buildConfiguration = new BuildConfiguration();
            _cmakeCache = new CMakeCache
            {
                CMakeCacheDir = _buildConfiguration.SolutionDir
            };
            _ctestInfo = new CTestInfo();
            _childWatcher = new ChildProcessWatcher();
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                $"CTestExecutor.RunTests(src)");
            var testInfoFilename = Path.Combine(_buildConfiguration.SolutionDir, CTestInfo.CTestInfoFileName);
            if (!File.Exists(testInfoFilename))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, $"CTestExecutor.RunTests: didn't find info file:{testInfoFilename}");
            }
            _ctestInfo.ReadTestInfoFile(testInfoFilename);
            foreach (var s in sources)
            {
                if (EnableLogging)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Informational,
                        $"CTestExecutor.RunTests(src) => CTestDiscoverer.ParseTestContainerFile({s})");
                }
                var cases = CTestDiscoverer.ParseTestContainerFile(s, frameworkHandle, EnableLogging, _ctestInfo);
                RunTests(cases.Values, runContext, frameworkHandle);
            }
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (!_buildConfiguration.HasDte)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error,
                    "CTestExecutor.RunTests: DTE object not found, cannot run tests.");
                return;
            }
            if (!File.Exists(_cmakeCache.CTestExecutable))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error,
                    $"CTestExecutor.RunTests: ctest not found: \"{_cmakeCache.CTestExecutable}\"");
                return;
            }
            if (!Directory.Exists(_cmakeCache.CMakeCacheDir))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error,
                    $"CTestExecutor.RunTests: working directory not found: \"{_cmakeCache.CMakeCacheDir}\"");
                return;
            }
            if (EnableLogging)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                $"CTestExecutor.RunTests: working directory is \"{_cmakeCache.CMakeCacheDir}\"");
            }
            // run test cases
            foreach (var test in tests)
            {
                if (_cancelled)
                {
                    break;
                }
                // verify we have a run directory and a ctest executable
                var args = $"-R \"^{test.FullyQualifiedName}$\"";
                args += $" -C \"{_buildConfiguration.ConfigurationName}\"";
                var startInfo = new ProcessStartInfo()
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
                var process = new System.Diagnostics.Process
                {
                    StartInfo = startInfo
                };
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    $"CTestExecutor.RunTests: {_cmakeCache.CTestExecutable} {args}");
                if (runContext.IsBeingDebugged)
                {
                    if (_childWatcherEnabled)
                    {
                        _childWatcher.Parent = process;
                        _childWatcher.Dte = _buildConfiguration.Dte;
                        _childWatcher.Framework = frameworkHandle;
                        _childWatcher.Start();
                    }
                    process.Start();
                    frameworkHandle.SendMessage(TestMessageLevel.Informational,
                        $"CTestExecutor.RunTests: ctest process id ({process.Id}) at ({process.StartTime.ToString()})");
#if true
                    var tryCount = 0;
                    int processId = 0;
                    while (tryCount++ < 10)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Informational,
                            $"CTestExecutor.RunTests: try attaching to process ({tryCount} run)");
                        try
                        {
                            var ctestChildren = ProcessExtensions.GetChildProcesses(process);
                            if(ctestChildren.Count == 0)
                            {
                                continue;
                            }
                            var dteChildren = _buildConfiguration.Dte.Debugger.LocalProcesses;
                            foreach (EnvDTE.Process dteChild in dteChildren)
                            {
                                foreach (var ctestChild in ctestChildren)
                                {
                                    if (dteChild.ProcessID == ctestChild.Id)
                                    {
                                        frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                            $"CTestExecutor.RunTests: attaching to process ({ctestChild.Id}) ...");
                                        try {
                                            dteChild.Attach();
                                            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                                $"CTestExecutor.RunTests: ... done");
                                            processId = ctestChild.Id;
                                        }
                                        catch (COMException e)
                                        {
                                            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                                $"CTestExecutor.RunTests: ... failed:{e.Message}");
                                        }
                                        break;
                                    }
                                }
                                if(processId != 0)
                                {
                                    break;
                                }
                            }
                            if (processId != 0)
                            {
                                break;
                            }
                        }
                        catch (COMException e)
                        {
                            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                $"CTestExecutor.RunTests: other error:{e.Message}");
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
#endif
                }
                else
                {
                    process.Start();
                }
                process.WaitForExit();
                if (_childWatcherEnabled)
                {
                    _childWatcher.Stop();
                }
                var output = process.StandardOutput.ReadToEnd();
                var logFileName = _cmakeCache.CMakeCacheDir + "/Testing/Temporary/LastTest.log";
                var content = File.ReadAllText(logFileName);
                var matchesDuration = RegexDuration.Match(content);
                var timeSpan = TimeSpan.FromSeconds(
                    double.Parse(matchesDuration.Groups[RegexFieldDuration].Value,
                    System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
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
                        $"CTestExecutor.RunTests: ERROR IN TEST {test.FullyQualifiedName}:");
                    frameworkHandle.SendMessage(TestMessageLevel.Error, $"{output}");
                    frameworkHandle.SendMessage(TestMessageLevel.Error,
                        $"CTestExecutor.RunTests: END OF TEST OUTPUT FROM {test.FullyQualifiedName}");
                }
                frameworkHandle.RecordResult(testResult);
            }
        }
    }
}