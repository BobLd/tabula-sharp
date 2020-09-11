using ClipperLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
            if (p1.Y > p2.Y)
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
                horizontal = this_l; vertical = other_l;
            }
            else if (this_l.vertical() && other_l.horizontal())
            {
                vertical = this_l; horizontal = other_l;
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

        #region clipper temporary
        private static IntPoint ToClipperIntPoint(PdfPoint point)
        {
            return new IntPoint(point.X * 10_000.0, point.Y * 10_000.0);
        }

        private static List<IntPoint> ToClipperIntPoints(PdfRectangle rect)
        {
            return new List<IntPoint>()
            {
                ToClipperIntPoint(rect.BottomLeft),
                ToClipperIntPoint(rect.TopLeft),
                ToClipperIntPoint(rect.TopRight),
                ToClipperIntPoint(rect.BottomRight),
                ToClipperIntPoint(rect.BottomLeft),
            };
        }
        private static List<IntPoint> ToClipperIntPoints(Ruling rect)
        {
            return new List<IntPoint>() { ToClipperIntPoint(rect.line.Point1), ToClipperIntPoint(rect.line.Point2) };
        }
        #endregion

        public static List<Ruling> cropRulingsToArea(List<Ruling> rulings, PdfRectangle area)
        {
            // use clipper
            var clipper = new Clipper();
            clipper.AddPath(ToClipperIntPoints(area), PolyType.ptClip, true);

            foreach (Ruling r in rulings)
            {
                clipper.AddPath(ToClipperIntPoints(r), PolyType.ptSubject, false);
            }

            var solutions = new PolyTree();
            if (clipper.Execute(ClipType.ctIntersection, solutions))
            {
                List<Ruling> rv = new List<Ruling>();
                foreach (var solution in solutions.Childs)
                {
                    rv.Add(new Ruling(new PdfPoint(solution.Contour[0].X / 10_000.0, solution.Contour[0].Y / 10_000.0),
                                      new PdfPoint(solution.Contour[1].X / 10_000.0, solution.Contour[1].Y / 10_000.0)));

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

        /// <summary>
        /// log(n) implementation of find_intersections
        /// based on http://people.csail.mit.edu/indyk/6.838-old/handouts/lec2.pdf
        /// </summary>
        /// <param name="horizontals"></param>
        /// <param name="verticals"></param>
        /// <returns></returns>
        public static Dictionary<PdfPoint, Ruling[]> findIntersections(List<Ruling> horizontals, List<Ruling> verticals)
        {
            //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Ruling.java#L312
            throw new NotImplementedException();
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
                return (diff == 0 ? a.getStart() - b.getStart() : diff).CompareTo(0f);
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

        public double getY2() => y2;
        public double getY1() => y1;
        public PdfPoint getP1() => line.Point1;
        public PdfPoint getP2() => line.Point2;

        public bool intersectsLine(Ruling other)
        {
            // include case point are the same
            if (this.line.Point1.Equals(other.line.Point1) ||
                this.line.Point1.Equals(other.line.Point2) ||
                this.line.Point2.Equals(other.line.Point1) ||
                this.line.Point2.Equals(other.line.Point2)) return true;

                return this.line.IntersectsWith(other.line);
        }
        #endregion
    }
}
