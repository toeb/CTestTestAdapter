using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace ImplemenationCTestTestAdapter
{
    public static class BuildConfiguration
    {
        public static string GetCurrentActiveConfiguration()
        {
            var dte =
                (DTE2) ServiceProvider.GlobalProvider.GetService(typeof (DTE)) ??
                (DTE2) DteHelper.GetCurrent();
            if (dte == null)
            {
                CTestLogger.Instance.LogMessage(
                    "BuildConfiguration.GetCurrentActiveConfiguration: DTE object not found!");
            }
            var p = dte?.Solution.SolutionBuild;
            var sc = p?.ActiveConfiguration;
            return sc == null ? string.Empty : sc.Name;
        }
    }
}