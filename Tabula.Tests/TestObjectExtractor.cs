using System;
using System.Collections.Generic;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;
using UglyToad.PdfPig.Geometry;
using Xunit;

namespace Tabula.Tests
{
    public class TestObjectExtractor
    {
        [Fact]
        public void TestEmptyOnEncryptedFileRaisesException()
        {
            Assert.Throws<PdfDocumentEncryptedException>(() => PdfDocument.Open("Resources/encrypted.pdf"));
            //PdfDocument pdf_document = PdfDocument.Open(@"Resources/encrypted.pdf");
            //ObjectExtractor oe = new ObjectExtractor(pdf_document);
            //Assert.Throws<Exception>(() => oe.extract().next());
        }

        [Fact]
        public void TestCanReadPDFWithOwnerEncryption()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/S2MNCEbirdisland.pdf"))
            {
                ObjectExtractor oe = new ObjectExtractor(pdf_document);
                PageIterator pi = oe.Extract();
                int i = 0;
                while (pi.MoveNext())
                {
                    i++;
                }
                Assert.Equal(2, i);
            }
        }

        [Fact]
        public void TestGoodPassword()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/encrypted.pdf", new ParsingOptions() { Password = "userpassword" }))
            {
                ObjectExtractor oe = new ObjectExtractor(pdf_document);
                List<PageArea> pages = new List<PageArea>();
                PageIterator pi = oe.Extract();
                while (pi.MoveNext())
                {
                    pages.Add(pi.Current);
                }
                Assert.Single(pages);
            }
        }

        [Fact]
        public void TestTextExtractionDoesNotRaise()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/rotated_page.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                ObjectExtractor oe = new ObjectExtractor(pdf_document);
                PageIterator pi = oe.Extract();

                Assert.True(pi.MoveNext());
                Assert.NotNull(pi.Current);
                Assert.False(pi.MoveNext());
            }
        }

        [Fact]
        public void TestShouldDetectRulings()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/should_detect_rulings.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                ObjectExtractor oe = new ObjectExtractor(pdf_document);
                PageIterator pi = oe.Extract();

                PageArea page = pi.Next();
                IReadOnlyList<Ruling> rulings = page.GetRulings();

                foreach (Ruling r in rulings)
                {
                    Assert.True(page.BoundingBox.Contains(r.Line.GetBoundingRectangle(), true));
                }
            }
        }

        [Fact]
        public void TestDontThrowNPEInShfill()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/labor.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                ObjectExtractor oe = new ObjectExtractor(pdf_document);
                PageIterator pi = oe.Extract();
                Assert.True(pi.MoveNext());

                PageArea p = pi.Current;
                Assert.NotNull(p);
            }
        }

        [Fact]
        public void TestExtractOnePage()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/S2MNCEbirdisland.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                Assert.Equal(2, pdf_document.NumberOfPages);

                ObjectExtractor oe = new ObjectExtractor(pdf_document);
                PageArea page = oe.Extract(2);

                Assert.NotNull(page);
            }
        }

        [Fact]
        public void TestExtractWrongPageNumber()// throws IOException
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/S2MNCEbirdisland.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                Assert.Equal(2, pdf_document.NumberOfPages);

                ObjectExtractor oe = new ObjectExtractor(pdf_document);
                Assert.Throws<IndexOutOfRangeException>(() => oe.Extract(3));
            }
        }

        [Fact]
        public void TestTextElementsContainedInPage()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/cs-en-us-pbms.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                ObjectExtractor oe = new ObjectExtractor(pdf_document);

                PageArea page = oe.ExtractPage(1);

                foreach (TextElement te in page.GetText())
                {
                    Assert.True(page.BoundingBox.Contains(te.BoundingBox));
                }
            }
        }

        [Fact]
        public void TestDoNotNPEInPointComparator()
        {
            using (PdfDocument pdf_document = PdfDocument.Open("Resources/npe_issue_206.pdf", new ParsingOptions() { ClipPaths = true }))
            {
                ObjectExtractor oe = new ObjectExtractor(pdf_document);

                PageArea p = oe.ExtractPage(1);
                Assert.NotNull(p);
            }
        }
    }
}
