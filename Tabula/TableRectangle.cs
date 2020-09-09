using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/ebc83ac2bb1a1cbe54ab8081d70f3c9fe81886ea/src/main/java/technology/tabula/Rectangle.java
    public class TableRectangle : IComparable<TableRectangle>
    {
        /// <summary>
        /// Ill-defined comparator, from when Rectangle was Comparable.
        /// @see <a href="https://github.com/tabulapdf/tabula-java/issues/116">PR 116</a>
        /// </summary>
        [Obsolete("with no replacement")]
        public class ILL_DEFINED_ORDER : IComparer<TableRectangle>
        {
            public int Compare([AllowNull] TableRectangle o1, [AllowNull] TableRectangle o2)
            {
                if (o1.Equals(o2)) return 0;
                if (o1.verticalOverlap(o2) > VERTICAL_COMPARISON_THRESHOLD)
                {
                    return o1.isLtrDominant() == -1 && o2.isLtrDominant() == -1
                         ? -o1.getX().CompareTo(o2.getX())
                         : o1.getX().CompareTo(o2.getX());
                }
                else
                {
                    return o1.getBottom().CompareTo(o2.getBottom());
                }
            }
        }

        protected static float VERTICAL_COMPARISON_THRESHOLD = 0.4f;

        public PdfRectangle BoundingBox { get; internal set; }

        public TableRectangle() : base()
        {
            BoundingBox = new PdfRectangle();
        }

        public TableRectangle(PdfRectangle rectangle) : this()
        {
            if (rectangle.Height == 0)
            {
                // hack: force height to be at least 1
                BoundingBox = new PdfRectangle(rectangle.BottomLeft.X, rectangle.BottomLeft.Y,
                                               rectangle.TopRight.X, rectangle.TopRight.Y + 1);
            }
            else
            {
                BoundingBox = rectangle;
            }
        }

        public TableRectangle(double top, double left, double width, double height) : this()
        {
            this.setRect(left, top, width, height);
        }

        public int CompareTo([AllowNull] TableRectangle other)
        {
            return new ILL_DEFINED_ORDER().Compare(this, other);
        }

        // I'm bad at Java and need this for fancy sorting in
        // technology.tabula.TextChunk.
        public virtual int isLtrDominant()
        {
            return 0;
        }

        public double getArea() => BoundingBox.Area;

        public double getWidth() => BoundingBox.Width;

        public double getHeight() => BoundingBox.Height;

        public double verticalOverlap(TableRectangle other)
        {
            return Math.Max(0, Math.Min(this.BoundingBox.Top, other.BoundingBox.Top)
                             - Math.Max(this.BoundingBox.Bottom, other.BoundingBox.Bottom));

            /*
                var overl = Math.Min(this.BoundingBox.Top, other.BoundingBox.Top)
                        - Math.Max(this.BoundingBox.Bottom, other.BoundingBox.Bottom);

                if (overl < 0)
                {
                    return 0;
                }
                else if (overl == 0)
                {
                    return 1; // set min overlap to 1
                }
                return overl;
             */
        }

        public bool verticallyOverlaps(TableRectangle other)
        {
            return verticalOverlap(other) > 0;
        }

        public double horizontalOverlap(TableRectangle other)
        {
            return Math.Max(0, Math.Min(this.getRight(), other.getRight()) - Math.Max(this.getLeft(), other.getLeft()));
        }

        public bool horizontallyOverlaps(TableRectangle other)
        {
            return horizontalOverlap(other) > 0;
        }

        public double verticalOverlapRatio(TableRectangle other)
        {
            double delta = Math.Min(this.BoundingBox.Top - this.BoundingBox.Bottom,
                                                other.BoundingBox.Top - other.BoundingBox.Bottom);
            //if (delta == 0) delta = 1; // set min delta to 1
            var overl = verticalOverlap(other);
            return overl / delta;

            /*
            if (other.getTop() <= this.getTop()
                && this.getTop() <= other.getBottom()
                && other.getBottom() <= this.getBottom())
            {
                rv = (other.getBottom() - this.getTop()) / delta;
            }
            else if (this.getTop() <= other.getTop() 
                && other.getTop() <= this.getBottom()
                && this.getBottom() <= other.getBottom())
            {
                rv = (this.getBottom() - other.getTop()) / delta;
            }
            else if (this.getTop() <= other.getTop() 
                && other.getTop() <= other.getBottom()
                && other.getBottom() <= this.getBottom())
            {
                rv = (other.getBottom() - other.getTop()) / delta;
            }
            else if (other.getTop() <= this.getTop()
                && this.getTop() <= this.getBottom()
                && this.getBottom() <= other.getBottom())
            {
                rv = (this.getBottom() - this.getTop()) / delta;
            }

            return rv;
            */
        }

        public double overlapRatio(TableRectangle other)
        {
            double intersectionWidth = Math.Max(0,
                    Math.Min(this.getRight(), other.getRight()) - Math.Max(this.getLeft(), other.getLeft()));
            double intersectionHeight = Math.Max(0,
                    Math.Min(this.getBottom(), other.getBottom()) - Math.Max(this.getTop(), other.getTop()));
            double intersectionArea = Math.Max(0, intersectionWidth * intersectionHeight);
            double unionArea = this.getArea() + other.getArea() - intersectionArea;

            return intersectionArea / unionArea;
        }

        public TableRectangle merge(TableRectangle other)
        {
            this.setRect(createUnion(this.BoundingBox, other.BoundingBox));
            return this;
        }

        public double getTop() => BoundingBox.Top;

        public void setTop(double top)
        {
            double deltaHeight = top - this.y;
            this.setRect(this.x, top, this.width, this.height - deltaHeight);
        }

        public double getRight() => BoundingBox.Right;

        public void setRight(float right)
        {
            this.setRect(this.x, this.y, right - this.x, this.height);
        }

        public double getLeft() => BoundingBox.Left;

        public void setLeft(float left)
        {
            double deltaWidth = left - this.x;
            this.setRect(left, this.y, this.width - deltaWidth, this.height);
        }

        public double getBottom() => BoundingBox.Bottom;

        public void setBottom(float bottom)
        {
            this.setRect(this.x, this.y, this.width, bottom - this.y);
        }

        public PdfPoint[] getPoints()
        {
            return new PdfPoint[] { new PdfPoint(this.getLeft(), this.getTop()),
                new PdfPoint(this.getRight(), this.getTop()), new PdfPoint(this.getRight(), this.getBottom()),
                new PdfPoint(this.getLeft(), this.getBottom()) };
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.GetType());
            string s = this.BoundingBox.ToString();
            sb.Append(s, 0, s.Length - 1);
            sb.Append($",bottom={this.getBottom():0.00},right={this.getRight():0.00}]");
            return sb.ToString();
        }

        public static TableRectangle boundingBoxOf(IEnumerable<TableRectangle> rectangles)
        {
            double minx = double.MaxValue;
            double miny = double.MaxValue;
            double maxx = double.MinValue;
            double maxy = double.MinValue;

            foreach (TableRectangle r in rectangles)
            {
                minx = Math.Min(r.getMinX(), minx);
                miny = Math.Min(r.getMinY(), miny);
                maxx = Math.Max(r.getMaxX(), maxx);
                maxy = Math.Max(r.getMaxY(), maxy);
            }
            return new TableRectangle(miny, minx, maxx - minx, maxy - miny);
        }

        public bool intersects(TableRectangle tableRectangle)
        {
            throw new NotImplementedException();
        }

        public bool intersectsLine(Ruling ruling)
        {
            throw new NotImplementedException();
        }

        #region helpers
