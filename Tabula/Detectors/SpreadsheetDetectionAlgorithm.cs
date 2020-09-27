using System.Collections.Generic;
using System.Linq;
using Tabula.Extractors;

namespace Tabula.Detectors
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/detectors/SpreadsheetDetectionAlgorithm.java
    /*
     * Created by matt on 2015-12-14.
     * This is the basic spreadsheet table detection algorithm currently implemented in tabula (web).
     * It uses intersecting ruling lines to find tables.
     */

    /// <summary>
    /// This is the basic spreadsheet table detection algorithm currently implemented in tabula (web).
    /// It uses intersecting ruling lines to find tables.
    /// </summary>
    public class SpreadsheetDetectionAlgorithm : IDetectionAlgorithm
    {
        /// <summary>
        /// Detects the tables in the page.
        /// </summary>
        /// <param name="page">The page where to detect the tables.</param>
        public List<TableRectangle> Detect(PageArea page)
        {
            List<Cell> cells = SpreadsheetExtractionAlgorithm.FindCells(page.HorizontalRulings, page.VerticalRulings);

            List<TableRectangle> tables = SpreadsheetExtractionAlgorithm.FindSpreadsheetsFromCells(cells.Cast<TableRectangle>().ToList());

            // we want tables to be returned from top to bottom on the page
            Utils.Sort(tables, new TableRectangle.ILL_DEFINED_ORDER());
            return tables;
        }
    }
}
