using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestRectangleSpatialIndex
	{

		[Fact]
		public void testIntersects()
		{
			TableRectangle r = new TableRectangle(new PdfRectangle());

			RectangleSpatialIndex<TableRectangle> rSpatialIndex = new RectangleSpatialIndex<TableRectangle>();
			rSpatialIndex.add(r);

			Assert.True(rSpatialIndex.intersects(r).Count > 0);
		}
	}
}
