using System;
using System.ComponentModel.Composition;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace ImplemenationCTestTestAdapter
{
    public class BuildConfiguration
    {
        private readonly IServiceProvider _serviceProvider;
        private DTE _dte;

        public bool HasDte
        {
            get
            {
                SetDte();
                return _dte != null;
            }
        }

        public string SolutionDir
        {
            get
            {
                SetDte();
                return _dte == null ? string.Empty : Path.GetDirectoryName(_dte.Solution.FileName);
            }
        }

        public string ConfigurationName
        {
            get
            {
                SetDte();
                var p = _dte?.Solution.SolutionBuild;
                var sc = p?.ActiveConfiguration;
                return sc == null ? string.Empty : sc.Name;
            }
        }

        public BuildConfiguration(
            [Import(typeof (SVsServiceProvider))] IServiceProvider serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
            SetDte();
        }

        private void SetDte()
        {
            if (_dte != null)
            {
                return;
            }
            _dte = DteHelper.GetCurrent(_serviceProvider);
        }
    }
}