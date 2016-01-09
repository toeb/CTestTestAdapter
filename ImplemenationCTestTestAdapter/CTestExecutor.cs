using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Management;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    /// This class executes a ctest TestCase by calling ctest -R for
    /// each test case in the directory of the belonging CTestTestfile.cmake
    /// source file.
    /// </summary>
    [ExtensionUri(CTestExecutor.ExecutorUriString)]
    public class CTestExecutor : ITestExecutor
    {
        private bool cancelled;
         
        /// <summary>
        /// this identifies the testexecuter
        /// </summary>
        public const string ExecutorUriString = "executor://CTestExecutor/v1";

        public void Cancel()
        {
            cancelled = true;
        }

        /// <summary>
        /// delegates to other RunTests signature
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="runContext"></param>
        /// <param name="frameworkHandle"></param>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            foreach (var source in sources)
            {
                var testcases = CTestHelpers.GetTestCases(source);
                RunTests(testcases, runContext, frameworkHandle);
            }
        }

        /// <summary>
        /// Runs each test using the FullyQualifiedName name using
        /// -R option of ctest.
        /// </summary>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var configuration = GetCurrentConfiguration();

            foreach (var test in tests)
            {
                if (cancelled)
                    break;

                var testName = test.FullyQualifiedName;
                var process = new Process();

                var fileInfo = new FileInfo(test.Source);
                process.StartInfo = new ProcessStartInfo()
                {
                    Arguments = "-C " + configuration + " -R " + testName,
                    FileName = "ctest",
                    WorkingDirectory = fileInfo.DirectoryName,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var exitCode = process.ExitCode;
                var time = Regex.Match(output, @"[d]\s*(?<time>\S*)\s*sec");
                var message = "";
                
                if(exitCode != 0)
                {
                    // In case of a failure, try to parse the fileInfo.DirectoryName/Testing/Temporary
                    // file for failed tests and try to extract the reason for the test failure.
                    
                    var content = File.ReadAllText(fileInfo.DirectoryName + "/Testing/Temporary/LastTest.log");
                    var error = Regex.Match(content, @"Output:\r\n-{58}\r\n(?<output>.*)\r\n<end of output>", RegexOptions.Singleline);
                    message = error.Groups["output"].Value;
                }
                
                var testResult = new TestResult(test);
                testResult.ComputerName = Environment.MachineName;
                testResult.Duration = TimeSpan.FromSeconds(double.Parse(time.Groups["time"].Value,
                    System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
                testResult.ErrorMessage = message;
                testResult.Outcome = exitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
                frameworkHandle.RecordResult(testResult);
            }
        }

        private string GetCurrentConfiguration()
        {
            var dte = DTEHelper.GetCurrent();
            var activeConfiguration = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
            return activeConfiguration;
        }

        public static Uri ExecutorUri = new Uri(ExecutorUriString);
    }
}
