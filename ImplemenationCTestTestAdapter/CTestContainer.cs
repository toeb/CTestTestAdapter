using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    /// A CTestContainer 
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

        public List<CTestTestCollector.TestInfo> CTestList { get; set; }

        public string CTestExecutable { get; set; } = "";

        public string CTestWorkingDirectory { get; set; } = "";

        public bool SaveContainer()
        {
            var directory = Path.GetDirectoryName(Source);
            if (directory == null)
            {
                CTestLogger.Instance.LogMessage("directory of test container is null!");
                return false;
            }
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var doc = new XmlDocument();
            var rootNode = doc.CreateElement("CTestContainer");
            doc.AppendChild(rootNode);

            var workingDirNode = doc.CreateElement("CTestWorkingDirectory");
            rootNode.AppendChild(workingDirNode);
            var workingDirNode2 = doc.CreateTextNode(CTestWorkingDirectory);
            workingDirNode.AppendChild(workingDirNode2);

            var ctestExecutableNode = doc.CreateElement("CTestExecutable");
            rootNode.AppendChild(ctestExecutableNode);
            var ctestExecutableNode2 = doc.CreateTextNode(CTestExecutable);
            ctestExecutableNode.AppendChild(ctestExecutableNode2);

            foreach (var test in CTestList)
            {
                var testNode = doc.CreateElement("Test");

                var attributeName = doc.CreateAttribute("Name");
                attributeName.Value = test.Name;
                testNode.Attributes.Append(attributeName);

                var attributeNumber = doc.CreateAttribute("Number");
                attributeNumber.Value = test.Number.ToString();
                testNode.Attributes.Append(attributeNumber);

                rootNode.AppendChild(testNode);
            }
            if (File.Exists(Source))
            {
                File.Delete(Source);
            }
            doc.Save(Source);
            return true;
        }
    }
}