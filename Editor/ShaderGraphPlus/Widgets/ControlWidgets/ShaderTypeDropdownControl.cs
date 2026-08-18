using Editor;

namespace ShaderGraphPlus;

[CustomEditor( typeof( string ), NamedEditor = "ShaderTypeDropdown" )]
public sealed class ShaderTypeDropdownControl : DropdownControlWidget<string>
{
	private ShaderGraphPlus _graph;
	private string _currentEntryIcon;

	public ShaderTypeDropdownControl( SerializedProperty property ) : base( property )
	{
		_graph = property.Parent?.Targets?.FirstOrDefault() as ShaderGraphPlus;

		_currentEntryIcon = _graph.HasTemplate ? _graph.UserTemplateInfo.Icon : "view_in_ar";

		var value = property.GetValue<string>();

		var template = AssetSystem.All.FirstOrDefault( x => x.Path == value );

		if ( template == null && !ShaderTemplate.BuiltInTemplateEntries.ContainsKey( value ) )
		{
			property.SetValue( ShaderTemplate.BuiltInTemplateEntries.FirstOrDefault().Key );
		}
	}

	protected override string GetDisplayText()
	{
		var value = SerializedProperty.GetValue<string>();

		if ( string.IsNullOrEmpty( value ) )
		{
			return ShaderTemplate.BuiltInTemplateEntries.FirstOrDefault().Key;
		}

		if ( _graph.HasTemplate )
		{
			return _graph.UserTemplateInfo.Title;
		}
		else
		{
			return value;
		}
	}

	protected override void OnItemSelected( object item )
	{
		if ( item is Entry entry )
		{
			_currentEntryIcon = entry.Icon;
		}

		base.OnItemSelected( item );
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		var templates = AssetSystem.All.Where( x => x.Path.EndsWith( $".{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension}", StringComparison.OrdinalIgnoreCase ) );

		List<ShaderTemplate.TemplateEntry> entries = ShaderTemplate.BuiltInTemplateEntries.Values.ToList();

		entries.AddRange( templates.Select( x => new ShaderTemplate.TemplateEntry( x.Name, x.Path, "view_in_ar" ) ) );

		foreach ( var templateEntry in entries )
		{
			var label = templateEntry.Name;
			var icon = templateEntry.Icon;

			var isUserTemplate = !string.IsNullOrWhiteSpace( templateEntry.Path ) && templateEntry.Path.EndsWith( $".{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension}" );

			if ( isUserTemplate )
			{
				var templateTxt = Editor.FileSystem.Content.ReadAllText( templateEntry.Path );
				var template = new ShaderTemplateResource();

				template.Deserialize( templateTxt );

				if ( !string.IsNullOrWhiteSpace( template.Title ) )
				{
					label = template.Title;
				}

				if ( !string.IsNullOrWhiteSpace( template.Icon ) )
				{
					icon = template.Icon;
				}
			}

			var entry = new Entry()
			{
				Value = !isUserTemplate ? templateEntry.Name : templateEntry.Path,
				Label = label,
				Icon = icon
			};

			yield return entry;
		}
	}

	protected override void PaintControl()
	{
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		var rect = new Rect( 0, 0, Width, Theme.RowHeight ).Shrink( 8, 0 );

		Paint.SetPen( color );
		Paint.SetDefaultFont();

		if ( !string.IsNullOrEmpty( _currentEntryIcon ) )
		{
			var i = Paint.DrawIcon( rect, _currentEntryIcon, 16, TextFlag.LeftCenter );
			rect.Left += i.Width + 8;
		}

		if ( SerializedProperty.IsMultipleDifferentValues )
		{
			Paint.SetPen( Theme.MultipleValues );
			Paint.DrawText( rect, "Multiple Values", TextFlag.LeftCenter );
		}
		else
		{
			Paint.DrawText( rect, GetDisplayText(), TextFlag.LeftCenter );
		}

		Paint.SetPen( color );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	protected override void OnPaint()
	{
		base.OnPaint();
	}
}
