using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    ///  Collects all tests from CTestTestfile.cmake which is the
    ///  source of a container. Uses ".cmake" file extension to avoid
    ///  analysis of unrelated files.
    /// </summary>
    [DefaultExecutorUri(CTestExecutor.ExecutorUriString)]
    [FileExtension(".cmake")]
    public class CTestDiscoverer : ITestDiscoverer
    {
        [Import(typeof(SVsServiceProvider))]
        IServiceProvider ServiceProvider { get; set; }

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            foreach (var source in sources)
            {
                var testcases = CTestHelpers.GetTestCases(source);
                foreach (var t in testcases)
                {
                    discoverySink.SendTestCase(t);
                }
            }
        }
    }
}
