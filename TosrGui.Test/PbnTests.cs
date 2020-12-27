using Xunit;
using Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TosrGui.Test
{
    public class PbnTests
    {
        [Fact()]
        public void LoadSaveTest()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ExecuteAndCompare(path, "Example.pbn");
            ExecuteAndCompare(path, "CursusSlotdrive.pbn");
        }

        private static void ExecuteAndCompare(string path, string filename)
        {
            var filePath = Path.Combine(path, filename);
            var filePathActual = Path.Combine(path, $"{Path.GetFileNameWithoutExtension(filename)}_1.pbn");
            var pbn = new Pbn();
            pbn.Load(filePath);
            pbn.Save(filePathActual);

            var expected = File.ReadAllLines(filePath).Select(x => x.Trim()).Select(x => Regex.Replace(Regex.Replace(x, " =[0-9]= ", "\t"), " {2,}", "\t"));
            var actual = File.ReadAllLines(filePathActual);
            foreach (var line in actual)
            {
                Assert.Contains(line, expected);
            }
        }
    }
}