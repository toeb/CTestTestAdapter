using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestWindow.Data;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImplemenationCTestTestAdapter
{

  /// <summary>
  /// this class gets a list of sourcefiles and returns TestCases 
  /// the list is provided by CTestContainerDiscoverer in Visual Studio
  /// 
  /// It currently accepts every solution file which might be wrong (no [FileExtension] attribute given)
  /// </summary>
  [DefaultExecutorUri(CTestExecutor.ExecutorUriString)]
  public class CTestDiscoverer : ITestDiscoverer
  {
    [Import(typeof(SVsServiceProvider))]
    IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// @todo add more metadata to test cases (however ctest alone does not provide everything needed)
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="discoveryContext"></param>
    /// <param name="logger"></param>
    /// <param name="discoverySink"></param>
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
      foreach (var source in sources)
      {
        var testcase = CTestCase.Parse(source);
        if (testcase == null) continue;        
        discoverySink.SendTestCase(testcase);
      }
    }
  }
}
