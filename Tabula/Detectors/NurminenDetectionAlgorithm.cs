using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace Tabula.Detectors
{
    // https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/detectors/NurminenDetectionAlgorithm.java
    /**
     * Created by matt on 2015-12-17.
     * <p>
     * Attempt at an implementation of the table finding algorithm described by
     * Anssi Nurminen's master's thesis:
     * http://dspace.cc.tut.fi/dpub/bitstream/handle/123456789/21520/Nurminen.pdf?sequence=3
     */
    public class NurminenDetectionAlgorithm : DetectionAlgorithm
    {
        private static int GRAYSCALE_INTENSITY_THRESHOLD = 25;
        private static int HORIZONTAL_EDGE_WIDTH_MINIMUM = 50;
        private static int VERTICAL_EDGE_HEIGHT_MINIMUM = 10;
        private static int CELL_CORNER_DISTANCE_MAXIMUM = 10;
        private static float POINT_SNAP_DISTANCE_THRESHOLD = 8f;
        private static float TABLE_PADDING_AMOUNT = 1.0f;
        private static int REQUIRED_TEXT_LINES_FOR_EDGE = 4;
        private static int REQUIRED_CELLS_FOR_TABLE = 4;
        private static float IDENTICAL_TABLE_OVERLAP_RATIO = 0.9f;

        /// <summary>
        /// Helper class that encapsulates a text edge
        /// </summary>
        private class TextEdge // static
        {
            public readonly PdfLine line;
            // types of text edges
            public const int LEFT = 0;
            public const int MID = 1;
            public const int RIGHT = 2;
            public const int NUM_TYPES = 3;

            public int intersectingTextRowCount;

            public TextEdge(double x1, double y1, double x2, double y2)
            {
                this.line = new PdfLine(x1, y1, x2, y2); // bobld: careful with order here
                //super(x1, y1, x2, y2);
                this.intersectingTextRowCount = 0;
            }
        }

        /// <summary>
        /// Helper container for all text edges on a page
        /// </summary>
        private class TextEdges : List<List<TextEdge>> // ArrayList<List<TextEdge>> // static
        {
            public TextEdges(List<TextEdge> leftEdges, List<TextEdge> midEdges, List<TextEdge> rightEdges)
                : base(3)
            {
                //super(3);
                this.Add(leftEdges);
                this.Add(midEdges);
                this.Add(rightEdges);
            }
        }

        /// <summary>
        /// Helper container for relevant text edge info
        /// </summary>
        private class RelevantEdges // static
        {
            public int edgeType;
            public int edgeCount;

            public RelevantEdges(int edgeType, int edgeCount)
            {
                this.edgeType = edgeType;
                this.edgeCount = edgeCount;
            }
        }

        public List<TableRectangle> detect(PageArea page)
        {
            throw new NotImplementedException();

            /*
            // get horizontal & vertical lines
            // we get these from an image of the PDF and not the PDF itself because sometimes there are invisible PDF
            // instructions that are interpreted incorrectly as visible elements - we really want to capture what a
            // person sees when they look at the PDF
            BufferedImage image;
            Page pdfPage = page.getPDPage();
            try
            {
                image = Utils.pageConvertToImage(page.getPDDoc(), pdfPage, 144, ImageType.GRAY);
            }
            catch (IOException e)
            {
                return new List<TableRectangle>(); //ArrayList<>();
            }

            List<Ruling> horizontalRulings = this.getHorizontalRulings(image);

            // now check the page for vertical lines, but remove the text first to make things less confusing
            PdfDocument removeTextDocument = null;
            try
            {
                removeTextDocument = this.removeText(pdfPage);
                pdfPage = removeTextDocument.GetPage(1); //.getPage(0);
                image = Utils.pageConvertToImage(removeTextDocument, pdfPage, 144); //, ImageType.GRAY);
            }
            catch (Exception e)
            {
                return new List<TableRectangle>(); //ArrayList<>();
            }
            finally
            {
                if (removeTextDocument != null)
                {
                    try
                    {
                        removeTextDocument.Dispose(); //.close();
                    }
                    catch (IOException e)
                    {
                        // TODO Auto-generated catch block
                        // e.printStackTrace();
                    }
                }
            }

            List<Ruling> verticalRulings = this.getVerticalRulings(image);

            List<Ruling> allEdges = new List<Ruling>(horizontalRulings);
            allEdges.AddRange(verticalRulings);

            List<TableRectangle> tableAreas = new List<TableRectangle>();

            // if we found some edges, try to find some tables based on them
            if (allEdges.Count > 0)
            {
                // now we need to snap edge endpoints to a grid
                Utils.snapPoints(allEdges, POINT_SNAP_DISTANCE_THRESHOLD, POINT_SNAP_DISTANCE_THRESHOLD);

                // normalize the rulings to make sure snapping didn't create any wacky non-horizontal/vertical rulings
                foreach (List<Ruling> rulings in new[] { horizontalRulings, verticalRulings }) // Arrays.asList(horizontalRulings, verticalRulings))
                {
                    //foreach (Iterator<Ruling> iterator = rulings.iterator(); iterator.hasNext();)
                    foreach (var ruling in rulings.ToList()) // ToList() to do a copy to allow remove in original
                    {
                        //Ruling ruling = iterator.next();

                        ruling.normalize();
                        if (ruling.oblique())
                        {
                            rulings.Remove(ruling); //iterator.remove();
                        }
                    }
                }

                // merge the edge lines into rulings - this makes finding edges between crossing points in the next step easier
                // we use a larger pixel expansion than the normal spreadsheet extraction method to cover gaps in the
                // edge detection/pixel snapping steps
                horizontalRulings = Ruling.collapseOrientedRulings(horizontalRulings, 5);
                verticalRulings = Ruling.collapseOrientedRulings(verticalRulings, 5);

                // use the rulings and points to find cells
                var cells = SpreadsheetExtractionAlgorithm.findCells(horizontalRulings, verticalRulings); // List<TableRectangle>

                // then use those cells to make table areas
                tableAreas = this.getTableAreasFromCells(cells.Cast<TableRectangle>().ToList());
            }

            // next find any vertical rulings that intersect tables - sometimes these won't have completely been captured as
            // cells if there are missing horizontal lines (which there often are)
            // let's assume though that these lines should be part of the table
            foreach (Ruling verticalRuling in verticalRulings) // Line2D.Float
            {
                foreach (TableRectangle tableArea in tableAreas)
                {
                    if (verticalRuling.intersects(tableArea) && !(tableArea.contains(verticalRuling.getP1()) && tableArea.contains(verticalRuling.getP2())))
                    {
                        tableArea.setTop((float)Math.Floor(Math.Max(tableArea.getTop(), verticalRuling.getY1())));          // min
                        tableArea.setBottom((float)Math.Ceiling(Math.Min(tableArea.getBottom(), verticalRuling.getY2())));  // max
                        break;
                    }
                }
            }

            // the tabula Page coordinate space is half the size of the PDFBox image coordinate space
            // so halve the table area size before proceeding and add a bit of padding to make sure we capture everything
            foreach (TableRectangle area in tableAreas)
            {
                area.x = (float)Math.Floor(area.x / 2) - TABLE_PADDING_AMOUNT;
                area.y = (float)Math.Floor(area.y / 2) - TABLE_PADDING_AMOUNT;
                area.width = (float)Math.Ceiling(area.width / 2) + TABLE_PADDING_AMOUNT;
                area.height = (float)Math.Ceiling(area.height / 2) + TABLE_PADDING_AMOUNT;
            }

            // we're going to want halved horizontal lines later too
            foreach (Ruling ruling in horizontalRulings) // Line2D.Float
            {
                ruling.x1 = ruling.x1 / 2;
                ruling.y1 = ruling.y1 / 2;
                ruling.x2 = ruling.x2 / 2;
                ruling.y2 = ruling.y2 / 2;
            }

            // now look at text rows to help us find more tables and flesh out existing ones
            List<TextChunk> textChunks = TextElement.mergeWords(page.getText());
            List<TableLine> lines = TextChunk.groupByLines(textChunks);

            // first look for text rows that intersect an existing table - those lines should probably be part of the table
            foreach (TableLine textRow in lines)
            {
                foreach (TableRectangle tableArea in tableAreas)
                {
                    if (!tableArea.contains(textRow) && textRow.intersects(tableArea))
                    {
                        tableArea.setLeft((float)Math.Floor(Math.Min(textRow.getLeft(), tableArea.getLeft())));
                        tableArea.setRight((float)Math.Ceiling(Math.Max(textRow.getRight(), tableArea.getRight())));
                    }
                }
            }

            // get rid of tables that DO NOT intersect any text areas - these are likely graphs or some sort of graphic
            //for (Iterator<Rectangle> iterator = tableAreas.iterator(); iterator.hasNext();)
            foreach (var table in tableAreas.ToList()) // ToList() to do a copy to allow remove in original
            {
                //TableRectangle table = iterator.next();

                bool intersectsText = false;
                foreach (TableLine textRow in lines)
                {
                    if (table.intersects(textRow))
                    {
                        intersectsText = true;
                        break;
                    }
                }

                if (!intersectsText)
                {
                    //iterator.remove();
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
                //for (Iterator<Line> iterator = lines.iterator(); iterator.hasNext();)
                foreach (var textRow in lines)
                {
                    //TableLine textRow = iterator.next();
                    foreach (TableRectangle table in tableAreas.ToList()) // ToList() to do a copy to allow remove in original
                    {
                        if (table.contains(textRow))
                        {
                            //iterator.remove();
                            lines.Remove(textRow);
                            break;
                        }
                    }
                }

                // get text edges from remaining lines in the document
                TextEdges textEdges = this.getTextEdges(lines);
                List<TextEdge> leftTextEdges = textEdges.get(TextEdge.LEFT);
                List<TextEdge> midTextEdges = textEdges.get(TextEdge.MID);
                List<TextEdge> rightTextEdges = textEdges.get(TextEdge.RIGHT);

                // find the relevant text edges (the ones we think define where a table is)
                RelevantEdges relevantEdgeInfo = this.getRelevantEdges(textEdges, lines);

                // we found something relevant so let's look for rows that fit our criteria
                if (relevantEdgeInfo.edgeType != -1)
                {
                    List<TextEdge> relevantEdges = null;
                    switch (relevantEdgeInfo.edgeType)
                    {
                        case TextEdge.LEFT:
                            relevantEdges = leftTextEdges;
                            break;
                        case TextEdge.MID:
                            relevantEdges = midTextEdges;
                            break;
                        case TextEdge.RIGHT:
                            relevantEdges = rightTextEdges;
                            break;
                    }

                    TableRectangle table = this.getTableFromText(lines, relevantEdges, relevantEdgeInfo.edgeCount, horizontalRulings);

                    if (table != null)
                    {
                        foundTable = true;
                        tableAreas.Add(table);
                    }
                }
            } while (foundTable);

            // create a set of our current tables that will eliminate duplicate tables
            // Set<Rectangle> tableSet = new TreeSet<>(new Comparator<Rectangle>() {
            // not sure if works with sorted set??
            SortedSet<TableRectangle> tableSet = new SortedSet<TableRectangle>(new TreeSetRectangleComparer());

            //tableSet.addAll(tableAreas);
            foreach (var ta in tableAreas)
            {
                tableSet.Add(ta);
            }

            return new List<TableRectangle>(tableSet); //ArrayList<>(tableSet);
            */
        }

        public class TreeSetRectangleComparer : IComparer<TableRectangle>
        {
            public int Compare(TableRectangle o1, TableRectangle o2)
            {
                if (o1.Equals(o2))
                {
                    return 0;
                }

                // o1 is "equal" to o2 if o2 contains all of o1
                if (o2.contains(o1))
                {
                    return 0;
                }

                if (o1.contains(o2))
                {
                    return 0;
                }

                // otherwise see if these tables are "mostly" the same
                double overlap = o1.overlapRatio(o2);
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

        private TableRectangle getTableFromText(List<TableLine> lines,
                                       List<TextEdge> relevantEdges,
                                       int relevantEdgeCount,
                                       List<Ruling> horizontalRulings)
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
                    double lineDistance = textRow.getTop() - prevRow.getTop();

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
                    if (textRow.intersectsLine(edge.line))
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
                        totalRowSpacing += (textRow.getTop() - prevRow.getTop());
                    }

                    // row is part of a table
                    if (table.getArea() == 0)
                    {
                        firstTableRow = textRow;
                        table.setRect(textRow);
                    }
                    else
                    {
                        table.setLeft(Math.Min(table.getLeft(), textRow.getLeft()));
                        table.setBottom(Math.Min(table.getBottom(), textRow.getBottom())); // max
                        table.setRight(Math.Max(table.getRight(), textRow.getRight()));
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
            if (table.getArea() == 0)
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
                avgRowHeight = totalRowSpacing / tableSpaceCount;
            }
            else
            {
                avgRowHeight = lastTableRow.height;
            }

            double rowHeightThreshold = avgRowHeight * 1.5;

            // check lines after the bottom of the table
            foreach (Ruling ruling in horizontalRulings) // Line2D.Float 
            {

                if (ruling.getY1() < table.getBottom())
                {
                    continue;
                }

                double distanceFromTable = ruling.getY1() - table.getBottom();
                if (distanceFromTable <= rowHeightThreshold)
                {
                    // use this ruling to help define the table
                    table.setBottom(Math.Min(table.getBottom(), ruling.getY1())); // max
                    table.setLeft(Math.Min(table.getLeft(), ruling.getX1()));
                    table.setRight(Math.Max(table.getRight(), ruling.getX2()));
                }
                else
                {
                    // no use checking any further
                    break;
                }
            }

            // do the same for lines at the top, but make the threshold greater since table headings tend to be
            // larger to fit up to three-ish rows of text (at least but we don't want to grab too much)
            rowHeightThreshold = avgRowHeight * 3.8f;

            for (int i = horizontalRulings.Count - 1; i >= 0; i--)
            {
                Ruling ruling = horizontalRulings[i];//.get(i); Line2D.Float

                if (ruling.getY1() > table.getTop()) // bobld or <??
                {
                    continue;
                }

                double distanceFromTable = table.getTop() - ruling.getY1();
                if (distanceFromTable <= rowHeightThreshold)
                {
                    table.setTop((float)Math.Max(table.getTop(), ruling.getY1()));      //min
                    table.setLeft((float)Math.Min(table.getLeft(), ruling.getX1()));
                    table.setRight((float)Math.Max(table.getRight(), ruling.getX2()));
                }
                else
                {
                    break;
                }
            }

            // add a bit of padding since the halved horizontal lines are a little fuzzy anyways
            table.setTop((float)Math.Floor(table.getTop()) - TABLE_PADDING_AMOUNT);
            table.setBottom((float)Math.Ceiling(table.getBottom()) + TABLE_PADDING_AMOUNT);
            table.setLeft((float)Math.Floor(table.getLeft()) - TABLE_PADDING_AMOUNT);
            table.setRight((float)Math.Ceiling(table.getRight()) + TABLE_PADDING_AMOUNT);

            return table;
        }

        private RelevantEdges getRelevantEdges(TextEdges textEdges, List<TableLine> lines)
        {
            List<TextEdge> leftTextEdges = textEdges[TextEdge.LEFT]; //.get(TextEdge.LEFT);
            List<TextEdge> midTextEdges = textEdges[TextEdge.MID]; //.get(TextEdge.MID);
            List<TextEdge> rightTextEdges = textEdges[TextEdge.RIGHT]; //.get(TextEdge.RIGHT);

            // first we'll find the number of lines each type of edge crosses
            int[][] edgeCountsPerLine = new int[lines.Count][]; //[TextEdge.NUM_TYPES];
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
            List<TextEdge> leftTextEdges = new List<TextEdge>(); // ArrayList<>();
            List<TextEdge> midTextEdges = new List<TextEdge>(); //ArrayList<>();
            List<TextEdge> rightTextEdges = new List<TextEdge>(); // ArrayList<>();

            var currLeftEdges = new Dictionary<int, List<TextChunk>>(); // Map<Integer, List<TextChunk>> currLeftEdges = new HashMap<>();
            var currMidEdges = new Dictionary<int, List<TextChunk>>(); // Map<Integer, List<TextChunk>> currMidEdges = new HashMap<>();
            var currRightEdges = new Dictionary<int, List<TextChunk>>(); // Map<Integer, List<TextChunk>> currRightEdges = new HashMap<>();

            foreach (TableLine textRow in lines)
            {
                foreach (TextChunk text in textRow.getTextElements())
                {
                    int left = (int)Math.Floor(text.getLeft()); // new Integer(
                    int right = (int)Math.Floor(text.getRight()); //new Integer(
                    int mid = left + ((right - left) / 2);//new Integer(

                    // first put this chunk into any edge buckets it belongs to
                    List<TextChunk> leftEdge = currLeftEdges[left]; //.get(left);
                    if (leftEdge == null)
                    {
                        leftEdge = new List<TextChunk>(); //ArrayList<>();
                        currLeftEdges[left] = leftEdge; //.put(left, leftEdge);
                    }
                    leftEdge.Add(text);

                    List<TextChunk> midEdge = currMidEdges[mid];//.get(mid);
                    if (midEdge == null)
                    {
                        midEdge = new List<TextChunk>(); //ArrayList<>();
                        currMidEdges[mid] = midEdge; //.put(mid, midEdge);
                    }
                    midEdge.Add(text);

                    List<TextChunk> rightEdge = currRightEdges[right]; //.get(right);
                    if (rightEdge == null)
                    {
                        rightEdge = new List<TextChunk>(); //ArrayList<>();
                        currRightEdges[right] = rightEdge; //.put(right, rightEdge);
                    }
                    rightEdge.Add(text);

                    // now see if this text chunk blows up any other edges
                    //for (Iterator<Map.Entry<Integer, List<TextChunk>>> iterator = currLeftEdges.entrySet().iterator(); iterator.hasNext();)
                    foreach (var entry in currLeftEdges.ToList())
                    {
                        //Map.Entry<Integer, List<TextChunk>> entry = iterator.next();
                        int key = entry.Key; //.getKey();
                        if (key > left && key < right)
                        {
                            //iterator.remove();
                            currLeftEdges.Remove(key);
                            List<TextChunk> edgeChunks = entry.Value; //.getValue();
                            if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                            {
                                TextChunk first = edgeChunks[0];//.get(0);
                                TextChunk last = edgeChunks[edgeChunks.Count - 1];//.get(edgeChunks.size() - 1);

                                TextEdge edge = new TextEdge(key, first.getTop(), key, last.getBottom());
                                edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                                leftTextEdges.Add(edge);
                            }
                        }
                    }

                    //for (Iterator<Map.Entry<Integer, List<TextChunk>>> iterator = currMidEdges.entrySet().iterator(); iterator.hasNext();)
                    foreach (var entry in currMidEdges.ToList())
                    {
                        //Map.Entry<Integer, List<TextChunk>> entry = iterator.next();
                        int key = entry.Key; //.getKey();
                        if (key > left && key < right && Math.Abs(key - mid) > 2)
                        {
                            //iterator.remove();
                            currMidEdges.Remove(key);
                            List<TextChunk> edgeChunks = entry.Value; //.getValue();
                            if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                            {
                                TextChunk first = edgeChunks[0];//.get(0);
                                TextChunk last = edgeChunks[edgeChunks.Count - 1]; //.get(edgeChunks.size() - 1);

                                TextEdge edge = new TextEdge(key, first.getTop(), key, last.getBottom());
                                edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                                midTextEdges.Add(edge);
                            }
                        }
                    }

                    //for (Iterator<Map.Entry<Integer, List<TextChunk>>> iterator = currRightEdges.entrySet().iterator(); iterator.hasNext();)
                    foreach (var entry in currRightEdges.ToList())
                    {
                        //Map.Entry<Integer, List<TextChunk>> entry = iterator.next();
                        int key = entry.Key; //.getKey();
                        if (key > left && key < right)
                        {
                            //iterator.remove();
                            currRightEdges.Remove(key);
                            List<TextChunk> edgeChunks = entry.Value; //.getValue();
                            if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                            {
                                TextChunk first = edgeChunks[0];//.get(0);
                                TextChunk last = edgeChunks[edgeChunks.Count - 1]; //.get(edgeChunks.size() - 1);

                                TextEdge edge = new TextEdge(key, first.getTop(), key, last.getBottom());
                                edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                                rightTextEdges.Add(edge);
                            }
                        }
                    }
                }
            }

            // add the leftovers
            //foreach (Integer key in currLeftEdges.keySet())
            foreach (var key in currLeftEdges.Keys)
            {
                List<TextChunk> edgeChunks = currLeftEdges[key]; //.get(key);
                if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                {
                    TextChunk first = edgeChunks[0]; //.get(0);
                    TextChunk last = edgeChunks[edgeChunks.Count - 1]; //.get(edgeChunks.size() - 1);

                    TextEdge edge = new TextEdge(key, first.getTop(), key, last.getBottom());
                    edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                    leftTextEdges.Add(edge);
                }
            }

            foreach (int key in currMidEdges.Keys) //.keySet())
            {
                List<TextChunk> edgeChunks = currMidEdges[key]; // .get(key);
                if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                {
                    TextChunk first = edgeChunks[0]; //.get(0);
                    TextChunk last = edgeChunks[edgeChunks.Count - 1];//.get(edgeChunks.size() - 1);

                    TextEdge edge = new TextEdge(key, first.getTop(), key, last.getBottom());
                    edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                    midTextEdges.Add(edge);
                }
            }

            foreach (int key in currRightEdges.Keys) //.keySet())
            {
                List<TextChunk> edgeChunks = currRightEdges[key]; //.get(key);
                if (edgeChunks.Count >= REQUIRED_TEXT_LINES_FOR_EDGE)
                {
                    TextChunk first = edgeChunks[0];//.get(0);
                    TextChunk last = edgeChunks[edgeChunks.Count - 1]; // .get(edgeChunks.size() - 1);

                    TextEdge edge = new TextEdge(key, first.getTop(), key, last.getBottom());
                    edge.intersectingTextRowCount = Math.Min(edgeChunks.Count, lines.Count);

                    rightTextEdges.Add(edge);
                }
            }

            return new TextEdges(leftTextEdges, midTextEdges, rightTextEdges);
        }

        private List<TableRectangle> getTableAreasFromCells(List<TableRectangle> cells)
        {
            List<List<TableRectangle>> cellGroups = new List<List<TableRectangle>>(); // ArrayList<>();
            foreach (TableRectangle cell in cells)
            {
                bool addedToGroup = false;

                bool breakCellCheck = false;
                //cellCheck:
                foreach (List<TableRectangle> cellGroup in cellGroups)
                {
                    if (breakCellCheck) break; // simulates 'break cellCheck;'
                    foreach (TableRectangle groupCell in cellGroup)
                    {
                        PdfPoint[] groupCellCorners = groupCell.getPoints();
                        PdfPoint[] candidateCorners = cell.getPoints();

                        for (int i = 0; i < candidateCorners.Length; i++)
                        {
                            for (int j = 0; j < groupCellCorners.Length; j++)
                            {
                                //if (candidateCorners[i].distance(groupCellCorners[j]) < CELL_CORNER_DISTANCE_MAXIMUM)
                                if (Distances.Euclidean(candidateCorners[i], groupCellCorners[j]) < CELL_CORNER_DISTANCE_MAXIMUM)
                                {
                                    cellGroup.Add(cell);
                                    addedToGroup = true;
                                    //break cellCheck;
                                    breakCellCheck = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!addedToGroup)
                {
                    List<TableRectangle> cellGroup = new List<TableRectangle>();  //ArrayList<Rectangle> cellGroup = new ArrayList<>();
                    cellGroup.Add(cell);
                    cellGroups.Add(cellGroup);
                }
            }

            // create table areas based on cell group
            List<TableRectangle> tableAreas = new List<TableRectangle>(); //new ArrayList<>();
            foreach (List<TableRectangle> cellGroup in cellGroups)
            {
                // less than four cells should not make a table
                if (cellGroup.Count < REQUIRED_CELLS_FOR_TABLE)
                {
                    continue;
                }

                double top = double.MinValue; // Float.MAX_VALUE;
                double left = double.MaxValue; // Float.MAX_VALUE;
                double bottom = double.MaxValue; // Float.MIN_VALUE;
                double right = double.MinValue; // Float.MIN_VALUE;

                foreach (TableRectangle cell in cellGroup)
                {
                    if (cell.getTop() > top) top = cell.getTop(); // (cell.getTop() < top)
                    if (cell.getLeft() < left) left = cell.getLeft();
                    if (cell.getBottom() < bottom) bottom = cell.getBottom(); // (cell.getBottom() > bottom)
                    if (cell.getRight() > right) right = cell.getRight();
                }

                tableAreas.Add(new TableRectangle(top, left, right - left, bottom - top));
            }

            return tableAreas;
        }

        private List<Ruling> getHorizontalRulings(object image) // BufferedImage
        {
            throw new NotImplementedException();

            /*
            // get all horizontal edges, which we'll define as a change in grayscale colour
            // along a straight line of a certain length
            List<Ruling> horizontalRulings = new List<Ruling>(); // ArrayList<>();

            Raster r = image.getRaster();
            int width = r.getWidth();
            int height = r.getHeight();

            for (int x = 0; x < width; x++)
            {
                int[] lastPixel = r.getPixel(x, 0, (int[])null);

                for (int y = 1; y < height - 1; y++)
                {
                    int[] currPixel = r.getPixel(x, y, (int[])null);

                    int diff = Math.Abs(currPixel[0] - lastPixel[0]);
                    if (diff > GRAYSCALE_INTENSITY_THRESHOLD)
                    {
                        // we hit what could be a line
                        // don't bother scanning it if we've hit a pixel in the line before
                        bool alreadyChecked = false;
                        foreach (var line in horizontalRulings) // 
                        {
                            if (y == line.getY1() && x >= line.getX1() && x <= line.getX2())
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
                            int[] linePixel = r.getPixel(lineX, y, (int[])null);
                            int[] abovePixel = r.getPixel(lineX, y - 1, (int[])null);

                            if (Math.Abs(linePixel[0] - abovePixel[0]) <= GRAYSCALE_INTENSITY_THRESHOLD
                                    || Math.Abs(currPixel[0] - linePixel[0]) > GRAYSCALE_INTENSITY_THRESHOLD)
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
            */
        }

        private List<Ruling> getVerticalRulings(object image) // BufferedImage
        {
            throw new NotImplementedException();

            /*
            // get all vertical edges, which we'll define as a change in grayscale colour
            // along a straight line of a certain length
            List<Ruling> verticalRulings = new List<Ruling>();//new ArrayList<>();

            Raster r = image.getRaster();
            int width = r.getWidth();
            int height = r.getHeight();

            for (int y = 0; y < height; y++)
            {
                int[] lastPixel = r.getPixel(0, y, (int[])null);

                for (int x = 1; x < width - 1; x++)
                {
                    int[] currPixel = r.getPixel(x, y, (int[])null);

                    int diff = Math.Abs(currPixel[0] - lastPixel[0]);
                    if (diff > GRAYSCALE_INTENSITY_THRESHOLD)
                    {
                        // we hit what could be a line
                        // don't bother scanning it if we've hit a pixel in the line before
                        bool alreadyChecked = false;
                        foreach (var line in verticalRulings)
                        {
                            if (x == line.getX1() && y >= line.getY1() && y <= line.getY2())
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
                            int[] linePixel = r.getPixel(x, lineY, (int[])null);
                            int[] leftPixel = r.getPixel(x - 1, lineY, (int[])null);

                            if (Math.Abs(linePixel[0] - leftPixel[0]) <= GRAYSCALE_INTENSITY_THRESHOLD
                                    || Math.Abs(currPixel[0] - linePixel[0]) > GRAYSCALE_INTENSITY_THRESHOLD)
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
            */
        }

        // taken from http://www.docjar.com/html/api/org/apache/pdfbox/examples/util/RemoveAllText.java.html
        private PdfDocument removeText(Page page)
        {
            throw new NotImplementedException();
            /*
            PDFStreamParser parser = new PDFStreamParser(page);
            parser.parse();
            List<object> tokens = parser.getTokens();
            List<object> newTokens = new List<object>(); //ArrayList<>();
            foreach (object token in tokens)
            {
                if (token is Operator op) // instanceof
                {
                    //Operator op = (Operator)token;
                    if (op.getName().equals("TJ") || op.getName().equals("Tj"))
                    {
                        //remove the one argument to this operator
                        newTokens.remove(newTokens.size() - 1);
                        continue;
                    }
                }
                newTokens.Add(token);
            }

            PdfDocument document = new PdfDocument();
            Page newPage = document.importPage(page);
            newPage.setResources(page.getResources());

            PDStream newContents = new PDStream(document);
            OutputStream outp = newContents.createOutputStream(COSName.FLATE_DECODE);
            ContentStreamWriter writer = new ContentStreamWriter(outp);
            writer.writeTokens(newTokens);
            outp.close();
            newPage.setContents(newContents);
            return document;
            */
        }
    }
}
