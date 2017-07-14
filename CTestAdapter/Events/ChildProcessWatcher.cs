using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Linq;

namespace CTestAdapter.Events
{
    public class ChildProcessWatcher : IDisposable
    {
        public Process Parent { get; set; }

        public EnvDTE.DTE Dte { get; set; }

        public IFrameworkHandle Framework { get; set; }

        public List<Process> ChildProcesses
        {
            get { return _children; }
            private set { _children = value; }
        }

        private List<Process> _children;

        private const string _computerName = "localhost";

        private const string _startQueryString =
            "Select * From __InstanceCreationEvent Within 1 " +
            "Where TargetInstance ISA 'Win32_Process' ";

        private WqlEventQuery _startQuery;
        private ManagementEventWatcher _startWatcher;
        private ManagementScope _scope;

        public ChildProcessWatcher()
        {
            _scope = new ManagementScope(
                @"\\" + _computerName + @"\root\CIMV2",
                null);
            _scope.Connect();
            _startQuery = new WqlEventQuery(_startQueryString);
            _startWatcher = new ManagementEventWatcher(
                _scope,
                _startQuery);
            _startWatcher.EventArrived += ProcessStarted;
        }

        public void Start()
        {
            if (null != _startWatcher)
            {
                _startWatcher.Start();
            }
        }

        public void Stop()
        {
            if (null != _startWatcher)
            {
                _startWatcher.Stop();
            }
        }

        private void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            if (Parent == null)
            {
                Framework.SendMessage(TestMessageLevel.Informational,
                    "ChildProcessWatcher.ProcessStarted: no parent id");
                return;
            }
#if false
            Framework.SendMessage(TestMessageLevel.Informational,
                $"ChildProcessWatcher.ProcessStarted: parent id:{Parent.Id}");
#else
            Framework.SendMessage(TestMessageLevel.Informational,
                "ChildProcessWatcher.ProcessStarted: parent id:" + Parent.Id);
            var currentChildren = ProcessExtensions.GetChildProcesses(Parent);
            Framework.SendMessage(TestMessageLevel.Informational,
                "ChildProcessWatcher.ProcessStarted: child count:" + currentChildren.Count);
            try
            {
                ManagementBaseObject targetInstance =
                    (ManagementBaseObject) e.NewEvent.Properties["TargetInstance"].Value;
                var name = "";
                if (null != targetInstance)
                {
                    name = targetInstance.GetPropertyValue("Name") as string;
                }
                if (!string.IsNullOrEmpty(name))
                {
                    Framework.SendMessage(TestMessageLevel.Informational,
                        "ChildProcessWatcher.ProcessStarted: name (" + name + ")");
                }
            }
            catch (COMException err)
            {
                Framework.SendMessage(TestMessageLevel.Informational,
                    "ChildProcessWatcher.ProcessStarted: COM:" + err.Message);
            }
#endif
        }

        public void Dispose()
        {
            if (null != _startWatcher)
            {
                _startWatcher.Stop();
                _startWatcher.EventArrived -= ProcessStarted;
                _startWatcher.Dispose();
            }
        }
    }
}