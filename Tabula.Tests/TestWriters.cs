using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tabula.Extractors;
using Tabula.Writers;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestWriters
    {
        private static string EXPECTED_CSV_WRITER_OUTPUT = "\"ABDALA de MATARAZZO, Norma Amanda\",Frente Cívico por Santiago,Santiago del Estero,AFIRMATIVO";

        private Table GetTable()
        {
            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/argentina_diputados_voting_record.pdf", new PdfRectangle(12.75, 55.0, 561, 567)); // 269.875f, 12.75f, 790.5f, 561f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            return bea.Extract(page)[0];
        }

        private List<Table> GetTables()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/twotables.pdf", 1);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            return sea.Extract(page);
        }

        [Fact]
        public void TestCSVWriter()
        {
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/argentina_diputados_voting_record.csv");
            Table table = this.GetTable();
            /*
            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).write(sb, table);
            String s = sb.ToString();
            String[] lines = s.Split("\\r?\\n");
            assertEquals(EXPECTED_CSV_WRITER_OUTPUT, lines[0]);
            assertEquals(expectedCsv, s);
            */

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).Write(sb, table);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line
                string[] lines = s.Split("\r\n"); // "\\r?\\n"
                Assert.Equal(EXPECTED_CSV_WRITER_OUTPUT, lines[0]);
                Assert.Equal(expectedCsv, s.Replace("\r\n", "\n"));
            }
        }

        // TODO Add assertions
        [Fact] //(Skip = "Need to implement TSVWriter")]
        public void TestTSVWriter()
        {
            /*
            Table table = this.getTable();
            StringBuilder sb = new StringBuilder();
            (new TSVWriter()).write(sb, table);
            String s = sb.toString();
            //System.out.println(s);
            //String[] lines = s.split("\\r?\\n");
            //assertEquals(lines[0], EXPECTED_CSV_WRITER_OUTPUT);
            */

            Table table = this.GetTable();
            StringBuilder sb = new StringBuilder();
            (new TSVWriter()).Write(sb, table);
            string s = sb.ToString();
            //System.out.println(s);
            string[] lines = s.Replace("\r\n", "\n").Replace("\n", "\r\n").Split("\r\n");
            Assert.Equal(lines[0], EXPECTED_CSV_WRITER_OUTPUT);
        }

        [Fact]
        public void TestJSONWriter()
        {
            string expectedJson = UtilsForTesting.LoadJson("Resources/json/argentina_diputados_voting_record_new.json"); // argentina_diputados_voting_record.json
            Table table = this.GetTable();

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new JSONWriter()).Write(sb, table);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd();

                Assert.Equal(expectedJson, s);
            }
        }

        [Fact(Skip = "fails as of v0.9.1a + get correct area.")]
        public void TestJSONSerializeInfinity()
        {
            string expectedJson = UtilsForTesting.LoadJson("Resources/json/schools.json");
            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/schools.pdf", new PdfRectangle(double.NaN, double.NaN, double.NaN, double.NaN)); // 53.74f, 16.97f, 548.74f, 762.3f);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            Table table = sea.Extract(page)[0]; //.get(0);

            StringBuilder sb = new StringBuilder();
            (new JSONWriter()).Write(sb, table);
            string s = sb.ToString();
            Assert.Equal(expectedJson, s);
        }

        [Fact(Skip = "fails as of v0.9.1a")]
        public void TestCSVSerializeInfinity()
        {
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/schools.csv");
            // top,    left,   bottom,  right              // page height = 612
            // 53.74f, 16.97f, 548.74f, 762.3f)

            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/schools.pdf", new PdfRectangle(16.97, 612 - 548.74, 762.3, 612 - 53.74-1)); // remove 1 because add an empty line at the top if not
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            Table table = sea.Extract(page)[0];

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, table);
            string s = sb.ToString();
            Assert.Equal(expectedCsv.Trim(), s.Replace("\r\n", "\n"));

            /*
            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).write(sb, table);
                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line
                Assert.Equal(expectedCsv, s);
            }
            */
        }

        [Fact(Skip = "fails as of v0.9.1a")]
        public void TestJSONSerializeTwoTables()
        {
            string expectedJson = UtilsForTesting.LoadJson("Resources/json/twotables.json");
            List<Table> tables = this.GetTables();

            StringBuilder sb = new StringBuilder();
            (new JSONWriter()).Write(sb, tables);
            string s = sb.ToString();
            Assert.Equal(expectedJson, s);

            /*
            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new JSONWriter()).write(sb, tables);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd();

                //File.WriteAllText("twotables_new.json", s);

                Assert.Equal(expectedJson, s);

                // Gson gson = new Gson();
                //JsonArray json = gson.fromJson(s, JsonArray.class);
                //assertEquals(2, json.size());
                var json = JsonConvert.DeserializeObject<List<Table>>(s);
                Assert.Equal(2, json.Count);
            }
            */
        }

        [Fact(Skip = "fails as of v0.9.1a")]
        public void TestCSVSerializeTwoTables()
        {
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/twotables.csv");
            List<Table> tables = this.GetTables();

            /*
            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).write(sb, tables);
            String s = sb.toString();
            assertEquals(expectedCsv, s);
            */

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).Write(sb, tables);
                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line
                Assert.Equal(expectedCsv, s);
            }
        }

        [Fact(Skip = "fails as of v0.9.1a + get correct area.")]
        public void TestCSVMultilineRow()
        {
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/frx_2012_disclosure.csv");
            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/frx_2012_disclosure.pdf", new PdfRectangle(double.NaN, double.NaN, double.NaN, double.NaN)); // 53.0f, 49.0f, 735.0f, 550.0f);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            Table table = sea.Extract(page)[0];

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, table);
            string s = sb.ToString();
            Assert.Equal(expectedCsv, s);

            /*
            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).write(sb, table);
                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line
                Assert.Equal(expectedCsv, s);
            }
            */
        }
    }
}
