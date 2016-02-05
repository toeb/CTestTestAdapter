using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace ImplemenationCTestTestAdapter
{
    public static class ProcessExtensions
    {
        public static List<Process> GetChildProcesses(this Process process)
        {
            List<Process> children = new List<Process>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                string.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));
            foreach (ManagementObject mo in mos.Get())
            {
                children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
            }
            return children;
        }
    }
}
