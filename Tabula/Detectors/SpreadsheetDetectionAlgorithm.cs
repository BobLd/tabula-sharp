using System.Collections.Generic;
using System.Linq;
using Tabula.Extractors;

namespace Tabula.Detectors
{
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
        public List<TableRectangle> Detect(PageArea page)
        {
            List<Cell> cells = SpreadsheetExtractionAlgorithm.FindCells(page.HorizontalRulings, page.VerticalRulings);

            //SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();

            List<TableRectangle> tables = SpreadsheetExtractionAlgorithm.FindSpreadsheetsFromCells(cells.Cast<TableRectangle>().ToList());

            // we want tables to be returned from top to bottom on the page
            //Collections.sort(tables, TableRectangle.ILL_DEFINED_ORDER);
            Utils.Sort(tables, new TableRectangle.ILL_DEFINED_ORDER()); // tables.Sort(new TableRectangle.ILL_DEFINED_ORDER());
            return tables;
        }
    }
}
