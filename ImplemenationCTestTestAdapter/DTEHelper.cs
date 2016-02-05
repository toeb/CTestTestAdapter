using EnvDTE;
using System;
using System.Management;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;

/*
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
*/

namespace ImplemenationCTestTestAdapter
{
    /// <remarks>
    ///  Taken from here: https://github.com/getgauge/gauge-visualstudio/blob/master/Gauge.VisualStudio/Helpers/DteHelper.cs
    /// </remarks>
    internal static class DteHelper
    {
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        private static readonly Regex DteRegex = new Regex(@"!\S+\.DTE\.[\d\.]+:\d+");

        internal static DTE GetCurrent(IServiceProvider serviceProvider = null)
        {
            if (serviceProvider != null)
            {
                return (DTE) serviceProvider.GetService(typeof (DTE));
            }
            /*
            log?.SendMessage(TestMessageLevel.Informational, "DteHelper.GetCurrent: no service provider given");
            */
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var vsProcessId = GetVisualStudioProcessId(currentProcess.Id);
            /*
            log?.SendMessage(TestMessageLevel.Informational, $"DteHelper.GetCurrent: process:{currentProcess.Id}");
            log?.SendMessage(TestMessageLevel.Informational, $"DteHelper.GetCurrent: vs process:{vsProcessId}");
            */
            object runningObject = null;
            IBindCtx bindCtx = null;
            IRunningObjectTable rot = null;
            IEnumMoniker enumMonikers = null;
            try
            {
                Marshal.ThrowExceptionForHR(CreateBindCtx(0, out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMonikers);
                var moniker = new IMoniker[1];
                var numberFetched = IntPtr.Zero;
                while (enumMonikers.Next(1, moniker, numberFetched) == 0)
                {
                    var runningObjectMoniker = moniker[0];
                    string name = null;
                    try
                    {
                        runningObjectMoniker?.GetDisplayName(bindCtx, null, out name);
                        /*
                        log?.SendMessage(TestMessageLevel.Informational, $"DteHelper.GetCurrent: displayname:{name}");
                        */
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // do nothing.
                    }
                    if (string.IsNullOrEmpty(name) || !DteRegex.IsMatch(name))
                    {
                        continue;
                    }
                    if (!name.EndsWith(vsProcessId.ToString()))
                    {
                        continue;
                    }
                    /*
                    log?.SendMessage(TestMessageLevel.Informational, $"DteHelper.GetCurrent: using:{name}");
                    */
                    rot.GetObject(runningObjectMoniker, out runningObject);
                    break;
                }
            }
            finally
            {
                if (enumMonikers != null)
                {
                    Marshal.ReleaseComObject(enumMonikers);
                }
                if (rot != null)
                {
                    Marshal.ReleaseComObject(rot);
                }
                if (bindCtx != null)
                {
                    Marshal.ReleaseComObject(bindCtx);
                }
            }
            return (DTE) runningObject;
        }

        private static int GetVisualStudioProcessId(int testRunnerProcessId)
        {
            var mos =
                new ManagementObjectSearcher($"Select * From Win32_Process Where ProcessID={testRunnerProcessId}");
            var processes =
                mos.Get().Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ParentProcessID"])).ToList();
            if (processes.Any())
            {
                return processes.First();
            }
            return -1;
        }
    }
}