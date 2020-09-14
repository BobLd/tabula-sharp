using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tabula.Csv;
using Tabula.Extractors;
using Tabula.Json;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestWriters
    {
        private static String EXPECTED_CSV_WRITER_OUTPUT = "\"ABDALA de MATARAZZO, Norma Amanda\",Frente Cívico por Santiago,Santiago del Estero,AFIRMATIVO";

        private Table getTable()
        {
            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/argentina_diputados_voting_record.pdf", new PdfRectangle(12.75, 55.0, 561, 567)); // 269.875f, 12.75f, 790.5f, 561f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];
            return table;
        }

        private List<Table> getTables()
        {
            PageArea page = UtilsForTesting.getPage("Resources/twotables.pdf", 1);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            return sea.extract(page);
        }

        [Fact]
        public void testCSVWriter()
        {
            string expectedCsv = UtilsForTesting.loadCsv("Resources/csv/argentina_diputados_voting_record.csv");
            Table table = this.getTable();
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
                (new CSVWriter()).write(sb, table);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line
                String[] lines = s.Split("\r\n"); // "\\r?\\n"
                Assert.Equal(EXPECTED_CSV_WRITER_OUTPUT, lines[0]);
                Assert.Equal(expectedCsv, s.Replace("\r\n", "\n"));
            }
        }

        // TODO Add assertions
        [Fact] //(Skip = "Need to implement TSVWriter")]
        public void testTSVWriter()
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

            Table table = this.getTable();
            StringBuilder sb = new StringBuilder();
            (new TSVWriter()).write(sb, table);
            String s = sb.ToString();
            //System.out.println(s);
            String[] lines = s.Split("\r\n");
            Assert.Equal(lines[0], EXPECTED_CSV_WRITER_OUTPUT);
        }


        [Fact]
        public void testJSONWriter()
        {
            string expectedJson = UtilsForTesting.loadJson("Resources/json/argentina_diputados_voting_record_new.json"); // argentina_diputados_voting_record.json
            Table table = this.getTable();

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new JSONWriter()).write(sb, table);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd();

                Assert.Equal(expectedJson, s);
            }
        }

        [Fact] //(Skip = "SpreadsheetExtractionAlgorithm not implemented")]
        public void testJSONSerializeInfinity()
        {
            String expectedJson = UtilsForTesting.loadJson("Resources/json/schools.json");
            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/schools.pdf", new PdfRectangle(double.NaN, double.NaN, double.NaN, double.NaN)); // 53.74f, 16.97f, 548.74f, 762.3f);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            Table table = sea.extract(page)[0]; //.get(0);

            StringBuilder sb = new StringBuilder();
            (new JSONWriter()).write(sb, table);
            string s = sb.ToString();
            Assert.Equal(expectedJson, s);
        }


        [Fact] //(Skip = "SpreadsheetExtractionAlgorithm not implemented + get correct area.")]
        public void testCSVSerializeInfinity()
        {
            // 612
            String expectedCsv = UtilsForTesting.loadCsv("Resources/csv/schools.csv");
            // 53.74f, 16.97f, 548.74f, 762.3f)
            //Table table = bea.extract(page.getArea(new PdfRectangle(148.44, 543 - (711.875 - 299.625), 452.32, 543)))[0]; //299.625f, 148.44f, 711.875f, 452.32f))[0];

            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/schools.pdf", new PdfRectangle(double.NaN, double.NaN, double.NaN, double.NaN));
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            Table table = sea.extract(page)[0];

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).write(sb, table);
            String s = sb.ToString();
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

        [Fact] //(Skip = "SpreadsheetExtractionAlgorithm not implemented")]
        public void testJSONSerializeTwoTables()
        {
            string expectedJson = UtilsForTesting.loadJson("Resources/json/twotables.json");
            List<Table> tables = this.getTables();
            //StringBuilder sb = new StringBuilder();
            //(new JSONWriter()).write(sb, tables);
            //String s = sb.toString();

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
        }


        [Fact]//(Skip = "SpreadsheetExtractionAlgorithm not implemented.")]
        public void testCSVSerializeTwoTables()
        {
            String expectedCsv = UtilsForTesting.loadCsv("Resources/csv/twotables.csv");
            List<Table> tables = this.getTables();

            /*
            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).write(sb, tables);
            String s = sb.toString();
            assertEquals(expectedCsv, s);
            */

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).write(sb, tables);
                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line
                Assert.Equal(expectedCsv, s);
            }
        }


        [Fact] //(Skip = "SpreadsheetExtractionAlgorithm not implemented + get correct area.")]
        public void testCSVMultilineRow()
        {
            String expectedCsv = UtilsForTesting.loadCsv("Resources/csv/frx_2012_disclosure.csv");
            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/frx_2012_disclosure.pdf", new PdfRectangle(double.NaN, double.NaN, double.NaN, double.NaN)); // 53.0f, 49.0f, 735.0f, 550.0f);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            Table table = sea.extract(page)[0];

            /*
            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).write(sb, table);
            String s = sb.toString();
            assertEquals(expectedCsv, s);
            */

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).write(sb, table);
                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line
                Assert.Equal(expectedCsv, s);
            }
        }
    }
}
