using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;

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
        private const int GRAYSCALE_INTENSITY_THRESHOLD = 25;
        private const int HORIZONTAL_EDGE_WIDTH_MINIMUM = 50;
        private const int VERTICAL_EDGE_HEIGHT_MINIMUM = 10;
        private const int CELL_CORNER_DISTANCE_MAXIMUM = 10;
        private const float POINT_SNAP_DISTANCE_THRESHOLD = 8f;
        private const float TABLE_PADDING_AMOUNT = 1.0f;
        private const int REQUIRED_TEXT_LINES_FOR_EDGE = 4;
        private const int REQUIRED_CELLS_FOR_TABLE = 4;
        private const float IDENTICAL_TABLE_OVERLAP_RATIO = 0.9f;

        /// <summary>
        /// Helper class that encapsulates a text edge
        /// </summary>
        private class TextEdge
        {
            // types of text edges
            public static int LEFT = 0;
            public static int MID = 1;
            public static int RIGHT = 2;
            public static int NUM_TYPES = 3;

            public int intersectingTextRowCount;

            public PdfLine line { get; }

            public TextEdge(float x1, float y1, float x2, float y2)
            {
                line = new PdfLine(x1, y1, x2, y2); // super(x1, y1, x2, y2);
                this.intersectingTextRowCount = 0;
            }
        }

        /// <summary>
        /// Helper container for all text edges on a page
        /// </summary>
        private class TextEdges : List<List<TextEdge>>
        {
            public TextEdges(List<TextEdge> leftEdges, List<TextEdge> midEdges, List<TextEdge> rightEdges) : base(3)
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
        private  class RelevantEdges
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
            // get horizontal & vertical lines
            // we get these from an image of the PDF and not the PDF itself because sometimes there are invisible PDF
            // instructions that are interpreted incorrectly as visible elements - we really want to capture what a
            // person sees when they look at the PDF
            throw new NotImplementedException();
        }
    }
}
