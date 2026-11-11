// Temporary copy of the engine's Sandbox.UI.GenericControl (panel-controlsheet-objects branch). Delete this
// folder and point the Properties panel back at Sandbox.UI.ControlSheet once it ships in the engine.
using System.Reflection;
using Sandbox.UI;

namespace EngineControlSheet;

/// <summary>
/// Edits a class or struct with a nested <see cref="ControlSheet"/>. Panel version of GenericControlWidget.
/// </summary>
[StyleSheet.Inline( "localgenericcontrol", Styles )]
public class GenericControl : BaseControl, IControlLayout
{
	const string Styles = """
		GenericControl
		{
			flex-direction: column;
			flex-grow: 1;
			min-width: 0px;
		}

		GenericControl > .title
		{
			flex-shrink: 0;
			padding: 6px 6px 2px 6px;
			font-weight: 600;
		}

		GenericControl > .empty
		{
			padding: 6px;
			opacity: 0.5;
		}

		GenericControl > ControlSheet
		{
			flex-shrink: 0;
		}
		""";

	bool _inline;

	public override bool IsWideMode => _inline;

	public override bool IncludeLabel => !_inline;

	/// <summary>
	/// Not primitives or readonly structs, like the editor's.
	/// </summary>
	internal static bool CanEdit( SerializedProperty property )
	{
		var type = property.PropertyType;
		if ( type is null || type.IsPrimitive ) return false;
		if ( type.IsValueType && type.GetCustomAttribute<System.Runtime.CompilerServices.IsReadOnlyAttribute>() is not null ) return false;

		return true;
	}

	public override void Rebuild()
	{
		DeleteChildren( true );

		if ( Property is null )
			return;

		_inline = Property.TryGetAttribute<InlineEditorAttribute>( out var inlineEditor );
		SetClass( "inline", _inline );

		if ( _inline && inlineEditor.Label && Property.Parent is not SerializedCollection )
		{
			AddChild<Label>( "title" ).Text = Property.DisplayName;
		}

		if ( !Property.TryGetAsObject( out var serialized ) )
		{
			AddChild<Label>( "empty" ).Text = "None";
			return;
		}

		AddChild( new ControlSheet { Target = serialized.ToList() } );
	}
}
