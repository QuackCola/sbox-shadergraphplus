using ControlSheet = EngineControlSheet.ControlSheet;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// Edits whatever is selected - the graph, a node or a blackboard parameter - with the local copy of the
/// engine's <see cref="ControlSheet"/> (<c>Editor/Internal/EngineControlSheet</c>), like the Qt Properties widget.
/// </summary>
[StyleSheet.Inline( "sgp_propertiespanel", Styles )]
public sealed partial class PropertiesPanel : Panel
{
	private readonly Panel _heading;
	private readonly Panel _headingRow;
	private readonly Label _subtitle;
	private readonly Panel _body;

	private Label _title;
	
	private bool _builtSlider;

	/// <summary>
	/// The object being edited, or null for nothing.
	/// </summary>
	public object Target
	{
		get;
		set
		{
			if ( value == field )
				return;

			field = value;
			Rebuild();
		}
	}

	/// <summary>
	/// A property of the target was edited.
	/// </summary>
	public Action<SerializedProperty> PropertyUpdated { get; set; }

	public PropertiesPanel() : base()
	{
		AddClass( "sg-props" );

		_heading = Add.Panel( "heading" );
		_headingRow = _heading.Add.Panel( "row" );
		_subtitle = _heading.Add.Label( "", "subtitle" );

		_body = Add.Panel( "body" );

		Rebuild();
	}

	public override void Tick()
	{
		base.Tick();

		if ( Target is not FloatParameter parameter )
			return;

		// Switching between Default and Slider swaps the Value control
		if ( UsesSlider( parameter ) != _builtSlider )
		{
			Rebuild();
			return;
		}

		// The slider reads its range once, so keep it in step with Min, Max and Step
		foreach ( var slider in _body.Descendants.OfType<SliderControl>() )
		{
			if ( slider.Min != parameter.Min ) slider.Min = parameter.Min;
			if ( slider.Max != parameter.Max ) slider.Max = parameter.Max;
			if ( parameter.UI.Step > 0 && slider.Step != parameter.UI.Step ) slider.Step = parameter.UI.Step;
		}
	}

	private void Rebuild()
	{
		_body.DeleteChildren( true );
		_headingRow.DeleteChildren( true );
		_title = null;
		_builtSlider = false;

		_heading.Style.Display = Target is null ? DisplayMode.None : DisplayMode.Flex;
		_body.SetClass( "is-empty", Target is null );

		if ( Target is null )
		{
			_body.Add.Label( "Nothing selected", "empty" );
			return;
		}

		BuildHeading();

		var serialized = EditorTypeLibrary.GetSerializedObject( Target );

		// Also fires for edits inside nested structs
		serialized.OnPropertyChanged += property =>
		{
			if ( _title is not null && Target is BlackboardParameter p ) _title.Text = p.Name;
			PropertyUpdated?.Invoke( property );
		};

		var properties = serialized.Where( x => !x.IsMethod ).ToList();

		if ( Target is FloatParameter floatParameter && UsesSlider( floatParameter ) )
		{
			var index = properties.FindIndex( x => x.Name == nameof( FloatParameter.Value ) );
			if ( index >= 0 ) properties[index] = new SliderValueProperty( properties[index], floatParameter );
			_builtSlider = true;
		}

		// One sheet per group, Value first - a single sheet would sort Range above it
		var groups = properties
			.GroupBy( x => x.GroupName ?? "" )
			.OrderBy( x => x.Key == "" ? 0 : x.Key == "Value" ? 1 : 2 );

		foreach ( var group in groups )
		{
			_body.AddChild( new ControlSheet { Target = group.ToList() } );
		}
	}

	/// <summary>
	/// Icon or type chip, a title and a description - per target type, like the Qt header.
	/// </summary>
	private void BuildHeading()
	{
		string description = null;

		switch ( Target )
		{
			case BlackboardParameter parameter:
				ParameterRow.AddTypeChip( _headingRow, ParameterRow.TypeConfig( parameter ) );
				_title = _headingRow.Add.Label( parameter.Name, "title" );
				description = parameter.DisplayInfo.Description;
				break;

			case ShaderGraphPlus.ShaderGraphPlus:
				AddTitle( "settings", "Graph Settings" );
				break;

			case BaseNodePlus node:
				AddTitle( node.DisplayInfo.Icon ?? "account_tree", node.DisplayInfo.Name ?? node.GetType().Name );
				description = node.DisplayInfo.Description;
				break;

			case GroupData:
				AddTitle( "folder", "Group" );
				break;

			default:
				var info = DisplayInfo.For( Target );
				AddTitle( info.Icon ?? "settings", info.Name ?? "Properties" );
				description = info.Description;
				break;
		}

		_subtitle.Text = description ?? "";
		_subtitle.Style.Display = string.IsNullOrWhiteSpace( description ) ? DisplayMode.None : DisplayMode.Flex;
	}

	private void AddTitle( string icon, string title )
	{
		_headingRow.Add.Icon( icon, "icon" );
		_headingRow.Add.Label( title, "title" );
	}

	private static bool UsesSlider( FloatParameter parameter ) => parameter.UI.Type == UIType.Slider;

	/// <summary>
	/// The parameter's Value with its range and step as attributes, so the sheet picks a slider.
	/// </summary>
	private sealed class SliderValueProperty : SerializedProperty.Proxy
	{
		private readonly SerializedProperty _value;
		private readonly FloatParameter _parameter;

		public SliderValueProperty( SerializedProperty value, FloatParameter parameter )
		{
			_value = value;
			_parameter = parameter;
		}

		protected override SerializedProperty ProxyTarget => _value;

		public override IEnumerable<Attribute> GetAttributes()
		{
			foreach ( var attribute in _value.GetAttributes() )
				yield return attribute;

			yield return new RangeAttribute( _parameter.Min, _parameter.Max );

			if ( _parameter.UI.Step > 0 )
				yield return new StepAttribute( _parameter.UI.Step );
		}
	}
}
