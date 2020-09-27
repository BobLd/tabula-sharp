using ClipperLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    //ported from tabula-java/blob/master/src/main/java/technology/tabula/Ruling.java
    public class Ruling
    {
        private static int PERPENDICULAR_PIXEL_EXPAND_AMOUNT = 2;
        private static int COLINEAR_OR_PARALLEL_PIXEL_EXPAND_AMOUNT = 1;
        private enum SOType { VERTICAL, HRIGHT, HLEFT }

        public PdfLine Line { get; private set; }

        public void SetLine(double x1, double y1, double x2, double y2)
        {
            SetLine(new PdfPoint(x1, y1), new PdfPoint(x2, y2));
        }

        public void SetLine(PdfPoint p1, PdfPoint p2)
        {
            /*
            if (Math.Round(p1.Y, 2) > Math.Round(p2.Y, 2)) // using round here to account for almost vert. or horiz. line before normalize
            {
                throw new ArgumentException("Points order is wrong. p1 needs to be below p2 (p1.Y <= p2.Y)");
            }
            */

            // test X?
            Debug.Assert(Math.Round(p1.Y, 2) <= Math.Round(p2.Y, 2), "Points order is wrong. p1 needs to be below p2 (p1.Y <= p2.Y)");

            Line = new PdfLine(p1, p2);
        }

        public Ruling(double bottom, double left, double width, double height)
            : this(new PdfPoint(left, bottom), new PdfPoint(left + width, height + bottom))
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
        /// Normalize almost horizontal or almost vertical lines.
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

        /// <summary>
        /// Is the <see cref="Ruling"/> vertical?
        /// </summary>
        public bool IsVertical=> this.Length > 0 && Utils.Feq(this.X1, this.X2);

        /// <summary>
        /// Is the <see cref="Ruling"/> horizontal?
        /// </summary>
        public bool IsHorizontal => this.Length > 0 && Utils.Feq(this.Y1, this.Y2);

        /// <summary>
        /// Is the <see cref="Ruling"/> oblique? Neither vertical nor horizontal.
        /// </summary>
        public bool IsOblique => !(this.IsVertical || this.IsHorizontal);

        /// <summary>
        /// Gets the ruling's length.
        /// </summary>
        public double Length => this.Line.Length;

        /// <summary>
        /// Point 1's X coordinate.
        /// </summary>
        public double X1 => this.P1.X;

        /// <summary>
        /// Point 2's X coordinate.
        /// </summary>
        public double X2 => this.P2.X;

        /// <summary>
        /// Point 1's Y coordinate.
        /// </summary>
        public double Y1 => this.P1.Y;

        /// <summary>
        /// Point 2's Y coordinate.
        /// </summary>
        public double Y2 => this.P2.Y;

        /// <summary>
        /// First ruling point.
        /// </summary>
        public PdfPoint P1 => Line.Point1;

        /// <summary>
        /// Second ruling point.
        /// </summary>
        public PdfPoint P2 => Line.Point2;

        /// <summary>
        /// Gets the ruling's position: The X coordinate if ruling is vertical or the Y coordinate if horizontal.
        /// <para>Attributes that make sense only for non-oblique lines these are used to have a single collapse method (in page, currently).</para>
        /// </summary>
        public double Position
        {
            get
            {
                if (this.IsOblique)
                {
                    throw new InvalidOperationException();
                }

                return this.IsVertical ? this.Left : this.Bottom; //this.getTop();
            }
        }

        /// <summary>
        /// Sets the ruling's position: The X coordinate if ruling is vertical or the Y coordinate if horizontal.
        /// </summary>
        /// <param name="v"></param>
        public void SetPosition(float v)
        {
            if (this.IsOblique)
            {
                throw new InvalidOperationException();
            }

            if (this.IsVertical)
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

        /// <summary>
        /// Gets the ruling's start coordinate: The Top coordinate if ruling is vertical or the Right coordinate if horizontal.
        /// </summary>
        public double Start
        {
            get
            {
                if (this.IsOblique)
                {
                    throw new InvalidOperationException();
                }

                return this.IsVertical ? this.Top : this.Right; //this.getLeft();
            }
        }

        /// <summary>
        /// Sets the ruling's start coordinate: The Top coordinate if ruling is vertical or the Right coordinate if horizontal.
        /// </summary>
        /// <param name="v"></param>
        public void SetStart(double v)
        {
            if (this.IsOblique)
            {
                throw new InvalidOperationException();
            }

            if (this.IsVertical)
            {
                this.SetTop(v);
            }
            else
            {
                this.SetRight(v); //this.setLeft(v);
            }
        }

        /// <summary>
        /// Gets the ruling's end coordinate: The Bottom coordinate if ruling is vertical or the Left coordinate if horizontal.
        /// </summary>
        public double End
        {
            get
            {
                if (this.IsOblique)
                {
                    throw new InvalidOperationException();
                }

                return this.IsVertical ? this.Bottom : this.Left; //this.getRight();
            }
        }

        /// <summary>
        /// Sets the ruling's end coordinate: The Bottom coordinate if ruling is vertical or the Left coordinate if horizontal.
        /// </summary>
        /// <param name="v"></param>
        public void SetEnd(double v)
        {
            if (this.IsOblique)
            {
                throw new InvalidOperationException();
            }

            if (this.IsVertical)
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
            if (this.IsOblique)
            {
                throw new InvalidOperationException();
            }

            if (this.IsVertical)
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

        /// <summary>
        /// Perpendicular?
        /// <para>Confusing function: only checks if (this.IsVertical == other.IsHorizontal)</para>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsPerpendicularTo(Ruling other)
        {
            return this.IsVertical == other.IsHorizontal;
        }

        public bool IsColinear(PdfPoint point)
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
            if (this.IsPerpendicularTo(another))
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

        public Ruling Expand(double amount)
        {
            Ruling r = this.Clone(); //.MemberwiseClone(); //??? .clone();
            r.SetStart(this.Start + amount); //- amount);
            r.SetEnd(this.End - amount); //+ amount);
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

            if (this_l.IsHorizontal && other_l.IsVertical)
            {
                horizontal = this_l;
                vertical = other_l;
            }
            else if (this_l.IsVertical && other_l.IsHorizontal)
            {
                vertical = this_l;
                horizontal = other_l;
            }
            else
            {
                throw new ArgumentException("lines must be orthogonal, vertical and horizontal", nameof(other));
            }
            return new PdfPoint(vertical.Left, horizontal.Top);
        }

        public override bool Equals(object other)
        {
            if (other is Ruling o)
            {
                return this.P1.Equals(o.P1) && this.P2.Equals(o.P2);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Line.GetHashCode();
        }

        /// <summary>
        /// Gets the point 2's Y coordinate.
        /// </summary>
        public double Top => this.Y2; //.y1;

        /// <summary>
        /// Sets the point 2's Y coordinate.
        /// </summary>
        /// <param name="v"></param>
        public void SetTop(double v)
        {
            SetLine(this.Left, this.Bottom, this.Right, v);
        }

        /// <summary>
        /// Gets the point 1's X coordinate.
        /// </summary>
        public double Left => this.X1; // not sure here!!

        /// <summary>
        /// Sets the point 1's X coordinate.
        /// </summary>
        /// <param name="v"></param>
        public void SetLeft(double v)
        {
            SetLine(v, this.Top, this.Right, this.Bottom);
        }

        /// <summary>
        /// Gets the point 1's Y coordinate.
        /// </summary>
        public double Bottom => this.Y1; //.y2;

        /// <summary>
        /// Sets the point 1's Y coordinate.
        /// </summary>
        /// <param name="v"></param>
        public void SetBottom(double v)
        {
            SetLine(this.Left, v, this.Right, this.Top);
        }

        /// <summary>
        /// Gets the point 2's X coordinate.
        /// </summary>
        public double Right => this.X2;  // not sure here!!

        /// <summary>
        /// Sets the point 2's X coordinate.
        /// </summary>
        /// <param name="v"></param>
        public void SetRight(double v)
        {
            SetLine(this.Left, this.Top, v, this.Bottom);
        }

        public double Width => this.Right - this.Left;
        public double Height => this.Top - this.Bottom;

        /// <summary>
        /// Computes the angle.
        /// <para>0 ≤ θ ≤ 360</para>
        /// </summary>
        public double GetAngle()
        {
            return Distances.BoundAngle0to360(Distances.Angle(this.P1, this.P2));
        }

        public override string ToString()
        {
            return $"{this.GetType()}[x1={this.X1:0.00},y1={this.Y1:0.00},x2={this.X2:0.00},y2={this.Y2:0.00}]";
        }

        /// <summary>
        /// Clips the rulings to the area.
        /// <para>Warning: force xMin to be in p1, xMax to be in p2, might lead to odd stuff if not vertical or horizontal.</para>
        /// </summary>
        /// <param name="rulings"></param>
        /// <param name="area"></param>
        public static List<Ruling> CropRulingsToArea(IReadOnlyList<Ruling> rulings, PdfRectangle area)
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

        private class TreeMapRulingComparer : IComparer<Ruling>
        {
            public int Compare(Ruling o1, Ruling o2)
            {
                return -o1.Top.CompareTo(o2.Top);  //bobld multiply by -1 to sort from top to bottom (reading order)
            }
        }

        private class TreeMapPdfPointComparer : IComparer<PdfPoint>
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
        public static SortedDictionary<PdfPoint, Ruling[]> FindIntersections(IReadOnlyList<Ruling> horizontals, IReadOnlyList<Ruling> verticals)
        {
            //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Ruling.java#L312

            List<SortObject> sos = new List<SortObject>();
            SortedDictionary<Ruling, bool> tree = new SortedDictionary<Ruling, bool>(new TreeMapRulingComparer()); // TreeMap<Ruling, Boolean> tree

            SortedDictionary<PdfPoint, Ruling[]> rv = new SortedDictionary<PdfPoint, Ruling[]>(new TreeMapPdfPointComparer()); // TreeMap<Point2D, Ruling[]> rv

            foreach (Ruling h in horizontals)
            {
                sos.Add(new SortObject(SOType.HLEFT, h.Left - PERPENDICULAR_PIXEL_EXPAND_AMOUNT, h));
                sos.Add(new SortObject(SOType.HRIGHT, h.Right + PERPENDICULAR_PIXEL_EXPAND_AMOUNT, h));
            }

            foreach (Ruling v in verticals)
            {
                sos.Add(new SortObject(SOType.VERTICAL, v.Left, v));
            }

            sos.Sort(new SortObjectComparer());  //Collections.sort(sos, new Comparator<SortObject>() ...

            foreach (SortObject so in sos)
            {
                switch (so.type)
                {
                    case SOType.VERTICAL:
                        foreach (var h in tree)
                        {
                            PdfPoint? i = h.Key.IntersectionPoint(so.ruling);
                            if (!i.HasValue) continue;

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
                        tree[so.ruling] = true;
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
                double diff = a.Position - b.Position;
                return (diff == 0 ? a.Start - b.Start : diff).CompareTo(0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="expandAmount"></param>
        public static List<Ruling> CollapseOrientedRulings(List<Ruling> lines, int expandAmount)
        {
            List<Ruling> rv = new List<Ruling>();
            lines.Sort(new RulingComparer());

            foreach (Ruling next_line in lines)
            {
                Ruling last = rv.Count == 0 ? null : rv[rv.Count - 1];
                // if current line colinear with next, and are "close enough": expand current line
                if (last != null && Utils.Feq(next_line.Position, last.Position) && last.NearlyIntersects(next_line, expandAmount))
                {
                    double lastStart = last.Start;
                    double lastEnd = last.End;

                    bool lastFlipped = lastStart > lastEnd;
                    bool nextFlipped = next_line.Start > next_line.End;

                    bool differentDirections = nextFlipped != lastFlipped;
                    double nextS = differentDirections ? next_line.End : next_line.Start;
                    double nextE = differentDirections ? next_line.Start : next_line.End;

                    double newStart = lastFlipped ? Math.Max(nextS, lastStart) : Math.Min(nextS, lastStart);
                    double newEnd = lastFlipped ? Math.Min(nextE, lastEnd) : Math.Max(nextE, lastEnd);
                    last.SetStartEnd(newStart, newEnd);

                    Debug.Assert(!last.IsOblique);
                }
                else if (next_line.Length == 0)
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
        /// <summary>
        /// True if both horizontal, aligned and overlap (i.e. infinite intersection points).
        /// True if both vertical, aligned and overlap (i.e. infinite intersection points).
        /// True if not parallel and intersect (i.e. in intersection point).
        /// </summary>
        /// <param name="other"></param>
        public bool IntersectsLine(Ruling other)
        {
            // include case point are the same
            if (this.Line.Point1.Equals(other.Line.Point1) ||
                this.Line.Point1.Equals(other.Line.Point2) ||
                this.Line.Point2.Equals(other.Line.Point1) ||
                this.Line.Point2.Equals(other.Line.Point2)) return true;

            // include case where both are horizontal and overlap
            if (this.IsHorizontal && other.IsHorizontal)
            {
                if (this.Y1.Equals(other.Y1) && // share same y
                    Math.Max(0, Math.Min(this.Right, other.Right) - Math.Max(this.Left, other.Left)) > 0) // overlap
                {
                    return true;
                }
            }
            // include case where both are vertical and overlap
            else if (this.IsVertical && other.IsVertical)
            {
                if (this.X1.Equals(other.X1) && // share same x
                    Math.Max(0, Math.Min(this.Top, other.Top) - Math.Max(this.Bottom, other.Bottom)) > 0) // overlap
                {
                    return true;
                }
            }
            // else check if parallel and overlap

            return this.Line.IntersectsWith(other.Line);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
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
