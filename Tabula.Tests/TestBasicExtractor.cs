using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Tabula.Extractors;
using Tabula.Writers;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestBasicExtractor
    {
        private static readonly string EU_002_PDF = "Resources/eu-002.pdf";
        private static readonly string[][] EU_002_EXPECTED = new[]
        {
            new[] {"",                                              "",                "Involvement of pupils in", ""},
            new[] {"",                                              "Preperation and", "Production of",            "Presentation and"}, // 'Presentation an' -> 'Presentation and' thanks to RectangleSpatialIndex.Expand()
            new[] {"",                                              "planing",         "materials",                "evaluation"},
            new[] {"Knowledge and awareness of different cultures", "0,2885",          "0,3974",                   "0,3904"},
            new[] {"Foreign language competence",                   "0,3057",          "0,4184",                   "0,3899"},
            new[] {"Social skills and abilities",                   "0,3416",          "0,3369",                   "0,4303"},
            new[] {"Acquaintance of special knowledge",             "0,2569",          "0,2909",                   "0,3557"},
            new[] {"Self competence",                               "0,3791",          "0,3320",                   "0,4617"}
        };

        private static readonly string ARGENTINA_DIPUTADOS_VOTING_RECORD_PDF = "Resources/argentina_diputados_voting_record.pdf";
        private static readonly string[][] ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED = new[]
        {
            new[] {"ABDALA de MATARAZZO, Norma Amanda",                 "Frente Cívico por Santiago",   "Santiago del Estero", "AFIRMATIVO"},
            new[] {"ALBRIEU, Oscar Edmundo Nicolas",                    "Frente para la Victoria - PJ", "Rio Negro",           "AFIRMATIVO"},
            new[] {"ALONSO, María Luz",                                 "Frente para la Victoria - PJ", "La Pampa",            "AFIRMATIVO"},
            new[] {"ARENA, Celia Isabel",                               "Frente para la Victoria - PJ", "Santa Fe",            "AFIRMATIVO"},
            new[] {"ARREGUI, Andrés Roberto",                           "Frente para la Victoria - PJ", "Buenos Aires",        "AFIRMATIVO"},
            new[] {"AVOSCAN, Herman Horacio",                           "Frente para la Victoria - PJ", "Rio Negro",           "AFIRMATIVO"},
            new[] {"BALCEDO, María Ester",                              "Frente para la Victoria - PJ", "Buenos Aires",        "AFIRMATIVO"},
            new[] {"BARRANDEGUY, Raúl Enrique",                         "Frente para la Victoria - PJ", "Entre Ríos",          "AFIRMATIVO"},
            new[] {"BASTERRA, Luis Eugenio",                            "Frente para la Victoria - PJ", "Formosa",             "AFIRMATIVO"},
            new[] {"BEDANO, Nora Esther",                               "Frente para la Victoria - PJ", "Córdoba",             "AFIRMATIVO"},
            new[] {"BERNAL, María Eugenia",                             "Frente para la Victoria - PJ", "Jujuy",               "AFIRMATIVO"},
            new[] {"BERTONE, Rosana Andrea",                            "Frente para la Victoria - PJ", "Tierra del Fuego",    "AFIRMATIVO"},
            new[] {"BIANCHI, María del Carmen",                         "Frente para la Victoria - PJ", "Cdad. Aut. Bs. As.",  "AFIRMATIVO"},
            new[] {"BIDEGAIN, Gloria Mercedes",                         "Frente para la Victoria - PJ", "Buenos Aires",        "AFIRMATIVO"},
            new[] {"BRAWER, Mara",                                      "Frente para la Victoria - PJ", "Cdad. Aut. Bs. As.",  "AFIRMATIVO"},
            new[] {"BRILLO, José Ricardo",                              "Movimiento Popular Neuquino",  "Neuquén",             "AFIRMATIVO"},
            new[] {"BROMBERG, Isaac Benjamín",                          "Frente para la Victoria - PJ", "Tucumán",             "AFIRMATIVO"},
            new[] {"BRUE, Daniel Agustín",                              "Frente Cívico por Santiago",   "Santiago del Estero", "AFIRMATIVO"},
            new[] {"CALCAGNO, Eric",                                    "Frente para la Victoria - PJ", "Buenos Aires",        "AFIRMATIVO"},
            new[] {"CARLOTTO, Remo Gerardo",                            "Frente para la Victoria - PJ", "Buenos Aires",        "AFIRMATIVO"},
            new[] {"CARMONA, Guillermo Ramón",                          "Frente para la Victoria - PJ", "Mendoza",             "AFIRMATIVO"},
            new[] {"CATALAN MAGNI, Julio César",                        "Frente para la Victoria - PJ", "Tierra del Fuego",    "AFIRMATIVO"},
            new[] {"CEJAS, Jorge Alberto",                              "Frente para la Victoria - PJ", "Rio Negro",           "AFIRMATIVO"},
            new[] {"CHIENO, María Elena",                               "Frente para la Victoria - PJ", "Corrientes",          "AFIRMATIVO"},
            new[] {"CIAMPINI, José Alberto",                            "Frente para la Victoria - PJ", "Neuquén",             "AFIRMATIVO"},
            new[] {"CIGOGNA, Luis Francisco Jorge",                     "Frente para la Victoria - PJ", "Buenos Aires",        "AFIRMATIVO"},
            new[] {"CLERI, Marcos",                                     "Frente para la Victoria - PJ", "Santa Fe",            "AFIRMATIVO"},
            new[] {"COMELLI, Alicia Marcela",                           "Movimiento Popular Neuquino",  "Neuquén",             "AFIRMATIVO"},
            new[] {"CONTI, Diana Beatriz",                              "Frente para la Victoria - PJ", "Buenos Aires",        "AFIRMATIVO"},
            new[] {"CORDOBA, Stella Maris",                             "Frente para la Victoria - PJ", "Tucumán",             "AFIRMATIVO"},
            new[] {"CURRILEN, Oscar Rubén",                             "Frente para la Victoria - PJ", "Chubut",              "AFIRMATIVO"}
        };

        private static readonly string EU_017_PDF = "Resources/eu-017.pdf";
        private static readonly string[][] EU_017_EXPECTED = new[]
        {
            new[] {"", "Austria",         "77",  "1",  "78"},
            new[] {"", "Belgium",        "159",  "2", "161"},
            new[] {"", "Bulgaria",        "52",  "0",  "52"},
            new[] {"", "Croatia",        "144",  "0", "144"},
            new[] {"", "Cyprus",          "43",  "2",  "45"},
            new[] {"", "Czech Republic",  "78",  "0",  "78"},
            new[] {"", "Denmark",        "151",  "2", "153"},
            new[] {"", "Estonia",         "46",  "0",  "46"},
            new[] {"", "Finland",        "201",  "1", "202"},
            new[] {"", "France",         "428",  "7", "435"},
            new[] {"", "Germany",        "646", "21", "667"},
            new[] {"", "Greece",         "113",  "2", "115"},
            new[] {"", "Hungary",        "187",  "0", "187"},
            new[] {"", "Iceland",         "18",  "0",  "18"},
            new[] {"", "Ireland",        "213",  "4", "217"},
            new[] {"", "Israel",          "25",  "0",  "25"},
            new[] {"", "Italy",          "627", "12", "639"},
            new[] {"", "Latvia",           "7",  "0",   "7"},
            new[] {"", "Lithuania",       "94",  "1",  "95"},
            new[] {"", "Luxembourg",      "22",  "0",  "22"},
            new[] {"", "Malta",           "18",  "0",  "18"},
            new[] {"", "Netherlands",    "104",  "1", "105"},
            new[] {"", "Norway",         "195",  "0", "195"},
            new[] {"", "Poland",         "120",  "1", "121"},
            new[] {"", "Portugal",       "532",  "3", "535"},
            new[] {"", "Romania",        "110",  "0", "110"},
            new[] {"", "Slovakia",       "176",  "0", "176"},
            new[] {"", "Slovenia",        "56",  "0",  "56"},
            new[] {"", "Spain",          "614",  "3", "617"},
            new[] {"", "Sweden",         "122",  "3", "125"},
            new[] {"", "Switzerland",     "64",  "0",  "64"},
            new[] {"", "Turkey",          "96",  "0",  "96"},
            new[] {"", "United Kingdom", "572", "14", "586"}
        };

        private static readonly string FRX_2012_DISCLOSURE_PDF = "Resources/frx_2012_disclosure.pdf";
        private static readonly string[][] FRX_2012_DISCLOSURE_EXPECTED = new[]
        {
            new[] {"AANONSEN, DEBORAH, A", "",                                      "STATEN ISLAND, NY", "MEALS",                "$85.00"},
            new[] {"TOTAL",                "",                                      "",                  "",                     "$85.00"},
            new[] {"AARON, CAREN, T",      "",                                      "RICHMOND, VA",      "EDUCATIONAL ITEMS",    "$78.80"},
            new[] {"AARON, CAREN, T",      "",                                      "RICHMOND, VA",      "MEALS",               "$392.45"},
            new[] {"TOTAL",                "",                                      "",                  "",                    "$471.25"},
            new[] {"AARON, JOHN",          "",                                      "CLARKSVILLE, TN",   "MEALS",                "$20.39"},
            new[] {"TOTAL",                "",                                      "",                  "",                     "$20.39"},
            new[] {"AARON, JOSHUA, N",     "",                                      "WEST GROVE, PA",    "MEALS",               "$310.33"},
            //new[] {"",                     "REGIONAL PULMONARY & SLEEP", "",                  "",                           ""},
            new[] {"AARON, JOSHUA, N",     "REGIONAL PULMONARY & SLEEP\rMEDICINE",  "WEST GROVE, PA",    "SPEAKING FEES",     "$4,700.00"},
            //new[] {"",                     "MEDICINE",                   "",                  "",                           ""},
            new[] {"TOTAL",                "",                                      "",                  "",                  "$5,010.33"},
            new[] {"AARON, MAUREEN, M",    "",                                      "MARTINSVILLE, VA",  "MEALS",               "$193.67"},
            new[] {"TOTAL",                "",                                      "",                  "",                    "$193.67"},
            new[] {"AARON, MICHAEL, L",    "",                                      "WEST ISLIP, NY",    "MEALS",                "$19.50"},
            new[] {"TOTAL",                "",                                      "",                  "",                     "$19.50"},
            new[] {"AARON, MICHAEL, R",    "",                                      "BROOKLYN, NY",      "MEALS",                "$65.92"}
        };

        private static readonly string[][] EXPECTED_EMPTY_TABLE = { /* actually empty! */ };

        [Fact]
        public void TestRemoveSequentialSpaces()
        {
            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/m27.pdf", new PdfRectangle(28.28, 532 - (103.04 - 79.2), 732.6, 532));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.Extract(page)[0];
            var firstRow = table.Rows[0];

            Assert.Equal("ALLEGIANT AIR", firstRow[1].GetText());
            Assert.Equal("ALLEGIANT AIR LLC", firstRow[2].GetText());
        }

        [Fact]
        public void TestColumnRecognition()
        {
            PageArea page = UtilsForTesting.GetAreaFromFirstPage(ARGENTINA_DIPUTADOS_VOTING_RECORD_PDF, new PdfRectangle(12.75, 55, 557, 567));

            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.Extract(page)[0];
            var results = UtilsForTesting.TableToArrayOfRows(table);

            Assert.Equal(ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED.Length, results.Length);

            for (int i = 0; i < ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED.Length; i++)
            {
                var expected = ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED[i];
                var result = results[i];
                Assert.Equal(expected.Length, result.Length);
                for (int j = 0; j < expected.Length; j++)
                {
                    var e = expected[j];
                    var r = result[j];
                    Assert.Equal(e, r);
                }
            }
        }

        [Fact]
        public void TestVerticalRulingsPreventMergingOfColumns()
        {
            List<Ruling> rulings = new List<Ruling>();
            double[] rulingsVerticalPositions = { 147, 256, 310, 375, 431, 504 };
            for (int i = 0; i < 6; i++)
            {
                rulings.Add(new Ruling(new PdfPoint(rulingsVerticalPositions[i], 40.43), new PdfPoint(rulingsVerticalPositions[i], 755)));
            }

            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/campaign_donors.pdf", new PdfRectangle(40.43, 755 - (398.76 - 255.57), 557.35, 755));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm(rulings);
            Table table = bea.Extract(page)[0];
            var sixthRow = table.Rows[5];

            Assert.Equal("VALSANGIACOMO BLANC", sixthRow[0].GetText());
            Assert.Equal("OFERNANDO JORGE", sixthRow[1].GetText());
        }

        [Fact]
        public void TestExtractColumnsCorrectly()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                PageArea page = UtilsForTesting.GetAreaFromPage(EU_002_PDF, 1, new PdfRectangle(70.0, 725 - (233 - 115), 510.0, 725));
                BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
                Table table = bea.Extract(page)[0];

                var actualArray = UtilsForTesting.TableToArrayOfRows(table);
                Assert.Equal(EU_002_EXPECTED.Length, actualArray.Length);

                for (int i = 0; i < EU_002_EXPECTED.Length; i++)
                {
                    var expecteds = EU_002_EXPECTED[i];
                    var actuals = actualArray[i];
                    Assert.Equal(expecteds.Length, actuals.Length);
                    for (int j = 0; j < expecteds.Length; j++)
                    {
                        var e = expecteds[j];
                        var a = actuals[j];
                        Assert.Equal(e, a);
                    }
                }
            }
            else
            {
                // fails on linux and mac os. Linked to PdfPig not finding the correct font.
                // need to use apt-get -y install ttf-mscorefonts-installer
                // still have mscorefonts - eula license could not be presented
            }
        }

        [Fact]
        public void TestExtractColumnsCorrectly2()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                PageArea page = UtilsForTesting.GetPage(EU_017_PDF, 3);
                BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm(page.VerticalRulings);
                Table table = bea.Extract(page.GetArea(new PdfRectangle(148.44, 543 - (711.875 - 299.625), 452.32, 543)))[0];

                var result = UtilsForTesting.TableToArrayOfRows(table);

                Assert.Equal(EU_017_EXPECTED.Length, result.Length);
                for (int i = 0; i < EU_017_EXPECTED.Length; i++)
                {
                    var expecteds = EU_017_EXPECTED[i];
                    var actuals = result[i];
                    Assert.Equal(expecteds.Length, actuals.Length);
                    for (int j = 0; j < expecteds.Length; j++)
                    {
                        var e = expecteds[j];
                        var a = actuals[j];
                        Assert.Equal(e, a);
                    }
                }
            }
            else
            {
                // fails on linux and mac os. Linked to PdfPig not finding the correct font.
                // need to use apt-get -y install ttf-mscorefonts-installer
                // still have mscorefonts - eula license could not be presented
            }
        }

        [Fact]
        public void TestExtractColumnsCorrectly3()
        {
            PageArea page = UtilsForTesting.GetAreaFromFirstPage(FRX_2012_DISCLOSURE_PDF, new PdfRectangle(48.09, 563, 551.89, 685.5));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.Extract(page)[0];
            var result = UtilsForTesting.TableToArrayOfRows(table);

            Assert.Equal(FRX_2012_DISCLOSURE_EXPECTED.Length, result.Length);
            for (int i = 0; i < FRX_2012_DISCLOSURE_EXPECTED.Length; i++)
            {
                var expecteds = FRX_2012_DISCLOSURE_EXPECTED[i];
                var actuals = result[i];
                Assert.Equal(expecteds.Length, actuals.Length);
                for (int j = 0; j < expecteds.Length; j++)
                {
                    var e = expecteds[j];
                    var a = actuals[j];
                    Assert.Equal(e, a);
                }
            }
        }

        [Fact]
        public void TestCheckSqueezeDoesntBreak()
        {
            PageArea page = UtilsForTesting.GetAreaFromFirstPage("Resources/12s0324.pdf", new PdfRectangle(17.25, 342, 410.25, 560.5));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.Extract(page)[0];
            var rows = table.Rows;
            var firstRow = rows[0];
            var firstRowFirstCell = firstRow[0].GetText();
            var lastRow = rows[rows.Count - 1];
            var lastRowLastCell = lastRow[lastRow.Count - 1].GetText();

            Assert.Equal("Violent crime  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .", firstRowFirstCell);
            Assert.Equal("(X)", lastRowLastCell);
        }

        [Fact]
        public void TestNaturalOrderOfRectangles()
        {
            PageArea page = UtilsForTesting.GetPage("Resources/us-017.pdf", 2).GetArea(new PdfRectangle(90, 97, 532, 352));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm(page.VerticalRulings);
            Table table = bea.Extract(page)[0];

            IReadOnlyList<Cell> cells = table.Cells;
            foreach (var rectangularTextContainer in cells)
            {
                Debug.Print(rectangularTextContainer.GetText());
            }

            // Now different form tabula-java, since PdfPig 0.1.5-alpha001

            //Column headers
            Assert.Equal("Project", cells[0].GetText());
            Assert.Equal("Agency", cells[1].GetText());
            Assert.Equal("Institution", cells[2].GetText());

            //First row
            Assert.Equal("Nanotechnology and its publics", cells[3].GetText());
            Assert.Equal("NSF", cells[4].GetText());
            Assert.Equal("Pennsylvania State University", cells[5].GetText());

            //Second row
            Assert.Equal("Public information and deliberation in nanoscience and\rnanotechnology policy (SGER)", cells[6].GetText());
            Assert.Equal("Interagency", cells[7].GetText());
            Assert.Equal("North Carolina State\rUniversity", cells[8].GetText());

            //Third row
            Assert.Equal("Social and ethical research and education in agrifood", cells[9].GetText());
            Assert.Equal("nanotechnology (NIRT)", cells[10].GetText());
            Assert.Equal("NSF", cells[11].GetText());
            Assert.Equal("Michigan State University", cells[12].GetText());

            //Fourth row
            Assert.Equal("From laboratory to society: developing an informed", cells[13].GetText());
            Assert.Equal("approach to nanoscale science and engineering (NIRT)", cells[14].GetText());
            Assert.Equal("NSF", cells[15].GetText());
            Assert.Equal("University of South Carolina", cells[16].GetText());

            //Fifth row
            Assert.Equal("Database and innovation timeline for nanotechnology", cells[17].GetText());
            Assert.Equal("NSF", cells[18].GetText());
            Assert.Equal("UCLA", cells[19].GetText());

            //Sixth row
            Assert.Equal("Social and ethical dimensions of nanotechnology", cells[20].GetText());
            Assert.Equal("NSF", cells[21].GetText());
            Assert.Equal("University of Virginia", cells[22].GetText());

            //Seventh row
            Assert.Equal("Undergraduate exploration of nanoscience,", cells[23].GetText());
            Assert.Equal("applications and societal implications (NUE)", cells[24].GetText());
            Assert.Equal("NSF", cells[25].GetText());
            Assert.Equal("Michigan Technological\rUniversity", cells[26].GetText());

            //Eighth row
            Assert.Equal("Ethics and belief inside the development of", cells[27].GetText());
            Assert.Equal("nanotechnology (CAREER)", cells[28].GetText());
            Assert.Equal("NSF", cells[29].GetText());
            Assert.Equal("University of Virginia", cells[30].GetText());

            //Ninth row
            Assert.Equal("All centers, NNIN and NCN have a societal", cells[31].GetText());
            Assert.Equal("NSF, DOE,", cells[32].GetText());
            Assert.Equal("All nanotechnology centers", cells[33].GetText());
            Assert.Equal("implications components", cells[34].GetText());
            Assert.Equal("DOD, and NIH", cells[35].GetText());
            Assert.Equal("and networks", cells[36].GetText());
        }

        [Fact]
        public void TestNaturalOrderOfRectanglesOneMoreTime()
        {
            var parse = UtilsForTesting.LoadCsvLines("Resources/csv/TestBasicExtractor-RECTANGLE_TEST_NATURAL_ORDER.csv");
            List<TableRectangle> rectangles = new List<TableRectangle>();

            foreach (var record in parse)
            {
                var top = double.Parse(record[0]);
                var left = double.Parse(record[1]);
                double w = double.Parse(record[2]);
                double h = double.Parse(record[3]);

                rectangles.Add(new TableRectangle(new PdfRectangle(left, top, left + w, top + h)));
            }

            Utils.Sort(rectangles, new TableRectangle.ILL_DEFINED_ORDER());

            for (int i = 0; i < rectangles.Count - 1; i++)
            {
                TableRectangle rectangle = rectangles[i];
                TableRectangle nextRectangle = rectangles[i + 1];
                Assert.True(rectangle.CompareTo(nextRectangle) < 0);
            }
        }

        [Fact]
        public void TestRealLifeRTL2()
        {
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/indictb1h_14.csv");
            PageArea page = UtilsForTesting.GetAreaFromPage("Resources/indictb1h_14.pdf", 1, new PdfRectangle(120.0, 842 - 622.82, 459.9, 842 - 120.0));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.Extract(page)[0];

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).Write(sb, table);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var data = reader.ReadToEnd().Replace("\r\n", "\n").Trim(); // trim to remove last new line

                Assert.Equal(expectedCsv, data);
            }
        }

        [Fact]
        public void TestEmptyRegion()
        {
            PageArea page = UtilsForTesting.GetAreaFromPage("Resources/indictb1h_14.pdf", 1, new PdfRectangle(0, 700, 100.9, 800));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.Extract(page)[0];
            Assert.Equal(EXPECTED_EMPTY_TABLE, UtilsForTesting.TableToArrayOfRows(table));
        }

        [Fact]
        public void TestTableWithMultilineHeader()
        {
            string expectedCsv = UtilsForTesting.LoadCsv("Resources/csv/us-020.csv");
            PageArea page = UtilsForTesting.GetAreaFromPage("Resources/us-020.pdf", 2, new PdfRectangle(35.0, 151, 560, 688.5));
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.Extract(page)[0];

            using (var stream = new MemoryStream())
            using (var sb = new StreamWriter(stream) { AutoFlush = true })
            {
                (new CSVWriter()).Write(sb, table);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                var data = reader.ReadToEnd().Replace("\r\n", "\n").Trim(); // trim to remove last new line

                Assert.Equal(expectedCsv, data);
            }
        }
    }
}
