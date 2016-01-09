using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    /// A CTestContainer contains all test belonging to a class, which means
    /// it has its own directory and CTestTestfile.cmake file. CTestTestfile.cmake is
    /// the Source of the container.
    /// </summary>
    public class CTestContainer : ITestContainer
    {

        public CTestContainer(CTestContainerDiscoverer discoverer, string source)
        {
            Source = source;
            Discoverer = discoverer;
        }

        public CTestContainer(CTestContainer cTestContainer)
            : this(cTestContainer.Discoverer as CTestContainerDiscoverer, cTestContainer.Source)
        {

        }

        public int CompareTo(ITestContainer other_)
        {
            var other = other_ as CTestContainer;
            if (other == null) return -1;

            var result = string.Compare(this.Source, other.Source, StringComparison.OrdinalIgnoreCase);

            if (result != 0) return result;

            return object.ReferenceEquals(this, other) ? 0 : -1;
        }

        public IEnumerable<Guid> DebugEngines
        {
            get { return new[] { VSConstants.DebugEnginesGuids.ManagedAndNative_guid }; /*Enumerable.Empty<Guid>();*/ }
        }

        public Microsoft.VisualStudio.TestWindow.Extensibility.Model.IDeploymentData DeployAppContainer()
        {
            return null;
        }

        public ITestContainerDiscoverer Discoverer
        {
            get;
            set;
        }

        public bool IsAppContainerTestContainer
        {
            get { return false; }
        }

        public ITestContainer Snapshot()
        {
            return new CTestContainer(this);
        }

        public string Source
        {
            get;
            set;
        }

        public FrameworkVersion TargetFramework
        {
            get { return FrameworkVersion.None; }
        }

        public Architecture TargetPlatform
        {
            get { return Architecture.AnyCPU; }
        }
    }
}
