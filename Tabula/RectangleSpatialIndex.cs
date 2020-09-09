using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/RectangleSpatialIndex.java
    public class RectangleSpatialIndex<T> where T : TableRectangle
    {
        //private STRtree si = new STRtree();
        private List<T> rectangles = new List<T>();

        public void add(T te)
        {
            rectangles.Add(te);
            //si.insert(new Envelope(te.getLeft(), te.getRight(), te.getBottom(), te.getTop()), te);
        }

        public List<T> contains(PdfRectangle r)
        {
            return rectangles.Where(tr => r.Contains(tr.BoundingBox)).ToList();
        }

        public List<T> contains(TableRectangle r)
        {

            return rectangles.Where(tr => r.BoundingBox.Contains(tr.BoundingBox)).ToList();

            /*
            List<T> intersection = si.query(new Envelope(r.getLeft(), r.getRight(), r.getTop(), r.getBottom()));
            List<T> rv = new List<T>(); // ArrayList<T>();

            foreach (T ir in intersection)
            {
                if (r.contains(ir))
                {
                    rv.Add(ir);
                }
            }

            Utils.sort(rv, new RectangleCell.ILL_DEFINED_ORDER());
            return rv;
            */
        }

        public List<T> intersects(TableRectangle r)
        {
            return rectangles.Where(tr => IntersectsWithNoBug(r.BoundingBox, tr.BoundingBox)).ToList();

            /*
            List rv = si.query(new Envelope(r.getLeft(), r.getRight(), r.getTop(), r.getBottom()));
            return rv;
            */
        }

        /// <summary>
        /// TO REMOVE: need to check PdfPig's 'IntersectsWith' for bug with empty rectangles. they should instersect
        /// </summary>
        private bool IntersectsWithNoBug(PdfRectangle rectangle, PdfRectangle other)
        {
            if (rectangle.Left > other.Right || other.Left > rectangle.Right)
            {
                return false;
            }

            if (rectangle.Top < other.Bottom || other.Top < rectangle.Bottom)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Minimum bounding box of all the Rectangles contained on this RectangleSpatialIndex.
        /// </summary>
        /// <returns></returns>
        public TableRectangle getBounds()
        {
            return TableRectangle.boundingBoxOf(rectangles.Cast<TableRectangle>());
        }
    }
}
