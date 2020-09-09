using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
public	class TestRectangle
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
		public void testCompareAlignedHorizontalRectangle()
		{
			TableRectangle lower = new TableRectangle(0f, 10f, 10f, 10f);
			TableRectangle upper = new TableRectangle(0f, 20f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void testCompareAlignedVerticalRectangle()
		{
			TableRectangle lower = new TableRectangle(10f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(20f, 0f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void testCompareVerticalOverlapRectangle()
		{
			TableRectangle lower = new TableRectangle(5f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(0f, 10f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}

		[Fact]
		public void testCompareVerticalOverlapLessThresholdRectangle()
		{
			TableRectangle lower = new TableRectangle(0f, 10f, 10f, 10f);
			TableRectangle upper = new TableRectangle(9.8f, 0f, 10f, 10f);

			Assert.True(lower.CompareTo(upper) < 0);
		}



		[Fact]
		public void testQuickSortOneUpperThanOther()
		{

			TableRectangle lower = new TableRectangle(175.72f, 72.72f, 1.67f, 1.52f); //, (Comma after AARON)
			TableRectangle upper = new TableRectangle(169.21f, 161.16f, 4.33f, 4.31f); // R (REGIONAL PULMONARY)

			Assert.True(lower.CompareTo(upper) > 0);

		}


		[Fact]
		public void testQuickSortRectangleList()
		{

			//Testing wrong sorting
			// Expected: AARON, JOSHUA, N
			// but was: AARON JOSHUA N , ,
			TableRectangle first = new TableRectangle(172.92999267578125f, 51.47999954223633f, 4.0f, 4.309999942779541f); //A
			TableRectangle second = new TableRectangle(175.72000122070312f, 72.72000122070312f, 1.6699999570846558f, 1.5199999809265137f); //,
			TableRectangle third = new TableRectangle(172.92999267578125f, 96.36000061035156f, 4.0f, 4.309999942779541f); //A
			TableRectangle fourth = new TableRectangle(175.72000122070312f, 100.31999969482422f, 1.6699999570846558f, 1.5199999809265137f); //,
			TableRectangle fifth = new TableRectangle(172.92999267578125f, 103.68000030517578f, 4.329999923706055f, 4.309999942779541f); //N
			TableRectangle sixth = new TableRectangle(169.2100067138672f, 161.16000366210938f, 4.329999923706055f, 4.309999942779541f); //R

			List<TableRectangle> expectedList = new List<TableRectangle>();
			expectedList.Add(first);
			expectedList.Add(sixth);
			expectedList.Add(second);
			expectedList.Add(third);
			expectedList.Add(fourth);
			expectedList.Add(fifth);
			List<TableRectangle> toSortList = new List<TableRectangle>();
			toSortList.Add(sixth);
			toSortList.Add(second);
			toSortList.Add(third);
			toSortList.Add(fifth);
			toSortList.Add(first);
			toSortList.Add(fourth);

			toSortList.Sort(new TableRectangle.ILL_DEFINED_ORDER());
			//Collections.sort(toSortList, TableRectangle.ILL_DEFINED_ORDER);

			Assert.Equal(expectedList, toSortList);
		}

		[Fact]
		public void testGetVerticalOverlapShouldReturnZero()
		{

			TableRectangle lower = new TableRectangle(10f, 0f, 10f, 10f);
			TableRectangle upper = new TableRectangle(20f, 0f, 10f, 10f);

			double overlap = lower.verticalOverlap(upper);

			Assert.Equal(0, overlap, 0);
			Assert.True(!lower.verticallyOverlaps(upper));
			Assert.Equal(0, lower.verticalOverlapRatio(upper), 0);
			Assert.Equal(0, lower.overlapRatio(upper), 0);

		}

		[Fact]
		public void testGetVerticalOverlapShouldReturnMoreThanZero()
		{

			TableRectangle lower = new TableRectangle(15f, 10f, 10f, 10f);
			TableRectangle upper = new TableRectangle(20f, 0f, 10f, 10f);

			double overlap = lower.verticalOverlap(upper);

			Assert.Equal(5, overlap, 0);
			Assert.True(lower.verticallyOverlaps(upper));
			Assert.Equal(0.5, lower.verticalOverlapRatio(upper), 0);
			Assert.Equal(0, lower.overlapRatio(upper), 0);

		}

		[Fact]
		public void testGetHorizontalOverlapShouldReturnZero()
		{

			TableRectangle one = new TableRectangle(0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(10f, 10f, 10f, 10f);

			Assert.True(!one.horizontallyOverlaps(two));
			Assert.Equal(0f, one.overlapRatio(two), 0);

		}

		[Fact]
		public void testGetHorizontalOverlapShouldReturnMoreThanZero()
		{

			TableRectangle one = new TableRectangle(0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(10f, 5f, 10f, 10f);

			Assert.True(one.horizontallyOverlaps(two));
			Assert.Equal(5f, one.horizontalOverlap(two), 0);
			Assert.Equal(0f, one.overlapRatio(two), 0);

		}

		[Fact]
		public void testGetOverlapShouldReturnMoreThanZero()
		{

			TableRectangle one = new TableRectangle(0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(5f, 5f, 10f, 10f);

			Assert.True(one.horizontallyOverlaps(two));
			Assert.True(one.verticallyOverlaps(two));
			Assert.Equal(5f, one.horizontalOverlap(two), 0);
			Assert.Equal(5f, one.verticalOverlap(two), 0);
			Assert.Equal((25f / 175), one.overlapRatio(two), 0);

		}

		[Fact]
		public void testMergeNoOverlappingRectangles()
		{

			TableRectangle one = new TableRectangle(0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(0f, 10f, 10f, 10f);

			one.merge(two);

			Assert.Equal(20f, one.getWidth(), 0);
			Assert.Equal(10f, one.getHeight(), 0);
			Assert.Equal(0f, one.getLeft(), 0);
			Assert.Equal(0f, one.getTop(), 0);
			Assert.Equal(10f, one.getBottom(), 0);
			Assert.Equal(20f * 10f, one.getArea(), 0);

		}

		[Fact]
		public void testMergeOverlappingRectangles()
		{

			TableRectangle one = new TableRectangle(0f, 0f, 10f, 10f);
			TableRectangle two = new TableRectangle(5f, 5f, 10f, 10f);

			one.merge(two);

			Assert.Equal(15f, one.getWidth(), 0);
			Assert.Equal(15f, one.getHeight(), 0);
			Assert.Equal(0f, one.getLeft(), 0);
			Assert.Equal(0f, one.getTop(), 0);

		}

		[Fact]
		public void testRectangleGetPoints()
		{

			TableRectangle one = new TableRectangle(10f, 20f, 30f, 40f);

			PdfPoint[] points = one.getPoints();

			PdfPoint[] expectedPoints = new PdfPoint[]
			{
				new PdfPoint(20f, 10f),
				new PdfPoint(50f, 10f),
				new PdfPoint(50f, 50f),
				new PdfPoint(20f, 50f)
			};

			Assert.Equal(expectedPoints, points);

		}

		[Fact]
		public void testGetBoundingBox()
		{

			List<TableRectangle> rectangles = new List<TableRectangle>();
			rectangles.Add(new TableRectangle(0f, 0f, 10f, 10f));
			rectangles.Add(new TableRectangle(20f, 30f, 10f, 10f));

			TableRectangle boundingBoxOf = TableRectangle.boundingBoxOf(rectangles);

			Assert.Equal(new TableRectangle(0f, 0f, 40f, 30f), boundingBoxOf);
		}

		[Fact]
		public void testTransitiveComparison1()
		{
			// +-------+
			// |       |
			// |   A   | +-------+
			// |       | |       |
			// +-------+ |   B   | +-------+
			//           |       | |       |
			//           +-------+ |   C   |
			//                     |       |
			//                     +-------+
			TableRectangle a = new TableRectangle(0, 0, 2, 2);
			TableRectangle b = new TableRectangle(1, 1, 2, 2);
			TableRectangle c = new TableRectangle(2, 2, 2, 2);
			Assert.True(a.CompareTo(b) < 0);
			Assert.True(b.CompareTo(c) < 0);
			Assert.True(a.CompareTo(c) < 0);
		}

		[Fact] // @Ignore
		public void testTransitiveComparison2()
		{
			//                     +-------+
			//                     |       |
			//           +-------+ |   C   |
			//           |       | |       |
			// +-------+ |   B   | +-------+
			// |       | |       |
			// |   A   | +-------+
			// |       |
			// +-------+
			TableRectangle a = new TableRectangle(2, 0, 2, 2);
			TableRectangle b = new TableRectangle(1, 1, 2, 2);
			TableRectangle c = new TableRectangle(0, 2, 2, 2);
			Assert.True(a.CompareTo(b) < 0);
			Assert.True(b.CompareTo(c) < 0);
			Assert.True(a.CompareTo(c) < 0);
		}

		//[Fact] // @Ignore
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