#pragma warning disable IDE1006 // Naming Styles
        public double x => this.BoundingBox.TopLeft.X;

        public double y => this.BoundingBox.TopLeft.Y;

        public double width => this.BoundingBox.Width;

        public double height => this.BoundingBox.Height;

        private double getX() => this.x;

        private double getY() => this.y;
        private double getMinX() => this.BoundingBox.Left;

        private double getMaxX() => this.BoundingBox.Right;

        private double getMinY() => this.BoundingBox.Bottom;

        private double getMaxY() => this.BoundingBox.Top;

        /// <summary>
        /// Sets the location and size of this Rectangle2D to the specified double values.
        /// </summary>
        /// <param name="x">the X coordinate of the upper-left corner of this Rectangle2D</param>
        /// <param name="y">the Y coordinate of the upper-left corner of this Rectangle2D</param>
        /// <param name="w">the width of this Rectangle2D</param>
        /// <param name="h">the height of this Rectangle2D</param>
        internal void setRect(double x, double y, double w, double h)
        {
            setRect(new PdfRectangle(x, y - h, x + w, y));
            throw new ArgumentOutOfRangeException();
        }

        internal void setRect(PdfRectangle rectangle)
        {
            this.BoundingBox = rectangle;
        }

        internal void setRect(TableRectangle rectangle)
        {
            this.BoundingBox = rectangle.BoundingBox;
        }

        private PdfRectangle createUnion(PdfRectangle rectangle, PdfRectangle other)
        {
            double left = Math.Min(rectangle.Left, other.Left);
            double right = Math.Max(rectangle.Right, other.Right);
            double bottom = Math.Min(rectangle.Bottom, other.Bottom);
            double top = Math.Max(rectangle.Top, other.Top);
            return new PdfRectangle(left, bottom, right, top);
        }
#pragma warning restore IDE1006 // Naming Styles
        #endregion
    }
}
