using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tabula.Tests
{
	public class TestRectangleSpatialIndex
	{

		[Fact]
		public void testIntersects()
		{

			TableRectangle r = new TableRectangle(0, 0, 0, 0);

			RectangleSpatialIndex<TableRectangle> rSpatialIndex = new RectangleSpatialIndex<TableRectangle>();
			rSpatialIndex.add(r);

			Assert.True(rSpatialIndex.intersects(r).Count > 0);

		}
	}
}
