using System;
using System.Collections.Generic;
using System.Text;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;
using Xunit;

namespace Tabula.Tests
{
    public class TestsIcdar2013
    {
        [Fact(Skip = "TO DO")]
        public void Eu004()
        {
            using (PdfDocument document = PdfDocument.Open("Resources/icdar2013-dataset/competition-dataset-eu/eu-004.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                ObjectExtractor oe = new ObjectExtractor(document);
                PageArea page = oe.Extract(3);

                var detector = new SimpleNurminenDetectionAlgorithm();
                var regions = detector.Detect(page);

                var newArea = page.GetArea(regions[0].BoundingBox);

                var sea = new SpreadsheetExtractionAlgorithm();
                var tables = sea.Extract(newArea);

                /*
                var detector = new SimpleNurminenDetectionAlgorithm();
                var regions = detector.Detect(page);

                foreach (var a in regions)
                {
                    IExtractionAlgorithm ea = new BasicExtractionAlgorithm();
                    var newArea = page.GetArea(a.BoundingBox);
                    List<Table> tables = ea.Extract(newArea);
                }
                */
            }
        }
    }
}
