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
                Assert.Single(regions);
            }
        }
    }
}
