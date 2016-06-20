using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace ImplemenationCTestTestAdapter
{
    [FileExtension(".cmake")]
    [DefaultExecutorUri(CTestExecutor.ExecutorUriString)]
    public class CTestDiscoverer : ITestDiscoverer
    {
        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }

        private const string FieldNameTestname = "testname";
        private const string FieldNameIsExe = "isexe";

        private static readonly Regex AddTestRegex = new Regex($@"^\s*add_test\s*\((?<{FieldNameTestname}>\S+)\s.*\).*$");

        private static readonly Regex IsExeRegex =
            new Regex($"^\\s*add_test\\s*\\(\\S+\\s+\"(?<{FieldNameIsExe}>[^\"]+)\".*\\)");

        private readonly BuildConfiguration _buildConfig;
        private readonly CTestInfo _testInfo;

        public bool EnableLogging { get; set; } = true;

        public CTestDiscoverer()
        {
            _testInfo = new CTestInfo();
            _buildConfig = new BuildConfiguration();
        }

        public void DiscoverTests(IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger log,
            ITestCaseDiscoverySink discoverySink)
        {
#if false
            if (EnableLogging)
            {
                var p = System.Diagnostics.Process.GetCurrentProcess();
                log.SendMessage(TestMessageLevel.Informational, "-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
                log.SendMessage(TestMessageLevel.Informational, $"CTestDiscoverer.DiscoverTests: process id={p.Id}");
            }
#endif
            var testInfoFilename = Path.Combine(_buildConfig.SolutionDir, CTestInfo.CTestInfoFileName);
            if (!File.Exists(testInfoFilename))
            {
                log.SendMessage(TestMessageLevel.Warning,
                    $"CTestDiscoverer.DiscoverTests: test discoverer didn't find file:{testInfoFilename}");
            }
            _testInfo.ReadTestInfoFile(testInfoFilename);
            var testCount = 0;
            foreach (var source in sources)
            {
#if false
                if (EnableLogging)
                {
                    log.SendMessage(TestMessageLevel.Informational,
                        $"CTestDiscoverer.DiscoverTests => CTestDiscoverer.ParseTestContainerFile({source})");
                }
#endif
                var cases = ParseTestContainerFile(source, log, EnableLogging, _testInfo);
                foreach (var c in cases)
                {
                    discoverySink.SendTestCase(c.Value);
                }
                testCount += cases.Count;
            }
#if false
            log.SendMessage(TestMessageLevel.Informational,
                $"CTestDiscoverer.DiscoverTests: {testCount} tests found");
#endif
        }

        public static Dictionary<string, TestCase> ParseTestContainerFile(string source, IMessageLogger log,
            bool enableLogging, CTestInfo info)
        {
            //var p = System.Diagnostics.Process.GetCurrentProcess();
#if false
            if (enableLogging)
            {
                log.SendMessage(TestMessageLevel.Informational,
                    $"CTestDiscoverer.ParseTestContainerFile: ({source})");
            }
#endif
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
                    if (!info.TestExists(testname))
                    {
                        log.SendMessage(TestMessageLevel.Warning,
                            $"CTestDiscoverer.ParseTestContainerFile: test not listed by ctest -N :{testname}");
                        continue;
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
                    testcase.DisplayName = info[testname].Number.ToString().PadLeft(3, '0') + ": " + testname;

                    var isExe = IsExeRegex.Match(line);


#if false
                    log.SendMessage(TestMessageLevel.Informational, $"guid:{testname}=>{testcase.Id}");
                    log.SendMessage(TestMessageLevel.Informational, $"lext:{testname}=>{testcase.LocalExtensionData}");
#endif
                    if (enableLogging)
                    {
#if false
                        log.SendMessage(TestMessageLevel.Informational,
                            $"CTestDiscoverer.ParseTestContainerFile: + {testname}");
#endif
                    }
                    cases.Add(testname, testcase);
                }
            }
#if false
            if (enableLogging)
            {
                log.SendMessage(TestMessageLevel.Informational,
                    "CTestDiscoverer.ParseTestContainerFile: DONE");
            }
#endif
            return cases;
        }
    }
}