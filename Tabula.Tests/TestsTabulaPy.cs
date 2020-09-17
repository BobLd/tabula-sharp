using System.Collections.Generic;
using System.Text;
using Tabula.Extractors;
using Tabula.Writers;
using Xunit;

namespace Tabula.Tests
{
    public class TestsTabulaPy
    {
        [Fact]
        public void Latice1()
        {
            // tabula.read_pdf(pdf_path, stream=False)

            PageArea page = UtilsForTesting.GetPage("Resources/data.pdf", 1);
            // data_lattice.csv was modified to add the last row, missing in tabula_py
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/data_lattice.csv");

            SpreadsheetExtractionAlgorithm se = new SpreadsheetExtractionAlgorithm();

            List<Table> tables = se.Extract(page);

            Assert.Single(tables);

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, tables[0]);
            Assert.Equal(expectedCsv, sb.ToString().Replace("\r\n", "\n"));
        }

        [Fact(Skip = "see PdfPig issue #217")]
        public void StreamNoGuess1()
        {
            // tabula.read_pdf(pdf_path, stream=True, guess=False)

            PageArea page = UtilsForTesting.GetPage("Resources/data.pdf", 1);
            // 
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/data_stream_noguess.csv");

            BasicExtractionAlgorithm se = new BasicExtractionAlgorithm();

            List<Table> tables = se.Extract(page);

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, tables[0]);
            Assert.Equal(expectedCsv, sb.ToString().Replace("\r\n", "\n"));
        }
    }
}
