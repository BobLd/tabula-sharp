using System;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestRuling
	{
		private Ruling ruling = new Ruling(0, 0, 10, 10);

		[Fact]
		public void TestGetWidth()
		{
			Assert.Equal(10f, ruling.Width, 5);
		}

		[Fact]
		public void TestGetHeight()
		{
			Assert.Equal(10f, ruling.Height,5);
		}

		[Fact]
		public void TestToString()
		{
			//Assert.Equal("class technology.tabula.Ruling[x1=0.000000 y1=0.000000 x2=10.000000 y2=10.000000]", ruling.ToString());
			Assert.Equal("Tabula.Ruling[x1=0.00,y1=0.00,x2=10.00,y2=10.00]", ruling.ToString());
		}

		[Fact]
		public void TestEqualsOther()
		{
			Ruling other = new Ruling(0, 0, 11, 10);
			Assert.True(ruling.Equals(ruling));
		}

		[Fact]
		public void TestEqualsDifferentInstance()
		{
			Assert.False(ruling.Equals("testTabula5"));
		}

		[Fact]
		public void TestNearlyIntersects()
		{
			Ruling another = new Ruling(0, 0, 11, 10);

			Assert.True(ruling.NearlyIntersects(another));
		}

		[Fact]
		public void TestGetPositionError()
		{
			Ruling other = new Ruling(0, 0, 1, 1);
			Assert.Throws<InvalidOperationException>(() => other.Position);
		}

		[Fact]
		public void TestSetPositionError()
		{
			Ruling other = new Ruling(0, 0, 1, 1);
			Assert.Throws<InvalidOperationException>(() => other.SetPosition(5f));
		}

		[Fact]
		public void TestsetPosition()
		{
			Assert.Throws<InvalidOperationException>(() => ruling.SetPosition(0));
		}

		[Fact]
		public void TestGetStartError()
		{
			Ruling other = new Ruling(0, 0, 1, 1);
			Assert.Throws<InvalidOperationException>(() => other.Start);
		}

		[Fact]
		public void TestGetEndError()
		{
			Ruling other = new Ruling(0, 0, 1, 1);
			Assert.Throws<InvalidOperationException>(() => other.End);
		}

		[Fact]
		public void TestSetEndError()
		{
			Ruling other = new Ruling(0, 0, 1, 1);
			Assert.Throws<InvalidOperationException>(() => other.SetEnd(5f));
		}

		[Fact]
		public void TestColinear()
		{
			// Ruling another = new Ruling(0, 0, 500, 5);
			PdfPoint float1 = new PdfPoint(20, 20);
			PdfPoint float2 = new PdfPoint(0, 0);
			PdfPoint float3 = new PdfPoint(20, 0);
			PdfPoint float4 = new PdfPoint(0, 20);

			Assert.False(ruling.IsColinear(float1));
			Assert.True(ruling.IsColinear(float2));
			Assert.False(ruling.IsColinear(float3));
			Assert.False(ruling.IsColinear(float4));
		}
	}
}
