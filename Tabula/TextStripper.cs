using System;
using System.Collections.Generic;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Util;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/TextStripper.java
    public class TextStripper
    {
        private static readonly string NBSP = "\u00A0";
        private static float AVG_HEIGHT_MULT_THRESHOLD = 6.0f;

        public List<TextElement> textElements;
        public double minCharWidth = double.MaxValue;
        public double minCharHeight = double.MaxValue;
        public RectangleSpatialIndex<TextElement> spatialIndex;
        public int pageNumber;

        private PdfDocument document;
        private double totalHeight;
        private int countHeight;

        /// <summary>
        /// Create a TextStripper for the given page.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber"></param>
        public TextStripper(PdfDocument document, int pageNumber)
        {
            this.document = document;
            this.pageNumber = pageNumber;
        }

        /// <summary>
        /// Process the page.
        /// </summary>
        public void Process()
        {
            var page = document.GetPage(pageNumber);
            textElements = new List<TextElement>();
            spatialIndex = new RectangleSpatialIndex<TextElement>();

            foreach (var letter in page.Letters)
            {
                string c = letter.Value;

                // if c not printable, return
                if (!IsPrintable(c)) continue;

                if (c.Equals(NBSP))
                {
                    c = " "; // replace non-breaking space for space
                }

                double wos = GetExpectedWhitespaceSize(letter); //textPosition.getWidthOfSpace();

                TextElement te = new TextElement(GetBbox(letter), letter.Font, letter.PointSize, c, wos, letter.GlyphRectangle.Rotation)
                {
                    letter = letter
                };

                if (!string.IsNullOrWhiteSpace(c)) this.minCharWidth = Math.Min(this.minCharWidth, te.Width);
                if (!string.IsNullOrWhiteSpace(c)) this.minCharHeight = Math.Min(this.minCharHeight, Math.Max(te.Height, 1)); // added by bobld: min height value to 1

                countHeight++;
                totalHeight += Math.Max(te.Height, 1); // added by bobld: min height value to 1
                double avgHeight = totalHeight / countHeight;

                if (avgHeight > 0 && te.Height >= (avgHeight * AVG_HEIGHT_MULT_THRESHOLD) && (te.GetText()?.Trim().Equals("") != false))
                {
                    continue;
                }

                textElements.Add(te);
                spatialIndex.Add(te);
            }
        }

        private bool IsPrintable(string s)
        {
            char c;
            bool printable = false;
            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                printable |= !char.IsControl(c) && !IsSpecial(c); //!Character.isISOControl(c) && block != null && block != Character.UnicodeBlock.SPECIALS;
            }
            return printable;
        }

        private bool IsSpecial(char c)
        {
#if NETCOREAPP3_1
            return c >= System.Text.Unicode.UnicodeRanges.Specials.FirstCodePoint && c < (System.Text.Unicode.UnicodeRanges.Specials.FirstCodePoint + System.Text.Unicode.UnicodeRanges.Specials.Length);
#else
            return c >= '\uFFF0' && c <= '\uFFFF';
#endif
        }

        private static double GetExpectedWhitespaceSize(Letter letter)
        {
            if (letter.Value == " ")
            {
                return letter.Width;
            }
            return WhitespaceSizeStatistics.GetExpectedWhitespaceSize(letter);
        }

        private static PdfRectangle GetBbox(Letter letter)
        {
            switch (letter.TextOrientation)
            {
                case TextOrientation.Horizontal:
                    double add = 0;
                    if (letter.GlyphRectangle.Height == 0)
                    {
                        add = 1; // force minimum height to be 1
                    }
                    else if (letter.Value == " ")
                    {
                        add = -(letter.GlyphRectangle.Height - 1); // force height of space to be 1
                    }
                    return new PdfRectangle(Utils.Round(letter.StartBaseLine.X, 2),
                                            Utils.Round(letter.StartBaseLine.Y, 2),
                                            Utils.Round(letter.StartBaseLine.X + Math.Max(letter.Width, letter.GlyphRectangle.Width), 2),
                                            Utils.Round(letter.GlyphRectangle.TopLeft.Y + add, 2));

                case TextOrientation.Rotate180:
                    // need to force min height = 1 and height of space to be 1
                    return new PdfRectangle(Utils.Round(letter.StartBaseLine.X, 2),
                                            Utils.Round(letter.StartBaseLine.Y, 2),
                                            Utils.Round(letter.StartBaseLine.X - Math.Max(letter.Width, letter.GlyphRectangle.Width), 2),
                                            Utils.Round(letter.GlyphRectangle.TopRight.Y, 2));

                case TextOrientation.Rotate90:
                    // need to force min height = 1 and height of space to be 1
                    return new PdfRectangle(new PdfPoint(Utils.Round(letter.StartBaseLine.X + letter.GlyphRectangle.Height, 2), Utils.Round(letter.GlyphRectangle.BottomLeft.Y, 2)),
                                            new PdfPoint(Utils.Round(letter.StartBaseLine.X + letter.GlyphRectangle.Height, 2), Utils.Round(letter.EndBaseLine.Y, 2)),
                                            new PdfPoint(Utils.Round(letter.StartBaseLine.X, 2), Utils.Round(letter.GlyphRectangle.BottomLeft.Y, 2)),
                                            new PdfPoint(Utils.Round(letter.StartBaseLine.X, 2), Utils.Round(letter.EndBaseLine.Y, 2)));

                case TextOrientation.Rotate270:
                    // need to force min height = 1 and height of space to be 1
                    return new PdfRectangle(new PdfPoint(Utils.Round(letter.StartBaseLine.X - letter.GlyphRectangle.Height, 2), Utils.Round(letter.StartBaseLine.Y, 2)),
                                            new PdfPoint(Utils.Round(letter.StartBaseLine.X - letter.GlyphRectangle.Height, 2), Utils.Round(letter.GlyphRectangle.BottomRight.Y, 2)),
                                            new PdfPoint(Utils.Round(letter.StartBaseLine.X, 2), Utils.Round(letter.StartBaseLine.Y, 2)),
                                            new PdfPoint(Utils.Round(letter.StartBaseLine.X, 2), Utils.Round(letter.GlyphRectangle.BottomRight.Y, 2)));

                case TextOrientation.Other:
                default:
                    return GetBoundingBoxOther(letter);
            }
        }

        // NEED TO IMPLEMENT ROUNDING
        private static PdfRectangle GetBoundingBoxOther(Letter letter)
        {
            // not very useful, need axis aligned bbox anyway
            // -> rotate back? or normalise?
            var points = new[]
            {
                letter.StartBaseLine,
                letter.EndBaseLine,
                letter.GlyphRectangle.TopLeft,
                letter.GlyphRectangle.TopRight
            };

            // Candidates bounding boxes
            var obb = GeometryExtensions.MinimumAreaRectangle(points);
            var obb1 = new PdfRectangle(obb.BottomLeft, obb.TopLeft, obb.BottomRight, obb.TopRight);
            var obb2 = new PdfRectangle(obb.BottomRight, obb.BottomLeft, obb.TopRight, obb.TopLeft);
            var obb3 = new PdfRectangle(obb.TopRight, obb.BottomRight, obb.TopLeft, obb.BottomLeft);

            // Find the orientation of the OBB, using the baseline angle
            // Assumes line order is correct

            var baseLineAngle = Distances.BoundAngle180(Distances.Angle(letter.GlyphRectangle.BottomLeft, letter.GlyphRectangle.BottomRight));

            double deltaAngle = Math.Abs(Distances.BoundAngle180(obb.Rotation - baseLineAngle));
            double deltaAngle1 = Math.Abs(Distances.BoundAngle180(obb1.Rotation - baseLineAngle));
            if (deltaAngle1 < deltaAngle)
            {
                deltaAngle = deltaAngle1;
                obb = obb1;
            }

            double deltaAngle2 = Math.Abs(Distances.BoundAngle180(obb2.Rotation - baseLineAngle));
            if (deltaAngle2 < deltaAngle)
            {
                deltaAngle = deltaAngle2;
                obb = obb2;
            }

            double deltaAngle3 = Math.Abs(Distances.BoundAngle180(obb3.Rotation - baseLineAngle));
            if (deltaAngle3 < deltaAngle)
            {
                obb = obb3;
            }

            return obb;
        }
    }
}
