using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UglyToad.PdfPig.Core;

namespace Tabula.Extractors
{
    //https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/extractors/SpreadsheetExtractionAlgorithm.java
    public class SpreadsheetExtractionAlgorithm : ExtractionAlgorithm
    {
        private static double MAGIC_HEURISTIC_NUMBER = 0.65;

        public class POINT_COMPARATOR : IComparer<PdfPoint>
        {
            public int Compare([AllowNull] PdfPoint arg0, [AllowNull] PdfPoint arg1)
            {
                int rv = 0;
                double arg0X = Utils.round(arg0.X, 2);
                double arg0Y = Utils.round(arg0.Y, 2);
                double arg1X = Utils.round(arg1.X, 2);
                double arg1Y = Utils.round(arg1.Y, 2);

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

        public class X_FIRST_POINT_COMPARATOR : IComparer<PdfPoint>
        {
            public int Compare([AllowNull] PdfPoint arg0, [AllowNull] PdfPoint arg1)
            {
                int rv = 0;
                double arg0X = Utils.round(arg0.X, 2);
                double arg0Y = Utils.round(arg0.Y, 2);
                double arg1X = Utils.round(arg1.X, 2);
                double arg1Y = Utils.round(arg1.Y, 2);

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

        public List<Table> extract(PageArea page)
        {
            return extract(page, page.getRulings());
        }

        /// <summary>
        /// Extract a list of Table from page using rulings as separators
        /// </summary>
        /// <param name="page"></param>
        /// <param name="rulings"></param>
        /// <returns></returns>
        public List<Table> extract(PageArea page, List<Ruling> rulings)
        {
            // split rulings into horizontal and vertical
            List<Ruling> horizontalR = new List<Ruling>(), verticalR = new List<Ruling>();

            foreach (Ruling r in rulings)
            {
                if (r.horizontal())
                {
                    horizontalR.Add(r);
                }
                else if (r.vertical())
                {
                    verticalR.Add(r);
                }
            }

            horizontalR = Ruling.collapseOrientedRulings(horizontalR);
            verticalR = Ruling.collapseOrientedRulings(verticalR);

            List<Cell> cells = findCells(horizontalR, verticalR);
            List<TableRectangle> spreadsheetAreas = findSpreadsheetsFromCells(cells.Cast<TableRectangle>().ToList());

            List<Table> spreadsheets = new List<Table>();
            foreach (TableRectangle area in spreadsheetAreas)
            {

                List<Cell> overlappingCells = new List<Cell>();
                foreach (Cell c in cells)
                {
                    if (c.intersects(area))
                    {
                        c.setTextElements(TextElement.mergeWords(page.getText(c.BoundingBox)));
                        overlappingCells.Add(c);
                    }
                }

                List<Ruling> horizontalOverlappingRulings = new List<Ruling>();
                foreach (Ruling hr in horizontalR)
                {
                    if (area.intersectsLine(hr))
                    {
                        horizontalOverlappingRulings.Add(hr);
                    }
                }

                List<Ruling> verticalOverlappingRulings = new List<Ruling>();
                foreach (Ruling vr in verticalR)
                {
                    if (area.intersectsLine(vr))
                    {
                        verticalOverlappingRulings.Add(vr);
                    }
                }

                TableWithRulingLines t = new TableWithRulingLines(area, overlappingCells, horizontalOverlappingRulings, verticalOverlappingRulings, this);
                spreadsheets.Add(t);
            }

            Utils.sort(spreadsheets, new TableRectangle.ILL_DEFINED_ORDER());
            return spreadsheets;
        }

        public bool isTabular(PageArea page)
        {
            // if there's no text at all on the page, it's not a table 
            // (we won't be able to do anything with it though)
            if (page.getText().Count == 0)
            {
                return false;
            }

            // get minimal region of page that contains every character (in effect,
            // removes white "margins")
            PageArea minimalRegion = page.getArea(Utils.bounds(page.getText().Select(t => t.BoundingBox).ToList()));

            List<Table> tables = new SpreadsheetExtractionAlgorithm().extract(minimalRegion);
            if (tables.Count == 0)
            {
                return false;
            }
            Table table = tables[0];
            int rowsDefinedByLines = table.getRowCount();
            int colsDefinedByLines = table.getColCount();

            tables = new BasicExtractionAlgorithm().extract(minimalRegion);
            if (tables.Count == 0)
            {
                // TODO WHAT DO WE DO HERE?
            }
            table = tables[0];
            int rowsDefinedWithoutLines = table.getRowCount();
            int colsDefinedWithoutLines = table.getColCount();

            float ratio = (((float)colsDefinedByLines / colsDefinedWithoutLines) + ((float)rowsDefinedByLines / rowsDefinedWithoutLines)) / 2.0f;

            return ratio > MAGIC_HEURISTIC_NUMBER && ratio < (1 / MAGIC_HEURISTIC_NUMBER);
        }

        public static List<Cell> findCells(List<Ruling> horizontalRulingLines, List<Ruling> verticalRulingLines)
        {
            List<Cell> cellsFound = new List<Cell>();
            SortedDictionary<PdfPoint, Ruling[]> intersectionPoints = Ruling.findIntersections(horizontalRulingLines, verticalRulingLines);
            List<PdfPoint> intersectionPointsList = new List<PdfPoint>(intersectionPoints.Keys);
            intersectionPointsList.Sort(new POINT_COMPARATOR());
            bool doBreak = false;

            for (int i = 0; i < intersectionPointsList.Count; i++)
            {
                PdfPoint topLeft = intersectionPointsList[i];
                Ruling[] hv = intersectionPoints[topLeft];
                doBreak = false;

                // CrossingPointsDirectlyBelow( topLeft );
                List<PdfPoint> xPoints = new List<PdfPoint>();
                // CrossingPointsDirectlyToTheRight( topLeft );
                List<PdfPoint> yPoints = new List<PdfPoint>();

                foreach (PdfPoint p in intersectionPointsList.subList(i, intersectionPointsList.Count))
                {
                    if (p.X == topLeft.X && p.Y > topLeft.Y)
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

        public static List<TableRectangle> findSpreadsheetsFromCells(List<TableRectangle> cells)
        {
            // via: http://stackoverflow.com/questions/13746284/merging-multiple-adjacent-rectangles-into-one-polygon
            List<TableRectangle> rectangles = new List<TableRectangle>();
            HashSet<PdfPoint> pointSet = new HashSet<PdfPoint>();
            Dictionary<PdfPoint, PdfPoint> edgesH = new Dictionary<PdfPoint, PdfPoint>();
            Dictionary<PdfPoint, PdfPoint> edgesV = new Dictionary<PdfPoint, PdfPoint>();
            int i = 0;

            cells = new List<TableRectangle>(new HashSet<TableRectangle>(cells));

            Utils.sort(cells, new TableRectangle.ILL_DEFINED_ORDER());

            foreach (TableRectangle cell in cells)
            {
                foreach (PdfPoint pt in cell.getPoints())
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
            pointsSortX.Sort(new X_FIRST_POINT_COMPARATOR());

            // Y first sort
            List<PdfPoint> pointsSortY = new List<PdfPoint>(pointSet);
            pointsSortY.Sort(new POINT_COMPARATOR());

            while (i < pointSet.Count)
            {
                float currY = (float)pointsSortY[i].Y;
                while (i < pointSet.Count && Utils.feq(pointsSortY[i].Y, currY))
                {
                    //edgesH.put(pointsSortY.get(i), pointsSortY.get(i + 1));
                    //edgesH.put(pointsSortY.get(i + 1), pointsSortY.get(i));
                    edgesH[pointsSortY[i]] = pointsSortY[i + 1];
                    edgesH[pointsSortY[i + 1]] = pointsSortY[i];
                    i += 2;
                }
            }

            i = 0;
            while (i < pointSet.Count)
            {
                float currX = (float)pointsSortX[i].X;
                while (i < pointSet.Count && Utils.feq(pointsSortX[i].X, currX))
                {
                    //edgesV.put(pointsSortX.get(i), pointsSortX.get(i + 1));
                    //edgesV.put(pointsSortX.get(i + 1), pointsSortX.get(i));
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
                PdfPoint first = edgesH.Keys.First(); ////.keySet().iterator().next();
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

            // calculate grid-aligned minimum area rectangles for each found polygon
            foreach (List<PolygonVertex> poly in polygons)
            {
                double top = double.MaxValue;//java.lang.Float.MAX_VALUE;
                double left = double.MaxValue;//java.lang.Float.MAX_VALUE;
                double bottom = double.MinValue;//java.lang.Float.MIN_VALUE;
                double right = double.MinValue;//java.lang.Float.MIN_VALUE;
                foreach (PolygonVertex pt in poly)
                {
                    top = Math.Min(top, pt.point.Y);
                    left = Math.Min(left, pt.point.X);
                    bottom = Math.Max(bottom, pt.point.Y);
                    right = Math.Max(right, pt.point.X);
                }
                rectangles.Add(new TableRectangle(top, left, right - left, bottom - top));
            }

            return rectangles;
        }

        public override string ToString()
        {
            return "lattice";
        }

        private enum Direction
        {
            HORIZONTAL,
            VERTICAL
        }

        class PolygonVertex
        {
            public PdfPoint point;
            public Direction direction;

            public PolygonVertex(PdfPoint point, Direction direction)
            {
                this.direction = direction;
                this.point = point;
            }

            public override bool Equals(Object other)
            {
                if (this == other)
                    return true;
                if (!(other is PolygonVertex)) return false;
                return this.point.Equals(((PolygonVertex)other).point);
            }

            public override int GetHashCode()
            {
                return this.point.GetHashCode();
            }

            public override string ToString()
            {
                return $"{this.GetType().Name}[point={this.point},direction={this.direction}]";
            }

            /*
            public String toString()
            {
                return $"{this.GetType().Name}[point={this.point},direction={this.direction}]";
            }
            */
        }
    }
}
