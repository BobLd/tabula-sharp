using System.Collections.Generic;

namespace Tabula
{
    public sealed class TextStripperResult
    {
        public TextStripperResult(double minCharWidth, double minCharHeight, List<TextElement> textElements, RectangleSpatialIndex<TextElement> spatialIndex)
        {
            this.MinCharWidth = minCharWidth;
            this.MinCharHeight = minCharHeight;
            this.TextElements = textElements;
            this.SpatialIndex = spatialIndex;
        }

        public double MinCharWidth { get; }

        public double MinCharHeight { get; }

        public List<TextElement> TextElements { get; }

        public RectangleSpatialIndex<TextElement> SpatialIndex { get; }
    }
}
