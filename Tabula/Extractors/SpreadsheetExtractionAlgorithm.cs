using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Core;

namespace Tabula.Extractors
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/extractors/SpreadsheetExtractionAlgorithm.java
    /// <summary>
    /// Lattice extraction algorithm.
    /// </summary>
    public class SpreadsheetExtractionAlgorithm : IExtractionAlgorithm
    {
        /// <summary>
        /// Lattice extraction algorithm.
        /// </summary>
        public SpreadsheetExtractionAlgorithm()
        {
        }

        private static double MAGIC_HEURISTIC_NUMBER = 0.65;

        private class POINT_COMPARER : IComparer<PdfPoint>
        {
            public int Compare(PdfPoint arg0, PdfPoint arg1)
            {
                int rv = 0;
                double arg0X = Utils.Round(arg0.X, 2);
                double arg0Y = Utils.Round(arg0.Y, 2);
                double arg1X = Utils.Round(arg1.X, 2);
                double arg1Y = Utils.Round(arg1.Y, 2);

                if (arg0Y < arg1Y) //(arg0Y > arg1Y)
                {
                    rv = 1;
                }
                else if (arg0Y > arg1Y) // (arg0Y < arg1Y)
                {
                    rv = -1;
                }
                else if (arg0X > arg1X)
                {
                    rv = 1;
                }
                else if (arg0X < arg1X)
                {
                    rv = -1;
                }

                return rv;
            }
        }

        private class X_FIRST_POINT_COMPARER : IComparer<PdfPoint>
        {
            public int Compare(PdfPoint arg0, PdfPoint arg1)
            {
                int rv = 0;
                double arg0X = Utils.Round(arg0.X, 2);
                double arg0Y = Utils.Round(arg0.Y, 2);
                double arg1X = Utils.Round(arg1.X, 2);
                double arg1Y = Utils.Round(arg1.Y, 2);

                if (arg0X > arg1X)
                {
                    rv = 1;
                }
                else if (arg0X < arg1X)
                {
                    rv = -1;
                }
                else if (arg0Y < arg1Y) //(arg0Y > arg1Y)
                {
                    rv = 1;
                }
                else if (arg0Y > arg1Y) //(arg0Y < arg1Y)
                {
                    rv = -1;
                }
                return rv;
            }
        }

        /// <summary>
        /// Extracts the tables in the page.
        /// </summary>
        /// <param name="page">The page where to extract the tables.</param>
        public List<Table> Extract(PageArea page)
        {
            return Extract(page, page.GetRulings());
        }

        /// <summary>
        /// Extracts the tables in the page using rulings as separators.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="rulings"></param>
        public List<Table> Extract(PageArea page, IReadOnlyList<Ruling> rulings)
        {
            // split rulings into horizontal and vertical
            List<Ruling> horizontalR = new List<Ruling>();
            List<Ruling> verticalR = new List<Ruling>();

            foreach (Ruling r in rulings)
            {
                if (r.IsHorizontal)
                {
                    horizontalR.Add(r);
                }
                else if (r.IsVertical)
                {
                    verticalR.Add(r);
                }
            }

            horizontalR = Ruling.CollapseOrientedRulings(horizontalR);
            verticalR = Ruling.CollapseOrientedRulings(verticalR);

            List<Cell> cells = FindCells(horizontalR, verticalR);
            List<TableRectangle> spreadsheetAreas = FindSpreadsheetsFromCells(cells.Cast<TableRectangle>().ToList());

            List<Table> spreadsheets = new List<Table>();
            foreach (TableRectangle area in spreadsheetAreas)
            {
                List<Cell> overlappingCells = new List<Cell>();
                foreach (Cell c in cells)
                {
                    if (c.Intersects(area))
                    {
                        c.SetTextElements(TextElement.MergeWords(page.GetText(c.BoundingBox)));
                        overlappingCells.Add(c);
                    }
                }

                List<Ruling> horizontalOverlappingRulings = new List<Ruling>();
                foreach (Ruling hr in horizontalR)
                {
                    if (area.IntersectsLine(hr))
                    {
                        horizontalOverlappingRulings.Add(hr);
                    }
                }

                List<Ruling> verticalOverlappingRulings = new List<Ruling>();
                foreach (Ruling vr in verticalR)
                {
                    if (area.IntersectsLine(vr))
                    {
                        verticalOverlappingRulings.Add(vr);
                    }
                }

                TableWithRulingLines t = new TableWithRulingLines(area, overlappingCells, horizontalOverlappingRulings, verticalOverlappingRulings, this);
                spreadsheets.Add(t);
            }

            Utils.Sort(spreadsheets, new TableRectangle.ILL_DEFINED_ORDER());
            return spreadsheets;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        public bool IsTabular(PageArea page)
        {
            // if there's no text at all on the page, it's not a table 
            // (we won't be able to do anything with it though)
            if (page.GetText().Count == 0)
            {
                return false;
            }

            // get minimal region of page that contains every character (in effect,
            // removes white "margins")
            PageArea minimalRegion = page.GetArea(Utils.Bounds(page.GetText().Select(t => t.BoundingBox).ToList()));

            List<Table> tables = new SpreadsheetExtractionAlgorithm().Extract(minimalRegion);
            if (tables.Count == 0)
            {
                return false;
            }

            Table table = tables[0];
            int rowsDefinedByLines = table.RowCount;
            int colsDefinedByLines = table.ColumnCount;

            tables = new BasicExtractionAlgorithm().Extract(minimalRegion);
            if (tables.Count == 0)
            {
                // TODO WHAT DO WE DO HERE?
                System.Diagnostics.Debug.Write("SpreadsheetExtractionAlgorithm.isTabular(): no table found.");
            }

            table = tables[0];
            int rowsDefinedWithoutLines = table.RowCount;
            int colsDefinedWithoutLines = table.ColumnCount;

            float ratio = (((float)colsDefinedByLines / colsDefinedWithoutLines) + ((float)rowsDefinedByLines / rowsDefinedWithoutLines)) / 2.0f;

            return ratio > MAGIC_HEURISTIC_NUMBER && ratio < (1 / MAGIC_HEURISTIC_NUMBER);
        }

        /// <summary>
        /// Find cells from horizontal and vertical ruling lines.
        /// </summary>
        /// <param name="horizontalRulingLines"></param>
        /// <param name="verticalRulingLines"></param>
        public static List<Cell> FindCells(IReadOnlyList<Ruling> horizontalRulingLines, IReadOnlyList<Ruling> verticalRulingLines)
        {
            List<Cell> cellsFound = new List<Cell>();
            SortedDictionary<PdfPoint, Ruling[]> intersectionPoints = Ruling.FindIntersections(horizontalRulingLines, verticalRulingLines);
            List<PdfPoint> intersectionPointsList = new List<PdfPoint>(intersectionPoints.Keys);
            intersectionPointsList.Sort(new POINT_COMPARER());

            for (int i = 0; i < intersectionPointsList.Count; i++)
            {
                PdfPoint topLeft = intersectionPointsList[i];
                Ruling[] hv = intersectionPoints[topLeft];
                bool doBreak = false;

                // CrossingPointsDirectlyBelow( topLeft );
                List<PdfPoint> xPoints = new List<PdfPoint>();
                // CrossingPointsDirectlyToTheRight( topLeft );
                List<PdfPoint> yPoints = new List<PdfPoint>();

                foreach (PdfPoint p in intersectionPointsList.SubList(i, intersectionPointsList.Count))
                {
                    if (p.X == topLeft.X && p.Y < topLeft.Y) //  p.Y > topLeft.Y
                    {
                        xPoints.Add(p);
                    }
                    if (p.Y == topLeft.Y && p.X > topLeft.X)
                    {
                        yPoints.Add(p);
                    }
                }

                //outer:
                foreach (PdfPoint xPoint in xPoints)
                {
                    if (doBreak) { break; }

                    // is there a vertical edge b/w topLeft and xPoint?
                    if (!hv[1].Equals(intersectionPoints[xPoint][1]))
                    {
                        continue;
                    }

                    foreach (PdfPoint yPoint in yPoints)
                    {
                        // is there an horizontal edge b/w topLeft and yPoint ?
                        if (!hv[0].Equals(intersectionPoints[yPoint][0]))
                        {
                            continue;
                        }

                        PdfPoint btmRight = new PdfPoint(yPoint.X, xPoint.Y);
                        if (intersectionPoints.ContainsKey(btmRight)
                                && intersectionPoints[btmRight][0].Equals(intersectionPoints[xPoint][0])
                                && intersectionPoints[btmRight][1].Equals(intersectionPoints[yPoint][1]))
                        {
                            cellsFound.Add(new Cell(topLeft, btmRight));
                            doBreak = true;
                            //break outer;
                            break;
                        }
                    }
                }
            }

            // TODO create cells for vertical ruling lines with aligned endpoints at the top/bottom of a grid 
            // that aren't connected with an horizontal ruler?
            // see: https://github.com/jazzido/tabula-extractor/issues/78#issuecomment-41481207

            return cellsFound;
        }

        /// <summary>
        /// Find spreadsheets areas from cells.
        /// <para>Based on O'Rourke's `Uniqueness of orthogonal connect-the-dots`.</para>
        /// </summary>
        /// <param name="cells"></param>
        public static List<TableRectangle> FindSpreadsheetsFromCells(List<TableRectangle> cells)
        {
            // via: http://stackoverflow.com/questions/13746284/merging-multiple-adjacent-rectangles-into-one-polygon
            List<TableRectangle> rectangles = new List<TableRectangle>();
            HashSet<PdfPoint> pointSet = new HashSet<PdfPoint>();
            Dictionary<PdfPoint, PdfPoint> edgesH = new Dictionary<PdfPoint, PdfPoint>();
            Dictionary<PdfPoint, PdfPoint> edgesV = new Dictionary<PdfPoint, PdfPoint>();
            int i = 0;

            cells = new List<TableRectangle>(new HashSet<TableRectangle>(cells));

            Utils.Sort(cells, new TableRectangle.ILL_DEFINED_ORDER());

            foreach (TableRectangle cell in cells)
            {
                foreach (PdfPoint pt in cell.Points)
                {
                    if (pointSet.Contains(pt))
                    {
                        pointSet.Remove(pt); // shared vertex, remove it
                    }
                    else
                    {
                        pointSet.Add(pt);
                    }
                }
            }

            // X first sort
            List<PdfPoint> pointsSortX = new List<PdfPoint>(pointSet);
            pointsSortX.Sort(new X_FIRST_POINT_COMPARER());

            // Y first sort
            List<PdfPoint> pointsSortY = new List<PdfPoint>(pointSet);
            pointsSortY.Sort(new POINT_COMPARER());

            while (i < pointSet.Count)
            {
                float currY = (float)pointsSortY[i].Y;
                while (i < pointSet.Count && Utils.Feq(pointsSortY[i].Y, currY))
                {
                    edgesH[pointsSortY[i]] = pointsSortY[i + 1];
                    edgesH[pointsSortY[i + 1]] = pointsSortY[i];
                    i += 2;
                }
            }

            i = 0;
            while (i < pointSet.Count)
            {
                float currX = (float)pointsSortX[i].X;
                while (i < pointSet.Count && Utils.Feq(pointsSortX[i].X, currX))
                {
                    edgesV[pointsSortX[i]] = pointsSortX[i + 1];
                    edgesV[pointsSortX[i + 1]] = pointsSortX[i];
                    i += 2;
                }
            }

            // Get all the polygons
            List<List<PolygonVertex>> polygons = new List<List<PolygonVertex>>();
            PdfPoint nextVertex;
            while (edgesH.Count != 0)
            {
                List<PolygonVertex> polygon = new List<PolygonVertex>();
                PdfPoint first = edgesH.Keys.First();
                polygon.Add(new PolygonVertex(first, Direction.HORIZONTAL));
                edgesH.Remove(first);

                while (true)
                {
                    PolygonVertex curr = polygon[polygon.Count - 1];
                    PolygonVertex lastAddedVertex;
                    if (curr.direction == Direction.HORIZONTAL)
                    {
                        nextVertex = edgesV[curr.point];
                        edgesV.Remove(curr.point);
                        lastAddedVertex = new PolygonVertex(nextVertex, Direction.VERTICAL);
                        polygon.Add(lastAddedVertex);
                    }
                    else
                    {
                        nextVertex = edgesH[curr.point];
                        edgesH.Remove(curr.point);
                        lastAddedVertex = new PolygonVertex(nextVertex, Direction.HORIZONTAL);
                        polygon.Add(lastAddedVertex);
                    }

                    if (lastAddedVertex.Equals(polygon[0]))
                    {
                        // closed polygon
                        polygon.RemoveAt(polygon.Count - 1);
                        break;
                    }
                }

                foreach (PolygonVertex vertex in polygon)
                {
                    edgesH.Remove(vertex.point);
                    edgesV.Remove(vertex.point);
                }
                polygons.Add(polygon);
            }

            // calculate axis-aligned minimum area rectangles for each found polygon
            foreach (List<PolygonVertex> poly in polygons)
            {
                double top = double.MinValue;       //java.lang.Float.MAX_VALUE;
                double left = double.MaxValue;      //java.lang.Float.MAX_VALUE;
                double bottom = double.MaxValue;    //java.lang.Float.MIN_VALUE;
                double right = double.MinValue;     //java.lang.Float.MIN_VALUE;
                foreach (PolygonVertex pt in poly)
                {
                    top = Math.Max(top, pt.point.Y); // Min
                    left = Math.Min(left, pt.point.X);
                    bottom = Math.Min(bottom, pt.point.Y); // Max
                    right = Math.Max(right, pt.point.X);
                }
                rectangles.Add(new TableRectangle(new PdfRectangle(left, bottom, right, top))); // top, left, right - left, bottom - top));
            }

            return rectangles;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "lattice";
        }

        private enum Direction
        {
            HORIZONTAL,
            VERTICAL
        }

        private class PolygonVertex
        {
            public PdfPoint point;
            public Direction direction;

            public PolygonVertex(PdfPoint point, Direction direction)
            {
                this.direction = direction;
                this.point = point;
            }

            public override bool Equals(object other)
            {
                if (other is PolygonVertex o)
                {
                    return this.point.Equals(o.point);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return this.point.GetHashCode();
            }

            public override string ToString()
            {
                return $"{this.GetType().Name}[point={this.point},direction={this.direction}]";
            }
        }
    }
}
