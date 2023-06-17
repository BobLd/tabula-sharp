using System;
using System.Collections;
using System.Collections.Generic;
using UglyToad.PdfPig;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/PageIterator.java

    /// <summary>
    /// A tabula page iterator.
    /// </summary>
    public sealed class PageIterator : IEnumerator<PageArea>
    {
        //private readonly ObjectExtractor oe;
        private readonly IEnumerator<int> pageIndexIterator;
        private readonly PdfDocument pdfDocument;

        /// <summary>
        /// Create a tabula page iterator.
        /// </summary>
        /// <param name="pdfDocument"></param>
        /// <param name="pages"></param>
        public PageIterator(PdfDocument pdfDocument, IEnumerable<int> pages) : base()
        {
            this.pdfDocument = pdfDocument;
            this.pageIndexIterator = pages.GetEnumerator();
        }

        /// <inheritdoc/>
        public PageArea Current
        {
            get
            {
                try
                {
                    return ObjectExtractor.ExtractPage(this.pdfDocument, pageIndexIterator.Current);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        /// <inheritdoc/>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Helper function that does MoveNext() + Current;
        /// </summary>
        public PageArea Next()
        {
            if (MoveNext())
            {
                return Current;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.pageIndexIterator.Dispose();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            return this.pageIndexIterator.MoveNext();
        }

        /// <inheritdoc/>
        public void Reset()
        {
            this.pageIndexIterator.Reset();
        }
    }
}
