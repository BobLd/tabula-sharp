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
    public class ObjectExtractor
    {
        private const int rounding = 6;

        private PdfDocument pdfDocument;

        private const float RULING_MINIMUM_LENGTH = 0.01f;

        /// <summary>
        /// Create a Tabula object extractor.
        /// </summary>
        /// <param name="pdfDocument"></param>
        public ObjectExtractor(PdfDocument pdfDocument)
        {
            this.pdfDocument = pdfDocument;
        }

        private class PointComparer : IComparer<PdfPoint>
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

        private PdfPoint RoundPdfPoint(PdfPoint pdfPoint, int decimalPlace)
        {
            return new PdfPoint(Utils.Round(pdfPoint.X, decimalPlace), Utils.Round(pdfPoint.Y, decimalPlace));
        }

        /// <summary>
        /// Extract the <see cref="PageArea"/>, with its text elements (letters) and rulings (processed PdfPath and PdfSubpath).
        /// </summary>
        /// <param name="pageNumber">The page number to extract.</param>
        public PageArea ExtractPage(int pageNumber)
        {
            if (pageNumber > this.pdfDocument.NumberOfPages || pageNumber < 1)
            {
                throw new IndexOutOfRangeException("Page number does not exist");
            }

            Page p = this.pdfDocument.GetPage(pageNumber);

            /**************** ObjectExtractorStreamEngine(PDPage page)*******************/
            // Replaces:
            // ObjectExtractorStreamEngine se = new ObjectExtractorStreamEngine(p);
            // se.processPage(p);
            var rulings = new List<Ruling>();

            foreach (var path in p.ExperimentalAccess.Paths)
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
                    PointComparer pc = new PointComparer();

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
                            Ruling r = new Ruling(line.Point1, line.Point2); //.intersect(this.currentClippingPath());
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

            TextStripper pdfTextStripper = new TextStripper(this.pdfDocument, pageNumber);
            pdfTextStripper.Process();
            Utils.Sort(pdfTextStripper.textElements, new TableRectangle.ILL_DEFINED_ORDER());

            return new PageArea(p.CropBox.Bounds,
                p.Rotation.Value,
                pageNumber,
                p,
                this.pdfDocument,
                pdfTextStripper.textElements,
                rulings,
                pdfTextStripper.minCharWidth,
                pdfTextStripper.minCharHeight,
                pdfTextStripper.spatialIndex);
        }

        /// <summary>
        /// Enumerate and extract over the given pages.
        /// </summary>
        /// <param name="pages"></param>
        public PageIterator Extract(IEnumerable<int> pages)
        {
            return new PageIterator(this, pages);
        }

        /// <summary>
        /// Enumerate and extract over all the pages.
        /// </summary>
        public PageIterator Extract()
        {
            return Extract(Utils.Range(1, this.pdfDocument.NumberOfPages + 1));
        }

        /// <summary>
        /// Extract the <see cref="PageArea"/>, with its text elements (letters) and rulings (processed PdfPath and PdfSubpath).
        /// </summary>
        /// <param name="pageNumber">The page number to extract.</param>
        public PageArea Extract(int pageNumber)
        {
            return Extract(Utils.Range(pageNumber, pageNumber + 1)).Next();
        }

        /// <summary>
        /// Close the ObjectExtractor.
        /// </summary>
        public void Close()
        {
            this.pdfDocument.Dispose();
        }
    }
}
