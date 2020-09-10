using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Tabula.Extractors;
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
            new[] {"",                                              "Preperation and", "Production of",            "Presentation an"},
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
            new[] {"AANONSEN, DEBORAH, A", "",                           "STATEN ISLAND, NY", "MEALS",                "$85.00"},
            new[] {"TOTAL",                "",                           "",                  "",                     "$85.00"},
            new[] {"AARON, CAREN, T",      "",                           "RICHMOND, VA",      "EDUCATIONAL ITEMS",    "$78.80"},
            new[] {"AARON, CAREN, T",      "",                           "RICHMOND, VA",      "MEALS",               "$392.45"},
            new[] {"TOTAL",                "",                           "",                  "",                    "$471.25"},
            new[] {"AARON, JOHN",          "",                           "CLARKSVILLE, TN",   "MEALS",                "$20.39"},
            new[] {"TOTAL",                "",                           "",                  "",                     "$20.39"},
            new[] {"AARON, JOSHUA, N",     "",                           "WEST GROVE, PA",    "MEALS",               "$310.33"},
            new[] {"",                     "REGIONAL PULMONARY & SLEEP", "",                  "",                           ""},
            new[] {"AARON, JOSHUA, N",     "",                           "WEST GROVE, PA",    "SPEAKING FEES",     "$4,700.00"},
            new[] {"",                     "MEDICINE",                   "",                  "",                           ""},
            new[] {"TOTAL",                "",                           "",                  "",                  "$5,010.33"},
            new[] {"AARON, MAUREEN, M",    "",                           "MARTINSVILLE, VA",  "MEALS",               "$193.67"},
            new[] {"TOTAL",                "",                           "",                  "",                    "$193.67"},
            new[] {"AARON, MICHAEL, L",    "",                           "WEST ISLIP, NY",    "MEALS",                "$19.50"},
            new[] {"TOTAL",                "",                           "",                  "",                     "$19.50"},
            new[] {"AARON, MICHAEL, R",    "",                           "BROOKLYN, NY",      "MEALS",                "$65.92"}
        };

        private static readonly string[][] EXPECTED_EMPTY_TABLE = { /* actually empty! */ };

        [Fact]
        public void testRemoveSequentialSpaces()
        {
            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/m27.pdf", new PdfRectangle(28.28, 532 - (103.04 - 79.2), 732.6, 532)); // 79.2f, 28.28f, 103.04f, 732.6f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];
            var firstRow = table.getRows()[0];

            Assert.Equal("ALLEGIANT AIR", firstRow[1].getText());
            Assert.Equal("ALLEGIANT AIR LLC", firstRow[2].getText());
        }

        [Fact]
        public void testColumnRecognition()
        {
            PageArea page = UtilsForTesting.getAreaFromFirstPage(ARGENTINA_DIPUTADOS_VOTING_RECORD_PDF, new PdfRectangle(12.75, 55, 557, 567)); // 269.875f, 12.75f, 790.5f, 561f);

            //PageArea page = UtilsForTesting.getAreaFromFirstPage(ARGENTINA_DIPUTADOS_VOTING_RECORD_PDF, new PdfRectangle(395, 388, 420, 400));

            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];
            var results = UtilsForTesting.tableToArrayOfRows(table);

            Assert.Equal(ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED.Length, results.Length);

            for (int i = 0; i < ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED.Length; i++)
            {
                var expected = ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED[i];
                var result = results[i];
                Assert.Equal(expected.Length, result.Length);
                for (int j = 0; j < expected.Length; j++)
                {
                    // problems with too much spaces
                    //if (i == 10 && j == 2) continue;
                    //if (i == 16 && j == 0) continue;
                    // end

                    var e = expected[j];
                    var r = result[j];
                    if (e != r)
                    {
                        Console.WriteLine();
                    }
                    Assert.Equal(e, r);
                }
            }

            //Assert.Equal(ARGENTINA_DIPUTADOS_VOTING_RECORD_EXPECTED, results);
        }

        [Fact]
        public void testVerticalRulingsPreventMergingOfColumns()
        {
            List<Ruling> rulings = new List<Ruling>();
            double[] rulingsVerticalPositions = { 147, 256, 310, 375, 431, 504 };
            for (int i = 0; i < 6; i++)
            {
                rulings.Add(new Ruling(255.57f, rulingsVerticalPositions[i], 0, 398.76f - 255.57f));
            }

            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/campaign_donors.pdf", new PdfRectangle(40.43, double.NaN, 557.35, double.NaN)); //255.57f, 40.43f, 398.76f, 557.35f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm(rulings);
            Table table = bea.extract(page)[0];
            var sixthRow = table.getRows()[5];

            Assert.True(sixthRow[0].getText().Equals("VALSANGIACOMO BLANC"));
            Assert.True(sixthRow[1].getText().Equals("OFERNANDO JORGE"));
        }

        //@Test
        [Fact]
        public void testExtractColumnsCorrectly()
        {
            PageArea page = UtilsForTesting.getAreaFromPage(EU_002_PDF, 1, new PdfRectangle(70.0, double.NaN, 510.0, double.NaN)); // 115.0f, 70.0f, 233.0f, 510.0f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];
            Assert.Equal(EU_002_EXPECTED, UtilsForTesting.tableToArrayOfRows(table));
        }

        [Fact]
        public void testExtractColumnsCorrectly2()
        {
            PageArea page = UtilsForTesting.getPage(EU_017_PDF, 3);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm(page.getVerticalRulings());
            Table table = bea.extract(page.getArea(148, 105, 452, 543))[0]; //299.625f, 148.44f, 711.875f, 452.32f))[0];
            var result = UtilsForTesting.tableToArrayOfRows(table);
            Assert.Equal(EU_017_EXPECTED, result);
        }

        [Fact]
        public void testExtractColumnsCorrectly3()
        {
            PageArea page = UtilsForTesting.getAreaFromFirstPage(FRX_2012_DISCLOSURE_PDF, new PdfRectangle(48.09, 57, 551.89, 685.5)); // 106.01f, 48.09f, 227.31f, 551.89f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];
            var result = UtilsForTesting.tableToArrayOfRows(table);
            Assert.Equal(FRX_2012_DISCLOSURE_EXPECTED, result);
        }

        [Fact]
        public void testCheckSqueezeDoesntBreak()
        {
            PageArea page = UtilsForTesting.getAreaFromFirstPage("Resources/12s0324.pdf", new PdfRectangle(17.25, 342, 410.25, 560.5)); // 99.0f, 17.25f, 316.5f, 410.25f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];
            var rows = table.getRows();
            var firstRow = rows[0];
            var firstRowFirstCell = firstRow[0].getText();
            var lastRow = rows[rows.Count - 1];
            var lastRowLastCell = lastRow[lastRow.Count - 1].getText();

            Assert.Equal("Violent crime  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .", firstRowFirstCell); // original="Violent crime  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  ."
            Assert.Equal("(X)", lastRowLastCell);
        }

        [Fact]
        public void testNaturalOrderOfRectangles()
        {
            PageArea page = UtilsForTesting.getPage("Resources/us-017.pdf", 2).getArea(new PdfRectangle(90, 97, 532, 352)); //446.0f, 97.0f, 685.0f, 520.0f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm(page.getVerticalRulings());
            Table table = bea.extract(page)[0];

            List<RectangularTextContainer> cells = new List<RectangularTextContainer>(table.cells.Values);
            foreach (RectangularTextContainer rectangularTextContainer in cells)
            {
                //Console.WriteLine(rectangularTextContainer.getText());
                Debug.Print(rectangularTextContainer.getText());
            }

            //Column headers
            Assert.Equal("Project", cells[0].getText());
            Assert.Equal("Agency", cells[1].getText());
            Assert.Equal("Institution", cells[2].getText());

            //First row
            Assert.Equal("Nanotechnology and its publics", cells[3].getText());
            Assert.Equal("NSF", cells[4].getText());
            Assert.Equal("Pennsylvania State University", cells[5].getText());

            //Second row
            Assert.Equal("Public information and deliberation in nanoscience and", cells[6].getText());
            //Assert.Equal("North Carolina State", cells[7].getText());
            Assert.Equal("Interagency", cells[8].getText());
            //Assert.Equal("nanotechnology policy (SGER)", cells[9].getText());
            Assert.Equal("University", cells[10].getText());

            //Third row
            Assert.Equal("Social and ethical research and education in agrifood", cells[11].getText());
            //Assert.Equal("NSF", cells[12].getText());
            //Assert.Equal("Michigan State University", cells[13].getText());
            //Assert.Equal("nanotechnology (NIRT)", cells[14].getText());

            //Fourth row
            Assert.Equal("From laboratory to society: developing an informed", cells[15].getText());
            //Assert.Equal("NSF", cells[16].getText());
            //Assert.Equal("University of South Carolina", cells[17].getText());
            //Assert.Equal("approach to nanoscale science and engineering (NIRT)", cells[18].getText());

            //Fifth row
            Assert.Equal("Database and innovation timeline for nanotechnology", cells[19].getText());
            Assert.Equal("NSF", cells[20].getText());
            Assert.Equal("UCLA", cells[21].getText());

            //Sixth row
            Assert.Equal("Social and ethical dimensions of nanotechnology", cells[22].getText());
            Assert.Equal("NSF", cells[23].getText());
            Assert.Equal("University of Virginia", cells[24].getText());

            //Seventh row
            Assert.Equal("Undergraduate exploration of nanoscience,", cells[25].getText());
            //Assert.Equal("Michigan Technological", cells[26].getText());
            Assert.Equal("NSF", cells[27].getText());
            //Assert.Equal("applications and societal implications (NUE)", cells[28].getText());
            Assert.Equal("University", cells[29].getText());

            //Eighth row
            Assert.Equal("Ethics and belief inside the development of", cells[30].getText());
            //Assert.Equal("NSF", cells[31].getText());
            //Assert.Equal("University of Virginia", cells[32].getText());
            //Assert.Equal("nanotechnology (CAREER)", cells[33].getText());

            //Ninth row
            Assert.Equal("All centers, NNIN and NCN have a societal", cells[34].getText());
            //Assert.Equal("NSF, DOE,", cells[35].getText());
            //Assert.Equal("All nanotechnology centers", cells[36].getText());
            //Assert.Equal("implications components", cells[37].getText());
            //Assert.Equal("DOD, and NIH", cells[38].getText());
            Assert.Equal("and networks", cells[39].getText());
        }

        /*
        [Fact]
        public void testNaturalOrderOfRectanglesOneMoreTime()// throws IOException
        {
            CSVParser parse = org.apache.commons.csv.CSVParser.parse(new File("src/test/resources/technology/tabula/csv/TestBasicExtractor-RECTANGLE_TEST_NATURAL_ORDER.csv"),
                        Charset.forName("utf-8"),
                        CSVFormat.DEFAULT);

            List<TableRectangle> rectangles = new List<TableRectangle>()

            foreach (CSVRecord record in parse) {
                rectangles.add(new Rectangle(
                        double.Parse(record.get(0)),
                        double.Parse(record.get(1)),
                        double.Parse(record.get(2)),
                        double.Parse(record.get(3))));
            }


            //List<Rectangle> rectangles = Arrays.asList(RECTANGLES_TEST_NATURAL_ORDER);
            Utils.sort(rectangles, new TableRectangle.ILL_DEFINED_ORDER());

            for (int i = 0; i < (rectangles.Count - 1); i++) 
            {
                TableRectangle rectangle = rectangles[i];
                TableRectangle nextRectangle = rectangles[i + 1];

                Assert.True(rectangle.CompareTo(nextRectangle) < 0);
            }
        }
        */

        [Fact(Skip ="TODO csv")]
        public void testRealLifeRTL2()
        {
            /*
            String expectedCsv = UtilsForTesting.loadCsv(@"Resources/indictb1h_14.csv");
            PageArea page = UtilsForTesting.getAreaFromPage(@"Resources/indictb1h_14.pdf", 1,
                        205.0f, 120.0f, 622.82f, 459.9f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).write(sb, table);
            Assert.Equal(expectedCsv, sb.ToString());
            */
        }

        [Fact]
        public void testEmptyRegion()
        {
            PageArea page = UtilsForTesting.getAreaFromPage("Resources/indictb1h_14.pdf", 1, new PdfRectangle(0, 700, 100.9, 800));  //0, 0, 80.82f, 100.9f); // an empty area
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];
            Assert.Equal(EXPECTED_EMPTY_TABLE, UtilsForTesting.tableToArrayOfRows(table));
        }

        [Fact(Skip = "TODO csv")]
        public void testTableWithMultilineHeader()
        {
            /*
            String expectedCsv = UtilsForTesting.loadCsv(@"Resources/us-020.csv");
            PageArea page = UtilsForTesting.getAreaFromPage(@"Resources/us-020.pdf", 2, 103.0f, 35.0f, 641.0f, 560.0f);
            BasicExtractionAlgorithm bea = new BasicExtractionAlgorithm();
            Table table = bea.extract(page)[0];

            StringBuilder sb = new StringBuilder();
            (new CSVWriter()).write(sb, table);
            Assert.Equal(expectedCsv, sb.ToString());
            */
        }
    }
}
