using EditorExtras;
using Sandbox.UI;
using static Editor.Label;

namespace ShaderGraphPlusPanelTest;

public sealed class GeneratedCodePanel : EditorDockPanel
{
	private ReadOnlyScriptControl _scriptControl;

	public string Code
	{
		get
		{
			if ( _scriptControl == null )
				return "";

			return _scriptControl.Source;
		}
		set
		{
			if ( _scriptControl == null )
				return;

			_scriptControl.Source = value;
		}
	}

	public GeneratedCodePanel() : base()
	{
		_scriptControl = AddChild<ReadOnlyScriptControl>();
		_scriptControl.Style.Set( "width: 100%; height: 250px; flex-shrink: 0;" );
	}
}

class ReadOnlyScriptControl : ScriptControl
{
	public ReadOnlyScriptControl() : base()
	{
		ReadOnly = true;
	}
}
