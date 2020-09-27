using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/Utils.java
    /*
     * @author manuel
     */
    public static class Utils
    {
        public static bool Within(double first, double second, double variance)
        {
            return second < first + variance && second > first - variance;
        }

        public static bool Overlap(double y1, double height1, double y2, double height2, double variance)
        {
            return Within(y1, y2, variance) || (y2 <= y1 && y2 >= y1 - height1) || (y1 <= y2 && y1 >= y2 - height2);
        }

        public static bool Overlap(double y1, double height1, double y2, double height2)
        {
            return Overlap(y1, height1, y2, height2, 0.1f);
        }

        private static float EPSILON = 0.01f;
        public static bool Feq(double f1, double f2)
        {
            return Math.Abs(f1 - f2) < EPSILON;
        }

        public static double Round(double d, int decimalPlace)
        {
            return Math.Round(d, decimalPlace);
        }

        public static PdfRectangle Bounds(IEnumerable<Ruling> shapes)
        {
            return Bounds(shapes.Select(r => r.Line.GetBoundingRectangle()));
        }

        public static PdfRectangle Bounds(IEnumerable<PdfRectangle> shapes)
        {
            if (!shapes.Any())
            {
                throw new ArgumentException("shapes can't be empty");
            }

            var minX = shapes.Min(r => r.Left);
            var minY = shapes.Min(r => r.Bottom);
            var maxX = shapes.Max(r => r.Right);
            var maxY = shapes.Max(r => r.Top);
            return new PdfRectangle(minX, minY, maxX, maxY);
        }

        public static TableRectangle Bounds(IEnumerable<TableRectangle> shapes)
        {
            if (!shapes.Any())
            {
                throw new ArgumentException("shapes can't be empty");
            }

            return new TableRectangle(Bounds(shapes.Select(s => s.BoundingBox)));
        }

        // range iterator
        public static IEnumerable<int> Range(int begin, int end)
        {
            return Enumerable.Range(begin, end - begin);
        }

        public static bool IsNumeric(string cs)
        {
            if (string.IsNullOrEmpty(cs))
            {
                return false;
            }

            int sz = cs.Length;
            for (int i = 0; i < sz; i++)
            {
                if (!char.IsNumber(cs, i))
                {
                    return false;
                }
            }
            return true;
        }

        public static string Join(string glue, params string[] s)
        {
            int k = s.Length;
            if (k == 0)
            {
                return null;
            }

            StringBuilder outp = new StringBuilder();
            outp.Append(s[0]);
            for (int x = 1; x < k; ++x)
            {
                outp.Append(glue).Append(s[x]);
            }
            return outp.ToString();
        }

        public static List<List<T>> Transpose<T>(List<List<T>> table)
        {
            List<List<T>> ret = new List<List<T>>();
            int N = table[0].Count;
            for (int i = 0; i < N; i++)
            {
                List<T> col = new List<T>();
                foreach (List<T> row in table)
                {
                    col.Add(row[i]);
                }
                ret.Add(col);
            }
            return ret;
        }

        /**
         * Wrap Collections.sort so we can fallback to a non-stable quicksort if we're
         * running on JDK7+
         */
        public static void Sort<T>(List<T> list) where T : TableRectangle
        {
            // Using OrderBy() insted of Sort() to keep order when equality
            var newList = list.OrderBy(x => x).ToList();
            list.Clear();
            list.AddRange(newList);
        }

        public static void Sort<T>(List<T> list, IComparer<T> comparer) where T : TableRectangle
        {
            // Using OrderBy() insted of Sort() to keep order when equality
            var newList = list.OrderBy(x => x, comparer).ToList();
            list.Clear();
            list.AddRange(newList);
        }

        public static List<int> ParsePagesOption(string pagesSpec)
        {
            if (pagesSpec.Equals("all"))
            {
                return null;
            }

            List<int> rv = new List<int>();

            string[] ranges = pagesSpec.Split(',');
            for (int i = 0; i < ranges.Length; i++)
            {
                string[] r = ranges[i].Split('-');
                if (r.Length == 0 || !Utils.IsNumeric(r[0]) || (r.Length > 1 && !Utils.IsNumeric(r[1])))
                {
                    throw new FormatException("Syntax error in page range specification");
                }

                if (r.Length < 2)
                {
                    rv.Add(int.Parse(r[0]));
                }
                else
                {
                    int t = int.Parse(r[0]);
                    int f = int.Parse(r[1]);
                    if (t > f)
                    {
                        throw new FormatException("Syntax error in page range specification");
                    }
                    rv.AddRange(Utils.Range(t, f + 1));
                }
            }

            rv.Sort();
            return rv;
        }

        private class PointXComparer : IComparer<PdfPoint>
        {
            public int Compare(PdfPoint arg0, PdfPoint arg1)
            {
                return arg0.X.CompareTo(arg1.X);
            }
        }

        private class PointYComparer : IComparer<PdfPoint>
        {
            public int Compare(PdfPoint arg0, PdfPoint arg1)
            {
                return -arg0.Y.CompareTo(arg1.Y); //bobld multiply by -1 to sort from top to bottom (reading order)
            }
        }

        /// <summary>
        /// re-implemented.
        /// </summary>
        /// <param name="rulings"></param>
        /// <param name="xThreshold"></param>
        /// <param name="yThreshold"></param>
        public static void SnapPoints(this List<Ruling> rulings, double xThreshold, double yThreshold)
        {
            // collect points and keep a Line -> p1,p2 map
            Dictionary<double, double> newXCoordinates = new Dictionary<double, double>();
            Dictionary<double, double> newYCoordinates = new Dictionary<double, double>();

            List<PdfPoint> points = new List<PdfPoint>();
            foreach (Ruling r in rulings)
            {
                points.Add(r.P1);
                points.Add(r.P2);
            }

            // snap by X
            points.Sort(new PointXComparer());

            List<List<PdfPoint>> groupedPoints = new List<List<PdfPoint>>();
            groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { points[0] }));

            foreach (PdfPoint p in points.SubList(1, points.Count)) // - 1)) error in the java version: the second bound is exclusive. fails 'testColumnRecognition' test + https://github.com/tabulapdf/tabula-java/pull/311
            {
                List<PdfPoint> last = groupedPoints[groupedPoints.Count - 1];
                if (Math.Abs(p.X - last[0].X) < xThreshold)
                {
                    groupedPoints[groupedPoints.Count - 1].Add(p);
                }
                else
                {
                    groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { p }));
                }
            }

            foreach (List<PdfPoint> group in groupedPoints)
            {
                double avgLoc = 0;
                foreach (PdfPoint p in group)
                {
                    avgLoc += p.X;
                }

                avgLoc /= group.Count;
                for (int p = 0; p < group.Count; p++)
                {
                    newXCoordinates[group[p].X] = Utils.Round(avgLoc, 6);
                }
            }
            // ---

            // snap by Y
            points.Sort(new PointYComparer());

            groupedPoints = new List<List<PdfPoint>>
            {
                new List<PdfPoint>(new PdfPoint[] { points[0] })
            };

            foreach (PdfPoint p in points.SubList(1, points.Count)) // - 1)) error in the java version: the second bound is exclusive + https://github.com/tabulapdf/tabula-java/pull/311
            {
                List<PdfPoint> last = groupedPoints[groupedPoints.Count - 1];
                if (Math.Abs(p.Y - last[0].Y) < yThreshold)
                {
                    groupedPoints[groupedPoints.Count - 1].Add(p);
                }
                else
                {
                    groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { p }));
                }
            }

            foreach (List<PdfPoint> group in groupedPoints)
            {
                double avgLoc = 0;
                foreach (PdfPoint p in group)
                {
                    avgLoc += p.Y;
                }

                avgLoc /= group.Count;
                for (int p = 0; p < group.Count; p++)
                {
                    newYCoordinates[group[p].Y] = Utils.Round(avgLoc, 6);
                }
            }
            // ---

            // finally, modify lines
            for (int i = 0; i < rulings.Count; i++)
            {
                var current = rulings[i];
                rulings[i] = new Ruling(new PdfPoint(newXCoordinates[current.Line.Point1.X], newYCoordinates[current.Line.Point1.Y]),
                                        new PdfPoint(newXCoordinates[current.Line.Point2.X], newYCoordinates[current.Line.Point2.Y]));
            }
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="dpi"></param>
        public static object PageConvertToImage(Page page, int dpi) //, ImageType imageType) // BufferedImage
        {
            throw new NotImplementedException();
            /*
            using (PdfDocument document = new PdfDocument())
            {
                document.addPage(page);
                PDFRenderer renderer = new PDFRenderer(document);
                document.close();
                return renderer.renderImageWithDPI(0, dpi, imageType);
            }
            */
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="page"></param>
        /// <param name="dpi"></param>
        public static object PageConvertToImage(PdfDocument doc, Page page, int dpi) //, ImageType imageType) // BufferedImage
        {
            throw new NotImplementedException();
            //PDFRenderer renderer = new PDFRenderer(doc);
            //return renderer.renderImageWithDPI(doc.getPages().indexOf(page), dpi, imageType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="fromIndex">Low endpoint (inclusive) of the subList</param>
        /// <param name="toIndex">High endpoint (exclusive) of the subList</param>
        public static List<T> SubList<T>(this IReadOnlyList<T> list, int fromIndex, int toIndex)
        {
            return list.ToList().GetRange(fromIndex, toIndex - fromIndex);
        }
    }
}
