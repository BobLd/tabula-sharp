using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Tabula.Extractors;
using Tabula.Writers;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestSpreadsheetExtractor
    {
        public static TableRectangle[] EXPECTED_RECTANGLES = new TableRectangle[]
        {
            // top, left, width, height
            new TableRectangle(new PdfRectangle(18.0, 40.0, 18.0 + 208.0, 40.0 + 40.0)),  //new TableRectangle(40.0f, 18.0f, 208.0f, 40.0f),
            new TableRectangle(new PdfRectangle(18.0, 84.0, 18.0 + 962.0, 84.0 + 464.0))  //new TableRectangle(84.0f, 18.0f, 962.0f, 464.0f)
        };

        private static readonly Ruling[] VERTICAL_RULING_LINES = new[]
        {
            new Ruling(new PdfPoint(18.0, 40.0), new PdfPoint(18.0,   40.0 + 40.0)), // 40.0f, 18.0f, 0.0f, 40.0f),
            new Ruling(new PdfPoint(70.0, 44.0), new PdfPoint(70.0,   44.0 + 36.0)), // 44.0f, 70.0f, 0.0f, 36.0f),
            new Ruling(new PdfPoint(226.0, 40.0), new PdfPoint(226.0, 40.0 + 40.0)), // 40.0f, 226.0f, 0.0f, 40.0f)
        };

        private static readonly Ruling[] HORIZONTAL_RULING_LINES = new[]
        {
            new Ruling(new PdfPoint(18.0, 40.0), new PdfPoint(208.0 + 18.0, 40.0)), // 40.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 44.0), new PdfPoint(208.0 + 18.0, 44.0)), // 44.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 50.0), new PdfPoint(208.0 + 18.0, 50.0)), // 50.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 54.0), new PdfPoint(208.0 + 18.0, 54.0)), // 54.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 60.0), new PdfPoint(208.0 + 18.0, 60.0)), // 60.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 64.0), new PdfPoint(208.0 + 18.0, 64.0)), // 64.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 70.0), new PdfPoint(208.0 + 18.0, 70.0)), // 70.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 74.0), new PdfPoint(208.0 + 18.0, 74.0)), // 74.0f, 18.0f, 208.0f, 0.0f),
            new Ruling(new PdfPoint(18.0, 80.0), new PdfPoint(208.0 + 18.0, 80.0)), // 80.0f, 18.0f, 208.0f, 0.0f)
        };

        private static readonly Cell[] EXPECTED_CELLS = new Cell[]
        {
            // re-done in Excel
            new Cell(new PdfRectangle(18, 74, 70, 80)),
            new Cell(new PdfRectangle(70, 74, 226, 80)),
            new Cell(new PdfRectangle(18, 70, 70, 74)),
            new Cell(new PdfRectangle(70, 70, 226, 74)),
            new Cell(new PdfRectangle(18, 64, 70, 70)),
            new Cell(new PdfRectangle(70, 64, 226, 70)),
            new Cell(new PdfRectangle(18, 60, 70, 64)),
            new Cell(new PdfRectangle(70, 60, 226, 64)),
            new Cell(new PdfRectangle(18, 54, 70, 60)),
            new Cell(new PdfRectangle(70, 54, 226, 60)),
            new Cell(new PdfRectangle(18, 50, 70, 54)),
            new Cell(new PdfRectangle(70, 50, 226, 54)),
            new Cell(new PdfRectangle(18, 44, 70, 50)),
            new Cell(new PdfRectangle(70, 44, 226, 50)),
            new Cell(new PdfRectangle(18, 40, 226, 44)),
        };

        private static readonly Ruling[][] SINGLE_CELL_RULINGS = new[]
        {
            new Ruling[]
            {
                new Ruling(new PdfPoint(151.653545f, 185.66929f), new PdfPoint(380.73438f, 185.66929f)),
                new Ruling(new PdfPoint(151.653545f, 314.64567f), new PdfPoint(380.73438f, 314.64567f))
            },
            new Ruling[]
            {
                new Ruling(new PdfPoint(151.653545f, 185.66929f), new PdfPoint(151.653545f, 314.64567f)),
                new Ruling(new PdfPoint(380.73438f, 185.66929f), new PdfPoint(380.73438f, 314.64567f))
            }
        };

        private static readonly Ruling[][] TWO_SINGLE_CELL_RULINGS = new[]
        {
            new Ruling[]
            {
                new Ruling(new PdfPoint(151.653545f, 185.66929f), new PdfPoint(287.4074f, 185.66929f)),
                new Ruling(new PdfPoint(151.653545f, 262.101f), new PdfPoint(287.4074f, 262.101f)),
                new Ruling(new PdfPoint(232.44095f, 280.62992f), new PdfPoint(368.1948f, 280.62992f)),
                new Ruling(new PdfPoint(232.44095f, 357.06164f), new PdfPoint(368.1948f, 357.06164f))
            },
            new Ruling[]
            {
                new Ruling(new PdfPoint(151.653545f, 185.66929f), new PdfPoint(151.653545f, 262.101f)),
                new Ruling(new PdfPoint(287.4074f, 185.66929f), new PdfPoint(287.4074f, 262.101f)),
                new Ruling(new PdfPoint(232.44095f, 280.62992f), new PdfPoint(232.44095f, 357.06164f)),
                new Ruling(new PdfPoint(368.1948f, 280.62992f), new PdfPoint(368.1948f, 357.06164f))
            }
        };

        private static readonly Ruling[] EXTERNALLY_DEFINED_RULINGS = new Ruling[]
        {
            // height of page = 792
            // horizontal lines
            new Ruling(new PdfPoint(320.0,      792 - 285.0), new PdfPoint(564.4409,  792 - 285.0)),
            new Ruling(new PdfPoint(320.0,      792 - 457.0), new PdfPoint(564.4409,  792 - 457.0)),
            new Ruling(new PdfPoint(320.0,      792 - 331.0), new PdfPoint(564.4409,  792 - 331.0)),
            new Ruling(new PdfPoint(320.0,      792 - 315.0), new PdfPoint(564.4409,  792 - 315.0)),
            new Ruling(new PdfPoint(320.0,      792 - 347.0), new PdfPoint(564.4409,  792 - 347.0)),
            new Ruling(new PdfPoint(320.0,      792 - 363.0), new PdfPoint(564.44088, 792 - 363.0)),
            new Ruling(new PdfPoint(320.0,      792 - 379.0), new PdfPoint(564.44087, 792 - 379.0)),
            new Ruling(new PdfPoint(320.0,      792 - 395.5), new PdfPoint(564.44086, 792 - 395.5)),
            new Ruling(new PdfPoint(320.00006,  792 - 415.0), new PdfPoint(564.4409,  792 - 415.0)),
            new Ruling(new PdfPoint(320.00007,  792 - 431.0), new PdfPoint(564.4409,  792 - 431.0)),

            // vertical lines
            new Ruling(new PdfPoint(320.0,      792 - 457.0), new PdfPoint(320.0,     792 - 285.0)),
            new Ruling(new PdfPoint(565.0,      792 - 457.0), new PdfPoint(564.4409,  792 - 285.0)),
            new Ruling(new PdfPoint(470.5542,   792 - 457.0), new PdfPoint(470.36865, 792 - 285.0))
        };

        private static readonly Ruling[] EXTERNALLY_DEFINED_RULINGS2 = new Ruling[]
        {
            // height of page = 792
            // horizontal lines
            new Ruling(new PdfPoint(51.796964,  792 - 180.0), new PdfPoint(560.20312,       792 - 180.0)),
            new Ruling(new PdfPoint(51.797017,  792 - 219.0), new PdfPoint(560.2031,        792 - 219.0)),
            new Ruling(new PdfPoint(51.797,     792 - 239.0), new PdfPoint(560.2031,        792 - 239.0)),
            new Ruling(new PdfPoint(51.797,     792 - 262.0), new PdfPoint(560.20312,       792 - 262.0)),
            new Ruling(new PdfPoint(51.797,     792 - 283.50247), new PdfPoint(560.05024,   792 - 283.50247)),
            new Ruling(new PdfPoint(51.796964,  792 - 309.0), new PdfPoint(560.20312,       792 - 309.0)),
            new Ruling(new PdfPoint(51.796982,  792 - 333.0), new PdfPoint(560.20312,       792 - 333.0)),
            new Ruling(new PdfPoint(51.797,     792 - 366.0), new PdfPoint(560.20312,       792 - 366.0)),

            // vertical lines
            new Ruling(new PdfPoint(52.0,       792 - 366.0), new PdfPoint(51.797,      792 - 181.0)),
            new Ruling(new PdfPoint(208.62891,  792 - 366.0), new PdfPoint(208.62891,   792 - 181.0)),
            new Ruling(new PdfPoint(357.11328,  792 - 366.0), new PdfPoint(357.0,       792 - 181.0)),
            new Ruling(new PdfPoint(560.11328,  792 - 366.0), new PdfPoint(560.0,       792 - 181.0))
        };

        [Fact]
        public void TestLinesToCells()
        {
            List<Cell> cells = SpreadsheetExtractionAlgorithm.FindCells(HORIZONTAL_RULING_LINES.ToList(), VERTICAL_RULING_LINES.ToList());
            Utils.Sort(cells, new TableRectangle.ILL_DEFINED_ORDER());
            List<Cell> expected = EXPECTED_CELLS.ToList();
            Utils.Sort(expected, new TableRectangle.ILL_DEFINED_ORDER());

            Assert.Equal(expected.Count, cells.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i], cells[i]);
            }

            Assert.Equal(expected, cells);
        }

        [Fact]
        public void TestDetectSingleCell()
        {
            List<Cell> cells = SpreadsheetExtractionAlgorithm.FindCells(SINGLE_CELL_RULINGS[0].ToList(), SINGLE_CELL_RULINGS[1].ToList());
            Assert.Single(cells);
            Cell cell = cells[0];
            Assert.True(Utils.Feq(151.65355, cell.Left));
            Assert.True(Utils.Feq(185.6693, cell.Bottom)); // .getTop()
            Assert.True(Utils.Feq(229.08083, cell.Width));
            Assert.True(Utils.Feq(128.97636, cell.Height));
        }

        [Fact]
        public void TestDetectTwoSingleCells()
        {
            List<Cell> cells = SpreadsheetExtractionAlgorithm.FindCells(TWO_SINGLE_CELL_RULINGS[0].ToList(), TWO_SINGLE_CELL_RULINGS[1].ToList());
            Assert.Equal(2, cells.Count);
            // should not overlap
            Assert.False(cells[0].Intersects(cells[1]));
        }

        [Fact]//(Skip = "TODO")]
        public void TestFindSpreadsheetsFromCells()
        {
            var parse = UtilsForTesting.LoadCsvLines("Resources/csv/TestSpreadsheetExtractor-CELLS.csv");
            List<Cell> cells = new List<Cell>();
            foreach (var record in parse)
            {
                var top = double.Parse(record[0]);      // top
                var left = double.Parse(record[1]);     // left
                var width = double.Parse(record[2]);    // width
                var height = double.Parse(record[3]);   // height
                cells.Add(new Cell(new PdfRectangle(left, top, left + width, top + height)));
            }

            List<TableRectangle> expected = EXPECTED_RECTANGLES.ToList();
            Utils.Sort(expected, new TableRectangle.ILL_DEFINED_ORDER());
            List<TableRectangle> foundRectangles = SpreadsheetExtractionAlgorithm.FindSpreadsheetsFromCells(cells.Cast<TableRectangle>().ToList());
            Utils.Sort(foundRectangles, new TableRectangle.ILL_DEFINED_ORDER());

            Assert.Equal(foundRectangles, expected);
        }

        /*
        [Fact(Skip = "TODO Add assertions")]
        public void testSpreadsheetExtraction()
        {
            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/argentina_diputados_voting_record.pdf", 269.875f, 12.75f, 790.5f, 561f);
            SpreadsheetExtractionAlgorithm.findCells(page.getHorizontalRulings(), page.getVerticalRulings());
        }
        */

        [Fact]
        public void TestSpanningCells()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/spanning_cells.pdf", 1);
            string expectedJson = UtilsForTesting.LoadJson("Resources/json/spanning_cells.json");
            SpreadsheetExtractionAlgorithm se = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = se.Extract(page);
            Assert.Equal(2, tables.Count);

            var expectedJObject = (JArray)JsonConvert.DeserializeObject(expectedJson);

            StringBuilder sb = new StringBuilder();
            (new JSONWriter()).Write(sb, tables);
            var actualJObject = (JArray)JsonConvert.DeserializeObject(sb.ToString());

            double pageHeight = 842;
            double precision = 2;
            for (int i = 0; i < 2; i++)
            {
                Assert.Equal(expectedJObject[i]["extraction_method"], actualJObject[i]["extraction_method"]);

                Assert.True(Math.Abs(Math.Floor(pageHeight - expectedJObject[i]["top"].Value<double>()) - Math.Floor(actualJObject[i]["top"].Value<double>())) < precision);
                Assert.True(Math.Abs(Math.Floor(expectedJObject[i]["left"].Value<double>()) - Math.Floor(actualJObject[i]["left"].Value<double>())) < precision);
                Assert.True(Math.Abs(Math.Floor(expectedJObject[i]["width"].Value<double>()) - Math.Floor(actualJObject[i]["width"].Value<double>())) < precision);
                Assert.True(Math.Abs(Math.Floor(expectedJObject[i]["height"].Value<double>()) - Math.Floor(actualJObject[i]["height"].Value<double>())) < precision);
                Assert.True(Math.Abs(Math.Floor(expectedJObject[i]["right"].Value<double>()) - Math.Floor(actualJObject[i]["right"].Value<double>())) < precision);
                Assert.True(Math.Abs(Math.Floor(pageHeight - expectedJObject[i]["bottom"].Value<double>()) - Math.Floor(actualJObject[i]["bottom"].Value<double>())) < precision);

                var expectedData = (JArray)expectedJObject[i]["data"];
                var actualData = (JArray)actualJObject[i]["data"];
                Assert.Equal(expectedData.Count, actualData.Count);

                for (int r = 0; r < expectedData.Count; r++)
                {
                    var rowExpected = (JArray)expectedData[r];
                    var rowActual = (JArray)actualData[r];
                    Assert.Equal(rowExpected.Count, rowActual.Count);

                    for (int c = 0; c < rowExpected.Count; c++)
                    {
                        var cellExpected = (JObject)rowExpected[c];
                        var cellActual = (JObject)rowActual[c];

                        if (string.IsNullOrEmpty(cellExpected["text"].Value<string>())) continue; // empty cell have no coordinate data???

                        Assert.True(Math.Abs(Math.Floor(pageHeight - cellExpected["top"].Value<double>()) - Math.Floor(cellActual["top"].Value<double>())) < precision);
                        Assert.True(Math.Abs(Math.Floor(cellExpected["left"].Value<double>()) - Math.Floor(cellActual["left"].Value<double>())) < precision);
                        Assert.True(Math.Abs(Math.Floor(cellExpected["width"].Value<double>()) - Math.Floor(cellActual["width"].Value<double>())) < precision);
                        Assert.True(Math.Abs(Math.Floor(cellExpected["height"].Value<double>()) - Math.Floor(cellActual["height"].Value<double>())) < precision);
                        Assert.Equal(cellExpected["text"].Value<string>(), cellActual["text"].Value<string>());
                    }
                }
            }
            //Assert.Equal(expectedJson, sb.ToString());
        }

        [Fact]
        public void TestSpanningCellsToCsv()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/spanning_cells.pdf", 1);
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/spanning_cells.csv");
            SpreadsheetExtractionAlgorithm se = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = se.Extract(page);
            Assert.Equal(2, tables.Count);

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, tables);
            Assert.Equal(expectedCsv, sb.ToString().Replace("\r\n", "\n").Trim());
        }

        [Fact]
        public void TestIncompleteGrid()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/china.pdf", 1);
            SpreadsheetExtractionAlgorithm se = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = se.Extract(page);
            Assert.Equal(2, tables.Count);
        }

        [Fact]
        public void TestNaturalOrderOfRectanglesDoesNotBreakContract()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/us-017.pdf", 2);
            SpreadsheetExtractionAlgorithm se = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = se.Extract(page);

            string expected = "Project,Agency,Institution\r\nNanotechnology and its publics,NSF,Pennsylvania State University\r\n\"Public information and deliberation in nanoscience and\rnanotechnology policy (SGER)\",Interagency,\"North Carolina State\rUniversity\"\r\n\"Social and ethical research and education in agrifood\rnanotechnology (NIRT)\",NSF,Michigan State University\r\n\"From laboratory to society: developing an informed\rapproach to nanoscale science and engineering (NIRT)\",NSF,University of South Carolina\r\nDatabase and innovation timeline for nanotechnology,NSF,UCLA\r\nSocial and ethical dimensions of nanotechnology,NSF,University of Virginia\r\n\"Undergraduate exploration of nanoscience,\rapplications and societal implications (NUE)\",NSF,\"Michigan Technological\rUniversity\"\r\n\"Ethics and belief inside the development of\rnanotechnology (CAREER)\",NSF,University of Virginia\r\n\"All centers, NNIN and NCN have a societal\rimplications components\",\"NSF, DOE,\rDOD, and NIH\",\"All nanotechnology centers\rand networks\""; // \r\n

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, tables[0]);
            string result = sb.ToString().Trim();
            Assert.Equal(expected.Replace("\r\n", "\r"), result.Replace("\r\n", "\n").Replace("\n", "\r"));
        }

        [Fact]
        public void TestMergeLinesCloseToEachOther()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/20.pdf", 1);
            IReadOnlyList<Ruling> rulings = page.VerticalRulings;
            Assert.Equal(6, rulings.Count);

            double[] expectedRulings = new double[] { 105.554812, 107.522417, 160.57705, 377.172662, 434.963828, 488.268507 };

            var lefts = rulings.Select(x => x.Left).ToArray();
            for (int i = 0; i < rulings.Count; i++)
            {
                Assert.Equal(expectedRulings[i], rulings[i].Left, 2);
            }
        }

        [Fact(Skip = "fails as of v0.9.1a")]
        public void TestSpreadsheetWithNoBoundingFrameShouldBeSpreadsheet()
        {
            PageArea page = UtilsForTesting.GetAreaFromPage("Resources/spreadsheet_no_bounding_frame.pdf", 1, new PdfRectangle(58.9, 842 - 654.7, 536.12, 842 - 150.56)); // 842 - 150.56)); // 150.56f, 58.9f, 654.7f, 536.12f);
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/spreadsheet_no_bounding_frame.csv");

            SpreadsheetExtractionAlgorithm se = new SpreadsheetExtractionAlgorithm();
            bool isTabular = se.IsTabular(page);
            Assert.True(isTabular);
            List<Table> tables = se.Extract(page);

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, tables[0]);
            Assert.Equal(expectedCsv, sb.ToString());
        }

        [Fact(Skip = "TO DO")]
        public void TestExtractSpreadsheetWithinAnArea()
        {
            PageArea page = UtilsForTesting.GetAreaFromPage("Resources/puertos1.pdf", 1, new PdfRectangle(30.32142857142857, 793 - 554.8821428571429, 546.7964285714286, 793 - 273.9035714285714)); // 273.9035714285714f, 30.32142857142857f, 554.8821428571429f, 546.7964285714286f);
            SpreadsheetExtractionAlgorithm se = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = se.Extract(page);
            Table table = tables[0];
            Assert.Equal(15, table.Rows.Count);

            const string expected = "\"\",TM,M.U$S,TM,M.U$S,TM,M.U$S,TM,M.U$S,TM,M.U$S,TM,M.U$S,TM\n" +
                    "Peces vivos,1,25,1,23,2,38,1,37,2,67,2,89,1\n" +
                    "\"Pescado fresco\n" +
                    "o refrigerado.\n" +
                    "exc. filetes\",7.704,7.175,8.931,6.892,12.635,10.255,16.742,13.688,14.357,11.674,13.035,13.429,9.727\n" +
                    "\"Pescado congelado\n" +
                    "exc. filetes\",90.560,105.950,112.645,108.416,132.895,115.874,152.767,133.765,148.882,134.847,156.619,165.134,137.179\n" +
                    "\"Filetes y demás car-\n" +
                    "nes de pescado\",105.434,200.563,151.142,218.389,152.174,227.780,178.123,291.863,169.422,313.735,176.427,381.640,144.814\n" +
                    "\"Pescado sec./sal./\n" +
                    "en salm. har./pol./\n" +
                    "pell. aptos\n" +
                    "p/c humano\",6.837,14.493,6.660,9.167,14.630,17.579,18.150,21.302,18.197,25.739,13.460,23.549,11.709\n" +
                    "Crustáceos,61.691,375.798,52.488,251.043,47.635,387.783,27.815,217.443,7.123,86.019,39.488,373.583,45.191\n" +
                    "Moluscos,162.027,174.507,109.436,111.443,90.834,104.741,57.695,109.141,98.182,206.304,187.023,251.352,157.531\n" +
                    "\"Prod. no exp. en\n" +
                    "otros capítulos.\n" +
                    "No apto p/c humano\",203,328,7,35,521,343,\"1,710\",\"1,568\",125,246,124,263,131\n" +
                    "\"Grasas y aceites de\n" +
                    "pescado y mamíferos\n" +
                    "marinos\",913,297,\"1,250\",476,\"1,031\",521,\"1,019\",642,690,483,489,710,959\n" +
                    "\"Extractos y jugos de\n" +
                    "pescado y mariscos\",5,25,1,3,4,4,31,93,39,117,77,230,80\n" +
                    "\"Preparaciones y con-\n" +
                    "servas de pescado\",846,\"3,737\",\"1,688\",\"4,411\",\"1,556\",\"3,681\",\"2,292\",\"5,474\",\"2,167\",\"7,494\",\"2,591\",\"8,833\",\"2,795\"\n" +
                    "\"Preparaciones y con-\n" +
                    "servas de mariscos\",348,\"3,667\",345,\"1,771\",738,\"3,627\",561,\"2,620\",607,\"3,928\",314,\"2,819\",250\n" +
                    "\"Harina, polvo y pe-\n" +
                    "llets de pescado.No\n" +
                    "aptos p/c humano\",\"16,947\",\"8,547\",\"11,867\",\"6,315\",\"32,528\",\"13,985\",\"37,313\",\"18,989\",\"35,787\",\"19,914\",\"37,821\",\"27,174\",\"30,000\"\n" +
                    "TOTAL,\"453,515\",\"895,111\",\"456,431\",\"718,382\",\"487,183\",\"886,211\",\"494,220\",\"816,623\",\"495,580\",\"810,565\",\"627,469\",\"1,248,804\",\"540,367\"\n";

            // TODO add better assertions
            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, tables[0]);
            string result = sb.ToString();

            //List<CSVRecord> parsedExpected = org.apache.commons.csv.CSVParser.parse(expected, CSVFormat.EXCEL).getRecords();
            //List<CSVRecord> parsedResult = org.apache.commons.csv.CSVParser.parse(result, CSVFormat.EXCEL).getRecords();
            using (var csv = new CsvReader(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(result))), CultureInfo.InvariantCulture))
            {
                /*
                Assert.Equal(parsedResult.Count, parsedExpected.Count);
                for (int i = 0; i < parsedResult.Count; i++)
                {
                    Assert.Equal(parsedResult[i].size(), parsedExpected[i].size());
                }
                */
            }
        }

        [Fact]
        public void TestAlmostIntersectingRulingsShouldIntersect()
        {
            Ruling v = new Ruling(new PdfPoint(555.960876f, 271.569641f), new PdfPoint(555.960876f, 786.899902f));
            Ruling h = new Ruling(new PdfPoint(25.620499f, 786.899902f), new PdfPoint(555.960754f, 786.899902f));
            SortedDictionary<PdfPoint, Ruling[]> m = Ruling.FindIntersections(new Ruling[] { h }.ToList(), new Ruling[] { v }.ToList());
            Assert.Single(m.Values);
        }

        /*
        [Fact(Skip = "TODO add assertions")]
        public void testDontRaiseSortException()
        {
            //PageArea page = UtilsForTesting.getAreaFromPage("Resources/us-017.pdf", 2, 446.0f, 97.0f, 685.0f, 520.0f);
            //page.getText();
            //SpreadsheetExtractionAlgorithm bea = new SpreadsheetExtractionAlgorithm();
            //bea.extract(page)[0];
        }
        */

        [Fact(Skip = "fails as of v0.9.1a")]
        public void TestShouldDetectASingleSpreadsheet()
        {
            PageArea page = UtilsForTesting.GetAreaFromPage("Resources/offense.pdf", 1, new PdfRectangle(16.44, 792 - 680.85, 597.84, 792 - 16.44)); // 68.08f, 16.44f, 680.85f, 597.84f);
            SpreadsheetExtractionAlgorithm bea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = bea.Extract(page);
            Assert.Single(tables);
        }

        [Fact]
        public void TestExtractTableWithExternallyDefinedRulings()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/us-007.pdf", 1);
            SpreadsheetExtractionAlgorithm bea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = bea.Extract(page, EXTERNALLY_DEFINED_RULINGS.ToList());
            Assert.Single(tables);
            Table table = tables[0];
            Assert.Equal(18, table.Cells.Count);

            var rows = table.Rows;

            Assert.Equal("Payroll Period", rows[0][0].GetText());
            Assert.Equal("One Withholding\rAllowance", rows[0][1].GetText());
            Assert.Equal("Weekly", rows[1][0].GetText());
            Assert.Equal("$71.15", rows[1][1].GetText());
            Assert.Equal("Biweekly", rows[2][0].GetText());
            Assert.Equal("142.31", rows[2][1].GetText());
            Assert.Equal("Semimonthly", rows[3][0].GetText());
            Assert.Equal("154.17", rows[3][1].GetText());
            Assert.Equal("Monthly", rows[4][0].GetText());
            Assert.Equal("308.33", rows[4][1].GetText());
            Assert.Equal("Quarterly", rows[5][0].GetText());
            Assert.Equal("925.00", rows[5][1].GetText());
            Assert.Equal("Semiannually", rows[6][0].GetText());
            Assert.Equal("1,850.00", rows[6][1].GetText());
            Assert.Equal("Annually", rows[7][0].GetText());
            Assert.Equal("3,700.00", rows[7][1].GetText());
            Assert.Equal("Daily or Miscellaneous\r(each day of the payroll period)", rows[8][0].GetText());
            Assert.Equal("14.23", rows[8][1].GetText());
        }

        [Fact]
        public void TestAnotherExtractTableWithExternallyDefinedRulings()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/us-024.pdf", 1);
            SpreadsheetExtractionAlgorithm bea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = bea.Extract(page, EXTERNALLY_DEFINED_RULINGS2.ToList());
            Assert.Single(tables);
            Table table = tables[0];

            Assert.Equal("Total Supply", table.Rows[4][0].GetText());
            Assert.Equal("6.6", table.Rows[6][2].GetText());
        }

        [Fact]
        public void TestSpreadsheetsSortedByTopAndRight()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/sydney_disclosure_contract.pdf", 1);

            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = sea.Extract(page);
            for (int i = 1; i < tables.Count; i++)
            {
                Assert.True(tables[i - 1].Top >= tables[i].Top); // Assert.True(tables[i - 1].getTop() <= tables[i].getTop());
            }
        }

        [Fact]
        public void TestDontStackOverflowQuicksort()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/failing_sort.pdf", 1);

            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = sea.Extract(page);
            for (int i = 1; i < tables.Count; i++)
            {
                Assert.True(tables[i - 1].Top >= tables[i].Top); //Assert.True(tables[i - 1].getTop() <= tables[i].getTop());
            }
        }

        [Fact(Skip = "RtL text to do later")]
        public void TestRTL()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/arabic.pdf", 1);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = sea.Extract(page);
            // Assert.Equal(1, tables.size());
            Table table = tables[0];

            var rows = table.Rows;

            Assert.Equal("اسمي سلطان", rows[1][1].GetText());
            Assert.Equal("من اين انت؟", rows[2][1].GetText());
            Assert.Equal("1234", rows[3][0].GetText());
            Assert.Equal("هل انت شباك؟", rows[4][0].GetText());
            Assert.Equal("انا من ولاية كارولينا الشمال", rows[2][0].GetText()); // conjoined lam-alif gets missed
            Assert.Equal("اسمي Jeremy في الانجليزية", rows[4][1].GetText()); // conjoined lam-alif gets missed
            Assert.Equal("عندي 47 قطط", rows[3][1].GetText()); // the real right answer is 47.
            Assert.Equal("Jeremy is جرمي in Arabic", rows[5][0].GetText()); // the real right answer is 47.
            Assert.Equal("مرحباً", rows[1][0].GetText()); // really ought to be ً, but this is forgiveable for now

            // there is one remaining problems that are not yet addressed
            // - diacritics (e.g. Arabic's tanwinً and probably Hebrew nekudot) are put in the wrong place.
            // this should get fixed, but this is a good first stab at the problem.

            // these (commented-out) tests reflect the theoretical correct answer,
            // which is not currently possible because of the two problems listed above
            // Assert.Equal("مرحباً",                       table.getRows()[0][0].getText()); // really ought to be ً, but this is forgiveable for now
        }


        [Fact(Skip = "RtL text to do later")]
        public void TestRealLifeRTL()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/mednine.pdf", 1);
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = sea.Extract(page);
            Assert.Single(tables);
            Table table = tables[0];
            var rows = table.Rows;

            Assert.Equal("الانتخابات التشريعية  2014", rows[0][0].GetText()); // the doubled spaces might be a bug in my implementation. // bobld: missing space or worng words order
            Assert.Equal("ورقة كشف نتائج دائرة مدنين", rows[1][0].GetText());
            Assert.Equal("426", rows[4][0].GetText());
            Assert.Equal("63", rows[4][1].GetText());
            Assert.Equal("43", rows[4][2].GetText());
            Assert.Equal("56", rows[4][3].GetText());
            Assert.Equal("58", rows[4][4].GetText());
            Assert.Equal("49", rows[4][5].GetText());
            Assert.Equal("55", rows[4][6].GetText());
            Assert.Equal("33", rows[4][7].GetText());
            Assert.Equal("32", rows[4][8].GetText());
            Assert.Equal("37", rows[4][9].GetText());
            Assert.Equal("قائمة من أجل تحقيق سلطة الشعب", rows[4][10].GetText());

            // there is one remaining problems that are not yet addressed
            // - diacritics (e.g. Arabic's tanwinً and probably Hebrew nekudot) are put in the wrong place.
            // this should get fixed, but this is a good first stab at the problem.

            // these (commented-out) tests reflect the theoretical correct answer,
            // which is not currently possible because of the two problems listed above
            //Assert.Equal("مرحباً", rows[0][0].getText()); // really ought to be ً, but this is forgiveable for now
        }

        [Fact]
        public void TestExtractColumnsCorrectly3()
        {
            // top,     left,   bottom,  right
            // 106.01f, 48.09f, 227.31f, 551.89f
            // bottom = 792 - 227.31 = 564.69
            // top =  792 - 106.01 = 685.99
            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/frx_2012_disclosure.pdf", new PdfRectangle(48.09, 564.69, 551.89, 684.99)); // changed 685.99 to 684.99 because was adding an empty row at the top
            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            Table table = sea.Extract(page)[0];

            Assert.Equal("REGIONAL PULMONARY & SLEEP\rMEDICINE", table.Rows[8][1].GetText());
        }

        [Fact]
        public void TestSpreadsheetExtractionIssue656()
        {
            // page height = 482, width 762.3 // 612
            // top,     left,    bottom,   right
            // 56.925f, 24.255f, 549.945f, 786.555f);
            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/Publication_of_award_of_Bids_for_Transport_Sector__August_2016.pdf", new PdfRectangle(24.255, 71, 786.555, 553));
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/Publication_of_award_of_Bids_for_Transport_Sector__August_2016.csv");

            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();
            List<Table> tables = sea.Extract(page);
            Assert.Single(tables);
            Table table = tables[0];

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).Write(sb, table);
            string result = sb.ToString();
            Assert.Equal(expectedCsv.Replace("\n", "\r"), result.Replace("\r\n", "\n").Replace("\n", "\r").Trim());

            /*
            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).write(sb, table);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var s = reader.ReadToEnd().Trim(); // trim to remove last new line

                // is there an issue with \r and \n?
                Assert.Equal(expectedCsv.Replace("\n", "\r"), s.Replace("\r\n", "\n").Replace("\n", "\r"));
            }
            */
        }
    }
}
