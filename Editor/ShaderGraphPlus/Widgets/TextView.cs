using Editor;

namespace ShaderGraphPlus;

// TODO : Add Line Numbers to the left of the text area. Also add an option to toggle LineNumbers on or off.

public class TextView : Widget
{
	public Action TextChanged;

	private string _contents = "";
	private TextEdit _textEdit;

	public TextView( Widget parent, string windowTitle, string text ) : base( parent )
	{
		Name = windowTitle;
		WindowTitle = windowTitle;
		_contents = text;
		Parent = parent;
		SetWindowIcon( "edit" );

		Layout = Layout.Row();

		// TODO
		var LineNumbers = Layout.AddRow( 1 );
		LineNumbers.AddSpacingCell( 16f );

		//Layout.Add( LineNumbers );

		_textEdit = new TextEdit( this );
		_textEdit.ReadOnly = true;
		_contents = text;
		_textEdit.PlainText = _contents;
		_textEdit.VerticalScrollbarMode = ScrollbarMode.On;
		_textEdit.TextChanged += x =>
		{
			TextChanged?.Invoke();
		};

		_textEdit.SetStyles( $"font-size: 12px; font-weight: regular; color: {Theme.TextControl.Hex};" );

		Layout.Add( _textEdit );
	}


	public string GetTextContents()
	{
		return _contents;
	}

	public void SetTextContents( string text )
	{
		if ( !string.IsNullOrWhiteSpace( text ) )
		{
			text.ReplaceLineEndings();
			text = text.Replace( "    ", "\t" );
		}

		_contents = text;
		_textEdit.PlainText = _contents;
	}
}
