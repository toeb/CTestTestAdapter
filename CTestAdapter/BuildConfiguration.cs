using System;
using System.ComponentModel.Composition;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace CTestAdapter
{
  public class BuildConfiguration
  {
    private readonly IServiceProvider _serviceProvider;
    private DTE _dte;

    public DTE Dte
    {
      get { return this._dte; }
    }

    public bool HasDte
    {
      get
      {
        this.SetDte();
        return this._dte != null;
      }
    }

    public string SolutionDir
    {
      get
      {
        this.SetDte();
        return this._dte == null ?
            string.Empty : Path.GetDirectoryName(this._dte.Solution.FileName);
      }
    }

    public string ConfigurationName
    {
      get
      {
        this.SetDte();
        if (null != this._dte)
        {
          var p = this._dte.Solution.SolutionBuild;
          if (null != p)
          {
            var sc = p.ActiveConfiguration;
            return sc == null ? string.Empty : sc.Name;
          }
        }
        return string.Empty;
      }
    }

    public BuildConfiguration(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider = null)
    {
      this._serviceProvider = serviceProvider;
      this.SetDte();
    }

    private void SetDte()
    {
      if (this._dte != null)
      {
        return;
      }
      this._dte = DteHelper.GetCurrent(this._serviceProvider);
    }
  }
}
