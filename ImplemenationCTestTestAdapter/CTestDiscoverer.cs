using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    /// this class gets a list of sourcefiles and returns TestCases 
    /// the list is provided by CTestContainerDiscoverer in Visual Studio
    /// 
    /// It currently accepts every solution file which might be wrong (no [FileExtension] attribute given)
    /// </summary>
    [FileExtension(".ctest")]
    [DefaultExecutorUri(CTestExecutor.ExecutorUriString)]
    public class CTestDiscoverer : ITestDiscoverer
    {
        [Import(typeof (SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="discoveryContext"></param>
        /// <param name="logger"></param>
        /// <param name="discoverySink"></param>
        public void DiscoverTests(IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            foreach (var testcase in sources.Select(ParseTestSource).SelectMany(cases => cases))
            {
                discoverySink.SendTestCase(testcase);
            }
        }

        public static List<TestCase> ParseTestSource(string source)
        {
            var doc = new XmlDocument();
            doc.Load(source);
            var cases = new List<TestCase>();
            var testNodes = doc.SelectNodes("//CTestContainer/Test");
            if (testNodes == null)
            {
                return cases;
            }
            foreach (XmlNode testNode in testNodes)
            {
                if (testNode?.Attributes == null)
                {
                    continue;
                }
                var attributeName = testNode.Attributes["Name"].Value;
                var attributeNumber = testNode.Attributes["Number"].Value;
                while (attributeNumber.Length < 3)
                {
                    attributeNumber = attributeNumber.Insert(0, "0");
                }
                var testcase = new TestCase(attributeName, CTestExecutor.ExecutorUri, source)
                {
                    DisplayName = attributeNumber + ": " + attributeName
                };
                cases.Add(testcase);
            }
            return cases;
        }
    }
}