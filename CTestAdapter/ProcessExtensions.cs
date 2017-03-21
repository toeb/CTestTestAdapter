using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace CTestTestAdapter
{
    public static class ProcessExtensions
    {
        public static List<Process> GetChildProcesses(this Process process)
        {
            var children = new List<Process>();
            var mos = new ManagementObjectSearcher(
                "Select * From Win32_Process Where ParentProcessID=" + process.Id);
            foreach (ManagementObject mo in mos.Get())
            {
                children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
            }
            return children;
        }
    }
}