using System.IO;
using Xunit;

namespace Tabula.Tests
{
    public class TestsUnicodeExtensions
    {
        [Fact]
        public void Unicode13IntValues()
        {
            foreach (var line in File.ReadAllLines("Resources/UnicodeData_13.0.txt"))
            {
                var properties = line.Split(';');

                int int32 = int.Parse(properties[0], System.Globalization.NumberStyles.AllowHexSpecifier);
                string expected = properties[4];
                Assert.Equal(expected, UnicodeExtensions.getDirectionality(int32));
            }
        }
    }
}
