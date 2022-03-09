using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    /// <summary>
    /// ported from tabula-java/blob/master/src/main/java/technology/tabula/RectangleSpatialIndex.java
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RectangleSpatialIndex<T> where T : TableRectangle
    {
        private STRtree<T> si = new STRtree<T>();
        private List<T> rectangles = new List<T>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="te"></param>
        public void Add(T te)
        {
            rectangles.Add(te);
            si.Insert(new Envelope(te.Left, te.Right, te.Bottom, te.Top), te);
        }

        /// <summary>
        /// hack
        /// </summary>
        /// <param name="rectangle"></param>
        private PdfRectangle Expand(PdfRectangle rectangle)
        {
            return new PdfRectangle(rectangle.Left - 1, rectangle.Bottom - 1, rectangle.Right + 1, rectangle.Top + 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public List<T> Contains(PdfRectangle r)
        {
            IList<T> intersection = si.Query(new Envelope(r.Left, r.Right, r.Top, r.Bottom));
            List<T> rv = new List<T>();

            foreach (T ir in intersection)
            {
                if (r.Contains(ir.BoundingBox))
                {
                    rv.Add(ir);
                }
            }

            Utils.Sort(rv, new TableRectangle.ILL_DEFINED_ORDER());
            return rv;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public List<T> Contains(TableRectangle r)
        {
            IList<T> intersection = si.Query(new Envelope(r.Left, r.Right, r.Top, r.Bottom));
            List<T> rv = new List<T>();

            foreach (T ir in intersection)
            {
                if (r.Contains(ir))
                {
                    rv.Add(ir);
                }
            }

            Utils.Sort(rv, new TableRectangle.ILL_DEFINED_ORDER());
            return rv;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public List<T> Intersects(TableRectangle r)
        {
            IList<T> rv = si.Query(new Envelope(r.Left, r.Right, r.Top, r.Bottom));
            return (List<T>)rv;

        }

        /// <summary>
        /// Minimum bounding box of all the Rectangles contained on this RectangleSpatialIndex.
        /// </summary>
        public TableRectangle GetBounds()
        {
            return TableRectangle.BoundingBoxOf(rectangles.Cast<TableRectangle>());
        }
    }
}
