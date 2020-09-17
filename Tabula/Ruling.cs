using ClipperLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Ruling.java
    public class Ruling
    {
        public PdfLine Line { get; private set; }

        public void SetLine(double x1, double y1, double x2, double y2)
        {
            SetLine(new PdfPoint(x1, y1), new PdfPoint(x2, y2));
        }

        public void SetLine(PdfPoint p1, PdfPoint p2)
        {
            if (Math.Round(p1.Y, 2) > Math.Round(p2.Y, 2)) // using round here to account for almost vert. or horiz. line before normalize
            {
                throw new ArgumentException("Points order is wrong. p1 needs to be below p2 (p1.Y <= p2.Y)");
            }

            // test X?

            Line = new PdfLine(p1, p2);
        }

        private static int PERPENDICULAR_PIXEL_EXPAND_AMOUNT = 2;
        private static int COLINEAR_OR_PARALLEL_PIXEL_EXPAND_AMOUNT = 1;
        private enum SOType { VERTICAL, HRIGHT, HLEFT }


        public Ruling(double top, double left, double width, double height)
            : this(new PdfPoint(left, top), new PdfPoint(left + width, height + top))
        {
        }

        /// <summary>
        /// Point order matters!
        /// </summary>
        /// <param name="p1">bottom point.</param>
        /// <param name="p2">top point</param>
        public Ruling(PdfPoint p1, PdfPoint p2)
        {
            SetLine(p1, p2);
            this.Normalize();
        }

        /// <summary>
        /// Normalize almost horizontal or almost vertical lines
        /// </summary>
        public void Normalize()
        {
            double angle = this.GetAngle();
            if (Utils.Within(angle, 0, 1) || Utils.Within(angle, 180, 1))
            {
                // almost horizontal
                this.SetLine(this.X1, this.Y1, this.X2, this.Y1);
            }
            else if (Utils.Within(angle, 90, 1) || Utils.Within(angle, 270, 1))
            {
                // almost vertical
                this.SetLine(this.X1, this.Y1, this.X1, this.Y2);
            }
            //else 
            //{
            //    System.out.println("oblique: " + this + " ("+ this.getAngle() + ")");
            //}
        }

        public bool Vertical()
        {
            //return this.length() > 0 && Utils.feq(this.x1, this.x2); //diff < ORIENTATION_CHECK_THRESHOLD;
            return this.Length() > 0 && Utils.Feq(this.X1, this.X2); //diff < ORIENTATION_CHECK_THRESHOLD;
        }

        public bool Horizontal()
        {
            return this.Length() > 0 && Utils.Feq(this.Y1, this.Y2); //diff < ORIENTATION_CHECK_THRESHOLD;
        }

        public bool Oblique()
        {
            return !(this.Vertical() || this.Horizontal());
        }

        /// <summary>
        /// attributes that make sense only for non-oblique lines
        /// these are used to have a single collapse method (in page, currently)
        /// </summary>
        /// <returns></returns>
        public double GetPosition()
        {
            if (this.Oblique())
            {
                throw new InvalidOperationException();
            }

            return this.Vertical() ? this.GetLeft() : this.GetBottom(); //this.getTop();
        }

        public void SetPosition(float v)
        {
            if (this.Oblique())
            {
                throw new InvalidOperationException();
            }

            if (this.Vertical())
            {
                this.SetLeft(v);
                this.SetRight(v);
            }
            else
            {
                this.SetTop(v);
                this.SetBottom(v);
            }
        }

        public double GetStart()
        {
            if (this.Oblique())
            {
                throw new InvalidOperationException();
            }

            return this.Vertical() ? this.GetTop() : this.GetRight(); //this.getLeft();
        }

        public void SetStart(double v)
        {
            if (this.Oblique())
            {
                throw new InvalidOperationException();
            }

            if (this.Vertical())
            {
                this.SetTop(v);
            }
            else
            {
                this.SetRight(v); //this.setLeft(v);
            }
        }

        public double GetEnd()
        {
            if (this.Oblique())
            {
                throw new InvalidOperationException();
            }

            return this.Vertical() ? this.GetBottom() : this.GetLeft(); //this.getRight();
        }

        public void SetEnd(double v)
        {
            if (this.Oblique())
            {
                throw new InvalidOperationException();
            }

            if (this.Vertical())
            {
                this.SetBottom(v);
            }
            else
            {
                this.SetLeft(v); //this.setRight(v);
            }
        }

        private void SetStartEnd(double start, double end)
        {
            if (this.Oblique())
            {
                throw new InvalidOperationException();
            }

            if (this.Vertical())
            {
                this.SetTop(start);
                this.SetBottom(end);
            }
            else
            {
                this.SetRight(start);//this.setLeft(start);
                this.SetLeft(end);//this.setRight(end);
            }
        }

        public bool PerpendicularTo(Ruling other)
        {
            return this.Vertical() == other.Horizontal();
        }

        public bool Colinear(PdfPoint point)
        {
            return point.X >= this.X1 &&
                   point.X <= this.X2 &&
                   point.Y >= this.Y1 &&
                   point.Y <= this.Y2;
        }

        /// <summary>
        /// if the lines we're comparing are colinear or parallel, we expand them by a only 1 pixel,
        /// because the expansions are additive
        /// (e.g. two vertical lines, at x = 100, with one having y2 of 98 and the other having y1 of 102 would
        /// erroneously be said to nearlyIntersect if they were each expanded by 2 (since they'd both terminate at 100).
        /// By default the COLINEAR_OR_PARALLEL_PIXEL_EXPAND_AMOUNT is only 1 so the total expansion is 2.
        /// A total expansion amount of 2 is empirically verified to work sometimes. It's not a magic number from any
        /// source other than a little bit of experience.)
        /// </summary>
        /// <param name="another"></param>
        /// <returns></returns>
        public bool NearlyIntersects(Ruling another)
        {
            return this.NearlyIntersects(another, COLINEAR_OR_PARALLEL_PIXEL_EXPAND_AMOUNT);
        }

        public bool NearlyIntersects(Ruling another, int colinearOrParallelExpandAmount)
        {
            if (this.IntersectsLine(another))
            {
                return true;
            }

            bool rv;
            if (this.PerpendicularTo(another))
            {
                rv = this.Expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT).IntersectsLine(another);
            }
            else
            {
                rv = this.Expand(colinearOrParallelExpandAmount)
                        .IntersectsLine(another.Expand(colinearOrParallelExpandAmount));
            }

            return rv;
        }

        public Ruling Intersect(PdfRectangle clip)
        {
            // use clipper 
            throw new NotImplementedException();

            /*
            Line2D.Float clipee = (Line2D.Float)this.clone();
            boolean clipped = new CohenSutherlandClipping(clip).clip(clipee);

            if (clipped)
            {
                return new Ruling(clipee.getP1(), clipee.getP2());
            }
            else
            {
                return this;
            }
            */
        }

        public Ruling Expand(double amount)
        {
            Ruling r = this.Clone(); //.MemberwiseClone(); //?????? .clone();
            r.SetStart(this.GetStart() + amount); //- amount);
            r.SetEnd(this.GetEnd() - amount); //+ amount);
            return r;
        }

        public PdfPoint? IntersectionPoint(Ruling other)
        {
            Ruling this_l = this.Expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT);
            Ruling other_l = other.Expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT);
            Ruling horizontal, vertical;

            if (!this_l.IntersectsLine(other_l))
            {
                return null;
            }

            if (this_l.Horizontal() && other_l.Vertical())
            {
                horizontal = this_l;
                vertical = other_l;
            }
            else if (this_l.Vertical() && other_l.Horizontal())
            {
                vertical = this_l;
                horizontal = other_l;
            }
            else
            {
                throw new ArgumentException("lines must be orthogonal, vertical and horizontal");
            }
            return new PdfPoint(vertical.GetLeft(), horizontal.GetTop());
        }

        public override bool Equals(object other)
        {
            /*
            if (this == other)
                return true;

            if (!(other is Ruling)) return false;

            Ruling o = (Ruling)other;
            return this.getP1().Equals(o.getP1()) && this.getP2().Equals(o.getP2());
            */

            if (other is Ruling o)
            {
                return this.GetP1().Equals(o.GetP1()) && this.GetP2().Equals(o.GetP2());
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Line.GetHashCode();
        }

        public double GetTop()
        {
            return this.Y2; //.y1;
        }

        public void SetTop(double v)
        {
            //setLine(this.getLeft(), v, this.getRight(), this.getBottom());
            SetLine(this.GetLeft(), this.GetBottom(), this.GetRight(), v);
        }

        public double GetLeft()
        {
            return this.X1; // not sure here!!
        }

        public void SetLeft(double v)
        {
            SetLine(v, this.GetTop(), this.GetRight(), this.GetBottom());
        }

        public double GetBottom()
        {
            return this.Y1; //.y2;
        }

        public void SetBottom(double v)
        {
            //setLine(this.getLeft(), this.getTop(), this.getRight(), v);
            SetLine(this.GetLeft(), v, this.GetRight(), this.GetTop());
        }

        public double GetRight()
        {
            return this.X2;  // not sure here!!
        }

        public void SetRight(double v)
        {
            SetLine(this.GetLeft(), this.GetTop(), v, this.GetBottom());
        }

        public double GetWidth()
        {
            return this.GetRight() - this.GetLeft();
        }

        public double GetHeight()
        {
            //return this.getBottom() - this.getTop();
            return this.GetTop() - this.GetBottom();
        }

        public double GetAngle()
        {
            //double angle = Math.toDegrees(Math.Atan2(this.getP2().getY() - this.getP1().getY()
            //                                         this.getP2().getX() - this.getP1().getX()));
            double angle = Distances.Angle(this.GetP1(), this.GetP2());

            if (angle < 0)
            {
                angle += 360;
            }
            return angle;
        }

        public override string ToString()
        {
            /*
            StringBuilder sb = new StringBuilder();
            Formatter formatter = new Formatter(sb);
            String rv = str formatter.format(Locale.US, "%s[x1=%f y1=%f x2=%f y2=%f]", this.GetType().toString(), this.x1, this.y1, this.x2, this.y2).toString();
            formatter.close();
            return rv;
            */
            return $"{this.GetType()}[x1={this.X1:0.00} y1={this.Y1:0.00} x2={this.X2:0.00} y2={this.Y2:0.00}]";
        }

        public static List<Ruling> CropRulingsToArea(List<Ruling> rulings, PdfRectangle area)
        {
            // use clipper
            var clipper = new Clipper();
            clipper.AddPath(Clipper.ToClipperIntPoints(area), PolyType.ptClip, true);

            foreach (Ruling r in rulings)
            {
                clipper.AddPath(Clipper.ToClipperIntPoints(r), PolyType.ptSubject, false);
            }

            var solutions = new PolyTree();
            if (clipper.Execute(ClipType.ctIntersection, solutions))
            {
                List<Ruling> rv = new List<Ruling>();
                foreach (var solution in solutions.Childs)
                {
                    var x0 = solution.Contour[0].X / 10_000.0;
                    var x1 = solution.Contour[1].X / 10_000.0;
                    double xmin = Math.Min(x0, x1);
                    double xmax = Math.Max(x0, x1);

                    // warning
                    // force xmin to be in p1, xmax to be in p2, might lead to odd stuff if not vertic or horiz
                    // do not bother with y for the moment, will throw an error anyway in Ruling()
                    // needed for testExtractColumnsCorrectly3() and testSpreadsheetExtractionIssue656() to succeed
                    rv.Add(new Ruling(new PdfPoint(xmin, solution.Contour[0].Y / 10_000.0),
                                      new PdfPoint(xmax, solution.Contour[1].Y / 10_000.0)));
                }
                return rv;
            }
            else
            {
                return new List<Ruling>();
            }

            //List<Ruling> rv = new List<Ruling>();
            //foreach (Ruling r in rulings)
            //{
            //    if (r.intersects(area))
            //    {
            //        rv.Add(r.intersect(area));
            //    }
            //}
            //return rv;
        }

        private class TreeMapRulingComparator : IComparer<Ruling>
        {
            public int Compare(Ruling o1, Ruling o2)
            {
                return -o1.GetTop().CompareTo(o2.GetTop());  //bobld multiply by -1 to sort from top to bottom (reading order)
                //return java.lang.Double.compare(o1.getTop(), o2.getTop());
            }
        }

        private class TreeMapPdfPointComparator : IComparer<PdfPoint>
        {
            public int Compare(PdfPoint o1, PdfPoint o2)
            {
                if (o1.Y < o2.Y) return 1;  // (o1.Y > o2.Y)
                if (o1.Y > o2.Y) return -1; // (o1.Y < o2.Y)
                if (o1.X > o2.X) return 1;
                if (o1.X < o2.X) return -1;
                return 0;
            }
        }

        private class SortObjectComparer : IComparer<SortObject>
        {
            public int Compare(SortObject a, SortObject b)
            {
                int rv;
                if (Utils.Feq(a.position, b.position))
                {
                    if (a.type == SOType.VERTICAL && b.type == SOType.HLEFT)
                    {
                        rv = 1;
                    }
                    else if (a.type == SOType.VERTICAL && b.type == SOType.HRIGHT)
                    {
                        rv = -1;
                    }
                    else if (a.type == SOType.HLEFT && b.type == SOType.VERTICAL)
                    {
                        rv = -1;
                    }
                    else if (a.type == SOType.HRIGHT && b.type == SOType.VERTICAL)
                    {
                        rv = 1;
                    }
                    else
                    {
                        rv = a.position.CompareTo(b.position);
                    }
                }
                else
                {
                    return a.position.CompareTo(b.position);
                }
                return rv;
            }
        }

        private class SortObject
        {
            internal SOType type;      //protected
            internal double position;  //protected
            internal Ruling ruling;    //protected

            public SortObject(SOType type, double position, Ruling ruling)
            {
                this.type = type;
                this.position = position;
                this.ruling = ruling;
            }
        }

        /// <summary>
        /// log(n) implementation of find_intersections
        /// based on http://people.csail.mit.edu/indyk/6.838-old/handouts/lec2.pdf
        /// </summary>
        /// <param name="horizontals"></param>
        /// <param name="verticals"></param>
        /// <returns></returns>
        public static SortedDictionary<PdfPoint, Ruling[]> FindIntersections(List<Ruling> horizontals, List<Ruling> verticals)
        {
            //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Ruling.java#L312

            List<SortObject> sos = new List<SortObject>(); //ArrayList<>();
            SortedDictionary<Ruling, bool> tree = new SortedDictionary<Ruling, bool>(new TreeMapRulingComparator());
            // The SortedDictionary will throw ArgumentException on duplicate keys.

            //TreeMap<Ruling, Boolean> tree = new TreeMap<>(new Comparator<Ruling>() 
            //{
            //        @Override
            //        public int compare(Ruling o1, Ruling o2)
            //    {
            //        return java.lang.Double.compare(o1.getTop(), o2.getTo());
            //    }
            //});

            SortedDictionary<PdfPoint, Ruling[]> rv = new SortedDictionary<PdfPoint, Ruling[]>(new TreeMapPdfPointComparator());
            // The SortedDictionary will throw ArgumentException on duplicate keys.

            //TreeMap<Point2D, Ruling[]> rv = new TreeMap<>(new Comparator<Point2D>() 
            //{
            //        @Override
            //        public int compare(Point2D o1, Point2D o2)
            //    {
            //        if (o1.getY() > o2.getY()) return 1;
            //        if (o1.getY() < o2.getY()) return -1;
            //        if (o1.getX() > o2.getX()) return 1;
            //        if (o1.getX() < o2.getX()) return -1;
            //        return 0;
            //    }
            //});

            foreach (Ruling h in horizontals)
            {
                sos.Add(new SortObject(SOType.HLEFT, h.GetLeft() - PERPENDICULAR_PIXEL_EXPAND_AMOUNT, h));
                sos.Add(new SortObject(SOType.HRIGHT, h.GetRight() + PERPENDICULAR_PIXEL_EXPAND_AMOUNT, h));
            }

            foreach (Ruling v in verticals)
            {
                sos.Add(new SortObject(SOType.VERTICAL, v.GetLeft(), v));
            }

            sos.Sort(new SortObjectComparer());  //Collections.sort(sos, new Comparator<SortObject>() ...

            foreach (SortObject so in sos)
            {
                switch (so.type)
                {
                    case SOType.VERTICAL:
                        foreach (var h in tree)//.entrySet()) 
                        {
                            PdfPoint? i = h.Key.IntersectionPoint(so.ruling);
                            if (!i.HasValue)//== null)
                            {
                                continue;
                            }
                            //rv.put(i,
                            //       new Ruling[] { h.getKey().expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT),
                            //              so.ruling.expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT) });
                            rv[i.Value] = new Ruling[]
                            {
                                h.Key.Expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT),
                                so.ruling.Expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT)
                            };
                        }
                        break;
                    case SOType.HRIGHT:
                        tree.Remove(so.ruling);
                        break;
                    case SOType.HLEFT:
                        tree[so.ruling] = true; //.put(so.ruling, true);
                        break;
                }
            }

            return rv;
        }

        public static List<Ruling> CollapseOrientedRulings(List<Ruling> lines)
        {
            return CollapseOrientedRulings(lines, COLINEAR_OR_PARALLEL_PIXEL_EXPAND_AMOUNT);
        }

        private class RulingComparer : IComparer<Ruling>
        {
            public int Compare(Ruling a, Ruling b)
            {
                double diff = a.GetPosition() - b.GetPosition();
                return (diff == 0 ? a.GetStart() - b.GetStart() : diff).CompareTo(0);
            }
        }

        public static List<Ruling> CollapseOrientedRulings(List<Ruling> lines, int expandAmount)
        {
            List<Ruling> rv = new List<Ruling>();
            lines.Sort(new RulingComparer());

            foreach (Ruling next_line in lines)
            {
                Ruling last = rv.Count == 0 ? null : rv[rv.Count - 1];
                // if current line colinear with next, and are "close enough": expand current line
                if (last != null && Utils.Feq(next_line.GetPosition(), last.GetPosition()) && last.NearlyIntersects(next_line, expandAmount))
                {
                    double lastStart = last.GetStart();
                    double lastEnd = last.GetEnd();

                    bool lastFlipped = lastStart > lastEnd;
                    bool nextFlipped = next_line.GetStart() > next_line.GetEnd();

                    bool differentDirections = nextFlipped != lastFlipped;
                    double nextS = differentDirections ? next_line.GetEnd() : next_line.GetStart();
                    double nextE = differentDirections ? next_line.GetStart() : next_line.GetEnd();

                    double newStart = lastFlipped ? Math.Max(nextS, lastStart) : Math.Min(nextS, lastStart);
                    double newEnd = lastFlipped ? Math.Min(nextE, lastEnd) : Math.Max(nextE, lastEnd);
                    last.SetStartEnd(newStart, newEnd);

                    Debug.Assert(!last.Oblique());
                }
                else if (next_line.Length() == 0)
                {
                    continue;
                }
                else
                {
                    rv.Add(next_line);
                }
            }

            return rv;
        }

        #region helpers
        public double Length() => this.Line.Length;
        public double X1 => this.Line.Point1.X;
        public double X2 => this.Line.Point2.X;
        public double Y1 => this.Line.Point1.Y;
        public double Y2 => this.Line.Point2.Y;
        public double GetX1() => X1;
        public double GetX2() => X2;
        public double GetY2() => Y2;
        public double GetY1() => Y1;
        public PdfPoint GetP1() => Line.Point1;
        public PdfPoint GetP2() => Line.Point2;

        /// <summary>
        /// True if both horizontal, aligned and overlap (i.e. infinite intersection points).
        /// True if both vertical, aligned and overlap (i.e. infinite intersection points).
        /// True if not parallel and intersect (i.e. in intersection point).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IntersectsLine(Ruling other)
        {
            // include case point are the same
            if (this.Line.Point1.Equals(other.Line.Point1) ||
                this.Line.Point1.Equals(other.Line.Point2) ||
                this.Line.Point2.Equals(other.Line.Point1) ||
                this.Line.Point2.Equals(other.Line.Point2)) return true;

            // include case where both are horizontal and overlap
            if (this.Horizontal() && other.Horizontal())
            {
                if (this.Y1.Equals(other.Y1) && // share same y
                    Math.Max(0, Math.Min(this.GetRight(), other.GetRight()) - Math.Max(this.GetLeft(), other.GetLeft())) > 0) // overlap
                {
                    return true;
                }
            }
            // include case where both are vertical and overlap
            else if (this.Vertical() && other.Vertical())
            {
                if (this.X1.Equals(other.X1) && // share same x
                    Math.Max(0, Math.Min(this.GetTop(), other.GetTop()) - Math.Max(this.GetBottom(), other.GetBottom())) > 0) // overlap
                {
                    return true;
                }
            }
            // else check if parallel and overlap

            return this.Line.IntersectsWith(other.Line);
        }

        public bool Intersects(TableRectangle rectangle)
        {
            // should be the same???
            return rectangle.IntersectsLine(this);
        }

        /// <summary>
        /// Deep copy.
        /// </summary>
        public Ruling Clone()
        {
            return new Ruling(new PdfPoint(this.Line.Point1.X, this.Line.Point1.Y),
                              new PdfPoint(this.Line.Point2.X, this.Line.Point2.Y));
        }
        #endregion
    }
}
