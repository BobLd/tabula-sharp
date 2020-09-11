using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace Tabula
{
    public class ObjectExtractor
    {
        private PdfDocument pdfDocument;

        public ObjectExtractor(PdfDocument pdfDocument)
        {
            this.pdfDocument = pdfDocument;
        }

        private class PointComparator : IComparer<PdfPoint>
        {
            public int Compare([AllowNull] PdfPoint o1, [AllowNull] PdfPoint o2)
            {
                double o1X = Utils.round(o1.X, 2);
                double o1Y = Utils.round(o1.Y, 2);
                double o2X = Utils.round(o2.X, 2);
                double o2Y = Utils.round(o2.Y, 2);

                if (o1Y > o2Y)
                    return 1;
                if (o1Y < o2Y)
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
            return new PdfPoint(Utils.round(pdfPoint.X, decimalPlace), Utils.round(pdfPoint.Y, decimalPlace));
        }

        public PageArea extractPage(int pageNumber)// throws IOException
        {
            if (pageNumber > this.pdfDocument.NumberOfPages || pageNumber < 1)
            {
                throw new IndexOutOfRangeException("Page number does not exist");
            }

            Page p = this.pdfDocument.GetPage(pageNumber);
            //ObjectExtractorStreamEngine se = new ObjectExtractorStreamEngine(p);
            //se.processPage(p);

            /**************** ObjectExtractorStreamEngine(PDPage page)*******************/
            // https://github.com/tabulapdf/tabula-java/blob/ebc83ac2bb1a1cbe54ab8081d70f3c9fe81886ea/src/main/java/technology/tabula/ObjectExtractorStreamEngine.java#L138
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
                        continue;
                    }

                    // TODO: how to implement color filter?

                    PdfPoint? start_pos = RoundPdfPoint(first.Location, 2); //new PdfPoint(Utils.round(first.Location.X, 2), Utils.round(first.Location.Y, 2));
                    PdfPoint? last_move = start_pos;
                    PdfPoint? end_pos = null;
                    PdfLine line;
                    PointComparator pc = new PointComparator();

                    foreach (var command in subpath.Commands) //while (!pi.isDone())
                    {
                        if (command is Line linePath) // case PathIterator.SEG_LINETO:
                        {
                            end_pos = RoundPdfPoint(linePath.To, 2); // linePath.To // round it?
                            if (!start_pos.HasValue || !end_pos.HasValue)
                            {
                                break;
                            }

                            line = pc.Compare(start_pos.Value, end_pos.Value) == -1 ? new PdfLine(start_pos.Value, end_pos.Value) : new PdfLine(end_pos.Value, start_pos.Value);

                            // already clipped
                            Ruling r = new Ruling(line.Point1, line.Point2);
                            if (r.length() > 0.01)
                            {
                                rulings.Add(r);
                            }
                        }
                        // to finish
                        else if (command is Move move) // case PathIterator.SEG_MOVETO:
                        {
                            start_pos = RoundPdfPoint(move.Location, 2); // move.Location; // round it?
                            end_pos = start_pos;
                        }
                        else if (command is Close) // case PathIterator.SEG_CLOSE:
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
                            if (r.length() > 0.01)
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
            pdfTextStripper.process();
            Utils.sort(pdfTextStripper.textElements, new TableRectangle.ILL_DEFINED_ORDER());

            /*
             double w, h;
             int pageRotation = p.Rotation.Value; //.getRotation();
             if (Math.Abs(pageRotation) == 90 || Math.Abs(pageRotation) == 270)
             {
                 w = p.CropBox.Bounds.Height; //.getHeight();
                 h = p.CropBox.Bounds.Width;  //.getCropBox().getWidth();
             }
             else
             {
                 w = p.CropBox.Bounds.Width;  // .getCropBox().getWidth();
                 h = p.CropBox.Bounds.Height; //.getCropBox().getHeight();
             }
             */

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

        public PageIterator extract(IEnumerable<int> pages)
        {
            return new PageIterator(this, pages);
        }

        public PageIterator extract()
        {
            return extract(Utils.range(1, this.pdfDocument.NumberOfPages + 1));
        }

        public PageArea extract(int pageNumber)
        {
            return extract(Utils.range(pageNumber, pageNumber + 1)).next();
        }

        public void close()
        {
            this.pdfDocument.Dispose(); // .close();
        }
    }
}
