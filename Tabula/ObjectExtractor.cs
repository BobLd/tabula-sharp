using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/ObjectExtractorStreamEngine.java
    // and tabula-java/blob/master/src/main/java/technology/tabula/ObjectExtractor.java

    /// <summary>
    /// Tabula object extractor.
    /// </summary>
    public static class ObjectExtractor
    {
        private const int rounding = 6;

        private const float RULING_MINIMUM_LENGTH = 0.01f;

        private sealed class PointComparer : IComparer<PdfPoint>
        {
            public int Compare(PdfPoint o1, PdfPoint o2)
            {
                double o1X = Utils.Round(o1.X, 2);
                double o1Y = Utils.Round(o1.Y, 2);
                double o2X = Utils.Round(o2.X, 2);
                double o2Y = Utils.Round(o2.Y, 2);

                if (o1Y > o2Y) // bobld: do not inverse - makes tests fais 
                    return 1;
                if (o1Y < o2Y) // bobld: do not inverse - makes tests fais 
                    return -1;
                if (o1X > o2X)
                    return 1;
                if (o1X < o2X)
                    return -1;
                return 0;
            }
        }

        private static PdfPoint RoundPdfPoint(PdfPoint pdfPoint, int decimalPlace)
        {
            return new PdfPoint(Utils.Round(pdfPoint.X, decimalPlace), Utils.Round(pdfPoint.Y, decimalPlace));
        }

        /// <summary>
        /// Extract the <see cref="PageArea"/>, with its text elements (letters) and rulings (processed PdfPath and PdfSubpath).
        /// </summary>
        /// <param name="pdfDocument">The pdf document/param>
        /// <param name="pageNumber">The page number to extract.</param>
        public static PageArea ExtractPage(PdfDocument pdfDocument, int pageNumber)
        {
            if (pageNumber > pdfDocument.NumberOfPages || pageNumber < 1)
            {
                throw new IndexOutOfRangeException("Page number does not exist");
            }

            Page p = pdfDocument.GetPage(pageNumber);
            return ExtractPage(p);
        }

        /// <summary>
        /// Extract the <see cref="PageArea"/>, with its text elements (letters) and rulings (processed PdfPath and PdfSubpath).
        /// </summary>
        public static PageArea ExtractPage(Page page)
        {
            PointComparer pc = new PointComparer();

            /**************** ObjectExtractorStreamEngine(PDPage page)*******************/
            // Replaces:
            // ObjectExtractorStreamEngine se = new ObjectExtractorStreamEngine(p);
            // se.processPage(p);
            var rulings = new List<Ruling>();

            foreach (var path in page.ExperimentalAccess.Paths)
            {
                if (!path.IsFilled && !path.IsStroked) continue; // strokeOrFillPath operator => filter stroke and filled

                foreach (var subpath in path)
                {
                    if (!(subpath.Commands[0] is Move first))
                    {
                        // skip paths whose first operation is not a MOVETO
                        continue;
                    }

                    if (subpath.Commands.Any(c => c is BezierCurve))
                    {
                        // or contains operations other than LINETO, MOVETO or CLOSE
                        // bobld: skip at subpath or path level?
                        continue;
                    }

                    // TODO: how to implement color filter?

                    PdfPoint? start_pos = RoundPdfPoint(first.Location, rounding);
                    PdfPoint? last_move = start_pos;
                    PdfPoint? end_pos = null;
                    PdfLine line;

                    foreach (var command in subpath.Commands)
                    {
                        if (command is Line linePath)
                        {
                            end_pos = RoundPdfPoint(linePath.To, rounding);
                            if (!start_pos.HasValue || !end_pos.HasValue)
                            {
                                break;
                            }

                            line = pc.Compare(start_pos.Value, end_pos.Value) == -1 ? new PdfLine(start_pos.Value, end_pos.Value) : new PdfLine(end_pos.Value, start_pos.Value);

                            // already clipped
                            Ruling r = new Ruling(line.Point1, line.Point2);
                            if (r.Length > RULING_MINIMUM_LENGTH)
                            {
                                rulings.Add(r);
                            }
                        }
                        else if (command is Move move)
                        {
                            start_pos = RoundPdfPoint(move.Location, rounding);
                            end_pos = start_pos;
                        }
                        else if (command is Close)
                        {
                            // according to PathIterator docs:
                            // "the preceding subpath should be closed by appending a line
                            // segment
                            // back to the point corresponding to the most recent
                            // SEG_MOVETO."
                            if (!start_pos.HasValue || !end_pos.HasValue)
                            {
                                break;
                            }

                            line = pc.Compare(end_pos.Value, last_move.Value) == -1 ? new PdfLine(end_pos.Value, last_move.Value) : new PdfLine(last_move.Value, end_pos.Value);

                            // already clipped
                            Ruling r = new Ruling(line.Point1, line.Point2);
                            if (r.Length > 0.01)
                            {
                                rulings.Add(r);
                            }
                        }
                        start_pos = end_pos;
                    }
                }
            }
            /****************************************************************************/

            TextStripperResult textStripperResult = TextStripper.Process(page);
            Utils.Sort(textStripperResult.TextElements, new TableRectangle.ILL_DEFINED_ORDER());

            return new PageArea(page.CropBox.Bounds,
                page.Rotation.Value,
                page.Number,
                page,
                textStripperResult.TextElements,
                rulings,
                textStripperResult.MinCharWidth,
                textStripperResult.MinCharHeight,
                textStripperResult.SpatialIndex);
        }

        /// <summary>
        /// Enumerate and extract over the given pages.
        /// </summary>
        /// <param name="pages"></param>
        public static PageIterator Extract(PdfDocument pdfDocument, IEnumerable<int> pages)
        {
            return new PageIterator(pdfDocument, pages);
        }

        /// <summary>
        /// Enumerate and extract over all the pages.
        /// </summary>
        public static PageIterator Extract(PdfDocument pdfDocument)
        {
            return Extract(pdfDocument, Utils.Range(1, pdfDocument.NumberOfPages + 1));
        }

        /// <summary>
        /// Extract the <see cref="PageArea"/>, with its text elements (letters) and rulings (processed PdfPath and PdfSubpath).
        /// </summary>
        /// <param name="pageNumber">The page number to extract.</param>
        public static PageArea Extract(PdfDocument pdfDocument, int pageNumber)
        {
            return Extract(pdfDocument, Utils.Range(pageNumber, pageNumber + 1)).Next();
        }
    }
}
