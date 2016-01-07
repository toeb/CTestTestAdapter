using EnvDTE;
using System;
using System.Management;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ImplemenationCTestTestAdapter
{
    /// <summary>
    ///  FIXME: After a few tests, it seems impossible to inject an
    ///  instance of the IServiceProvider into CTestExecutor, so we
    ///  must use a static solution instead using crazy interop stuff.
    /// </summary>
    /// <remarks>
    ///  Taken from here: https://github.com/getgauge/gauge-visualstudio/blob/master/Gauge.VisualStudio/Helpers/DTEHelper.cs
    /// </remarks>
    internal static class DTEHelper
    {
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        internal static DTE GetCurrent()
        {
            var testRunnerProcess = System.Diagnostics.Process.GetCurrentProcess();
            if (!"vstest.executionengine.x86".Equals(testRunnerProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                return null;

            string progId = String.Format("!{0}.DTE.{1}:{2}", "VisualStudio", "12.0",
                GetVisualStudioProcessId(testRunnerProcess.Id));

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
                        if (runningObjectMoniker != null)
                        {
                            runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // do nothing.
                    }

                    if (String.IsNullOrEmpty(name) || !String.Equals(name, progId, StringComparison.Ordinal)) continue;

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
                new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ProcessID={0}",
                    testRunnerProcessId));
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