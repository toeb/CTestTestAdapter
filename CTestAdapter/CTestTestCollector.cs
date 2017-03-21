using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CTestTestAdapter
{
    public class CTestTestCollector
    {
        private const string FieldNameNumber = "number";
        private const string FieldNameTestname = "testname";

        private static readonly Regex TestRegex =
            new Regex(@".*#(?<" + FieldNameNumber + ">[1-9][0-9]*): *(?<" + FieldNameTestname + @">[\w-\.]+).*");

        public string CTestExecutable { get; set; }

        public string CTestWorkingDir { get; set; }

        public string CurrentActiveConfig { get; set; }

        public string CTestArguments { get; set; }

        public CTestTestCollector()
        {
            CTestArguments = " -N ";
        }

        public void CollectTestCases(CTestInfo info)
        {
            info.Tests.Clear();
            if (!File.Exists(CTestExecutable))
            {
                return;
            }
            if (!Directory.Exists(CTestWorkingDir))
            {
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
                var name = m.Groups[FieldNameTestname].Value;
                var numberStr = m.Groups[FieldNameNumber].Value;
                int number;
                int.TryParse(numberStr, out number);
                var newinfo = new CTestInfo.TestInfo
                {
                    Name = name,
                    Number = number,
                };
                info.Tests.Add(newinfo);
            }
        }
    }
}
