using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    /// this class executes a ctest TestCase by calling ctest -I 
    /// </summary>
    [ExtensionUri(ExecutorUriString)]
    public class CTestExecutor : ITestExecutor
    {
        /// <summary>
        /// this identifies the testexecuter
        /// </summary>
        public const string ExecutorUriString = "executor://CTestExecutor/v1";

        private bool _cancelled;

        public void Cancel()
        {
            _cancelled = true;
        }

        /// <summary>
        /// delegates to other RunTests signature
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="runContext"></param>
        /// <param name="frameworkHandle"></param>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            foreach (var tcs in sources.Select(CTestDiscoverer.ParseTestSource))
            {
                RunTests(tcs, runContext, frameworkHandle);
            }
        }

        /// <summary>
        /// runs a separate ctest call for every testcase
        /// 
        /// @maybe use -I to run all test cases
        /// @todo add more metadata to tests!
        /// </summary>
        /// <param name="tests"></param>
        /// <param name="runContext"></param>
        /// <param name="frameworkHandle"></param>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var cTestExecutable = "";
            var cTestWorkingDirectory = "";
            var firstRun = true;
            var cTestExecutableExists = false;
            var cTestWorkingDirectoryExists = false;
            var cfg = BuildConfiguration.GetCurrentActiveConfiguration();
            foreach (var test in tests)
            {
                if (_cancelled)
                {
                    break;
                }
                // verify we have a run directory and a ctest executable
                if (firstRun)
                {
                    firstRun = false;
                    var doc = new XmlDocument();
                    doc.Load(test.Source);
                    var ctestDir = doc.SelectSingleNode("//CTestContainer/CTestWorkingDirectory");
                    var ctestExe = doc.SelectSingleNode("//CTestContainer/CTestExecutable");
                    if (ctestDir == null)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error, "CTestWorkingDirectory not found in source");
                    }
                    else
                    {
                        var text = (XmlText) ctestDir.FirstChild;
                        cTestWorkingDirectory = text?.Value;
                        if (cTestWorkingDirectory != null)
                        {
                            cTestWorkingDirectoryExists = Directory.Exists(cTestWorkingDirectory);
                        }
                    }
                    if (ctestExe == null)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error, "CTestExecutable not found in source");
                    }
                    else
                    {
                        var text = (XmlText) ctestExe.FirstChild;
                        cTestExecutable = text?.Value;
                        if (cTestExecutable != null)
                        {
                            cTestExecutableExists = File.Exists(cTestExecutable);
                        }
                    }
                    frameworkHandle.SendMessage(TestMessageLevel.Informational,
                        $"running tests in: {cTestWorkingDirectory}");
                }
                if (!cTestExecutableExists)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error,
                        $"CTestExecutable not found: \"{cTestExecutable}\"");
                    return;
                }
                if (!cTestWorkingDirectoryExists)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error,
                        $"CTestWorkingDirectory not found: \"{cTestWorkingDirectory}\"");
                    return;
                }
                var args = $"-R \"{test.FullyQualifiedName}\"";
                args += $" -C \"{cfg}\"";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        Arguments = args,
                        FileName = cTestExecutable,
                        WorkingDirectory = cTestWorkingDirectory,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    $"running: {cTestExecutable} {args}");
                process.Start();
                process.WaitForExit();
                //var output = process.StandardOutput.ReadToEnd();
                var exitCode = process.ExitCode;
                var testResult = new TestResult(test)
                {
                    Outcome = exitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed
                };
                frameworkHandle.RecordResult(testResult);
            }
        }

        public static Uri ExecutorUri = new Uri(ExecutorUriString);
    }
}