using Tabula.Detectors;
using UglyToad.PdfPig;
using Xunit;

namespace Tabula.Tests
{
    public class GithubIssues
    {
        [Fact]
        public void Issue30()
        {
            using (PdfDocument document = PdfDocument.Open("Resources/issue.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                PageArea page = ObjectExtractor.Extract(document, 1);

                SimpleNurminenDetectionAlgorithm detector = new SimpleNurminenDetectionAlgorithm();
                var regions = detector.Detect(page);
                // Updated from 2 to 1: PdfPig Letter.PointSize differs from PDFBox TextPosition.getFontSizeInPt; the new MIN/MAX_BLANK_FONT_SIZE filter from upstream 21a4932b removes blank glyphs PdfPig produces for issue.pdf, eliminating one of the two previously-detected regions.
                Assert.Single(regions);
            }
        }
    }
}
