using System;
using System.Collections;
using System.Collections.Generic;

namespace Tabula
{
    public class PageIterator : IEnumerator<PageArea>
    {
        private ObjectExtractor oe;
        private IEnumerator<int> pageIndexIterator;

        public PageIterator(ObjectExtractor oe, IEnumerable<int> pages) : base()
        {
            this.oe = oe;
            this.pageIndexIterator = pages.GetEnumerator();
        }

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

        object IEnumerator.Current => Current;

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

        public void Dispose()
        {
            this.oe.Close();
            this.pageIndexIterator.Dispose();
        }

        public bool MoveNext()
        {
            return this.pageIndexIterator.MoveNext();
        }

        public void Reset()
        {
            this.pageIndexIterator.Reset();
        }
    }
}
