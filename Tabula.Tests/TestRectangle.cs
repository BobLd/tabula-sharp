using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestRectangle
	{
		[Fact]
		public void TestCompareEqualsRectangles()
		{
			TableRectangle first = new TableRectangle();
			TableRectangle second = new TableRectangle();

			Assert.True(first.Equals(second));
			Assert.True(second.Equals(first));
		}

		[Fact]
		public void TestCompareAlignedHorizontalRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(10, 0, 20, 10)); //0f, 10f, 10f, 10f));
			TableRectangle upper = new TableRectangle(new PdfRectangle(20, 0, 30, 10));//0f, 20f, 10f, 10f));

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void TestCompareAlignedVerticalRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(0, 10, 10, 20)); //10f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 20, 10, 30)); //20f, 0f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) > 0);  // upper precedes lower (reading order) // was < 0
		}

		[Fact]
		public void TestCompareVerticalOverlapRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(0, 0, 10, 5));//5f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(10, 0, 20, 10));//0f, 10f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void TestCompareVerticalOverlapLessThresholdRectangle()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(10, 0, 20, 10));//0f, 10f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 9.8, 10, 19.8)); //9.8f, 0f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) > 0); // upper precedes lower (reading order) // was < 0
		}

		[Fact]
		public void TestQuickSortOneUpperThanOther()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(72.72, 175.72, 72.72 + 1.67, 175.72 + 1.52)); //175.72f, 72.72f, 1.67f, 1.52f); //, (Comma after AARON)
			Assert.Equal(1.67, lower.Width, 2);
			Assert.Equal(1.52, lower.Height, 2);

			TableRectangle upper = new TableRectangle(new PdfRectangle(161.16, 169.21, 161.16 + 4.33, 169.21 + 4.31));//169.21f, 161.16f, 4.33f, 4.31f); // R (REGIONAL PULMONARY)
			Assert.Equal(4.33, upper.Width, 2);
			Assert.Equal(4.31, upper.Height, 2);

			Assert.True(lower.CompareTo(upper) < 0); // > 0
		}

		[Fact]
		public void TestQuickSortRectangleList()
		{
			// Testing wrong sorting
			// Expected: AARON, JOSHUA, N
			// but was: AARON JOSHUA N , ,
			TableRectangle first = new TableRectangle(new PdfRectangle(51.47999954223633, 172.92999267578125, 51.47999954223633 + 4.0, 172.92999267578125 + 4.309999942779541)); // 172.92999267578125f, 51.47999954223633f, 4.0f, 4.309999942779541f); //A
			Assert.Equal(4, first.Width);
			Assert.Equal(4.309999942779541, first.Height);

			TableRectangle second = new TableRectangle(new PdfRectangle(72.72000122070312, 175.72000122070312, 72.72000122070312 + 1.6699999570846558, 175.72000122070312 + 1.5199999809265137)); //175.72000122070312f, 72.72000122070312f, 1.6699999570846558f, 1.5199999809265137f); //,
			Assert.Equal(1.6699999570846558, second.Width);
			Assert.Equal(1.5199999809265137, second.Height);

			TableRectangle third = new TableRectangle(new PdfRectangle(96.36000061035156, 172.92999267578125, 96.36000061035156 + 4.0, 172.92999267578125 + 4.309999942779541)); //172.92999267578125f, 96.36000061035156f, 4.0f, 4.309999942779541f); //A
			Assert.Equal(4.0, third.Width);
			Assert.Equal(4.309999942779541, third.Height);

			TableRectangle fourth = new TableRectangle(new PdfRectangle(100.31999969482422, 175.72000122070312, 100.31999969482422 + 1.6699999570846558, 175.72000122070312 + 1.5199999809265137)); //175.72000122070312f, 100.31999969482422f, 1.6699999570846558f, 1.5199999809265137f); //,
			Assert.Equal(1.6699999570846558, fourth.Width);
			Assert.Equal(1.5199999809265137, fourth.Height);

			TableRectangle fifth = new TableRectangle(new PdfRectangle(103.68000030517578, 172.92999267578125, 103.68000030517578 + 4.329999923706055, 172.92999267578125 + 4.309999942779541)); //172.92999267578125f, 103.68000030517578f, 4.329999923706055f, 4.309999942779541f); //N
			Assert.Equal(4.329999923706055, fifth.Width);
			Assert.Equal(4.309999942779541, fifth.Height);

			TableRectangle sixth = new TableRectangle(new PdfRectangle(161.16000366210938, 169.2100067138672, 161.16000366210938 + 4.329999923706055, 169.2100067138672 + 4.309999942779541)); //169.2100067138672f, 161.16000366210938f, 4.329999923706055f, 4.309999942779541f); //R
			Assert.Equal(4.329999923706055, sixth.Width);
			Assert.Equal(4.309999942779541, sixth.Height);

			List<TableRectangle> expectedList = new List<TableRectangle>
			{
                first,
                //sixth,
                second,
                third,
                fourth,
                fifth,
				sixth, // put here, follows reading order
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

			Utils.Sort(toSortList, new TableRectangle.ILL_DEFINED_ORDER()); //Collections.sort(toSortList, TableRectangle.ILL_DEFINED_ORDER);
			Assert.Equal(expectedList, toSortList);
		}

		[Fact]
		public void TestGetVerticalOverlapShouldReturnZero()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(0, 10, 10, 20)); //10f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 20, 10, 30)); //20f, 0f, 10f, 10f);

			double overlap = lower.VerticalOverlap(upper);

			Assert.Equal(0, overlap, 0);
			Assert.True(!lower.VerticallyOverlaps(upper));
			Assert.Equal(0, lower.VerticalOverlapRatio(upper), 0);
			Assert.Equal(0, lower.OverlapRatio(upper), 0);
		}

		[Fact]
		public void TestGetVerticalOverlapShouldReturnMoreThanZero()
		{
			TableRectangle lower = new TableRectangle(new PdfRectangle(10, 15, 20, 25)); //15f, 10f, 10f, 10f);
			TableRectangle upper = new TableRectangle(new PdfRectangle(0, 20, 10, 30)); //20f, 0f, 10f, 10f);

			double overlap = lower.VerticalOverlap(upper);

			Assert.Equal(5, overlap, 0);
			Assert.True(lower.VerticallyOverlaps(upper));
			Assert.Equal(0.5, lower.VerticalOverlapRatio(upper), 0);
			Assert.Equal(0, lower.OverlapRatio(upper), 0);
		}

		[Fact]
		public void TestGetHorizontalOverlapShouldReturnZero()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); //0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(10, 10, 20, 20)); //10f, 10f, 10f, 10f);

			Assert.True(!one.HorizontallyOverlaps(two));
			Assert.Equal(0f, one.OverlapRatio(two), 0);
		}

		[Fact]
		public void TestGetHorizontalOverlapShouldReturnMoreThanZero()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); //0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(5, 0, 15, 10)); //10f, 5f, 10f, 10f);

			Assert.True(one.HorizontallyOverlaps(two));
			Assert.Equal(5f, one.HorizontalOverlap(two), 0);
			Assert.Equal(0f, one.OverlapRatio(two), 0);
		}

		[Fact]
		public void TestGetOverlapShouldReturnMoreThanZero()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); // 0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(5, 5, 15, 15)); //5f, 5f, 10f, 10f);

			Assert.True(one.HorizontallyOverlaps(two));
			Assert.True(one.VerticallyOverlaps(two));
			Assert.Equal(5f, one.HorizontalOverlap(two), 0);
			Assert.Equal(5f, one.VerticalOverlap(two), 0);
			Assert.Equal(25f / 175, one.OverlapRatio(two), 0);
		}

		[Fact]
		public void TestMergeNoOverlappingRectangles()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10)); //0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(10, 0, 20, 10)); //0f, 10f, 10f, 10f);

			one.Merge(two);

			Assert.Equal(20f, one.Width, 0);
			Assert.Equal(10f, one.Height, 0);
			Assert.Equal(0f, one.Left, 0);
			Assert.Equal(10, one.Top, 0);      //0f, one.getTop(), 0);
			Assert.Equal(0, one.Bottom, 0);    //10f, one.getBottom(), 0);
			Assert.Equal(20f * 10f, one.Area, 0);
		}

		[Fact]
		public void TestMergeOverlappingRectangles()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(0, 0, 10, 10));//0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(new PdfRectangle(5, 5, 15, 15));//5f, 5f, 10f, 10f);

			one.Merge(two);

			Assert.Equal(15f, one.Width, 0);
			Assert.Equal(15f, one.Height, 0);
			Assert.Equal(0f, one.Left, 0);
			Assert.Equal(0f, one.Bottom, 0); // one.getTop()
			Assert.Equal(15, one.Top, 0);
		}

		[Fact]
		public void TestRectangleGetPoints()
		{
			TableRectangle one = new TableRectangle(new PdfRectangle(20, 10, 50, 50)); //10f, 20f, 30f, 40f);
			Assert.Equal(30, one.Width);
			Assert.Equal(40, one.Height);

			PdfPoint[] points = one.Points;

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
		public void TestGetBoundingBox()
		{
			List<TableRectangle> rectangles = new List<TableRectangle>
			{
				new TableRectangle(new PdfRectangle(0, 0, 10, 10)),  //0f, 0f, 10f, 10f)
                new TableRectangle(new PdfRectangle(30, 10, 40, 20)) //20f, 30f, 10f, 10f)
			};

			TableRectangle boundingBoxOf = TableRectangle.BoundingBoxOf(rectangles);

			Assert.Equal(new TableRectangle(new PdfRectangle(0, 0, 40, 20)), boundingBoxOf); // 0f, 0f, 40f, 30f)
		}

		[Fact]//(Skip = "Comparison is not transitive. Transitivity needs to be implemented.")]
		public void TestTransitiveComparison1()
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
		public void TestTransitiveComparison2()
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
		public void TestWellDefinedComparison1()
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
