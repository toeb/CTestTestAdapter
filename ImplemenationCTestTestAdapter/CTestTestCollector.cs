using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using TestInfoList = System.Collections.Generic.List<ImplemenationCTestTestAdapter.CTestTestCollector.TestInfo>;

namespace ImplemenationCTestTestAdapter
{
    public class CTestTestCollector
    {
        public struct TestInfo
        {
            public string Name;
            public int Number;
        };


        private static readonly Regex TestRegex = new Regex(@".*#(?<number>[1-9][0-9]*): *(?<testname>[\w-\.]+).*");

        public TestInfoList CTestNames { get; } = new TestInfoList();

        public string CTestExecutable { get; set; }

        public string CTestWorkingDir { get; set; }

        public string CurrentActiveConfig { get; set; }

        public string CTestArguments { get; set; } = " -N ";

        public void CollectTestCases()
        {
            CTestNames.Clear();

            if (!File.Exists(CTestExecutable))
            {
                CTestLogger.Instance.LogMessage("CollectTestCases: ctest does not exist: " + CTestExecutable);
                return;
            }
            if (!Directory.Exists(CTestWorkingDir))
            {
                CTestLogger.Instance.LogMessage("CollectTestCases: directory does not exist: " + CTestWorkingDir);
                return;
            }

            var args = CTestArguments;
            if (!string.IsNullOrWhiteSpace(CurrentActiveConfig))
            {
                args += " -C ";
                args += CurrentActiveConfig;
            }

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = CTestExecutable,
                    WorkingDirectory = CTestWorkingDir,
                    Arguments = args,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                }
            };
            CTestLogger.Instance.LogMessage("collecting tests:"
                                            + CTestExecutable + " wd:" + CTestWorkingDir
                                            + " args: " + args);
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.Dispose();
            var matches = TestRegex.Matches(output);

            foreach (var match in matches)
            {
                var m = match as Match;
                if (m == null)
                {
                    continue;
                }
                var name = m.Groups["testname"].Value;
                var number = m.Groups["number"].Value;
                var myTestInfo = new TestInfo()
                {
                    Name = name,
                    Number = int.Parse(number)
                };
                CTestNames.Add(myTestInfo);
            }
        }
    }
}