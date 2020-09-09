using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;
using UglyToad.PdfPig.Geometry;
using Xunit;

namespace Tabula.Tests
{
    public class TestObjectExtractor
    {
        [Fact]//@Test(expected = IOException.class)
        public void testEmptyOnEncryptedFileRaisesException() //throws IOException
        {
            Assert.Throws<PdfDocumentEncryptedException>(() => PdfDocument.Open(@"Resources/encrypted.pdf"));
            //PdfDocument pdf_document = PdfDocument.Open(@"Resources/encrypted.pdf");
            //ObjectExtractor oe = new ObjectExtractor(pdf_document);
            //Assert.Throws<Exception>(() => oe.extract().next());
        }

        [Fact]
        public void testCanReadPDFWithOwnerEncryption() //throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open(@"Resources/S2MNCEbirdisland.pdf");
            ObjectExtractor oe = new ObjectExtractor(pdf_document);
            PageIterator pi = oe.extract();
            int i = 0;
            while (pi.MoveNext())
            {
                i++;
                //pi.next();
            }
            Assert.Equal(2, i);
        }

        [Fact]
        public void testGoodPassword() // throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open("Resources/encrypted.pdf", new ParsingOptions() { Password = "userpassword" });
            ObjectExtractor oe = new ObjectExtractor(pdf_document);
            List<PageArea> pages = new List<PageArea>();
            PageIterator pi = oe.extract();
            while (pi.MoveNext())
            {
                pages.Add(pi.Current);
            }
            Assert.Equal(1, pages.Count);
        }


        [Fact]
        public void testTextExtractionDoesNotRaise() //throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open("Resources/rotated_page.pdf", new ParsingOptions() { ClipPaths = true });
            ObjectExtractor oe = new ObjectExtractor(pdf_document);
            PageIterator pi = oe.extract();

            Assert.True(pi.MoveNext());
            Assert.NotNull(pi.Current);
            Assert.False(pi.MoveNext());
        }

        [Fact]
        public void testShouldDetectRulings() //throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open("Resources/should_detect_rulings.pdf", new ParsingOptions() { ClipPaths = true });
            ObjectExtractor oe = new ObjectExtractor(pdf_document);
            PageIterator pi = oe.extract();

            PageArea page = pi.next();
            List<Ruling> rulings = page.getRulings();

            foreach (Ruling r in rulings)
            {
                Assert.True(page.BoundingBox.Contains(r.line.GetBoundingRectangle(), true));
            }
        }

        [Fact]
        public void testDontThrowNPEInShfill() //throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open("Resources/labor.pdf", new ParsingOptions() { ClipPaths = true });
            ObjectExtractor oe = new ObjectExtractor(pdf_document);
            PageIterator pi = oe.extract();
            Assert.True(pi.MoveNext());
            //try
            //{
            PageArea p = pi.Current;
            Assert.NotNull(p);
            //}
            //catch (NullPointerException e) 
            //{
            //    fail("NPE in ObjectExtractor " + e.toString());
            //}
        }

        [Fact]
        public void testExtractOnePage()// throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open("Resources/S2MNCEbirdisland.pdf", new ParsingOptions() { ClipPaths = true });
            Assert.Equal(2, pdf_document.NumberOfPages);

            ObjectExtractor oe = new ObjectExtractor(pdf_document);
            PageArea page = oe.extract(2);

            Assert.NotNull(page);
        }

        [Fact]  //@Test(expected = IndexOutOfBoundsException.class)
        public void testExtractWrongPageNumber()// throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open("Resources/S2MNCEbirdisland.pdf", new ParsingOptions() { ClipPaths = true });
            Assert.Equal(2, pdf_document.NumberOfPages);

            ObjectExtractor oe = new ObjectExtractor(pdf_document);
            Assert.Throws<IndexOutOfRangeException>(() => oe.extract(3));
        }

        [Fact]
        public void testTextElementsContainedInPage() // throws IOException
        {
            // working with negative coordinates
            PdfDocument pdf_document = PdfDocument.Open("Resources/cs-en-us-pbms.pdf", new ParsingOptions() { ClipPaths = true });
            ObjectExtractor oe = new ObjectExtractor(pdf_document);

            PageArea page = oe.extractPage(1);

            foreach (TextElement te in page.getText())
            {
                Assert.True(page.BoundingBox.Contains(te.BoundingBox));
            }
        }

        [Fact]
        public void testDoNotNPEInPointComparator() // throws IOException
        {
            PdfDocument pdf_document = PdfDocument.Open("Resources/npe_issue_206.pdf", new ParsingOptions() { ClipPaths = true });
            ObjectExtractor oe = new ObjectExtractor(pdf_document);

            //try
            //{
            PageArea p = oe.extractPage(1);
            Assert.NotNull(p);
            //}
            //catch (NullPointerException e)
            //{
            //    fail("NPE in ObjectExtractor " + e.toString());
            //}
        }
    }
}
