using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Data;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        ///  Get all CTestContainers by traversing the file sytem using the CTestTestfile.cmake
        ///  file structure.
        /// </summary>
        public IEnumerable<ITestContainer> TestContainers
        {
            get
            {
                // TODO: Add a cache for the containers. Only needs an update if CMake was rerun.
                // Check if it is feasable to use the time stamp of the root CTestTestfile.cmake.

                /// gets solution directory
                var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
                object solutionpath_o = "";
                solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out solutionpath_o);
                var solution_path = solutionpath_o as string;

                var files = CollectCTestTestfiles(solution_path);

                return files.Select(f => new CTestContainer(this, f));
            }
        }

        private IEnumerable<string> CollectCTestTestfiles(string currentDir)
        {
            var file = new FileInfo(currentDir + "/CTestTestfile.cmake");

            var content = file.OpenText().ReadToEnd();
            var matches = Regex.Matches(content, @".*[sS][uB][bB][dD][iI][rR][sS]\s*\((?<subdir>.*)\)");
            var subdirs = new List<string>();
            foreach (Match match in matches)
            {
                subdirs.Add(match.Groups["subdir"].Value);
            }
            
            if(subdirs.Count == 0 && content.Contains("add_test"))
            {
                return Enumerable.Repeat(file.FullName, 1);
            }
            else
            {
                return subdirs
                    .SelectMany(d => CollectCTestTestfiles(currentDir + "/" + d));
            }
        }


        public event EventHandler TestContainersUpdated;
    }
}
