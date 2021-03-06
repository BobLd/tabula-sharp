using System;
using System.Collections.Generic;
using System.Text;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;
using Xunit;

namespace Tabula.Tests
{
    public class TestsNurminenDetector
    {
        [Fact(Skip = "TO DO")]
        public void TestLinesToCells()
        {
            using (PdfDocument document = PdfDocument.Open("test3.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                ObjectExtractor oe = new ObjectExtractor(document);
                PageArea page = oe.Extract(1);

                SimpleNurminenDetectionAlgorithm detector = new SimpleNurminenDetectionAlgorithm();
                var regions = detector.Detect(page);

                foreach (var a in regions)
                {
                    IExtractionAlgorithm ea = new BasicExtractionAlgorithm();
                    var newArea = page.GetArea(a.BoundingBox);
                    List<Table> tables = ea.Extract(newArea);
                }
            }
        }
    }
}
