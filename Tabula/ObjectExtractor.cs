using System;
using System.Collections.Generic;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    public class ObjectExtractor
    {
        private PdfDocument pdfDocument;

        public ObjectExtractor(PdfDocument pdfDocument)
        {
            this.pdfDocument = pdfDocument;
        }

        public PageArea extractPage(int pageNumber)// throws IOException
        {
            if (pageNumber > this.pdfDocument.NumberOfPages || pageNumber < 1)
            {
                throw new IndexOutOfRangeException("Page number does not exist");
            }

            Page p = this.pdfDocument.GetPage(pageNumber); // - 1);

            var rulings = new List<Ruling>();

            PdfPoint? start_pos = null;
            //PdfPoint? last_move = null;
            PdfPoint? end_pos = null;

            foreach (var path in p.ExperimentalAccess.Paths)
            {
                foreach (var subpath in path)
                {
                    foreach (var command in subpath.Commands)
                    {
                        if (command is PdfSubpath.Line line)
                        {
                            if (line.Length > 0.01)
                            {
                                rulings.Add(new Ruling(line.From, line.To));
                            }
                            end_pos = line.To;
                        }
                        else if (command is PdfSubpath.Move move)
                        {
                            start_pos = move.Location;
                            end_pos = start_pos;
                        }
                        else if (command is PdfSubpath.Close)
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
                            Ruling ruling = new Ruling(end_pos.Value, start_pos.Value);
                            if (ruling.length() > 0.01)
                            {
                                rulings.Add(ruling);
                            }
                        }
                    }
                }
            }

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

            return new PageArea(p.CropBox.Bounds, //.TopLeft.Y, p.CropBox.Bounds.TopLeft.X, w, h,
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
