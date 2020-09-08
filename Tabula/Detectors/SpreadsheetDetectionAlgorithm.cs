using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tabula.Extractors;

namespace Tabula.Detectors
{
    /**
 * Created by matt on 2015-12-14.
 *
 * This is the basic spreadsheet table detection algorithm currently implemented in tabula (web).
 *
 * It uses intersecting ruling lines to find tables.
 */
    public class SpreadsheetDetectionAlgorithm : DetectionAlgorithm
    {
        public List<TableRectangle> detect(PageArea page)
        {
            List<Cell> cells = SpreadsheetExtractionAlgorithm.findCells(page.getHorizontalRulings(), page.getVerticalRulings());

            SpreadsheetExtractionAlgorithm sea = new SpreadsheetExtractionAlgorithm();

            List<TableRectangle> tables = SpreadsheetExtractionAlgorithm.findSpreadsheetsFromCells(cells.Cast<TableRectangle>().ToList());

            // we want tables to be returned from top to bottom on the page
            //Collections.sort(tables, TableRectangle.ILL_DEFINED_ORDER);
            tables.Sort(new TableRectangle.ILL_DEFINED_ORDER());

            return tables;
        }
    }
}
