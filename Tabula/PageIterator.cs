using System;
using System.Collections;
using System.Collections.Generic;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/PageIterator.java
    /// <summary>
    /// A tabula page iterator.
    /// </summary>
    public class PageIterator : IEnumerator<PageArea>
    {
        private ObjectExtractor oe;
        private IEnumerator<int> pageIndexIterator;

        /// <summary>
        /// Create a tabula page iterator.
        /// </summary>
        /// <param name="oe"></param>
        /// <param name="pages"></param>
        public PageIterator(ObjectExtractor oe, IEnumerable<int> pages) : base()
        {
            this.oe = oe;
            this.pageIndexIterator = pages.GetEnumerator();
        }

        /// <inheritdoc/>
        public PageArea Current
        {
            get
            {
                try
                {
                    return oe.ExtractPage(pageIndexIterator.Current);
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
            this.oe.Close();
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
