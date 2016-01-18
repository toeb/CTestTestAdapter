using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace ImplemenationCTestTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class CTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://CTestExecutor/v1";

        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public bool EnableLogging { get; set; } = false;

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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        Arguments = args,
                        FileName = _cmakeCache.CTestExecutable,
                        WorkingDirectory = _cmakeCache.CMakeCacheDir,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    $"CTestExecutor.RunTests: {_cmakeCache.CTestExecutable} {args}");
                process.Start();
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var exitCode = process.ExitCode;
                var matchString =
                    $@"Test\s*#[0-9]+:\s+\S*{test.FullyQualifiedName}\S*\s*\.+[\* ]+.+\s+(?<time>\S*)\s+sec";
                var time = Regex.Matches(output, matchString);
                var timeCount = time.Count;
                if (timeCount != 1)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Warning,
                        $"CTestExecutor.RunTests: bad number of result times found:{timeCount}");
                    frameworkHandle.SendMessage(TestMessageLevel.Warning,
                        $"CTestExecutor.RunTests: output: {output}");
                    frameworkHandle.SendMessage(TestMessageLevel.Warning,
                        "CTestExecutor.RunTests: -----------------------------");
                }
                var message = "";
                if (exitCode != 0)
                {
                    // In case of a failure, try to parse the fileInfo.DirectoryName/Testing/Temporary
                    // file for failed tests and try to extract the reason for the test failure.
                    var logFileName = _cmakeCache.CMakeCacheDir + "/Testing/Temporary/LastTest.log";
                    var content = File.ReadAllText(logFileName);
                    var error = Regex.Match(content, @"Output:\r\n-{58}\r\n(?<output>.*)\r\n<end of output>",
                        RegexOptions.Singleline);
                    message = error.Groups["output"].Value;
                }
                var computerName = Environment.MachineName;
                var timeSpan = TimeSpan.FromSeconds(double.Parse(time[0].Groups["time"].Value,
                    System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
                var testResult = new TestResult(test)
                {
                    ComputerName = computerName,
                    Duration = timeSpan,
                    ErrorMessage = message,
                    Outcome = exitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed
                };
                frameworkHandle.RecordResult(testResult);
            }
        }
    }
}