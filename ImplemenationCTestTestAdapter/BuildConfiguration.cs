using EnvDTE80;

namespace ImplemenationCTestTestAdapter
{
    public static class BuildConfiguration
    {
        public static string GetCurrentActiveConfiguration()
        {
            var dte = (DTE2) DTEHelper.GetCurrent();
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