using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ImplemenationCTestTestAdapter
{

  /// <summary>
  /// this class executes a ctest TestCase by calling ctest -I 
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
      var tcs = sources.Select(src => (TestCase)CTestCase.Parse(src))
          .Where(it => it != null)
          .ToList();
      RunTests(tcs, runContext, frameworkHandle);
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
      foreach (var test in tests)
      {
        if (cancelled) break;

        CTestCase ctest = test;
        var process = new Process();
        process.StartInfo = new ProcessStartInfo()
        {
          Arguments = "-I \"" + ctest.Number + "," + ctest.Number + ",," + ctest.Number + "\"",
          FileName = "ctest",
          WorkingDirectory = ctest.CMakeBinaryDir,
          CreateNoWindow = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          WindowStyle = ProcessWindowStyle.Hidden
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();

        var exitCode = process.ExitCode;

        var testResult = new TestResult(test);
        testResult.Outcome = exitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
        frameworkHandle.RecordResult(testResult);


      }
    }

    public static Uri ExecutorUri = new Uri(ExecutorUriString);
  }
}
