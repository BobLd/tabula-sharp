using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/RectangularTextContainer.java
    public abstract class RectangularTextContainer : TableRectangle
	{
		public RectangularTextContainer(PdfRectangle pdfRectangle) : base(pdfRectangle)
		{ }

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

		/// <summary>
		/// Gets the RectangularTextContainer's TextElements.
		/// </summary>
		public List<T> TextElements => textElements;

		public void SetTextElements(List<T> textElements)
		{
			this.textElements = textElements;
		}
		public RectangularTextContainer(PdfRectangle pdfRectangle)
			: base(pdfRectangle)
		{ }

		public RectangularTextContainer<T> Merge(RectangularTextContainer<T> other)
		{
			if (this.CompareTo(other) < 0)
			{
				this.TextElements.AddRange(other.TextElements);
			}
			else
			{
				this.TextElements.InsertRange(0, other.TextElements);
			}
			base.Merge(other);
			return this;
		}
	}
}
