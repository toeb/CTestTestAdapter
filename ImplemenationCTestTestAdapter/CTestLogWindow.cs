using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ImplemenationCTestTestAdapter
{
    internal class CTestLogWindow
    {
        private readonly IVsOutputWindowPane _outWindowPane;
        private static Guid _outWindowGuid = new Guid("231F0144-E723-4FD5-A62B-DADCFF615067");
        private const string OutWindowTitle = "CTestAdapter";

        public bool AutoRaise { get; set; } = true;

        public bool Enabled { get; set; } = true;

        public CTestLogWindow()
        {
            var outWindow = Package.GetGlobalService(typeof (SVsOutputWindow)) as IVsOutputWindow;
            outWindow?.CreatePane(ref _outWindowGuid, OutWindowTitle, 1, 1);
            outWindow?.GetPane(ref _outWindowGuid, out _outWindowPane);
        }

        public void OutputLine(string line)
        {
            OutputString(line + "\n");
        }

        public void OutputString(string line)
        {
            if (!Enabled)
            {
                return;
            }
            if (line == null)
            {
                return;
            }
            _outWindowPane?.OutputString(line);
            if (AutoRaise)
            {
                _outWindowPane?.Activate();
            }
        }

        public void Activate()
        {
            _outWindowPane?.Activate();
        }
    }
}