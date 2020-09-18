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
		{ }

		[Obsolete("Use RectangularTextContainer(PdfRectangle) instead.")]
		public RectangularTextContainer(double top, double left, double width, double height)
			: base(top, left, width, height)
		{
			throw new ArgumentOutOfRangeException();
		}

		public abstract string GetText();

		public abstract string GetText(bool useLineReturns);

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			string s = base.ToString();
			sb.Append(s, 0, s.Length - 1);
			sb.Append($",text={(this.GetText() == null ? "null" : "\"" + this.GetText() + "\"")}]");
			return sb.ToString();
		}
	}

	public abstract class RectangularTextContainer<T> : RectangularTextContainer where T : IHasText
	{
		protected List<T> textElements;

		public RectangularTextContainer(PdfRectangle pdfRectangle)
			: base(pdfRectangle)
		{
		}

		public RectangularTextContainer<T> Merge(RectangularTextContainer<T> other)
		{
			if (this.CompareTo(other) < 0)
			{
				this.TextElements.AddRange(other.TextElements);// .AddAll(other.getTextElements());
			}
			else
			{
				this.TextElements.InsertRange(0, other.TextElements);  //this.getTextElements().AddAll(0, other.getTextElements());
			}
			base.Merge(other);
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		public List<T> TextElements => textElements;

		public void SetTextElements(List<T> textElements)
		{
			this.textElements = textElements;
		}
	}
}
