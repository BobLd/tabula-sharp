using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestRectangleSpatialIndex
	{
		[Fact]
		public void TestIntersects()
		{
			TableRectangle r = new TableRectangle(new PdfRectangle());

			RectangleSpatialIndex<TableRectangle> rSpatialIndex = new RectangleSpatialIndex<TableRectangle>();
			rSpatialIndex.Add(r);

			Assert.True(rSpatialIndex.Intersects(r).Count > 0);
		}
	}
}
