using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;

namespace Tabula
{
	//https://github.com/tabulapdf/tabula-java/blob/ebc83ac2bb1a1cbe54ab8081d70f3c9fe81886ea/src/main/java/technology/tabula/RectangularTextContainer.java
	public abstract class RectangularTextContainer : TableRectangle
	{
		public RectangularTextContainer(PdfRectangle pdfRectangle) : base(pdfRectangle)
		{

		}

		public RectangularTextContainer(double top, double left, double width, double height)
			: base(top, left, width, height)
		{
			throw new ArgumentOutOfRangeException();
		}

		public abstract String getText();

		public abstract String getText(bool useLineReturns);

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			string s = base.ToString();
			sb.Append(s, 0, s.Length - 1);
			sb.Append($",text={(this.getText() == null ? "null" : "\"" + this.getText() + "\"")}]");
			return sb.ToString();
		}
	}


	public abstract class RectangularTextContainer<T> : RectangularTextContainer where T : HasText
	{
		public RectangularTextContainer(PdfRectangle pdfRectangle)
			: base(pdfRectangle)
		{
		}

		public RectangularTextContainer(double top, double left, double width, double height)
			: base(top, left, width, height)
		{
			throw new ArgumentOutOfRangeException();
		}

		public RectangularTextContainer<T> merge(RectangularTextContainer<T> other)
		{
			if (this.CompareTo(other) < 0)
			{
				this.getTextElements().AddRange(other.getTextElements());// .AddAll(other.getTextElements());
			}
			else
			{
				this.getTextElements().InsertRange(0, other.getTextElements());  //this.getTextElements().AddAll(0, other.getTextElements());
			}
			base.merge(other);
			return this;
		}

		public abstract List<T> getTextElements();
	}
}
