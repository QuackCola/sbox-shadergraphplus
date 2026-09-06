using Editor;

namespace ShaderGraphPlus;

public class TextEditAreaWidget : Widget
{
	private TextEdit _textEdit;

	public Action<string> ValueChanged { get; set; }

	public string Value
	{
		get => _textEdit.PlainText;
		set => _textEdit.PlainText = value;
	}

	public TextEditAreaWidget( Widget parent, string text = "" ) : base( parent )
	{
		_textEdit = new TextEdit( this );
		_textEdit.PlainText = text;
		_textEdit.TabSize = 32;
		_textEdit.TextChanged = x => ValueChanged?.Invoke( x );
		_textEdit.AcceptDrops = false;
		_textEdit.SetStyles( $"font-size: 12px; font-weight: regular; color: {Theme.TextControl.Hex};" );

		Layout = Layout.Row();
		Layout.Spacing = 16;

		Layout.Add( _textEdit );
	}

	public TextEditAreaWidget( Widget parent, string windowTitle, string text ) : this( parent, text )
	{
		Name = windowTitle;
		WindowTitle = windowTitle;
	}

	/*
	string _oldValue;

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( _oldValue == textEdit.PlainText )
			return;

		_oldValue = textEdit.PlainText;
	}
	*/
}
