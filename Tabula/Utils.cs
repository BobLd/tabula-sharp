using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Utils.java
    /**
 * @author manuel
 */
    public static class Utils
    {
        public static bool within(double first, double second, double variance)
        {
            return second < first + variance && second > first - variance;
        }

        public static bool overlap(double y1, double height1, double y2, double height2, double variance)
        {
            return within(y1, y2, variance) || (y2 <= y1 && y2 >= y1 - height1) || (y1 <= y2 && y1 >= y2 - height2);
        }

        public static bool overlap(double y1, double height1, double y2, double height2)
        {
            return overlap(y1, height1, y2, height2, 0.1f);
        }

        private static float EPSILON = 0.01f;
        private static bool useQuickSort = useCustomQuickSort();

        public static bool feq(double f1, double f2)
        {
            return Math.Abs(f1 - f2) < EPSILON;
        }

        public static double round(double d, int decimalPlace)
        {
            return Math.Round(d, decimalPlace);
            /*
            BigDecimal bd = new BigDecimal(Double.ToString(d));
            bd = bd.setScale(decimalPlace, BigDecimal.ROUND_HALF_UP);
            return bd.floatValue();
            */
        }

        public static PdfRectangle bounds(IEnumerable<Ruling> shapes)
        {
            return bounds(shapes.Select(r => r.line.GetBoundingRectangle()));
        }

        public static PdfRectangle bounds(IEnumerable<PdfRectangle> shapes)
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

        public static TableRectangle bounds(IEnumerable<TableRectangle> shapes)
        {
            if (!shapes.Any()) //(shapes.isEmpty())
            {
                throw new ArgumentException("shapes can't be empty");
            }

            return new TableRectangle(bounds(shapes.Select(s => s.BoundingBox)));

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
        public static List<int> range(int begin, int end)
        {
            return Enumerable.Range(begin, end - begin).ToList();
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
        public static bool isNumeric(string cs)
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

        public static String join(String glue, params string[] s)
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

        public static List<List<T>> transpose<T>(List<List<T>> table)
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
        public static void sort<T>(List<T> list) where T : TableRectangle //IComparable<T> // <T extends Comparable<? super T>> 
        {
            list.Sort();

            //if (useQuickSort) QuickSort.sort(list);
            //else list.Sort();
        }

        public static void sort<T>(List<T> list, IComparer<T> comparator) where T : TableRectangle
        {
            list.Sort(comparator);

            //if (useQuickSort) QuickSort.sort(list, comparator);
            //else Collections.sort(list, comparator);
        }

        /// <summary>
        /// Always false.
        /// </summary>
        /// <returns></returns>
        private static bool useCustomQuickSort()
        {
            return false;
            /*
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
            */
        }

        public static List<int> parsePagesOption(string pagesSpec)
        {
            if (pagesSpec.Equals("all"))
            {
                return null;
            }

            List<int> rv = new List<int>();

            String[] ranges = pagesSpec.Split(",");
            for (int i = 0; i < ranges.Length; i++)
            {
                String[] r = ranges[i].Split("-");
                if (r.Length == 0 || !Utils.isNumeric(r[0]) || (r.Length > 1 && !Utils.isNumeric(r[1])))
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
                    rv.AddRange(Utils.range(t, f + 1));
                }
            }

            rv.Sort(); // Collections.sort(rv);
            return rv;
        }

        private class PointXComparer : IComparer<PdfPoint>
        {
            public int Compare([AllowNull] PdfPoint arg0, [AllowNull] PdfPoint arg1)
            {
                return arg0.X.CompareTo(arg1.X);
            }
        }

        private class PointYComparer : IComparer<PdfPoint>
        {
            public int Compare([AllowNull] PdfPoint arg0, [AllowNull] PdfPoint arg1)
            {
                return arg0.Y.CompareTo(arg1.Y);
            }
        }

        public static void snapPoints_old(this List<Ruling> rulings, double xThreshold, double yThreshold)
        {
            // collect points and keep a Line -> p1,p2 map
            Dictionary<Ruling, PdfPoint[]> linesToPoints = new Dictionary<Ruling, PdfPoint[]>();
            List<PdfPoint> points = new List<PdfPoint>();
            foreach (Ruling r in rulings)
            {
                PdfPoint p1 = r.getP1();
                PdfPoint p2 = r.getP2();
                linesToPoints[r] = new PdfPoint[] { p1, p2 }; // .put(r, new PdfPoint[] { p1, p2 });
                points.Add(p1);
                points.Add(p2);
            }

            // snap by X
            points.Sort(new PointXComparer());

            List<List<PdfPoint>> groupedPoints = new List<List<PdfPoint>>();
            groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { points[0] }));

            foreach (PdfPoint p in points.subList(1, points.Count - 1))
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
                    //p.setLocation(avgLoc, p.Y);
                    var current = group[p];
                    group[p] = new PdfPoint(avgLoc, current.Y);
                }
            }
            // ---

            // snap by Y
            points.Sort(new PointYComparer());

            groupedPoints = new List<List<PdfPoint>>();
            groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { points[0] }));

            foreach (PdfPoint p in points.subList(1, points.Count - 1))
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
                    //p.setLocation(p.X, avgLoc);
                    var current = group[p];
                    group[p] = new PdfPoint(current.X, avgLoc);
                }
            }
            // ---

            // finally, modify lines
            foreach (var ltp in linesToPoints)
            {
                PdfPoint[] p = ltp.Value;
                ltp.Key.setLine(p[0], p[1]);
                rulings[rulings.IndexOf(ltp.Key)] = new Ruling(p[0], p[1]);
            }
        }

        public static void snapPoints(this List<Ruling> rulings, double xThreshold, double yThreshold)
        {
            // collect points and keep a Line -> p1,p2 map
            Dictionary<double, double> newXCoordinates = new Dictionary<double, double>();
            Dictionary<double, double> newYCoordinates = new Dictionary<double, double>();

            List<PdfPoint> points = new List<PdfPoint>();
            foreach (Ruling r in rulings)
            {
                points.Add(r.getP1());
                points.Add(r.getP2());
            }

            // snap by X
            points.Sort(new PointXComparer());

            List<List<PdfPoint>> groupedPoints = new List<List<PdfPoint>>();
            groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { points[0] }));

            foreach (PdfPoint p in points.subList(1, points.Count)) // - 1)) error in the java version: the second bound is exclusive. fails 'testColumnRecognition' test + https://github.com/tabulapdf/tabula-java/pull/311
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
                    newXCoordinates[group[p].X] = Utils.round(avgLoc, 2); // round?
                }
            }
            // ---

            // snap by Y
            points.Sort(new PointYComparer());

            groupedPoints = new List<List<PdfPoint>>();
            groupedPoints.Add(new List<PdfPoint>(new PdfPoint[] { points[0] }));

            foreach (PdfPoint p in points.subList(1, points.Count)) // - 1)) error in the java version: the second bound is exclusive + https://github.com/tabulapdf/tabula-java/pull/311
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
                    newYCoordinates[group[p].Y] = Utils.round(avgLoc, 2); // round?
                }
            }
            // ---

            // finally, modify lines
            for (int i = 0; i < rulings.Count; i++)
            {
                var current = rulings[i];
                rulings[i] = new Ruling(new PdfPoint(newXCoordinates[current.line.Point1.X], newYCoordinates[current.line.Point1.Y]),
                                        new PdfPoint(newXCoordinates[current.line.Point2.X], newYCoordinates[current.line.Point2.Y]));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="fromIndex">low endpoint (inclusive) of the subList</param>
        /// <param name="toIndex">high endpoint (exclusive) of the subList</param>
        /// <returns></returns>
        public static List<T> subList<T>(this List<T> list, int fromIndex, int toIndex)
        {
            int count = toIndex - fromIndex; // - 1;
            return list.GetRange(fromIndex, count);
        }
    }
}
