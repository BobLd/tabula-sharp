using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Utils.java
    /**
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
        //private static bool useQuickSort = useCustomQuickSort();

        public static bool Feq(double f1, double f2)
        {
            return Math.Abs(f1 - f2) < EPSILON;
        }

        public static double Round(double d, int decimalPlace)
        {
            return Math.Round(d, decimalPlace);
            /*
            BigDecimal bd = new BigDecimal(Double.ToString(d));
            bd = bd.setScale(decimalPlace, BigDecimal.ROUND_HALF_UP);
            return bd.floatValue();
            */
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

            /*
            Iterator <? extends Shape > iter = shapes.iterator();
            Rectangle rv = new Rectangle();
            rv.setRect(iter.next().getBounds2D());

            while (iter.hasNext())
            {
                Rectangle2D.union(iter.next().getBounds2D(), rv, rv);
            }

            return rv;
            */

        }

        public static TableRectangle Bounds(IEnumerable<TableRectangle> shapes)
        {
            if (!shapes.Any()) //(shapes.isEmpty())
            {
                throw new ArgumentException("shapes can't be empty");
            }

            return new TableRectangle(Bounds(shapes.Select(s => s.BoundingBox)));

            /*
            Iterator <? extends Shape > iter = shapes.iterator();
            Rectangle rv = new Rectangle();
            rv.setRect(iter.next().getBounds2D());

            while (iter.hasNext())
            {
                Rectangle2D.union(iter.next().getBounds2D(), rv, rv);
            }

            return rv;
            */

        }

        // range iterator
        public static IEnumerable<int> Range(int begin, int end)
        {
            return Enumerable.Range(begin, end - begin); //.ToList();
            /* return new IList<int>()
             {
                 @Override
                 public Integer get(int index)
             {
                 return begin + index;
             }

             @Override
                 public int size()
             {
                 return end - begin;
             }*/
        }

        /* from apache.commons-lang */
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
        public static void Sort<T>(List<T> list) where T : TableRectangle //IComparable<T> // <T extends Comparable<? super T>> 
        {
            //list.Sort();

            // Using OrderBy() insted of Sort() to keep order when equality
            var newList = list.OrderBy(x => x).ToList();
            list.Clear();
            list.AddRange(newList);

            //if (useQuickSort) QuickSort.sort(list);
            //else list.Sort();
        }

        public static void Sort<T>(List<T> list, IComparer<T> comparator) where T : TableRectangle
        {
            //list.Sort(comparator);

            // Using OrderBy() insted of Sort() to keep order when equality
            var newList = list.OrderBy(x => x, comparator).ToList();
            list.Clear();
            list.AddRange(newList);
            //if (useQuickSort) QuickSort.sort(list, comparator);
            //else Collections.sort(list, comparator);
        }

        /*
        private static bool useCustomQuickSort()
        {
            // taken from PDFBOX:

            // check if we need to use the custom quicksort algorithm as a
            // workaround to the transitivity issue of TextPositionComparator:
            // https://issues.apache.org/jira/browse/PDFBOX-1512

            String numberybits = System.getProperty("java.version").split(
                    "-")[0]; // some Java version strings are 9-internal, which is dumb.
            String[] versionComponents = numberybits.split(
                    "\\.");
            int javaMajorVersion;
            int javaMinorVersion;
            if (versionComponents.length >= 2)
            {
                javaMajorVersion = Integer.parseInt(versionComponents[0]);
                javaMinorVersion = Integer.parseInt(versionComponents[1]);
            }
            else
            {
                javaMajorVersion = 1;
                javaMinorVersion = Integer.parseInt(versionComponents[0]);
            }
            bool is16orLess = javaMajorVersion == 1 && javaMinorVersion <= 6;
            String useLegacySort = System.getProperty("java.util.Arrays.useLegacyMergeSort");
            return !is16orLess || (useLegacySort != null && useLegacySort.equals("true"));
        }
        */

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
                    throw new FormatException("Syntax error in page range specification");// ParseException("Syntax error in page range specification");
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
                        throw new FormatException("Syntax error in page range specification");// throw new ParseException("Syntax error in page range specification");
                    }
                    rv.AddRange(Utils.Range(t, f + 1));
                }
            }

            rv.Sort(); // Collections.sort(rv);
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
                return -arg0.Y.CompareTo(arg1.Y);  //bobld multiply by -1 to sort from top to bottom (reading order)
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
                points.Add(r.GetP1());
                points.Add(r.GetP2());
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
                    newXCoordinates[group[p].X] = Utils.Round(avgLoc, 6); // round?
                }
            }
            // ---

            // snap by Y
            points.Sort(new PointYComparer());

            groupedPoints = new List<List<PdfPoint>>();
            groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { points[0] }));

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
                    newYCoordinates[group[p].Y] = Utils.Round(avgLoc, 6); // round?
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
        /// <param name="fromIndex">low endpoint (inclusive) of the subList</param>
        /// <param name="toIndex">high endpoint (exclusive) of the subList</param>
        /// <returns></returns>
        public static List<T> SubList<T>(this List<T> list, int fromIndex, int toIndex)
        {
            //int count = toIndex - fromIndex; // - 1;
            return list.GetRange(fromIndex, toIndex - fromIndex);
        }
    }
}
