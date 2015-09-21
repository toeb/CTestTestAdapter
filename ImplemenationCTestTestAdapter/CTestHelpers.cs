using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImplemenationCTestTestAdapter
{
    public static class CTestHelpers
    {
        /// <summary>
        /// Returns an enumerable of CTestCases which are queried by ctest -N
        /// for a given source file which is a CTestTestfile.cmake.
        /// </summary>
        public static IEnumerable<TestCase> GetTestCases(string source)
        {
            var file = new FileInfo(source);
            
            var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "ctest",
                Arguments = "-N",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = file.DirectoryName,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
            };

            process.Start();

            var output = process.StandardOutput.ReadToEnd();

            var matches = Regex.Matches(output, @".*#[1-9][0-9]*: *(?<testname>\w*)");
            foreach (var match in matches)
            {
                var m = match as Match;
                var name = m.Groups["testname"].Value;
                
                var t = new TestCase(name, CTestExecutor.ExecutorUri, source);
                // TODO: Add more meta information e.g. Line number, source file and
                // some traits for grouping.
                yield return t;
            }
        }
    }
}
