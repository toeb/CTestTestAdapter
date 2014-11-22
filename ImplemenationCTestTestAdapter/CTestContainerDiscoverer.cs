using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Data;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace ImplemenationCTestTestAdapter
{

  /// <summary>
  /// The CTestContainerDiscoverer connects with visual studios TestExplorer window
  /// it provides the filenames for CTestDiscoverer through the TestContainers enumerable
  /// </summary>
  [Export(typeof(ITestContainerDiscoverer))]
  public class CTestContainerDiscoverer : ITestContainerDiscoverer
  {
    [Import(typeof(SVsServiceProvider))]
    public IServiceProvider ServiceProvider { get; set; }
  
    [Import]
    public IUnitTestStorage Storage { get; set; }
    [ImportingConstructor]
    public CTestContainerDiscoverer()
    {
  
    }
    public Uri ExecutorUri
    {
      get { return CTestExecutor.ExecutorUri; }
    }
  

    /// <summary>
    /// This currently gets the solution path (which has to be equal to CMakeBinaryDir/place where ctest is to be executed)
    /// then it calls ctest -N and parses the resulting list of available ctest
    /// these are converted into CTestContainer s
    /// 
    /// this needs to be improved by caching/watching since this method is called often and a therfore a bottleneck
    /// </summary>
    public IEnumerable<ITestContainer> TestContainers
    {
      get
      {
        /// gets solution directory
        var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
        object solutionpath_o = "";
        solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out solutionpath_o);
        var solution_path = solutionpath_o as string;
  
         /// create test containers for every test
        var containers = CTestHelpers.GetTestCases(solution_path).Select(tc =>{
          return new CTestContainer(this, tc.Id) { };
        }).ToList();
        
        return containers;
  
      }
    }
  
    /// <summary>
    /// 
    /// </summary>
    public event EventHandler TestContainersUpdated;
  }
}
