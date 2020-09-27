using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tabula.Tests
{
    // https://github.com/BobLd/tabula-java/blob/master/src/test/java/technology/tabula/TestTableDetection.java
    public class TestTableDetection
    {
        private static int numTests = 0;
        private static int numPassingTests = 0;
        private static int totalExpectedTables = 0;
        private static int totalCorrectlyDetectedTables = 0;
        private static int totalErroneouslyDetectedTables = 0;

        //private static Level defaultLogLevel;

        private class TestStatus
        {
            public int numExpectedTables;
            public int numCorrectlyDetectedTables;
            public int numErroneouslyDetectedTables;
            public bool expectedFailure;

            private bool firstRun; // transient
            private string pdfFilename; // transient

            public TestStatus(string pdfFilename)
            {
                this.numExpectedTables = 0;
                this.numCorrectlyDetectedTables = 0;
                this.expectedFailure = false;
                this.pdfFilename = pdfFilename;
            }

            public static TestStatus load(string pdfFilename)
            {
                throw new NotImplementedException();
                /*
                TestStatus status;

                try
                {
                    string json = UtilsForTesting.loadJson(jsonFilename(pdfFilename));
                    status = new Gson().fromJson(json, TestStatus.class);
                status.pdfFilename = pdfFilename;
            } 
        catch (IOException ioe)
            {
                status = new TestStatus(pdfFilename);
        status.firstRun = true;
            }

            return status;
        */
            }

            public void save()
            {
                /*
                try (FileWriter w = new FileWriter(jsonFilename(this.pdfFilename))) {
                    Gson gson = new Gson();
                    w.write(gson.toJson(this));
                    w.close();
                } catch (Exception e)
                {
                    throw new Error(e);
                }
                */
            }

            public bool isFirstRun()
            {
                return this.firstRun;
            }

            private static string jsonFilename(string pdfFilename)
            {
                return pdfFilename.Replace(".pdf", ".json");
            }
        }

        //@BeforeClass
        public static void disableLogging()
        {
            //Logger pdfboxLogger = Logger.getLogger("org.apache.pdfbox");
            //defaultLogLevel = pdfboxLogger.getLevel();
            //pdfboxLogger.setLevel(Level.OFF);
        }

        //@AfterClass
        public static void enableLogging()
        {
            //Logger.getLogger("org.apache.pdfbox").setLevel(defaultLogLevel);
        }

        //@Parameterized.Parameters
        public static List<object[]> data()
        {
            throw new NotImplementedException();
            /*
            string[] regionCodes = { "eu", "us" };

            List<object[]> data = new List<object[]>(); //ArrayList<>();

            foreach (string regionCode in regionCodes)
            {
                String directoryName = "src/test/resources/technology/tabula/icdar2013-dataset/competition-dataset-" + regionCode + "/";
                File dir = new File(directoryName);

                File[] pdfs = dir.listFiles(new FilenameFilter() {
                @Override
                public boolean accept(File dir, String name)
                {
                    return name.toLowerCase().endsWith(".pdf");
                }
            });

            foreach (File pdf in pdfs)
            {
                data.Add(new Object[] { pdf });
            }
        }

        return data;
            */
        }

        //private File pdf;
        //private DocumentBuilder builder;
        private TestStatus status;

        private int numCorrectlyDetectedTables = 0;
        private int numErroneouslyDetectedTables = 0;

        public TestTableDetection(string pdf) //File pdf)
        {
            /*
            this.pdf = pdf;
            this.status = TestStatus.load(pdf.getAbsolutePath());

            DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
            try
            {
                this.builder = factory.newDocumentBuilder();
            }
            catch (Exception e)
            {
                // ignored
            }
            */
        }

        private void printTables(Dictionary<int, List<TableRectangle>> tables)
        {
            foreach (int page in tables.Keys)
            {
                /*
                System.out.println("Page " + page.toString());
                for (Rectangle table : tables.get(page))
                {
                    System.out.println(table);
                }
                */
            }
        }

        [Fact(Skip = "Need to implement NurminenDetectionAlgorithm")]
        public void TestDetectionOfTables()
        {
            /*
            numTests++;

            // xml parsing stuff for ground truth
            Document regionDocument = this.builder.parse(this.pdf.getAbsolutePath().replace(".pdf", "-reg.xml"));
            NodeList tables = regionDocument.getElementsByTagName("table");

            // tabula extractors
            PDDocument pdfDocument = PDDocument.load(this.pdf);
            ObjectExtractor extractor = new ObjectExtractor(pdfDocument);

            // parse expected tables from the ground truth dataset
            Map<Integer, List<Rectangle>> expectedTables = new HashMap<>();

            int numExpectedTables = 0;

            for (int i = 0; i < tables.getLength(); i++)
            {

                Element table = (Element)tables.item(i);
                Element region = (Element)table.getElementsByTagName("region").item(0);
                Element boundingBox = (Element)region.getElementsByTagName("bounding-box").item(0);

                // we want to know where tables appear in the document - save the page and areas where tables appear
                Integer page = Integer.decode(region.getAttribute("page"));
                float x1 = Float.parseFloat(boundingBox.getAttribute("x1"));
                float y1 = Float.parseFloat(boundingBox.getAttribute("y1"));
                float x2 = Float.parseFloat(boundingBox.getAttribute("x2"));
                float y2 = Float.parseFloat(boundingBox.getAttribute("y2"));

                List<Rectangle> pageTables = expectedTables.get(page);
                if (pageTables == null)
                {
                    pageTables = new ArrayList<>();
                    expectedTables.put(page, pageTables);
                }

                // have to invert y co-ordinates
                // unfortunately the ground truth doesn't contain page dimensions
                // do some extra work to extract the page with tabula and get the dimensions from there
                Page extractedPage = extractor.extractPage(page);

                float top = (float)extractedPage.getHeight() - y2;
                float left = x1;
                float width = x2 - x1;
                float height = y2 - y1;

                pageTables.add(new Rectangle(top, left, width, height));
                numExpectedTables++;
            }

            // now find tables detected by tabula-java
            Map<Integer, List<Rectangle>> detectedTables = new HashMap<>();

            // the algorithm we're going to be testing
            NurminenDetectionAlgorithm detectionAlgorithm = new NurminenDetectionAlgorithm();

            PageIterator pages = extractor.extract();
            while (pages.hasNext())
            {
                Page page = pages.next();
                List<Rectangle> tablesOnPage = detectionAlgorithm.detect(page);
                if (tablesOnPage.size() > 0)
                {
                    detectedTables.put(new Integer(page.getPageNumber()), tablesOnPage);
                }
            }

            // now compare
            System.out.println("Testing " + this.pdf.getName());

            List<String> errors = new ArrayList<>();
            this.status.numExpectedTables = numExpectedTables;
            totalExpectedTables += numExpectedTables;

            for (Integer page : expectedTables.keySet())
            {
                List<Rectangle> expectedPageTables = expectedTables.get(page);
                List<Rectangle> detectedPageTables = detectedTables.get(page);

                if (detectedPageTables == null)
                {
                    errors.add("Page " + page.toString() + ": " + expectedPageTables.size() + " expected tables not found");
                    continue;
                }

                errors.addAll(this.comparePages(page, detectedPageTables, expectedPageTables));

                detectedTables.remove(page);
            }

            // leftover pages means we detected extra tables
            for (Integer page : detectedTables.keySet())
            {
                List<Rectangle> detectedPageTables = detectedTables.get(page);
                errors.add("Page " + page.toString() + ": " + detectedPageTables.size() + " tables detected where there are none");

                this.numErroneouslyDetectedTables += detectedPageTables.size();
                totalErroneouslyDetectedTables += detectedPageTables.size();
            }

            boolean failed = errors.size() > 0;

            if (failed)
            {
                System.out.println("==== CURRENT TEST ERRORS ====");
                for (String error : errors)
                {
                    System.out.println(error);
                }
            }
            else
            {
                numPassingTests++;
            }

            System.out.println("==== CUMULATIVE TEST STATISTICS ====");

            System.out.println(numPassingTests + " out of " + numTests + " currently passing");
            System.out.println(totalCorrectlyDetectedTables + " out of " + totalExpectedTables + " expected tables detected");
            System.out.println(totalErroneouslyDetectedTables + " tables incorrectly detected");


            if (this.status.isFirstRun())
            {
                // make the baseline
                this.status.expectedFailure = failed;
                this.status.numCorrectlyDetectedTables = this.numCorrectlyDetectedTables;
                this.status.numErroneouslyDetectedTables = this.numErroneouslyDetectedTables;
                this.status.save();
            }
            else
            {
                // compare to baseline
                if (this.status.expectedFailure)
                {
                    // make sure the failure didn't get worse
                    assertTrue("This test is an expected failure, but it now detects even fewer tables.", this.numCorrectlyDetectedTables >= this.status.numCorrectlyDetectedTables);
                    assertTrue("This test is an expected failure, but it now detects more bad tables.", this.numErroneouslyDetectedTables <= this.status.numErroneouslyDetectedTables);
                    assertTrue("This test used to fail but now it passes! Hooray! Please update the test's JSON file accordingly.", failed);
                }
                else
                {
                    assertFalse("Table detection failed. Please see the error messages for more information.", failed);
                }
            }
            */
        }

        private List<string> comparePages(int page, List<TableRectangle> detected, List<TableRectangle> expected)
        {
            List<string> errors = new List<string>();

            // go through the detected tables and try to match them with expected tables
            // from http://www.orsigiorgio.net/wp-content/papercite-data/pdf/gho*12.pdf (comparing regions):
            // for other (e.g.“black-box”) algorithms, bounding boxes and content are used. A region is correct if it
            // contains the minimal bounding box of the ground truth without intersecting additional content.

            //for (Iterator<Rectangle> detectedIterator = detected.iterator(); detectedIterator.hasNext();)
            foreach (TableRectangle detectedTable in detected.ToList())
            {
                //TableRectangle detectedTable = detectedIterator.next();

                for (int i = 0; i < expected.Count; i++)
                {
                    if (detectedTable.Contains(expected[i]))
                    {
                        // we have a candidate for the detected table, make sure it doesn't intersect any others
                        bool intersectsOthers = false;
                        for (int j = 0; j < expected.Count; j++)
                        {
                            if (i == j) continue;
                            if (detectedTable.Intersects(expected[j]))
                            {
                                intersectsOthers = true;
                                break;
                            }
                        }

                        if (!intersectsOthers)
                        {
                            // success
                            //detectedIterator.remove();
                            detected.Remove(detectedTable);//bobld: not sure ????
                            expected.RemoveAt(i);

                            this.numCorrectlyDetectedTables++;
                            totalCorrectlyDetectedTables++;

                            break;
                        }
                    }
                }
            }

            // any expected tables left over weren't detected
            foreach (TableRectangle expectedTable in expected)
            {
                errors.Add("Page " + page.ToString() + ": " + expectedTable.ToString() + " not detected");
            }

            // any detected tables left over were detected erroneously
            foreach (TableRectangle detectedTable in detected)
            {
                errors.Add("Page " + page.ToString() + ": " + detectedTable.ToString() + " detected where there is no table");
                this.numErroneouslyDetectedTables++;
                totalErroneouslyDetectedTables++;
            }

            return errors;
        }
    }
}
