using System.Collections.Generic;
using System.Linq;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    public static class PdfPigExtensions
    {
        public static IEnumerable<Table> GetTablesStream(this Page pdfPage)
        {
            return GetTables(pdfPage, new SimpleNurminenDetectionAlgorithm(), new BasicExtractionAlgorithm());
        }

        public static IEnumerable<Table> GetTablesLattice(this Page pdfPage)
        {
            return GetTables(pdfPage, new SpreadsheetExtractionAlgorithm());
        }

        public static IEnumerable<Table> GetTables(this Page pdfPage, IExtractionAlgorithm extractionAlgorithm)
        {
            var page = ObjectExtractor.ExtractPage(pdfPage);
            return extractionAlgorithm.Extract(page);
        }

        public static IEnumerable<Table> GetTables(this Page pdfPage,
            IDetectionAlgorithm detectionAlgorithm, IExtractionAlgorithm extractionAlgorithm)
        {
            var page = ObjectExtractor.ExtractPage(pdfPage);

            // detect canditate table zones
            var canditateTableZones = detectionAlgorithm.Detect(page).Select(z => z.BoundingBox);

            return GetTables(pdfPage, canditateTableZones, extractionAlgorithm);
        }

        public static IEnumerable<Table> GetTables(this Page pdfPage,
            IEnumerable<PdfRectangle> canditateTableZones, IExtractionAlgorithm extractionAlgorithm)
        {
            var page = ObjectExtractor.ExtractPage(pdfPage);

            foreach (var region in canditateTableZones)
            {
                foreach (Table table in extractionAlgorithm.Extract(page.GetArea(region)))
                {
                    yield return table;
                }
            }
        }

        public static IEnumerable<PdfRectangle> GetTablesAreas(this Page pdfPage, IDetectionAlgorithm detectionAlgorithm)
        {
            var page = ObjectExtractor.ExtractPage(pdfPage);

            // detect canditate table zones
            return detectionAlgorithm.Detect(page).Select(z => z.BoundingBox);
        }
    }
}
