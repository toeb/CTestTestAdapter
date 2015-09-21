using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        /// <summary>
        /// this identifies the testexecuter
        /// </summary>
        public const string ExecutorUriString = "executor://CTestExecutor/v1";

        private bool cancelled;

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
            foreach (var test in tests)
            {
                if (cancelled)
                    break;

                // TODO: Need to figure out the currently selected configuration
                var configuration = "Debug";
                
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

                // TUDO: Regex to get Passed time
                //Test project C:/data/58481/build/working
                //Start 4: dynamic_libraries_dynamicLibrary_getFunction_not_working
                //1/1 Test #4: dynamic_libraries_dynamicLibrary_getFunction_not_working ...   Passed    0.07 sec

                //100% tests passed, 0 tests failed out of 1

                //Total Test time (real) =   1.13 sec

                var testResult = new TestResult(test);
                testResult.ComputerName = Environment.MachineName;
                
                testResult.Outcome = exitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
                frameworkHandle.RecordResult(testResult);
            }
        }

        public static Uri ExecutorUri = new Uri(ExecutorUriString);
    }
}
