using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace CTestTestAdapter
{
    [FileExtension(".cmake")]
    [DefaultExecutorUri(CTestExecutor.ExecutorUriString)]
    public class CTestDiscoverer : ITestDiscoverer
    {
        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }

        private const string FieldNameTestname = "testname";
        private const string FieldNameIsExe = "isexe";

        private static readonly Regex AddTestRegex =
            new Regex(@"^\s*add_test\s*\((?<" + FieldNameTestname + @">\S+)\s.*\).*$");

        private static readonly Regex IsExeRegex =
            new Regex("^\\s*add_test\\s*\\(\\S+\\s+\"(?<" + FieldNameIsExe + ">[^\"]+)\".*\\)");

        private readonly BuildConfiguration _buildConfig;
        private readonly CTestInfo _testInfo;

        public bool EnableLogging { get; set; }

        public CTestDiscoverer()
        {
            EnableLogging = true;
            _testInfo = new CTestInfo();
            _buildConfig = new BuildConfiguration();
        }

        public void DiscoverTests(IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger log,
            ITestCaseDiscoverySink discoverySink)
        {
            var testInfoFilename = Path.Combine(_buildConfig.SolutionDir, CTestInfo.CTestInfoFileName);
            if (!File.Exists(testInfoFilename))
            {
                log.SendMessage(TestMessageLevel.Warning,
                    "CTestDiscoverer.DiscoverTests: test discoverer didn't find file:" + testInfoFilename);
            }
            _testInfo.ReadTestInfoFile(testInfoFilename);
            foreach (var source in sources)
            {
                var cases = ParseTestContainerFile(source, log, EnableLogging, _testInfo);
                foreach (var c in cases)
                {
                    discoverySink.SendTestCase(c.Value);
                }
            }
        }

        public static Dictionary<string, TestCase> ParseTestContainerFile(string source, IMessageLogger log,
            bool enableLogging, CTestInfo info)
        {
            var cases = new Dictionary<string, TestCase>();
            var content = File.ReadLines(source);
            var lineNumber = 0;
            foreach (var line in content)
            {
                lineNumber++;
                var matches = AddTestRegex.Matches(line);
                foreach (var match in matches)
                {
                    var m = match as Match;
                    if (m == null)
                    {
                        continue;
                    }
                    var testname = m.Groups[FieldNameTestname].Value;
                    if (info.FileRead && !info.TestExists(testname))
                    {
                        log.SendMessage(TestMessageLevel.Warning,
                            "CTestDiscoverer.ParseTestContainerFile: test not listed by ctest -N :" + testname);
                    }
                    if (cases.ContainsKey(testname))
                    {
                        continue;
                    }
                    var testcase = new TestCase(testname, CTestExecutor.ExecutorUri, source)
                    {
                        CodeFilePath = source,
                        DisplayName = testname,
                        LineNumber = lineNumber,
                    };
                    if (info.TestExists(testname))
                    {
                        testcase.DisplayName = info[testname].Number.ToString().PadLeft(3, '0') + ": " + testname;
                    }
                    var isExe = IsExeRegex.Match(line);
                    cases.Add(testname, testcase);
                }
            }
            return cases;
        }
    }
}