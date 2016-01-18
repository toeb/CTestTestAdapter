using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    /// A CTestContainer 
    /// </summary>
    public class CTestContainer : ITestContainer
    {
        public CTestContainer(ITestContainerDiscoverer discoverer, string source)
        {
            Source = source;
            Discoverer = discoverer;
        }

        public CTestContainer(CTestContainer cTestContainer)
            : this(cTestContainer.Discoverer as CTestContainerDiscoverer, cTestContainer.Source)
        {
        }

        public int CompareTo(ITestContainer other2)
        {
            var other = other2 as CTestContainer;
            if (other == null)
            {
                return -1;
            }
            var result = string.Compare(Source, other.Source, StringComparison.OrdinalIgnoreCase);
            if (result != 0)
            {
                return result;
            }
            return ReferenceEquals(this, other) ? 0 : -1;
        }

        public IEnumerable<Guid> DebugEngines => new[] {VSConstants.DebugEnginesGuids.ManagedAndNative_guid};

        public Microsoft.VisualStudio.TestWindow.Extensibility.Model.IDeploymentData DeployAppContainer()
        {
            return null;
        }

        public ITestContainerDiscoverer Discoverer { get; set; }

        public bool IsAppContainerTestContainer => false;

        public ITestContainer Snapshot()
        {
            return new CTestContainer(this);
        }

        public string Source { get; set; }

        public FrameworkVersion TargetFramework => FrameworkVersion.None;

        public Architecture TargetPlatform => Architecture.AnyCPU;
    }
}