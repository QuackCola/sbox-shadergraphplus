using Editor;

namespace ShaderGraphPlus;

public class TextEditAreaWidget : Widget
{
	public Action<string> ValueChanged { get; set; }

	TextEdit textEdit;

	public string Value
	{
		get => textEdit.PlainText;
		set => textEdit.PlainText = value;
	}

	public TextEditAreaWidget( Widget parent ) : base( parent )
	{
		textEdit = new TextEdit( this );
		textEdit.TextChanged = x => ValueChanged?.Invoke( x );
		textEdit.AcceptDrops = false;

		Layout = Layout.Row();
		Layout.Spacing = 16;
		Layout.Add( textEdit );
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
