
using Sandbox.UI;
using System.Globalization;

// Remove this once the controls.cs copy is no longer needed.
internal static class LabelExtentions
{
	/// <summary>
	/// Insert text and put the caret after it. The inserted text can join a neighboring
	/// grapheme, so find its end in the resulting text rather than adding element counts.
	/// </summary>
	internal static void InsertTextAndMoveCaret( this Label label, string text, int pos, int? endpos = null )
	{
		StringInfo stringInfo = new StringInfo( label.Text );
	
		pos = Math.Clamp( pos, 0, label.TextLength );
		var insertionEnd = (pos > 0 ? stringInfo.SubstringByTextElements( 0, pos ).Length : 0) + (text?.Length ?? 0);
		label.InsertText( text, pos, endpos );

		var boundaries = StringInfo.ParseCombiningCharacters( label.Text );
		var index = Array.BinarySearch( boundaries, insertionEnd );
		label.SetCaretPosition( index >= 0 ? index : ~index );
	}

}
