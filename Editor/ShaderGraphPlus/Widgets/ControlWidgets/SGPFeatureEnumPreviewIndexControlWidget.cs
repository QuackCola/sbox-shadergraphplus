using Editor;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

[CustomEditor( typeof( int ), NamedEditor = ControlWidgetCustomEditors.ShaderFeatureEnumPreviewIndexEditor )]
internal sealed class SGPFeatureEnumPreviewIndexControlWidget : DropdownControlWidgetPlus<int>
{
	private ShaderFeatureEnumParameter _featureEnumParameter;
	private Entry _selectedEntry;
	private int _selectedIndex;

	public SGPFeatureEnumPreviewIndexControlWidget( SerializedProperty property ) : base( property )
	{
		_featureEnumParameter = property.Parent.Targets.OfType<ShaderFeatureEnumParameter>().FirstOrDefault();

		if ( _featureEnumParameter is null ) return;

		var currentSelctedIndex = SerializedProperty.GetValue<int>();
		if ( TryGetEntryFromIndex( currentSelctedIndex, out var entry ) )
		{
			_selectedEntry = entry;
			_selectedIndex = currentSelctedIndex;
		}
		else
		{
			SGPLogger.Error( $"Couldnt find entry at index \"{currentSelctedIndex}\"" );
		}
	}

	private bool TryGetEntryFromIndex( int selectedIndex, out Entry foundEntry )
	{
		foundEntry = new();

		foreach ( var entry in GetDropdownValues().OfType<Entry>().Index() )
		{
			if ( entry.Index == selectedIndex )
			{
				foundEntry = entry.Item;

				return true;
			}
		}

		return false;
	}

	private bool TryGetyIndexFromEntry( Entry selectedEntry, out int foundIndex )
	{
		foundIndex = new();

		foreach ( var entry in _featureEnumParameter.Options.Index() )
		{
			if ( entry.Item.Name == selectedEntry.Label )
			{
				foundIndex = entry.Index;

				return true;
			}
		}

		return false;
	}

	protected override void OnSelectItem( object item )
	{
		base.OnSelectItem( item );

		if ( item is Entry e )
		{
			_selectedEntry = e;

			if ( TryGetyIndexFromEntry( _selectedEntry, out var newSelectedIndex ) )
			{
				_selectedIndex = newSelectedIndex;
			}
		}
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		List<object> list = new();

		foreach ( var option in _featureEnumParameter.Options.Index() )
		{
			var entry = new Entry();
			entry.Value = option.Index;
			entry.Label = option.Item.Name;
			entry.Description = "";
			list.Add( entry );
		}

		return list;
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;
		PaintUnder();
		PaintControl();
		PaintOver();
	}

	protected override void PaintControl()
	{
		var entryLabel = _selectedEntry.Label; // at index {SelectedIndex}";
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		var rect = LocalRect;

		rect = rect.Shrink( 8, 0 );

		Paint.SetPen( color );
		Paint.SetDefaultFont();

		if ( SerializedProperty.IsMultipleDifferentValues )
		{
			Paint.SetPen( Theme.MultipleValues );
			Paint.DrawText( rect, "Multiple Values", TextFlag.LeftCenter );
		}
		else
		{
			Paint.DrawText( rect, entryLabel ?? "None", TextFlag.LeftCenter );
		}

		Paint.SetPen( color );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}
}
