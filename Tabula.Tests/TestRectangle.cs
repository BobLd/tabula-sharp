using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestRectangle
	{
		[Fact]
		public void testCompareEqualsRectangles()
		{
			TableRectangle first = new TableRectangle();
			TableRectangle second = new TableRectangle();

			Assert.True(first.Equals(second));
			Assert.True(second.Equals(first));
		}

		[Fact]
		public void testCompareAlignedVerticalRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); //0f, 10f, 10f, 10f));
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 10, 10, 20));//0f, 20f, 10f, 10f));

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void testCompareAlignedHorizontalRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(10, 0, 10, 10));//10f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(20, 0, 30, 10)); //20f, 0f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void testCompareVerticalOverlapRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(0, 0, 10, 5));//5f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(10, 0, 20, 10));//0f, 10f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void testCompareVerticalOverlapLessThresholdRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(10, 0, 20, 10));//0f, 10f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 9.8, 10, 19.8)); //9.8f, 0f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void testQuickSortOneUpperThanOther()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(72.72, 175.72, 72.72 + 1.67, 175.72 + 1.52)); //175.72f, 72.72f, 1.67f, 1.52f); //, (Comma after AARON)
			Assert.Equal(1.67, lower.width, 2);
			Assert.Equal(1.52, lower.height, 2);

			TableRectangle upper = new TableRectangle(new PdfRectangle(161.16, 169.21, 161.16 + 4.33, 169.21 + 4.31));//169.21f, 161.16f, 4.33f, 4.31f); // R (REGIONAL PULMONARY)
			Assert.Equal(4.33, upper.width, 2);
			Assert.Equal(4.31, upper.height, 2);

			Assert.True(lower.CompareTo(upper) > 0);
		}

		[Fact]
		public void testQuickSortRectangleList()
		{
			// Testing wrong sorting
			// Expected: AARON, JOSHUA, N
			// but was: AARON JOSHUA N , ,
			TableRectangle first = new TableRectangle(new PdfRectangle(51.47999954223633, 172.92999267578125, 51.47999954223633 + 4.0, 172.92999267578125 + 4.309999942779541)); // 172.92999267578125f, 51.47999954223633f, 4.0f, 4.309999942779541f); //A
			Assert.Equal(4, first.width);
			Assert.Equal(4.309999942779541, first.height);

			TableRectangle second = new TableRectangle(new PdfRectangle(72.72000122070312, 175.72000122070312, 72.72000122070312 + 1.6699999570846558, 175.72000122070312 + 1.5199999809265137)); //  175.72000122070312f, 72.72000122070312f, 1.6699999570846558f, 1.5199999809265137f); //,
			Assert.Equal(1.6699999570846558, second.width);
			Assert.Equal(1.5199999809265137, second.height);

			TableRectangle third = new TableRectangle(new PdfRectangle(96.36000061035156, 172.92999267578125, 96.36000061035156 + 4.0, 172.92999267578125 + 4.309999942779541));//172.92999267578125f, 96.36000061035156f, 4.0f, 4.309999942779541f); //A
			Assert.Equal(4.0, third.width);
			Assert.Equal(4.309999942779541, third.height);

			TableRectangle fourth = new TableRectangle(new PdfRectangle(100.31999969482422, 175.72000122070312, 100.31999969482422 + 1.6699999570846558, 175.72000122070312 + 1.5199999809265137));//175.72000122070312f, 100.31999969482422f, 1.6699999570846558f, 1.5199999809265137f); //,
			Assert.Equal(1.6699999570846558, fourth.width);
			Assert.Equal(1.5199999809265137, fourth.height);

			TableRectangle fifth = new TableRectangle(new PdfRectangle(103.68000030517578, 172.92999267578125, 103.68000030517578 + 4.329999923706055, 172.92999267578125 + 4.309999942779541));//172.92999267578125f, 103.68000030517578f, 4.329999923706055f, 4.309999942779541f); //N
			Assert.Equal(4.329999923706055, fifth.width);
			Assert.Equal(4.309999942779541, fifth.height);

			TableRectangle sixth = new TableRectangle(new PdfRectangle(161.16000366210938, 169.2100067138672, 161.16000366210938 + 4.329999923706055, 169.2100067138672 + 4.309999942779541)); //169.2100067138672f, 161.16000366210938f, 4.329999923706055f, 4.309999942779541f); //R
			Assert.Equal(4.329999923706055, sixth.width);
			Assert.Equal(4.309999942779541, sixth.height);

            List<TableRectangle> expectedList = new List<TableRectangle>
            {
                first,
                sixth,
                second,
                third,
                fourth,
                fifth
            };
            List<TableRectangle> toSortList = new List<TableRectangle>
            {
                sixth,
                second,
                third,
                fifth,
                first,
                fourth
            };

			Utils.sort(toSortList, new TableRectangle.ILL_DEFINED_ORDER()); // toSortList.Sort(new TableRectangle.ILL_DEFINED_ORDER()); //Collections.sort(toSortList, TableRectangle.ILL_DEFINED_ORDER);
			Assert.Equal(expectedList, toSortList);
		}

		[Fact]
		public void testGetVerticalOverlapShouldReturnZero()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(0, 10, 10, 20)); //10f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 20, 10, 30)); //20f, 0f, 10f, 10f);

			double overlap = lower.verticalOverlap(upper);

			Assert.Equal(0, overlap, 0);
			Assert.True(!lower.verticallyOverlaps(upper));
			Assert.Equal(0, lower.verticalOverlapRatio(upper), 0);
			Assert.Equal(0, lower.overlapRatio(upper), 0);
		}

		[Fact]
		public void testGetVerticalOverlapShouldReturnMoreThanZero()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(10, 15, 20, 25)); //15f, 10f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 20, 10, 30)); //20f, 0f, 10f, 10f);

			double overlap = lower.verticalOverlap(upper);

			Assert.Equal(5, overlap, 0);
			Assert.True(lower.verticallyOverlaps(upper));
			Assert.Equal(0.5, lower.verticalOverlapRatio(upper), 0);
			Assert.Equal(0, lower.overlapRatio(upper), 0);
		}

		[Fact]
		public void testGetHorizontalOverlapShouldReturnZero()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); //0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(10, 10, 20, 20)); //10f, 10f, 10f, 10f);

			Assert.True(!one.horizontallyOverlaps(two));
			Assert.Equal(0f, one.overlapRatio(two), 0);
		}

		[Fact]
		public void testGetHorizontalOverlapShouldReturnMoreThanZero()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); //0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(5, 0, 15, 10)); //10f, 5f, 10f, 10f);

			Assert.True(one.horizontallyOverlaps(two));
			Assert.Equal(5f, one.horizontalOverlap(two), 0);
			Assert.Equal(0f, one.overlapRatio(two), 0);
		}

		[Fact]
		public void testGetOverlapShouldReturnMoreThanZero()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); // 0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(5, 5, 15, 15)); //5f, 5f, 10f, 10f);

			Assert.True(one.horizontallyOverlaps(two));
			Assert.True(one.verticallyOverlaps(two));
			Assert.Equal(5f, one.horizontalOverlap(two), 0);
			Assert.Equal(5f, one.verticalOverlap(two), 0);
			Assert.Equal(25f / 175, one.overlapRatio(two), 0);
		}

		[Fact]
		public void testMergeNoOverlappingRectangles()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); //0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(10, 0, 20, 10)); //0f, 10f, 10f, 10f);

			one.merge(two);

			Assert.Equal(20f, one.getWidth(), 0);
			Assert.Equal(10f, one.getHeight(), 0);
			Assert.Equal(0f, one.getLeft(), 0);
			Assert.Equal(10, one.getTop(), 0);      //0f, one.getTop(), 0);
			Assert.Equal(0, one.getBottom(), 0);    //10f, one.getBottom(), 0);
			Assert.Equal(20f * 10f, one.getArea(), 0);
		}

		[Fact]
		public void testMergeOverlappingRectangles()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10));//0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(5, 5, 15, 15));//5f, 5f, 10f, 10f);

			one.merge(two);

			Assert.Equal(15f, one.getWidth(), 0);
			Assert.Equal(15f, one.getHeight(), 0);
			Assert.Equal(0f, one.getLeft(), 0);
			Assert.Equal(0f, one.getBottom(), 0); // one.getTop()
			Assert.Equal(15, one.getTop(), 0);
		}

		[Fact]
		public void testRectangleGetPoints()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(20, 10, 50, 50)); //10f, 20f, 30f, 40f);
			Assert.Equal(30, one.width);
			Assert.Equal(40, one.height);

			PdfPoint[] points = one.getPoints();

			PdfPoint[] expectedPoints = new PdfPoint[]
			{
				new PdfPoint(20, 10),
				new PdfPoint(50, 10),
				new PdfPoint(50, 50),
				new PdfPoint(20, 50)
			};

			Assert.Equal(expectedPoints, points);
		}

		[Fact]
		public void testGetBoundingBox()
		{
			List<TableRectangle> rectangles = new List<TableRectangle>
			{
				new TableRectangle(new PdfRectangle(0, 0, 10, 10)),  //0f, 0f, 10f, 10f)
                new TableRectangle(new PdfRectangle(30, 10, 40, 20)) //20f, 30f, 10f, 10f)
			};

			TableRectangle boundingBoxOf = TableRectangle.boundingBoxOf(rectangles);

			Assert.Equal(new TableRectangle(new PdfRectangle(0, 0, 40, 20)), boundingBoxOf); // 0f, 0f, 40f, 30f)
		}

		[Fact]//(Skip = "Comparison is not transitive. Transitivity needs to be implemented.")]
		public void testTransitiveComparison1()
		{
			// +-------+
			// |       |
			// |   a   | +-------+
			// |       | |       |
			// +-------+ |   b   | +-------+
			//           |       | |       |
			//           +-------+ |   c   |
			//                     |       |
			//                     +-------+
			TableRectangle a = new TableRectangle(new PdfRectangle(0, 2, 2, 4));
			TableRectangle b = new TableRectangle(new PdfRectangle(1, 1, 3, 3));
			TableRectangle c = new TableRectangle(new PdfRectangle(2, 0, 4, 2));
			Assert.True(a.CompareTo(b) < 0);
			Assert.True(b.CompareTo(c) < 0);
			Assert.True(a.CompareTo(c) < 0);
		}

		[Fact(Skip = "Comparison is not transitive. Transitivity needs to be implemented.")]
		public void testTransitiveComparison2()
		{
			// need to rewrite



			//                     +-------+
			//                     |       |
			//           +-------+ |   C   |
			//           |       | |       |
			// +-------+ |   B   | +-------+
			// |       | |       |
			// |   A   | +-------+
			// |       |
			// +-------+
			TableRectangle c = new TableRectangle(new PdfRectangle(0, 2, 2, 4)); // 2, 0, 2, 2); // a
			TableRectangle b = new TableRectangle(new PdfRectangle(1, 1, 3, 3)); // 1, 1, 2, 2);
			TableRectangle a = new TableRectangle(new PdfRectangle(2, 0, 4, 2)); // 0, 2, 2, 2); // c
			Assert.True(a.CompareTo(b) < 0);
			Assert.True(b.CompareTo(c) < 0);
			Assert.True(a.CompareTo(c) < 0);
		}

		[Fact(Skip = "Comparison is not transitive. Needs to be implemented.")]
		public void testWellDefinedComparison1()
		{
			/*
			TableRectangle a = new TableRectangle(2, 0, 2, 2);
			TableRectangle b = new TableRectangle(1, 1, 2, 2);
			TableRectangle c = new TableRectangle(0, 2, 2, 2);
			List<TableRectangle> l1 = new List<TableRectangle>() { b, a, c };
			List<TableRectangle> l2 = new List<TableRectangle>() { c, b, a };
			QuickSort.sort(l1, TableRectangle.ILL_DEFINED_ORDER);
			QuickSort.sort(l2, TableRectangle.ILL_DEFINED_ORDER);
			Assert.Equal(l1.get(0), l2.get(0));
			Assert.Equal(l1.get(1), l2.get(1));
			Assert.Equal(l1.get(2), l2.get(2));
			*/
		}
	}
}
