using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace CTestTestAdapter
{
    public class CTestInfo
    {
        public struct TestInfo
        {
            public string Name;
            public int Number;
        }

        public const string CTestInfoFileName = "CTestAdapter.txt";

        private const string RegexFieldNameNumber = "number";
        private const string RegexFieldNameTestname = "testname";

        private static readonly Regex NameNumberRegex =
            new Regex(@"#:(?<" + RegexFieldNameNumber + @">\d+) t:(?<" + RegexFieldNameTestname + @">\S+)");

        private static readonly Regex LinesRegex = new Regex(@"v:\d+");

        private readonly List<TestInfo> _tests;
        private bool _fileRead;

        public CTestInfo()
        {
            _fileRead = false;
            _tests = new List<TestInfo>();
        }

        public bool TestExists(string testname)
        {
            foreach (var v in _tests)
            {
                if (v.Name == testname)
                {
                    return true;
                }
            }
            return false;
        }

        public TestInfo this[int number]
        {
            get { return _tests.FirstOrDefault(item => item.Number == number); }
        }

        public TestInfo this[string name]
        {
            get { return _tests.FirstOrDefault(item => string.Equals(item.Name, name)); }
        }

        public List<TestInfo> Tests
        {
            get { return _tests; }
        }

        public bool FileRead
        {
            get { return _fileRead; }
        }

        public void ReadTestInfoFile(string fileName)
        {
            _tests.Clear();
            if (!File.Exists(fileName))
            {
                return;
            }
            var str = new StreamReader(fileName);
            while (!str.EndOfStream)
            {
                var line = str.ReadLine();
                var match = NameNumberRegex.Match(line);
                if (match == null)
                {
                    continue;
                }
                var name = match.Groups[RegexFieldNameTestname].Value;
                var numberStr = match.Groups[RegexFieldNameNumber].Value;
                int number;
                int.TryParse(numberStr, out number);
                var info = new TestInfo()
                {
                    Name = name,
                    Number = number,
                };
                _tests.Add(info);
            }
            _fileRead = true;
            str.Close();
        }

        public void WriteTestInfoFile(string fileName)
        {
            if (Tests.Count == 0)
            {
                return;
            }
            var str = new StreamWriter(fileName);
            foreach (var t in Tests)
            {
                str.WriteLine("#:" + t.Number + " t:" + t.Name);
            }
            str.Flush();
            str.Close();
        }
    }
}
