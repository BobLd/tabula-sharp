using ClipperLib;
using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/ebc83ac2bb1a1cbe54ab8081d70f3c9fe81886ea/src/main/java/technology/tabula/Rectangle.java
    public class TableRectangle : IComparable<TableRectangle>
    {
        /// <summary>
        /// Sort top to bottom (as in reading order).
        /// Ill-defined comparator, from when Rectangle was Comparable.
        /// @see <a href="https://github.com/tabulapdf/tabula-java/issues/116">PR 116</a>
        /// </summary>
        [Obsolete("with no replacement")]
        public class ILL_DEFINED_ORDER : IComparer<TableRectangle>
        {
            /// <summary>
            /// Sort top to bottom (as in reading order).
            /// </summary>
            /// <param name="o1"></param>
            /// <param name="o2"></param>
            /// <returns></returns>
            public int Compare(TableRectangle o1, TableRectangle o2)
            {
                /*
                if (o1.Equals(o2)) return 0;
                if (o1.verticalOverlap(o2) > VERTICAL_COMPARISON_THRESHOLD)
                {
                    if (o1.isLtrDominant() == -1 && o2.isLtrDominant() == -1)
                    {
                        return -o1.getX().CompareTo(o2.getX());
                    }
                    return o1.getX().CompareTo(o2.getX());
                }
                else
                {
                    return -o1.getBottom().CompareTo(o2.getBottom()); //bobld multiply by -1 to sort from top to bottom (reading order)
                }
                */

                //https://github.com/3stack-software/tabula-java/blob/bbb508ba41538a51de6e49c7777f5067dec85b74/src/main/java/technology/tabula/Rectangle.java
                if (o1.Equals(o2)) return 0;
                double overlap = o1.VerticalOverlap(o2);
                double requiredOverlap = Math.Min(o1.Height, o2.Height) * VERTICAL_COMPARISON_THRESHOLD;
                if (overlap < requiredOverlap)
                {
                    int retval = -o1.Bottom.CompareTo(o2.Bottom); //bobld multiply by -1 to sort from top to bottom (reading order)

                    if (retval != 0)
                    {
                        retval = -o1.Top.CompareTo(o2.Top); //bobld multiply by -1 to sort from top to bottom (reading order)
                    }

                    if (retval != 0)
                    {
                        return retval;
                    }
                }
                return o1.IsLtrDominant() == -1 && o2.IsLtrDominant() == -1 ? -o1.X.CompareTo(o2.X) : o1.X.CompareTo(o2.X);
            }
        }

        protected static double VERTICAL_COMPARISON_THRESHOLD = 0.4;

        public PdfRectangle BoundingBox { get; private set; }

        public TableRectangle() : base()
        {
            BoundingBox = new PdfRectangle();
        }

        public TableRectangle(PdfRectangle rectangle) : this()
        {
            BoundingBox = rectangle;
        }

        [Obsolete("Use TableRectangle(PdfRectangle) instead")]
        public TableRectangle(double top, double left, double width, double height) : this()
        {
            //this.setRect(left, top, width, height);
            throw new ArgumentException();
        }

        public int CompareTo(TableRectangle other)
        {
            return new ILL_DEFINED_ORDER().Compare(this, other);
        }

        /// <summary>
        /// 1 is LTR, 0 is neutral, -1 is RTL.
        /// <para>Need this for fancy sorting in Tabula.TextChunk</para>
        /// </summary>
        public virtual int IsLtrDominant()
        {
            return 0;
        }

        public double VerticalOverlap(TableRectangle other)
        {
            return Math.Max(0, Math.Min(this.BoundingBox.Top, other.BoundingBox.Top)
                             - Math.Max(this.BoundingBox.Bottom, other.BoundingBox.Bottom));
        }

        public bool VerticallyOverlaps(TableRectangle other)
        {
            return VerticalOverlap(other) > 0;
        }

        public double HorizontalOverlap(TableRectangle other)
        {
            return Math.Max(0, Math.Min(this.Right, other.Right) - Math.Max(this.Left, other.Left));
        }

        public bool HorizontallyOverlaps(TableRectangle other)
        {
            return HorizontalOverlap(other) > 0;
        }

        public double VerticalOverlapRatio(TableRectangle other)
        {
            double delta = Math.Min(this.BoundingBox.Top - this.BoundingBox.Bottom,
                                    other.BoundingBox.Top - other.BoundingBox.Bottom);
            var overl = VerticalOverlap(other);
            return overl / delta;
        }

        public double OverlapRatio(TableRectangle other)
        {
            double intersectionWidth = Math.Max(0, Math.Min(this.Right, other.Right) - Math.Max(this.Left, other.Left));
            double intersectionHeight = Math.Max(0, Math.Min(this.Top, other.Top) - Math.Max(this.Bottom, other.Bottom));
            double intersectionArea = Math.Max(0, intersectionWidth * intersectionHeight);
            double unionArea = this.Area + other.Area - intersectionArea;
            return intersectionArea / unionArea;
        }

        public TableRectangle Merge(TableRectangle other)
        {
            this.SetRect(CreateUnion(this.BoundingBox, other.BoundingBox));
            return this;
        }

        /// <summary>
        /// Get the <see cref="TableRectangle"/>'s top coordinate.
        /// </summary>
        public double Top => BoundingBox.Top;

        /// <summary>
        /// Set the <see cref="TableRectangle"/>'s top coordinate.
        /// </summary>
        public void SetTop(double top)
        {
            //double deltaHeight = top - this.y;
            //this.setRect(this.x, top, this.width, this.height - deltaHeight);

            // BobLD: Not sure between top and bottom!
            this.SetRect(new PdfRectangle(this.Left, this.Bottom, this.Right, top));
        }

        /// <summary>
        /// Get the <see cref="TableRectangle"/>'s right coordinate.
        /// </summary>
        public double Right => BoundingBox.Right;

        /// <summary>
        /// Set the <see cref="TableRectangle"/>'s right coordinate.
        /// </summary>
        public void SetRight(double right)
        {
            //this.setRect(this.x, this.y, right - this.x, this.height);

            this.SetRect(new PdfRectangle(this.Left, this.Bottom, right, this.Top));
        }

        /// <summary>
        /// Get the <see cref="TableRectangle"/>'s left coordinate.
        /// </summary>
        public double Left => BoundingBox.Left;

        /// <summary>
        /// Set the <see cref="TableRectangle"/>'s left coordinate.
        /// </summary>
        public void SetLeft(double left)
        {
            //double deltaWidth = left - this.x;
            //this.setRect(left, this.y, this.width - deltaWidth, this.height);

            this.SetRect(new PdfRectangle(left, this.Bottom, this.Right, this.Top));
        }

        /// <summary>
        /// Get the <see cref="TableRectangle"/>'s bottom coordinate.
        /// </summary>
        public double Bottom => BoundingBox.Bottom;

        /// <summary>
        /// Set the <see cref="TableRectangle"/>'s bottom coordinate.
        /// </summary>
        public void SetBottom(double bottom)
        {
            //this.SetRect(this.X, this.Y, this.Width, bottom - this.Y);

            // BobLD: Not sure between top and bottom!
            this.SetRect(new PdfRectangle(this.Left, bottom, this.Right, this.Top));
        }

        /// <summary>
        /// Get the <see cref="TableRectangle"/>'s points.
        /// Counter-clockwise, starting from bottom left point.
        /// </summary>
        public PdfPoint[] Points => new PdfPoint[]
        {
            this.BoundingBox.BottomLeft,
            this.BoundingBox.BottomRight,
            this.BoundingBox.TopRight,
            this.BoundingBox.TopLeft
        };

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            // need to implement hash and equal in PdfPig's PdfRectangle
            result = prime * result + BoundingBox.BottomLeft.GetHashCode();
            result = prime * result + BoundingBox.TopLeft.GetHashCode();
            result = prime * result + BoundingBox.TopRight.GetHashCode();
            result = prime * result + BoundingBox.BottomRight.GetHashCode();
            result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(BoundingBox.Rotation), 0);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj is TableRectangle other)
            {
                if (!this.BoundingBox.BottomLeft.Equals(other.BoundingBox.BottomLeft)) return false;
                if (!this.BoundingBox.TopLeft.Equals(other.BoundingBox.TopLeft)) return false;
                if (!this.BoundingBox.TopRight.Equals(other.BoundingBox.TopRight)) return false;
                if (!this.BoundingBox.BottomRight.Equals(other.BoundingBox.BottomRight)) return false;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.GetType());
            string s = this.BoundingBox.ToString();
            sb.Append(s, 0, s.Length - 1);
            sb.Append($",bottom={this.Bottom:0.00},right={this.Right:0.00}]");
            return sb.ToString();
        }

        public static TableRectangle BoundingBoxOf(IEnumerable<TableRectangle> rectangles)
        {
            return Utils.Bounds(rectangles);
        }

        public bool Intersects(TableRectangle tableRectangle)
        {
            return this.BoundingBox.IntersectsWith(tableRectangle.BoundingBox);
        }

        public bool IntersectsLine(Ruling ruling)
        {
            return IntersectsLine(ruling.Line);
        }

        public bool IntersectsLine(PdfLine line)
        {
            var clipper = new Clipper();
            clipper.AddPath(Clipper.ToClipperIntPoints(this.BoundingBox), PolyType.ptClip, true);

            clipper.AddPath(Clipper.ToClipperIntPoints(line), PolyType.ptSubject, false);

            var solutions = new PolyTree();
            if (clipper.Execute(ClipType.ctIntersection, solutions))
            {
                return solutions.Childs.Count > 0;
            }
            return false;
        }

        #region helpers
        public double Area => BoundingBox.Area;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s top-left X coordinate.
        /// </summary>
        public double X => this.BoundingBox.TopLeft.X;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s top-left Y coordinate.
        /// </summary>
        public double Y => this.BoundingBox.TopLeft.Y;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s width.
        /// </summary>
        public double Width => this.BoundingBox.Width;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s height.
        /// </summary>
        public double Height => this.BoundingBox.Height;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s left coordinate.
        /// </summary>
        public double MinX => this.BoundingBox.Left;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s right coordinate.
        /// </summary>
        public double MaxX => this.BoundingBox.Right;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s bottom coordinate.
        /// </summary>
        public double MinY => this.BoundingBox.Bottom;

        /// <summary>
        /// The <see cref="TableRectangle"/>'s top coordinate.
        /// </summary>
        public double MaxY => this.BoundingBox.Top;

        internal void SetRect(PdfRectangle rectangle)
        {
            this.BoundingBox = rectangle;
        }

        internal void SetRect(TableRectangle rectangle)
        {
            SetRect(rectangle.BoundingBox);
        }

        private PdfRectangle CreateUnion(PdfRectangle rectangle, PdfRectangle other)
        {
            return Utils.Bounds(new[] { rectangle, other });
        }

        public bool Contains(PdfPoint point)
        {
            return this.BoundingBox.Contains(point, true);
        }

        public bool Contains(TableLine tableLine)
        {
            return this.BoundingBox.Contains(tableLine.BoundingBox, true);
        }

        public bool Contains(TableRectangle other)
        {
            return this.BoundingBox.Contains(other.BoundingBox, true);
        }
        #endregion
    }
}
