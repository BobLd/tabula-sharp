using System;
using System.Collections.Generic;
using System.Linq;
using Tabula.Extractors;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace Tabula.Detectors
{
    // adapted from tabula-java/blob/master/src/main/java/technology/tabula/detectors/NurminenDetectionAlgorithm.java

    /*
     * Anssi Nurminen's master's thesis:
     * http://dspace.cc.tut.fi/dpub/bitstream/handle/123456789/21520/Nurminen.pdf?sequence=3
     */

    /// <summary>
    /// Simplified Nurminen detection algorithm.
    /// <para>Does not do any image processing.</para>
    /// </summary>
    public class SimpleNurminenDetectionAlgorithm : IDetectionAlgorithm
    {
        private static int HORIZONTAL_EDGE_WIDTH_MINIMUM = 50;
        private static int VERTICAL_EDGE_HEIGHT_MINIMUM = 10;
        private static int CELL_CORNER_DISTANCE_MAXIMUM = 10;
        private static double POINT_SNAP_DISTANCE_THRESHOLD = 8.0;
        private static double TABLE_PADDING_AMOUNT = 1.0;
        private static int REQUIRED_TEXT_LINES_FOR_EDGE = 4;
        private static int REQUIRED_CELLS_FOR_TABLE = 4;
        private static double IDENTICAL_TABLE_OVERLAP_RATIO = 0.9;

        /// <summary>
        /// Helper class that encapsulates a text edge
        /// </summary>
        private class TextEdge
        {
            public PdfLine Line;

            // types of text edges
            public const int LEFT = 0;
            public const int MID = 1;
            public const int RIGHT = 2;
            public const int NUM_TYPES = 3;

            public int intersectingTextRowCount;

            public TextEdge(double x1, double y1, double x2, double y2)
            {
                Line = new PdfLine(x1, y1, x2, y2);
                this.intersectingTextRowCount = 0;
            }

            public override string ToString()
            {
                return $"{Line.Point1}-{Line.Point2}";
            }
        }

        /// <summary>
        /// Helper container for all text edges on a page
        /// </summary>
        private class TextEdges : List<List<TextEdge>>
        {
            public TextEdges(List<TextEdge> leftEdges, List<TextEdge> midEdges, List<TextEdge> rightEdges) : base(3)
            {
                this.Add(leftEdges);
                this.Add(midEdges);
                this.Add(rightEdges);
            }
        }

        /// <summary>
        /// Helper container for relevant text edge info
        /// </summary>
        private class RelevantEdges
        {
            public int edgeType;
            public int edgeCount;

            public RelevantEdges(int edgeType, int edgeCount)
            {
                this.edgeType = edgeType;
                this.edgeCount = edgeCount;
            }
        }

        /// <summary>
        /// Simplified Nurminen detection algorithm.
        /// <para>Does not do any image processing.</para>
        /// </summary>
        public SimpleNurminenDetectionAlgorithm()
        { }

        /// <summary>
        /// Detects the tables in the page.
        /// </summary>
        /// <param name="page"></param>
        public List<TableRectangle> Detect(PageArea page)
        {
            // get horizontal & vertical lines
            // we get these from an image of the PDF and not the PDF itself because sometimes there are invisible PDF
            // instructions that are interpreted incorrectly as visible elements - we really want to capture what a
            // person sees when they look at the PDF
            // BobLd: hack here, we don't convert to an image
            var pageRulings = page.GetRulings();
            List<Ruling> horizontalRulings = this.getHorizontalRulings(pageRulings);
            List<Ruling> verticalRulings = this.getVerticalRulings(pageRulings);
            // end hack here

            List<Ruling> allEdges = new List<Ruling>(horizontalRulings);
            allEdges.AddRange(verticalRulings);

            List<TableRectangle> tableAreas = new List<TableRectangle>();

            // if we found some edges, try to find some tables based on them
            if (allEdges.Count > 0)
            {
                // now we need to snap edge endpoints to a grid
                Utils.SnapPoints(allEdges, POINT_SNAP_DISTANCE_THRESHOLD, POINT_SNAP_DISTANCE_THRESHOLD);

                // normalize the rulings to make sure snapping didn't create any wacky non-horizontal/vertical rulings
                foreach (List<Ruling> rulings in new[] { horizontalRulings, verticalRulings }) //Arrays.asList(horizontalRulings, verticalRulings))
                {
                    //for (Iterator<Ruling> iterator = rulings.iterator(); iterator.hasNext();)
                    foreach (var ruling in rulings.ToList()) // use ToList to be able to remove
                    {
                        ruling.Normalize();
                        if (ruling.IsOblique)
                        {
                            rulings.Remove(ruling);
                        }
                    }
                }

                // merge the edge lines into rulings - this makes finding edges between crossing points in the next step easier
                // we use a larger pixel expansion than the normal spreadsheet extraction method to cover gaps in the
                // edge detection/pixel snapping steps
                horizontalRulings = Ruling.CollapseOrientedRulings(horizontalRulings, 5);
                verticalRulings = Ruling.CollapseOrientedRulings(verticalRulings, 5);

                // use the rulings and points to find cells
                List<TableRectangle> cells = SpreadsheetExtractionAlgorithm.FindCells(horizontalRulings, verticalRulings).Cast<TableRectangle>().ToList();

                // then use those cells to make table areas
                tableAreas = getTableAreasFromCells(cells);
            }

            // next find any vertical rulings that intersect tables - sometimes these won't have completely been captured as
            // cells if there are missing horizontal lines (which there often are)
            // let's assume though that these lines should be part of the table
            foreach (Ruling verticalRuling in verticalRulings) // Line2D.Float
            {
                foreach (TableRectangle tableArea in tableAreas)
                {
                    if (verticalRuling.Intersects(tableArea) &&
                            !(tableArea.Contains(verticalRuling.P1) && tableArea.Contains(verticalRuling.P2)))
                    {
                        tableArea.SetTop(Math.Ceiling(Math.Max(tableArea.Top, verticalRuling.Y2)));     // bobld: Floor and Min, Y1
                        tableArea.SetBottom(Math.Floor(Math.Min(tableArea.Bottom, verticalRuling.Y1))); // bobld: Ceiling and Max, Y2
                        break;
                    }
                }
            }

            /* BobLd: not sure this is the case in tabula-sharp/PdfPig
            // the tabula Page coordinate space is half the size of the PDFBox image coordinate space
            // so halve the table area size before proceeding and add a bit of padding to make sure we capture everything
            foreach (TableRectangle area in tableAreas)
            {
                area.x = (float)Math.floor(area.x / 2) - TABLE_PADDING_AMOUNT;
                area.y = (float)Math.floor(area.y / 2) - TABLE_PADDING_AMOUNT;
                area.width = (float)Math.ceil(area.width / 2) + TABLE_PADDING_AMOUNT;
                area.height = (float)Math.ceil(area.height / 2) + TABLE_PADDING_AMOUNT;
            }

            // we're going to want halved horizontal lines later too
            foreach (Ruling ruling in horizontalRulings) // Line2D.Float 
            {
                ruling.x1 = ruling.x1 / 2;
                ruling.y1 = ruling.y1 / 2;
                ruling.x2 = ruling.x2 / 2;
                ruling.y2 = ruling.y2 / 2;
            }
            */

            // now look at text rows to help us find more tables and flesh out existing ones
            List<TextChunk> textChunks = TextElement.MergeWords(page.GetText());
            List<TableLine> lines = TextChunk.GroupByLines(textChunks);

            // first look for text rows that intersect an existing table - those lines should probably be part of the table
            foreach (TableLine textRow in lines)
            {
                foreach (TableRectangle tableArea in tableAreas)
                {
                    if (!tableArea.Contains(textRow) && textRow.Intersects(tableArea))
                    {
                        tableArea.SetLeft(Math.Floor(Math.Min(textRow.Left, tableArea.Left)));
                        tableArea.SetRight(Math.Ceiling(Math.Max(textRow.Right, tableArea.Right)));
                    }
                }
            }

            // get rid of tables that DO NOT intersect any text areas - these are likely graphs or some sort of graphic
            //for (Iterator<Rectangle> iterator = tableAreas.iterator(); iterator.hasNext();)
            foreach (TableRectangle table in tableAreas.ToList()) // use tolist to be able to remove
            {
                bool intersectsText = false;
                foreach (TableLine textRow in lines)
                {
                    if (table.Intersects(textRow))
                    {
                        intersectsText = true;
                        break;
                    }
                }

                if (!intersectsText)
                {
                    tableAreas.Remove(table);
                }
            }

            // lastly, there may be some tables that don't have any vertical rulings at all
            // we'll use text edges we've found to try and guess which text rows are part of a table

            // in his thesis nurminen goes through every row to try to assign a probability that the line is in a table
            // we're going to try a general heuristic instead, trying to find what type of edge (left/right/mid) intersects
            // the most text rows, and then use that magic number of "relevant" edges to decide what text rows should be
            // part of a table.

            bool foundTable;

            do
            {
                foundTable = false;

                // get rid of any text lines contained within existing tables, this allows us to find more tables
                //for (Iterator<TableLine> iterator = lines.iterator(); iterator.hasNext();)
                foreach (var textRow in lines.ToList())
                {
                    foreach (TableRectangle table in tableAreas)
                    {
                        if (table.Contains(textRow))
                        {
                            lines.Remove(textRow);
                            break;
                        }
                    }
                }

                // get text edges from remaining lines in the document
                TextEdges textEdges = getTextEdges(lines);
                //List<TextEdge> leftTextEdges = textEdges[TextEdge.LEFT];
                //List<TextEdge> midTextEdges = textEdges[TextEdge.MID];
                //List<TextEdge> rightTextEdges = textEdges[TextEdge.RIGHT];

                // find the relevant text edges (the ones we think define where a table is)
                RelevantEdges relevantEdgeInfo = getRelevantEdges(textEdges, lines);

                // we found something relevant so let's look for rows that fit our criteria
                if (relevantEdgeInfo.edgeType != -1)
                {
                    List<TextEdge> relevantEdges = null;
                    switch (relevantEdgeInfo.edgeType)
                    {
                        case TextEdge.LEFT:
                            relevantEdges = textEdges[TextEdge.LEFT];   // leftTextEdges;
                            break;
                        case TextEdge.MID:
                            relevantEdges = textEdges[TextEdge.MID];    // midTextEdges;
                            break;
                        case TextEdge.RIGHT:
                            relevantEdges = textEdges[TextEdge.RIGHT];  // rightTextEdges;
                            break;
                    }

                    TableRectangle table = getTableFromText(lines, relevantEdges, relevantEdgeInfo.edgeCount, horizontalRulings);

                    if (table != null)
                    {
                        foundTable = true;
                        tableAreas.Add(table);
                    }
                }
            } while (foundTable);

            // create a set of our current tables that will eliminate duplicate tables
            SortedSet<TableRectangle> tableSet = new SortedSet<TableRectangle>(new TreeSetComparer()); //Set<Rectangle> tableSet = new TreeSet<>(new Comparator<Rectangle>() {...
            foreach (var table in tableAreas.OrderByDescending(t => t.Area))
            {
                tableSet.Add(table);
            }

            return tableSet.ToList();
        }

        private class TreeSetComparer : IComparer<TableRectangle>
        {
            public int Compare(TableRectangle o1, TableRectangle o2)
            {
                if (o1.Equals(o2))
                {
                    return 0;
                }

                // o1 is "equal" to o2 if o2 contains all of o1
                if (o2.Contains(o1))
                {
                    return 0;
                }

                if (o1.Contains(o2))
                {
                    return 0;
                }

                // otherwise see if these tables are "mostly" the same
                double overlap = o1.OverlapRatio(o2);
                if (overlap >= IDENTICAL_TABLE_OVERLAP_RATIO)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        private TableRectangle getTableFromText(List<TableLine> lines, List<TextEdge> relevantEdges, int relevantEdgeCount, List<Ruling> horizontalRulings)
        {
            TableRectangle table = new TableRectangle();

            TableLine prevRow = null;
            TableLine firstTableRow = null;
            TableLine lastTableRow = null;

            int tableSpaceCount = 0;
            double totalRowSpacing = 0;

            // go through the lines and find the ones that have the correct count of the relevant edges
            foreach (TableLine textRow in lines)
            {
                int numRelevantEdges = 0;

                if (firstTableRow != null && tableSpaceCount > 0)
                {
                    // check to make sure this text row is within a line or so of the other lines already added
                    // if it's not, we should stop the table here
                    double tableLineThreshold = (totalRowSpacing / tableSpaceCount) * 2.5;
                    double lineDistance = prevRow.Bottom - textRow.Bottom; // bobld: textRow.Top - prevRow.Top

                    System.Diagnostics.Debug.Assert(lineDistance >= 0);

                    if (lineDistance > tableLineThreshold)
                    {
                        lastTableRow = prevRow;
                        break;
                    }
                }

                // for larger tables, be a little lenient on the number of relevant rows the text intersects
                // for smaller tables, not so much - otherwise we'll end up treating paragraphs as tables too
                int relativeEdgeDifferenceThreshold = 1;
                if (relevantEdgeCount <= 3)
                {
                    relativeEdgeDifferenceThreshold = 0;
                }

                foreach (TextEdge edge in relevantEdges)
                {
                    if (textRow.IntersectsLine(edge.Line))
                    {
                        numRelevantEdges++;
                    }
                }

                // see if we have a candidate text row
                if (numRelevantEdges >= (relevantEdgeCount - relativeEdgeDifferenceThreshold))
                {
                    // keep track of table row spacing
                    if (prevRow != null && firstTableRow != null)
                    {
                        tableSpaceCount++;
                        totalRowSpacing += prevRow.Bottom - textRow.Bottom; // bobld: textRow.Top - prevRow.Top
                    }

                    // row is part of a table
                    if (table.Area == 0)
                    {
                        firstTableRow = textRow;
                        table.SetRect(textRow);
                    }
                    else
                    {
                        table.SetLeft(Math.Min(table.Left, textRow.Left));
                        table.SetBottom(Math.Min(table.Bottom, textRow.Bottom)); // bobld: Max
                        table.SetRight(Math.Max(table.Right, textRow.Right));
                    }
                }
                else
                {
                    // no dice
                    // if we're at the end of the table, save the last row
                    if (firstTableRow != null && lastTableRow == null)
                    {
                        lastTableRow = prevRow;
                    }
                }

                prevRow = textRow;
            }

            // if we don't have a table now, we won't after the next step either
            if (table.Area == 0)
            {
                return null;
            }

            if (lastTableRow == null)
            {
                // takes care of one-row tables or tables that end at the bottom of a page
                lastTableRow = prevRow;
            }

            // use the average row height and nearby horizontal lines to extend the table area
            double avgRowHeight;
            if (tableSpaceCount > 0)
            {
                System.Diagnostics.Debug.Assert(totalRowSpacing >= 0);
                avgRowHeight = totalRowSpacing / tableSpaceCount;
            }
            else
            {
                avgRowHeight = lastTableRow.Height;
            }

            double rowHeightThreshold = avgRowHeight * 1.5;

            // check lines after the bottom of the table
            //foreach (Ruling ruling in sortedHorizontalRulings) //Line2D.Float
            for (int i = horizontalRulings.Count - 1; i >= 0; i--) // reverse order
            {
                var ruling = horizontalRulings[i];
                if (ruling.Y1 > table.Bottom) // bobld: <
                {
                    continue;
                }

                double distanceFromTable = table.Bottom - ruling.Y2; // bobld: Y1
                System.Diagnostics.Debug.Assert(distanceFromTable >= 0);
                if (distanceFromTable <= rowHeightThreshold)
                {
                    // use this ruling to help define the table
                    table.SetBottom(Math.Min(table.Bottom, ruling.Y2));  // bobld: Max Y1
                    table.SetLeft(Math.Min(table.Left, ruling.X1));
                    table.SetRight(Math.Max(table.Right, ruling.X2));
                }
                else
                {
                    // no use checking any further
                    break;
                }
            }

            // do the same for lines at the top, but make the threshold greater since table headings tend to be
            // larger to fit up to three-ish rows of text (at least but we don't want to grab too much)
            rowHeightThreshold = avgRowHeight * 3.8;

            //for (int i = horizontalRulings.Count - 1; i >= 0; i--)
            for (int i = 0; i < horizontalRulings.Count; i++)
            {
                Ruling ruling = horizontalRulings[i];

                if (ruling.Y1 < table.Top) //bobld: >
                {
                    continue;
                }

                double distanceFromTable = ruling.Y1 - table.Top; // bobld: table.Top - ruling.Y1
                System.Diagnostics.Debug.Assert(distanceFromTable >= 0);
                if (distanceFromTable <= rowHeightThreshold)
                {
                    table.SetTop(Math.Max(table.Top, ruling.Y2));  // bobld: Min Y1
                    table.SetLeft(Math.Min(table.Left, ruling.X1));
                    table.SetRight(Math.Max(table.Right, ruling.X2));
                }
                else
                {
                    break;
                }
            }

            // add a bit of padding since the halved horizontal lines are a little fuzzy anyways
            table.SetTop(Math.Ceiling(table.Top) + TABLE_PADDING_AMOUNT);       // bobld: Floor -
            table.SetBottom(Math.Floor(table.Bottom) - TABLE_PADDING_AMOUNT);   // bobld: Ceiling +
            table.SetLeft(Math.Floor(table.Left) - TABLE_PADDING_AMOUNT);
            table.SetRight(Math.Ceiling(table.Right) + TABLE_PADDING_AMOUNT);

            return table;
        }

        private RelevantEdges getRelevantEdges(TextEdges textEdges, List<TableLine> lines)
        {
            List<TextEdge> leftTextEdges = textEdges[TextEdge.LEFT];
            List<TextEdge> midTextEdges = textEdges[TextEdge.MID];
            List<TextEdge> rightTextEdges = textEdges[TextEdge.RIGHT];

            // first we'll find the number of lines each type of edge crosses
            int[][] edgeCountsPerLine = new int[lines.Count][];
            for (int i = 0; i < edgeCountsPerLine.Length; i++)
            {
                edgeCountsPerLine[i] = new int[TextEdge.NUM_TYPES];
            }

            foreach (TextEdge edge in leftTextEdges)
            {
                edgeCountsPerLine[edge.intersectingTextRowCount - 1][TextEdge.LEFT]++;
            }

            foreach (TextEdge edge in midTextEdges)
            {
                edgeCountsPerLine[edge.intersectingTextRowCount - 1][TextEdge.MID]++;
            }

            foreach (TextEdge edge in rightTextEdges)
            {
                edgeCountsPerLine[edge.intersectingTextRowCount - 1][TextEdge.RIGHT]++;
            }

            // now let's find the relevant edge type and the number of those edges we should look for
            // we'll only take a minimum of two edges to look for tables
            int relevantEdgeType = -1;
            int relevantEdgeCount = 0;
            for (int i = edgeCountsPerLine.Length - 1; i > 2; i--)
            {
                if (edgeCountsPerLine[i][TextEdge.LEFT] > 2 &&
                    edgeCountsPerLine[i][TextEdge.LEFT] >= edgeCountsPerLine[i][TextEdge.RIGHT] &&
                    edgeCountsPerLine[i][TextEdge.LEFT] >= edgeCountsPerLine[i][TextEdge.MID])
                {
                    relevantEdgeCount = edgeCountsPerLine[i][TextEdge.LEFT];
                    relevantEdgeType = TextEdge.LEFT;
                    break;
                }

                if (edgeCountsPerLine[i][TextEdge.RIGHT] > 1 &&
                    edgeCountsPerLine[i][TextEdge.RIGHT] >= edgeCountsPerLine[i][TextEdge.LEFT] &&
                    edgeCountsPerLine[i][TextEdge.RIGHT] >= edgeCountsPerLine[i][TextEdge.MID])
                {
                    relevantEdgeCount = edgeCountsPerLine[i][TextEdge.RIGHT];
                    relevantEdgeType = TextEdge.RIGHT;
                    break;
                }

                if (edgeCountsPerLine[i][TextEdge.MID] > 1 &&
                    edgeCountsPerLine[i][TextEdge.MID] >= edgeCountsPerLine[i][TextEdge.RIGHT] &&
                    edgeCountsPerLine[i][TextEdge.MID] >= edgeCountsPerLine[i][TextEdge.LEFT])
                {
                    relevantEdgeCount = edgeCountsPerLine[i][TextEdge.MID];
                    relevantEdgeType = TextEdge.MID;
                    break;
                }
            }

            return new RelevantEdges(relevantEdgeType, relevantEdgeCount);
        }

        private TextEdges getTextEdges(List<TableLine> lines)
        {
            // get all text edges (lines that align with the left, middle and right of chunks of text) that extend
            // uninterrupted over at least REQUIRED_TEXT_LINES_FOR_EDGE lines of text

            List<TextEdge> leftTextEdges = new List<TextEdge>();
            List<TextEdge> midTextEdges = new List<TextEdge>();
            List<TextEdge> rightTextEdges = new List<TextEdge>();

            Dictionary<int, List<TextChunk>> currLeftEdges = new Dictionary<int, List<TextChunk>>();
            Dictionary<int, List<TextChunk>> currMidEdges = new Dictionary<int, List<TextChunk>>();
            Dictionary<int, List<TextChunk>> currRightEdges = new Dictionary<int, List<TextChunk>>();

            foreach (TableLine textRow in lines)
            {
                foreach (TextChunk text in textRow.TextElements)
                {
                    if (text.GetText().Equals("")) continue; // added by bobld

                    int left = (int)Math.Floor(text.Left);
                    int right = (int)Math.Floor(text.Right);
                    int mid = (int)(left + ((right - left) / 2));

                    // first put this chunk into any edge buckets it belongs to
                    if (!currLeftEdges.TryGetValue(left, out List<TextChunk> leftEdge))
                    {
                        leftEdge = new List<TextChunk>();
                        currLeftEdges[left] = leftEdge;
                    }
                    leftEdge.Add(text);

                    if (!currMidEdges.TryGetValue(mid, out List<TextChunk> midEdge))
                    {
                        midEdge = new List<TextChunk>();
                        currMidEdges[mid] = midEdge;
                    }
                    midEdge.Add(text);

                    if (!currRightEdges.TryGetValue(right, out List<TextChunk> rightEdge))
                    {
                        rightEdge = new List<TextChunk>();
                        currRightEdges[right] = rightEdge;
                    }
                    rightEdge.Add(text);

                    // now see if this text chunk blows up any other edges
                    //for (Iterator<Map.Entry<Integer, List<TextChunk>>> iterator = currLeftEdges.entrySet().iterator(); iterator.hasNext();)
                    foreach (var entry in currLeftEdges.ToList()) // use tolist to be able to remove
                    {
                        int key = entry.Key;
                        if (key > left && key < right)
                        {
                            currLeftEdges.Remove(key);
                            List<TextChunk> edgeChunks = entry.Value;
                            if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                            {
                                TextChunk first = edgeChunks[0];
                                TextChunk last = edgeChunks[edgeChunks.Count - 1];

                                TextEdge edge = new TextEdge(key, last.Bottom, key, first.Top); // bobld: (key, first.Top, key, last.Bottom)
                                edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                                leftTextEdges.Add(edge);
                            }
                        }
                    }

                    //for (Iterator<Map.Entry<Integer, List<TextChunk>>> iterator = currMidEdges.entrySet().iterator(); iterator.hasNext();)
                    foreach (var entry in currMidEdges.ToList())
                    {
                        int key = entry.Key;
                        if (key > left && key < right && Math.Abs(key - mid) > 2)
                        {
                            currMidEdges.Remove(key);
                            List<TextChunk> edgeChunks = entry.Value;
                            if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                            {
                                TextChunk first = edgeChunks[0];
                                TextChunk last = edgeChunks[edgeChunks.Count - 1];

                                TextEdge edge = new TextEdge(key, last.Bottom, key, first.Top); // bobld: (key, first.Top, key, last.Bottom)
                                edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                                midTextEdges.Add(edge);
                            }
                        }
                    }

                    //for (Iterator<Map.Entry<Integer, List<TextChunk>>> iterator = currRightEdges.entrySet().iterator(); iterator.hasNext();)
                    foreach (var entry in currRightEdges.ToList())
                    {
                        int key = entry.Key;
                        if (key > left && key < right)
                        {
                            currRightEdges.Remove(key);
                            List<TextChunk> edgeChunks = entry.Value;
                            if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                            {
                                TextChunk first = edgeChunks[0];
                                TextChunk last = edgeChunks[edgeChunks.Count - 1];

                                TextEdge edge = new TextEdge(key, last.Bottom, key, first.Top); // bobld: (key, first.Top, key, last.Bottom)
                                edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                                rightTextEdges.Add(edge);
                            }
                        }
                    }
                }
            }

            // add the leftovers
            foreach (int key in currLeftEdges.Keys)
            {
                List<TextChunk> edgeChunks = currLeftEdges[key];
                if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                {
                    TextChunk first = edgeChunks[0];
                    TextChunk last = edgeChunks[edgeChunks.Count - 1];

                    TextEdge edge = new TextEdge(key, last.Bottom, key, first.Top); // bobld: (key, first.Top, key, last.Bottom)
                    edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                    leftTextEdges.Add(edge);
                }
            }

            foreach (int key in currMidEdges.Keys)
            {
                List<TextChunk> edgeChunks = currMidEdges[key];
                if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                {
                    TextChunk first = edgeChunks[0];
                    TextChunk last = edgeChunks[edgeChunks.Count - 1];

                    TextEdge edge = new TextEdge(key, last.Bottom, key, first.Top); // bobld: (key, first.Top, key, last.Bottom);
                    edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                    midTextEdges.Add(edge);
                }
            }

            foreach (int key in currRightEdges.Keys)
            {
                List<TextChunk> edgeChunks = currRightEdges[key];
                if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                {
                    TextChunk first = edgeChunks[0];
                    TextChunk last = edgeChunks[edgeChunks.Count - 1];

                    TextEdge edge = new TextEdge(key, last.Bottom, key, first.Top); // bobld: (key, first.Top, key, last.Bottom)
                    edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                    rightTextEdges.Add(edge);
                }
            }

            return new TextEdges(leftTextEdges, midTextEdges, rightTextEdges);
        }

        private List<TableRectangle> getTableAreasFromCells(List<TableRectangle> cells)
        {
            List<List<TableRectangle>> cellGroups = new List<List<TableRectangle>>();
            foreach (TableRectangle cell in cells)
            {
                bool addedToGroup = false;

                foreach (List<TableRectangle> cellGroup in cellGroups)
                {
                    foreach (TableRectangle groupCell in cellGroup)
                    {
                        PdfPoint[] groupCellCorners = groupCell.Points;
                        PdfPoint[] candidateCorners = cell.Points;

                        for (int i = 0; i < candidateCorners.Length; i++)
                        {
                            for (int j = 0; j < groupCellCorners.Length; j++)
                            {
                                //if (candidateCorners[i].distance(groupCellCorners[j]) < CELL_CORNER_DISTANCE_MAXIMUM)
                                if (Distances.Euclidean(candidateCorners[i], groupCellCorners[j]) < CELL_CORNER_DISTANCE_MAXIMUM)
                                {
                                    cellGroup.Add(cell);
                                    addedToGroup = true;
                                    goto cellCheck;
                                }
                            }
                        }
                    }
                }

                cellCheck:
                if (!addedToGroup)
                {
                    List<TableRectangle> cellGroup = new List<TableRectangle> { cell };
                    cellGroups.Add(cellGroup);
                }
            }

            // create table areas based on cell group
            List<TableRectangle> tableAreas = new List<TableRectangle>();
            foreach (List<TableRectangle> cellGroup in cellGroups)
            {
                // less than four cells should not make a table
                if (cellGroup.Count < REQUIRED_CELLS_FOR_TABLE)
                {
                    continue;
                }

                double top = double.MinValue;       // bobld: MaxValue
                double left = double.MaxValue;
                double bottom = double.MaxValue;    // bobld: MinValue
                double right = double.MinValue;

                foreach (TableRectangle cell in cellGroup)
                {
                    if (cell.Top > top) top = cell.Top;             // bobld: <
                    if (cell.Left < left) left = cell.Left;
                    if (cell.Bottom < bottom) bottom = cell.Bottom; // bobld: >
                    if (cell.Right > right) right = cell.Right;
                }

                tableAreas.Add(new TableRectangle(new PdfRectangle(left, bottom, right, top)));
            }

            return tableAreas;
        }

        private List<Ruling> getHorizontalRulings(IReadOnlyList<Ruling> rulings)
        {
            List<Ruling> horizontalR = new List<Ruling>();
            foreach (Ruling r in rulings)
            {
                if (r.IsHorizontal)
                {
                    horizontalR.Add(r);
                }
            }

            List<Ruling> horizontalRulings = new List<Ruling>();
            foreach (var r in horizontalR)
            {
                var endX = r.Right + 1;
                var startY = r.Left - 1;
                if (endX - startY > HORIZONTAL_EDGE_WIDTH_MINIMUM)
                {
                    horizontalRulings.Add(new Ruling(new PdfPoint(startY, r.Bottom), new PdfPoint(endX, r.Top)));
                }
            }

            return horizontalRulings;
        }

        private List<Ruling> getVerticalRulings(IReadOnlyList<Ruling> rulings)
        {
            List<Ruling> verticalR = new List<Ruling>();
            foreach (Ruling r in rulings)
            {
                if (r.IsVertical)
                {
                    verticalR.Add(r);
                }
            }

            List<Ruling> verticalRulings = new List<Ruling>();
            foreach (var r in verticalR)
            {
                var endY = r.Top + 1;
                var startY = r.Bottom - 1;
                if (endY - startY > VERTICAL_EDGE_HEIGHT_MINIMUM)
                {
                    verticalRulings.Add(new Ruling(new PdfPoint(r.Left, startY), new PdfPoint(r.Right, endY)));
                }
            }
            return verticalRulings;
        }
    }
}
