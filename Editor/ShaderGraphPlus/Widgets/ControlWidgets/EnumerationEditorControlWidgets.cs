
using Editor;

namespace ShaderGraphPlus;

/// <summary>
/// Custom enum editor for BlendMode that filters options based on template/shading model features
/// </summary>
[CustomEditor( typeof( BlendMode ) )]
internal class BlendModeControlWidget : ControlWidget
{
	ShaderGraphPlus _graph;
	EnumDescription _enumDesc;
	PopupWidget _menu;

	public override bool IsControlActive => base.IsControlActive || _menu.IsValid();
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	public BlendModeControlWidget( SerializedProperty property ) : base( property )
	{
		_graph = property.Parent?.Targets?.FirstOrDefault() as ShaderGraphPlus;

		var propertyType = property.PropertyType;
		Cursor = CursorShape.Finger;
		Layout = Layout.Row();
		Layout.Spacing = 2;
		_enumDesc = EditorTypeLibrary.GetEnumDescription( propertyType );
	}

	private bool IsEntrySupported( EnumDescription.Entry entry )
	{
		if ( _graph is null )
			return entry.Browsable;

		if ( !entry.Browsable )
			return false;

		return entry.Name switch
		{
			nameof( BlendMode.Opaque ) => _graph.UserTemplateInfo.SupportsOpaqueBlend,
			nameof( BlendMode.Masked ) => _graph.UserTemplateInfo.SupportsMaskedBlend,
			nameof( BlendMode.Translucent ) => _graph.UserTemplateInfo.SupportsTranslucentBlend,
			nameof( BlendMode.Dynamic ) => _graph.UserTemplateInfo.SupportsDynamicBlend,

			_ => true
		};
	}

	private bool IsNoneSupported()
	{
		var enumEntryCount = _enumDesc.Count();

		var notSupportedCount = 0;

		foreach ( var o in _enumDesc )
		{
			if ( !IsEntrySupported( o ) )
			{
				notSupportedCount++;
				continue;
			}
		}

		return notSupportedCount == _enumDesc.Count();
	}

	protected override void PaintControl()
	{
		if ( _enumDesc is null )
			return;

		// Auto-correct if current value is not supported
		_graph?.ValidateTemplateSettings();

		var value = SerializedProperty.GetValue<long>( 0 );
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		var rect = LocalRect.Shrink( 8, 0 );

		var e = _enumDesc.GetEntry( value );

		if ( !string.IsNullOrEmpty( e.Icon ) )
		{
			Paint.SetPen( color.WithAlpha( 0.5f ) );
			var i = Paint.DrawIcon( rect, e.Icon, 16, TextFlag.LeftCenter );
			rect.Left += i.Width + 8;
		}

		Paint.SetPen( color );
		Paint.DrawText( rect, e.Title ?? "Unset", TextFlag.LeftCenter );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		if ( e.LeftMouseButton && !_menu.IsValid() && !IsNoneSupported() )
		{
			OpenMenu();
		}
	}

	void ToggleValue( EnumDescription.Entry e )
	{
		SerializedProperty.SetValue( e.IntegerValue );
	}

	void OpenMenu()
	{
		if ( _enumDesc is null )
			return;

		// Auto-correct if current value is not supported
		_graph?.ValidateTemplateSettings();

		_menu = new PopupWidget( null );
		_menu.Layout = Layout.Column();
		var menuWidth = ScreenRect.Width;
		_menu.MinimumWidth = menuWidth;
		_menu.MaximumWidth = menuWidth;

		var scroller = _menu.Layout.Add( new ScrollArea( this ), 1 );
		scroller.Canvas = new Widget( scroller )
		{
			Layout = Layout.Column(),
			VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand,
			MaximumWidth = menuWidth
		};

		foreach ( var o in _enumDesc )
		{
			if ( !IsEntrySupported( o ) )
				continue;

			var b = scroller.Canvas.Layout.Add( new BlendModeMenuOption( o, SerializedProperty ) );
			b.MouseLeftPress = () =>
			{
				ToggleValue( o );
				_menu.Update();
				_menu.Close();
			};
		}

		_menu.Position = ScreenRect.BottomLeft;
		_menu.Visible = true;
		_menu.AdjustSize();
		_menu.ConstrainToScreen();
		_menu.OnPaintOverride = () =>
		{
			Paint.SetBrushAndPen( Theme.ControlBackground );
			Paint.DrawRect( Paint.LocalRect, 0 );
			return true;
		};
	}
}

