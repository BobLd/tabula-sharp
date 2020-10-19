using System;
using System.Collections.Generic;
using System.Drawing;
using Tabula.Drawing;
using UglyToad.PdfPig.Core;

namespace Tabula.Detectors
{
    /**
     * Created by matt on 2015-12-17.
     * <p>
     * Attempt at an implementation of the table finding algorithm described by
     * Anssi Nurminen's master's thesis:
     * http://dspace.cc.tut.fi/dpub/bitstream/handle/123456789/21520/Nurminen.pdf?sequence=3
     */

    // ported from https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/detectors/NurminenDetectionAlgorithm.java
    /// <summary>
    /// Nurminen table detection algorithm.
    /// </summary>
    public class NurminenDetectionAlgorithm : SimpleNurminenDetectionAlgorithm
    {
        private static int GRAYSCALE_INTENSITY_THRESHOLD = 25;

        /// <summary>
        /// Nurminen table detection algorithm.
        /// </summary>
        public NurminenDetectionAlgorithm()
            : base()
        {

        }

        public override List<TableRectangle> Detect(PageArea page)
        {
            // get horizontal & vertical lines
            // we get these from an image of the PDF and not the PDF itself because sometimes there are invisible PDF
            // instructions that are interpreted incorrectly as visible elements - we really want to capture what a
            // person sees when they look at the PDF

            List<Ruling> horizontalRulings = new List<Ruling>();
            List<Ruling> verticalRulings = new List<Ruling>();

            using (Bitmap imagePage = null) // TODO: render the page
            {
                try
                {
                    // Render GRAY levels
                    using (Bitmap image = UtilsDrawing.ToGrayscale(imagePage))
                    {
                        horizontalRulings = this.getHorizontalRulings(image);
                    }
                }
                catch (Exception e)
                {

                    throw;
                }

                // now check the page for vertical lines, but remove the text first to make things less confusing
                try
                {
                    // Render GRAY levels without letters
                    using (Bitmap image = UtilsDrawing.ToGrayscale(imagePage))
                    {
                        verticalRulings = this.getVerticalRulings(image);
                    }
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {

                }
            }

            return Process(page, horizontalRulings, verticalRulings);
        }

        /// <summary>
        /// Get all horizontal edges, which we'll define as a change in grayscale colour
        /// along a straight line of a certain length.
        /// </summary>
        /// <param name="image">Image in GRAY</param>
        private List<Ruling> getHorizontalRulings(Bitmap image)
        {
            // get all horizontal edges, which we'll define as a change in grayscale colour
            // along a straight line of a certain length
            List<Ruling> horizontalRulings = new List<Ruling>();

            int width = image.Width;
            int height = image.Height;

            for (int x = 0; x < width; x++)
            {
                Color lastPixel = image.GetPixel(x, 0);
                for (int y = 1; y < height - 1; y++)
                {
                    Color currPixel = image.GetPixel(x, y);

                    int diff = Math.Abs(currPixel.R - lastPixel.R);
                    if (diff > GRAYSCALE_INTENSITY_THRESHOLD)
                    {
                        // we hit what could be a line
                        // don't bother scanning it if we've hit a pixel in the line before
                        bool alreadyChecked = false;
                        foreach (var line in horizontalRulings)
                        {
                            if (y == line.Y1 && x >= line.X1 && x <= line.X2)
                            {
                                alreadyChecked = true;
                                break;
                            }
                        }

                        if (alreadyChecked)
                        {
                            lastPixel = currPixel;
                            continue;
                        }

                        int lineX = x + 1;

                        while (lineX < width)
                        {
                            Color linePixel = image.GetPixel(lineX, y);
                            Color abovePixel = image.GetPixel(lineX, y - 1);

                            if (Math.Abs(linePixel.R - abovePixel.R) <= GRAYSCALE_INTENSITY_THRESHOLD
                             || Math.Abs(currPixel.R - linePixel.R) > GRAYSCALE_INTENSITY_THRESHOLD)
                            {
                                break;
                            }

                            lineX++;
                        }

                        int endX = lineX - 1;
                        int lineWidth = endX - x;
                        if (lineWidth > HORIZONTAL_EDGE_WIDTH_MINIMUM)
                        {
                            horizontalRulings.Add(new Ruling(new PdfPoint(x, y), new PdfPoint(endX, y)));
                        }
                    }

                    lastPixel = currPixel;
                }
            }

            return horizontalRulings;
        }

        /// <summary>
        /// Get all vertical edges, which we'll define as a change in grayscale colour
        /// along a straight line of a certain length.
        /// </summary>
        /// <param name="image">Image in GRAY</param>
        private List<Ruling> getVerticalRulings(Bitmap image)
        {
            // get all vertical edges, which we'll define as a change in grayscale colour
            // along a straight line of a certain length
            List<Ruling> verticalRulings = new List<Ruling>();

            int width = image.Width;
            int height = image.Height;

            for (int y = 0; y < height; y++)
            {
                Color lastPixel = image.GetPixel(0, y);

                for (int x = 1; x < width - 1; x++)
                {
                    Color currPixel = image.GetPixel(x, y);

                    int diff = Math.Abs(currPixel.R - lastPixel.R);
                    if (diff > GRAYSCALE_INTENSITY_THRESHOLD)
                    {
                        // we hit what could be a line
                        // don't bother scanning it if we've hit a pixel in the line before
                        bool alreadyChecked = false;
                        foreach (var line in verticalRulings)
                        {
                            if (x == line.X1 && y >= line.Y1 && y <= line.Y2)
                            {
                                alreadyChecked = true;
                                break;
                            }
                        }

                        if (alreadyChecked)
                        {
                            lastPixel = currPixel;
                            continue;
                        }

                        int lineY = y + 1;

                        while (lineY < height)
                        {
                            Color linePixel = image.GetPixel(x, lineY);
                            Color leftPixel = image.GetPixel(x - 1, lineY);

                            if (Math.Abs(linePixel.R - leftPixel.R) <= GRAYSCALE_INTENSITY_THRESHOLD
                             || Math.Abs(currPixel.R - linePixel.R) > GRAYSCALE_INTENSITY_THRESHOLD)
                            {
                                break;
                            }

                            lineY++;
                        }

                        int endY = lineY - 1;
                        int lineLength = endY - y;
                        if (lineLength > VERTICAL_EDGE_HEIGHT_MINIMUM)
                        {
                            verticalRulings.Add(new Ruling(new PdfPoint(x, y), new PdfPoint(x, endY)));
                        }
                    }

                    lastPixel = currPixel;
                }
            }

            return verticalRulings;
        }
    }
}
