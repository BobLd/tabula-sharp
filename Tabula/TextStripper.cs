using System;
using System.Collections.Generic;
using System.Text.Unicode;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Util;

namespace Tabula
{
    public class TextStripper
    {
        private static string NBSP = "\u00A0";
        private static float AVG_HEIGHT_MULT_THRESHOLD = 6.0f;
        public List<TextElement> textElements;
        public double minCharWidth = double.MaxValue;
        public double minCharHeight = double.MaxValue;
        public RectangleSpatialIndex<TextElement> spatialIndex;
        public int pageNumber;
        private PdfDocument document;
        public double totalHeight;
        public int countHeight;

        public TextStripper(PdfDocument document, int pageNumber)
        {
            this.document = document;
            this.pageNumber = pageNumber;
        }

        public void process()
        {
            var page = document.GetPage(pageNumber);
            textElements = new List<TextElement>();
            spatialIndex = new RectangleSpatialIndex<TextElement>();

            foreach (var letter in page.Letters)
            {
                String c = letter.Value; //textPosition.getUnicode();

                // if c not printable, return
                if (!isPrintable(c)) continue;

                if (c.Equals(NBSP))
                {
                    c = " "; // replace non-breaking space for space
                }

                double wos = GetExpectedWhitespaceSize(letter); //textPosition.getWidthOfSpace();

                TextElement te = new TextElement(GetBbox(letter), letter.Font, letter.PointSize, c, wos, letter.GlyphRectangle.Rotation); // Rotation->The direction of the text(0, 90, 180, or 270)
                te.letter = letter;

                this.minCharWidth = (float)Math.Min(this.minCharWidth, te.width);
                this.minCharHeight = (float)Math.Min(this.minCharHeight, te.height);

                countHeight++;
                totalHeight += te.height;
                double avgHeight = totalHeight / countHeight;

                if (avgHeight > 0 && te.height >= (avgHeight * AVG_HEIGHT_MULT_THRESHOLD) && (te.getText()?.Trim().Equals("") != false))
                {
                    continue;
                }

                textElements.Add(te);
                spatialIndex.add(te);
            }
        }

        private bool isPrintable(string s)
        {
            char c;
            bool printable = false;
            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                bool isSpecial = c >= UnicodeRanges.Specials.FirstCodePoint && c <= (UnicodeRanges.Specials.FirstCodePoint + UnicodeRanges.Specials.Length); // really not sure!!

                printable |= !char.IsControl(c) && !isSpecial; //!Character.isISOControl(c) && block != null && block != Character.UnicodeBlock.SPECIALS;
            }
            return printable;
        }

        static double GetExpectedWhitespaceSize(Letter letter)
        {
            if (letter.Value == " ")
            {
                return letter.Width;
            }
            return WhitespaceSizeStatistics.GetExpectedWhitespaceSize(letter);
        }

        static PdfRectangle GetBbox(Letter letter)
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
                    return new PdfRectangle(Utils.round(letter.StartBaseLine.X, 2),
                                            Utils.round(letter.StartBaseLine.Y, 2),
                                            Utils.round(letter.StartBaseLine.X + Math.Max(letter.Width, letter.GlyphRectangle.Width), 2),
                                            Utils.round(letter.GlyphRectangle.TopLeft.Y + add, 2));

                case TextOrientation.Rotate180:
                    return new PdfRectangle(Utils.round(letter.StartBaseLine.X, 2),
                                            Utils.round(letter.StartBaseLine.Y, 2),
                                            Utils.round(letter.StartBaseLine.X - Math.Max(letter.Width, letter.GlyphRectangle.Width), 2),
                                            Utils.round(letter.GlyphRectangle.TopRight.Y, 2));

                case TextOrientation.Rotate90:
                    return new PdfRectangle(new PdfPoint(Utils.round(letter.StartBaseLine.X + letter.GlyphRectangle.Height, 2), Utils.round(letter.GlyphRectangle.BottomLeft.Y, 2)),
                                            new PdfPoint(Utils.round(letter.StartBaseLine.X + letter.GlyphRectangle.Height, 2), Utils.round(letter.EndBaseLine.Y, 2)),
                                            new PdfPoint(Utils.round(letter.StartBaseLine.X, 2), Utils.round(letter.GlyphRectangle.BottomLeft.Y, 2)),
                                            new PdfPoint(Utils.round(letter.StartBaseLine.X, 2), Utils.round(letter.EndBaseLine.Y, 2)));

                case TextOrientation.Rotate270:
                    return new PdfRectangle(new PdfPoint(Utils.round(letter.StartBaseLine.X - letter.GlyphRectangle.Height, 2), Utils.round(letter.StartBaseLine.Y, 2)),
                                            new PdfPoint(Utils.round(letter.StartBaseLine.X - letter.GlyphRectangle.Height, 2), Utils.round(letter.GlyphRectangle.BottomRight.Y, 2)),
                                            new PdfPoint(Utils.round(letter.StartBaseLine.X, 2), Utils.round(letter.StartBaseLine.Y, 2)),
                                            new PdfPoint(Utils.round(letter.StartBaseLine.X, 2), Utils.round(letter.GlyphRectangle.BottomRight.Y, 2)));

                case TextOrientation.Other:
                default:
                    return GetBoundingBoxOther(letter);
            }
        }

        // NEED TO IMPLEMENT ROUNDING
        private static PdfRectangle GetBoundingBoxOther(Letter letter)
        {
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
