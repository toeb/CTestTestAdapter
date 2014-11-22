using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.IO;

namespace ImplemenationCTestTestAdapter
{
  /// <summary>
  /// class represents a CTest
  /// </summary>
  public class CTestCase
  {
    /// <summary>
    /// implicit conversion from TestCase  returns null if invalid
    /// </summary>
    /// <param name="tc"></param>
    /// <returns></returns>
    public static implicit operator CTestCase(TestCase tc)
    {
      if (tc == null) return null;
      return Parse(tc.Source);
    }
    /// <summary>
    /// implicit conversion to TestCase returns null if invlaid
    /// </summary>
    /// <param name="tc"></param>
    /// <returns></returns>
    public static implicit operator TestCase(CTestCase tc){
      if (tc == null) return null;
      return new TestCase(tc.Name, CTestExecutor.ExecutorUri, tc.Id);
    }
    /// <summary>
    /// Id currently holds a the path to a ctestcase file which contains the name of the test
    /// @todo  Never ever ever create a file in a get accessor - what am I thinking...
    /// </summary>
    public string Id
    {
      get
      {        
        var p = Path.GetFullPath(CMakeBinaryDir + "\\" + Number + ".ctestcase");//.Replace("\\", "_");
        if (!File.Exists(p)) File.WriteAllText(p,Name);
        return p;
      }
    }
    /// <summary>
    /// the cmake binary dir (where you would execute ctest)
    /// </summary>
    public string CMakeBinaryDir { get; set; }
    /// <summary>
    /// the name of the test
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// the number of the test
    /// </summary>
    public string Number { get; set; }
  
    /// <summary>
    /// creates the CTestCase from the filename 
    /// returns null if file is not valid
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    internal static CTestCase Parse(string source)
    {
     //C:\Users\Tobi\Documents\projects\mbslib\packages\core\build\1.ctestcase
      var name = Path.GetFileNameWithoutExtension(source);
      var ext = Path.GetExtension(source);
      var dir = Path.GetDirectoryName(source);
  
      if (!ext.EndsWith("ctestcase")) return null;
  
      var testname = File.ReadAllText(source);
      return new CTestCase()
      {
        Name =testname,
        Number = name,
        CMakeBinaryDir = dir        
      };
  
    }
  }
}
