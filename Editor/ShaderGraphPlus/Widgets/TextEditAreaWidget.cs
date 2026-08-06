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

	public TextEditAreaWidget( Widget parent ) : base( parent )
	{
		_textEdit = new TextEdit( this );
		_textEdit.TextChanged = x => ValueChanged?.Invoke( x );
		_textEdit.AcceptDrops = false;

		Layout = Layout.Row();
		Layout.Spacing = 16;
		Layout.Add( _textEdit );
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
