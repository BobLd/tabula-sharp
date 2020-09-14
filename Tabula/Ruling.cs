using ClipperLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Ruling.java
    public class Ruling
    {
        public PdfLine line { get; private set; }

        public void setLine(double x1, double y1, double x2, double y2)
        {
            setLine(new PdfPoint(x1, y1), new PdfPoint(x2, y2));
        }

        public void setLine(PdfPoint p1, PdfPoint p2)
        {
            if (Math.Round(p1.Y, 2) > Math.Round(p2.Y, 2)) // using round here to account for almost vert. or horiz. line before normalize
            {
                throw new ArgumentException("Points order is wrong. p1 needs to be below p2 (p1.Y <= p2.Y)");
            }

            // test X?

            line = new PdfLine(p1, p2);
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
            setLine(p1, p2);
            this.normalize();
        }

        /// <summary>
        /// Normalize almost horizontal or almost vertical lines
        /// </summary>
        public void normalize()
        {
            double angle = this.getAngle();
            if (Utils.within(angle, 0, 1) || Utils.within(angle, 180, 1))
            {
                // almost horizontal
                this.setLine(this.x1, this.y1, this.x2, this.y1);
            }
            else if (Utils.within(angle, 90, 1) || Utils.within(angle, 270, 1))
            {
                // almost vertical
                this.setLine(this.x1, this.y1, this.x1, this.y2);
            }
            //else 
            //{
            //    System.out.println("oblique: " + this + " ("+ this.getAngle() + ")");
            //}
        }

        public bool vertical()
        {
            //return this.length() > 0 && Utils.feq(this.x1, this.x2); //diff < ORIENTATION_CHECK_THRESHOLD;
            return this.length() > 0 && Utils.feq(this.x1, this.x2); //diff < ORIENTATION_CHECK_THRESHOLD;
        }

        public bool horizontal()
        {
            return this.length() > 0 && Utils.feq(this.y1, this.y2); //diff < ORIENTATION_CHECK_THRESHOLD;
        }

        public bool oblique()
        {
            return !(this.vertical() || this.horizontal());
        }

        /// <summary>
        /// attributes that make sense only for non-oblique lines
        /// these are used to have a single collapse method (in page, currently)
        /// </summary>
        /// <returns></returns>
        public double getPosition()
        {
            if (this.oblique())
            {
                throw new InvalidOperationException(); // UnsupportedOperationException();
            }

            return this.vertical() ? this.getLeft() : this.getBottom(); //this.getTop();
        }

        public void setPosition(float v)
        {
            if (this.oblique())
            {
                throw new InvalidOperationException(); // UnsupportedOperationException();
            }

            if (this.vertical())
            {
                this.setLeft(v);
                this.setRight(v);
            }
            else
            {
                this.setTop(v);
                this.setBottom(v);
            }
        }

        public double getStart()
        {
            if (this.oblique())
            {
                throw new InvalidOperationException(); // UnsupportedOperationException();
            }

            return this.vertical() ? this.getTop() : this.getRight(); //this.getLeft();
        }

        public void setStart(double v)
        {
            if (this.oblique())
            {
                throw new InvalidOperationException(); // UnsupportedOperationException();
            }

            if (this.vertical())
            {
                this.setTop(v);
            }
            else
            {
                this.setRight(v); //this.setLeft(v);
            }
        }

        public double getEnd()
        {
            if (this.oblique())
            {
                throw new InvalidOperationException(); // UnsupportedOperationException();
            }

            return this.vertical() ? this.getBottom() : this.getLeft(); //this.getRight();
        }

        public void setEnd(double v)
        {
            if (this.oblique())
            {
                throw new InvalidOperationException(); // UnsupportedOperationException();
            }

            if (this.vertical())
            {
                this.setBottom(v);
            }
            else
            {
                this.setLeft(v); //this.setRight(v);
            }
        }

        private void setStartEnd(double start, double end)
        {
            if (this.oblique())
            {
                throw new InvalidOperationException(); // UnsupportedOperationException();
            }

            if (this.vertical())
            {
                this.setTop(start);
                this.setBottom(end);
            }
            else
            {
                this.setRight(start);//this.setLeft(start);
                this.setLeft(end);//this.setRight(end);
            }
        }

        public bool perpendicularTo(Ruling other)
        {
            return this.vertical() == other.horizontal();
        }

        public bool colinear(PdfPoint point)
        {
            return point.X >= this.x1 &&
                   point.X <= this.x2 &&
                   point.Y >= this.y1 &&
                   point.Y <= this.y2;
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
        public bool nearlyIntersects(Ruling another)
        {
            return this.nearlyIntersects(another, COLINEAR_OR_PARALLEL_PIXEL_EXPAND_AMOUNT);
        }

        public bool nearlyIntersects(Ruling another, int colinearOrParallelExpandAmount)
        {
            if (this.intersectsLine(another))
            {
                return true;
            }

            bool rv;
            if (this.perpendicularTo(another))
            {
                rv = this.expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT).intersectsLine(another);
            }
            else
            {
                rv = this.expand(colinearOrParallelExpandAmount)
                        .intersectsLine(another.expand(colinearOrParallelExpandAmount));
            }

            return rv;
        }

        public Ruling intersect(PdfRectangle clip) // 
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

        public Ruling expand(double amount)
        {
            Ruling r = (Ruling)this.MemberwiseClone(); //?????? .clone();
            r.setStart(this.getStart() + amount); //- amount);
            r.setEnd(this.getEnd() - amount); //+ amount);
            return r;
        }

        public PdfPoint? intersectionPoint(Ruling other)
        {
            Ruling this_l = this.expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT);
            Ruling other_l = other.expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT);
            Ruling horizontal, vertical;

            if (!this_l.intersectsLine(other_l))
            {
                return null;
            }

            if (this_l.horizontal() && other_l.vertical())
            {
                horizontal = this_l;
                vertical = other_l;
            }
            else if (this_l.vertical() && other_l.horizontal())
            {
                vertical = this_l;
                horizontal = other_l;
            }
            else
            {
                throw new ArgumentException("lines must be orthogonal, vertical and horizontal");
            }
            return new PdfPoint(vertical.getLeft(), horizontal.getTop());
        }

        public override bool Equals(object other)
        {
            if (this == other)
                return true;

            if (!(other is Ruling)) return false;

            Ruling o = (Ruling)other;
            return this.getP1().Equals(o.getP1()) && this.getP2().Equals(o.getP2());
        }

        public override int GetHashCode()
        {
            return line.GetHashCode();
        }

        public double getTop()
        {
            return this.y2; //.y1;
        }

        public void setTop(double v)
        {
            //setLine(this.getLeft(), v, this.getRight(), this.getBottom());
            setLine(this.getLeft(), this.getBottom(), this.getRight(), v);
        }

        public double getLeft()
        {
            return this.x1; // not sure here!!
        }

        public void setLeft(double v)
        {
            setLine(v, this.getTop(), this.getRight(), this.getBottom());
        }

        public double getBottom()
        {
            return this.y1; //.y2;
        }

        public void setBottom(double v)
        {
            //setLine(this.getLeft(), this.getTop(), this.getRight(), v);
            setLine(this.getLeft(), v, this.getRight(), this.getTop());
        }

        public double getRight()
        {
            return this.x2;  // not sure here!!
        }

        public void setRight(double v)
        {
            setLine(this.getLeft(), this.getTop(), v, this.getBottom());
        }

        public double getWidth()
        {
            return this.getRight() - this.getLeft();
        }

        public double getHeight()
        {
            //return this.getBottom() - this.getTop();
            return this.getTop() - this.getBottom();
        }

        public double getAngle()
        {
            //double angle = Math.toDegrees(Math.Atan2(this.getP2().getY() - this.getP1().getY()
            //                                         this.getP2().getX() - this.getP1().getX()));
            double angle = Distances.Angle(this.getP1(), this.getP2());

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
            return $"{this.GetType()}[x1={this.x1} y1={this.y1} x2={this.x2} y2={this.y2}]";
        }

        public static List<Ruling> cropRulingsToArea(List<Ruling> rulings, PdfRectangle area)
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

        public class TreeMapRulingComparator : IComparer<Ruling>
        {
            public int Compare([AllowNull] Ruling o1, [AllowNull] Ruling o2)
            {
                return -o1.getTop().CompareTo(o2.getTop());  //bobld multiply by -1 to sort from top to bottom (reading order)
                //return java.lang.Double.compare(o1.getTop(), o2.getTop());
            }
        }
        public class TreeMapPdfPointComparator : IComparer<PdfPoint>
        {
            public int Compare([AllowNull] PdfPoint o1, [AllowNull] PdfPoint o2)
            {
                if (o1.Y < o2.Y) return 1;  // (o1.Y> o2.Y)
                if (o1.Y > o2.Y) return -1; // (o1.Y < o2.Y)
                if (o1.X > o2.X) return 1;
                if (o1.X < o2.X) return -1;
                return 0;
            }
        }

        class SortObjectComparer : IComparer<SortObject>
        {
            public int Compare([AllowNull] SortObject a, [AllowNull] SortObject b)
            {
                int rv;
                if (Utils.feq(a.position, b.position))
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

        class SortObject
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
        public static SortedDictionary<PdfPoint, Ruling[]> findIntersections(List<Ruling> horizontals, List<Ruling> verticals)
        {
            //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Ruling.java#L312

            List<SortObject> sos = new List<SortObject>(); //ArrayList<>();
            SortedDictionary<Ruling, bool> tree = new SortedDictionary<Ruling, bool>(new TreeMapRulingComparator());
            // The SortedDictionary will not work because it throws ArgumentException on duplicate keys. As the OP said, 
            // duplicate keys will be present. – eugen_nw Mar 24 '16 at 16:04

            //TreeMap<Ruling, Boolean> tree = new TreeMap<>(new Comparator<Ruling>() 
            //{
            //        @Override
            //        public int compare(Ruling o1, Ruling o2)
            //    {
            //        return java.lang.Double.compare(o1.getTop(), o2.getTo());
            //    }
            //});

            SortedDictionary<PdfPoint, Ruling[]> rv = new SortedDictionary<PdfPoint, Ruling[]>(new TreeMapPdfPointComparator());
            // The SortedDictionary will not work because it throws ArgumentException on duplicate keys. As the OP said, 
            // duplicate keys will be present. – eugen_nw Mar 24 '16 at 16:04

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
                sos.Add(new SortObject(SOType.HLEFT, h.getLeft() - PERPENDICULAR_PIXEL_EXPAND_AMOUNT, h));
                sos.Add(new SortObject(SOType.HRIGHT, h.getRight() + PERPENDICULAR_PIXEL_EXPAND_AMOUNT, h));
            }

            foreach (Ruling v in verticals)
            {
                sos.Add(new SortObject(SOType.VERTICAL, v.getLeft(), v));
            }

            sos.Sort(new SortObjectComparer());  //Collections.sort(sos, new Comparator<SortObject>() ...

            foreach (SortObject so in sos)
            {
                switch (so.type)
                {
                    case SOType.VERTICAL:
                        foreach (var h in tree)//.entrySet()) 
                        {
                            PdfPoint? i = h.Key.intersectionPoint(so.ruling);
                            if (!i.HasValue)//== null)
                            {
                                continue;
                            }
                            //rv.put(i,
                            //       new Ruling[] { h.getKey().expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT),
                            //              so.ruling.expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT) });
                            rv[i.Value] = new Ruling[]
                            {
                                h.Key.expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT),
                                so.ruling.expand(PERPENDICULAR_PIXEL_EXPAND_AMOUNT)
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

        public static List<Ruling> collapseOrientedRulings(List<Ruling> lines)
        {
            return collapseOrientedRulings(lines, COLINEAR_OR_PARALLEL_PIXEL_EXPAND_AMOUNT);
        }

        private class RulingComparer : IComparer<Ruling>
        {
            public int Compare([AllowNull] Ruling a, [AllowNull] Ruling b)
            {
                double diff = a.getPosition() - b.getPosition();
                return (diff == 0 ? a.getStart() - b.getStart() : diff).CompareTo(0);
            }
        }

        public static List<Ruling> collapseOrientedRulings(List<Ruling> lines, int expandAmount)
        {
            List<Ruling> rv = new List<Ruling>();
            lines.Sort(new RulingComparer());

            foreach (Ruling next_line in lines)
            {
                Ruling last = rv.Count == 0 ? null : rv[rv.Count - 1];
                // if current line colinear with next, and are "close enough": expand current line
                if (last != null && Utils.feq(next_line.getPosition(), last.getPosition()) && last.nearlyIntersects(next_line, expandAmount))
                {
                    double lastStart = last.getStart();
                    double lastEnd = last.getEnd();

                    bool lastFlipped = lastStart > lastEnd;
                    bool nextFlipped = next_line.getStart() > next_line.getEnd();

                    bool differentDirections = nextFlipped != lastFlipped;
                    double nextS = differentDirections ? next_line.getEnd() : next_line.getStart();
                    double nextE = differentDirections ? next_line.getStart() : next_line.getEnd();

                    double newStart = lastFlipped ? Math.Max(nextS, lastStart) : Math.Min(nextS, lastStart);
                    double newEnd = lastFlipped ? Math.Min(nextE, lastEnd) : Math.Max(nextE, lastEnd);
                    last.setStartEnd(newStart, newEnd);

                    Debug.Assert(!last.oblique());
                }
                else if (next_line.length() == 0)
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
        public double length() => this.line.Length;
        public double x1 => this.line.Point1.X;
        public double x2 => this.line.Point2.X;
        public double y1 => this.line.Point1.Y;
        public double y2 => this.line.Point2.Y;
        public double getX1() => x1;
        public double getX2() => x2;
        public double getY2() => y2;
        public double getY1() => y1;
        public PdfPoint getP1() => line.Point1;
        public PdfPoint getP2() => line.Point2;

        /// <summary>
        /// True if both horizontal and overlap (i.e. infinite intersection points).
        /// True if both vertical and overlap (i.e. infinite intersection points).
        /// True if not parallel and intersect (i.e. in intersection point).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool intersectsLine(Ruling other)
        {
            // include case point are the same
            if (this.line.Point1.Equals(other.line.Point1) ||
                this.line.Point1.Equals(other.line.Point2) ||
                this.line.Point2.Equals(other.line.Point1) ||
                this.line.Point2.Equals(other.line.Point2)) return true;

            // include case where both are horizontal and overlap
            if (this.horizontal() && other.horizontal())
            {
                if (this.y1.Equals(other.y1)) return true;
            }
            // include case where both are vertical and overlap
            else if (this.vertical() && other.vertical())
            {
                if (this.x1.Equals(other.x1)) return true;
            }
            // else check if parallel and overlap

            return this.line.IntersectsWith(other.line);
        }

        public bool intersects(TableRectangle rectangle)
        {
            // should be the same???
            return rectangle.intersectsLine(this);
        }
        #endregion
    }
}