file class BlendModeMenuOption : Widget
{
	EnumDescription.Entry info;
	SerializedProperty property;

	public BlendModeMenuOption( EnumDescription.Entry e, SerializedProperty p ) : base( null )
	{
		info = e;
		property = p;

		Layout = Layout.Row();
		Layout.Margin = 8;
		VerticalSizeMode = SizeMode.CanGrow;

		if ( !string.IsNullOrWhiteSpace( e.Icon ) )
		{
			Layout.Add( new IconButton( e.Icon ) { Background = Color.Transparent, TransparentForMouseEvents = true, IconSize = 18 } );
		}

		Layout.AddSpacingCell( 8 );
		var c = Layout.AddColumn();
		var title = c.Add( new Label( e.Title ) );
		title.SetStyles( $"font-size: 12px; font-weight: bold; font-family: {Theme.DefaultFont}; color: white;" );

		if ( !string.IsNullOrWhiteSpace( e.Description ) )
		{
			var desc = c.Add( new Label( e.Description.Trim( '\n', '\r', '\t', ' ' ) ) );
			desc.WordWrap = true;
			desc.MinimumHeight = 1;
			desc.VerticalSizeMode = SizeMode.CanGrow;
		}
	}

	bool HasValue()
	{
		var value = property.GetValue<long>( 0 );
		return value == info.IntegerValue;
	}

	protected override void OnPaint()
	{
		if ( Paint.HasMouseOver || HasValue() )
		{
			Paint.SetBrushAndPen( Theme.Blue.WithAlpha( HasValue() ? 0.3f : 0.1f ) );
			Paint.DrawRect( LocalRect.Shrink( 2 ), 2 );
		}
	}
}

/// <summary>
/// Custom enum editor for ShadingModel that filters options based on template features
/// </summary>
[CustomEditor( typeof( ShadingModel ) )]
internal class ShadingModelControlWidget : ControlWidget
{
	ShaderGraphPlus _graph;
	EnumDescription _enumDesc;
	PopupWidget _menu;

	public override bool IsControlActive => base.IsControlActive || _menu.IsValid();
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	public ShadingModelControlWidget( SerializedProperty property ) : base( property )
	{
		_graph = property.Parent?.Targets?.FirstOrDefault() as ShaderGraphPlus;

		var propertyType = property.PropertyType;
		Cursor = CursorShape.Finger;
		Layout = Layout.Row();
		Layout.Spacing = 2;
		_enumDesc = EditorTypeLibrary.GetEnumDescription( propertyType );
	}

	private bool IsEntrySupported( EnumDescription.Entry entry )
	{
		if ( _graph is null )
			return entry.Browsable;

		if ( !entry.Browsable )
			return false;

		return entry.Name switch
		{
			nameof( ShadingModel.Lit ) => _graph.UserTemplateInfo.SupportsLitShading,
			nameof( ShadingModel.Unlit ) => _graph.UserTemplateInfo.SupportsUnlitShading,
			_ => true
		};
	}

