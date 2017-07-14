using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace CTestAdapter
{
  class CTestAdapterOptionPage : DialogPage
  {
    private bool enableLogging = false;

    [Category("CTest")]
    [DisplayName("Enable Logging")]
    [Description("Enable Logging of ctest results")]
    public bool EnableLogging
    {
      get { return this.enableLogging; }
      set { this.enableLogging = value; }
    }
  }
}