	protected override void PaintControl()
	{
		if ( _enumDesc is null )
			return;

		// Auto-correct if current value is not supported
		_graph?.ValidateTemplateSettings();

		var value = SerializedProperty.GetValue<long>( 0 );
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		var rect = LocalRect.Shrink( 8, 0 );

		var e = _enumDesc.GetEntry( value );

		if ( !string.IsNullOrEmpty( e.Icon ) )
		{
			Paint.SetPen( color.WithAlpha( 0.5f ) );
			var i = Paint.DrawIcon( rect, e.Icon, 16, TextFlag.LeftCenter );
			rect.Left += i.Width + 8;
		}

		Paint.SetPen( color );
		Paint.DrawText( rect, e.Title ?? "Unset", TextFlag.LeftCenter );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	private bool HasSingleOption()
	{
		var optionCount = 0;

		foreach ( var o in _enumDesc )
		{
			if ( !IsEntrySupported( o ) )
			{
				continue;
			}

			optionCount++;
		}

		if ( optionCount == 1 )
		{
			return true;
		}

		return false;
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		if ( e.LeftMouseButton && !_menu.IsValid() && !HasSingleOption() )
		{
			OpenMenu();
		}
	}

	void ToggleValue( EnumDescription.Entry e )
	{
		SerializedProperty.SetValue( e.IntegerValue );
	}

	void OpenMenu()
	{
		if ( _enumDesc is null )
			return;

		// Auto-correct if current value is not supported
		_graph?.ValidateTemplateSettings();

		_menu = new PopupWidget( null );
		_menu.Layout = Layout.Column();
		var menuWidth = ScreenRect.Width;
		_menu.MinimumWidth = menuWidth;
		_menu.MaximumWidth = menuWidth;

		var scroller = _menu.Layout.Add( new ScrollArea( this ), 1 );
		scroller.Canvas = new Widget( scroller )
		{
			Layout = Layout.Column(),
			VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand,
			MaximumWidth = menuWidth
		};

		foreach ( var o in _enumDesc )
		{
			if ( !IsEntrySupported( o ) )
				continue;

			var b = scroller.Canvas.Layout.Add( new ShadingModelMenuOption( o, SerializedProperty ) );
			b.MouseLeftPress = () =>
			{
				ToggleValue( o );
				_menu.Update();
				_menu.Close();
			};
		}

		_menu.Position = ScreenRect.BottomLeft;
		_menu.Visible = true;
		_menu.AdjustSize();
		_menu.ConstrainToScreen();
		_menu.OnPaintOverride = () =>
		{
			Paint.SetBrushAndPen( Theme.ControlBackground );
			Paint.DrawRect( Paint.LocalRect, 0 );
			return true;
		};
	}
}

file class ShadingModelMenuOption : Widget
{
	EnumDescription.Entry info;
	SerializedProperty property;

	public ShadingModelMenuOption( EnumDescription.Entry e, SerializedProperty p ) : base( null )
	{
		info = e;
		property = p;

		Layout = Layout.Row();
		Layout.Margin = 8;
		VerticalSizeMode = SizeMode.CanGrow;

		if ( !string.IsNullOrWhiteSpace( e.Icon ) )
		{
			Layout.Add( new IconButton( e.Icon ) { Background = Color.Transparent, TransparentForMouseEvents = true, IconSize = 18 } );
		}

		Layout.AddSpacingCell( 8 );
		var c = Layout.AddColumn();
		var title = c.Add( new Label( e.Title ) );
		title.SetStyles( $"font-size: 12px; font-weight: bold; font-family: {Theme.DefaultFont}; color: white;" );

		if ( !string.IsNullOrWhiteSpace( e.Description ) )
		{
			var desc = c.Add( new Label( e.Description.Trim( '\n', '\r', '\t', ' ' ) ) );
			desc.WordWrap = true;
			desc.MinimumHeight = 1;
			desc.VerticalSizeMode = SizeMode.CanGrow;
		}
	}

	bool HasValue()
	{
		var value = property.GetValue<long>( 0 );
		return value == info.IntegerValue;
	}

	protected override void OnPaint()
	{
		if ( Paint.HasMouseOver || HasValue() )
		{
			Paint.SetBrushAndPen( Theme.Blue.WithAlpha( HasValue() ? 0.3f : 0.1f ) );
			Paint.DrawRect( LocalRect.Shrink( 2 ), 2 );
		}
	}
}
