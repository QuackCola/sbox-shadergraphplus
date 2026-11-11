using Editor;
using System.Runtime.CompilerServices;

namespace ShaderGraphPlus;

public class Properties : Widget
{
	private Widget _sheetCanvas;
	private ScrollArea _scroller;
	private ControlSheet _sheet;
	private string _filterText;

	private PropertiesHeader _header;

	public object Target
	{
		get;
		set
		{
			if ( value == field )
				return;

			field = value;
			Rebuild();
			TargetChanged?.Invoke();
		}
	}

	private readonly Widget _sheetPanel;
	private readonly Layout _editor;

	public Action<SerializedProperty> PropertyUpdated { get; set; }
	public Action TargetChanged { get; set; }

	public Properties( Widget parent ) : base( parent )
	{
		Name = "Properties";
		WindowTitle = "Properties";
		SetWindowIcon( "edit" );
		MinimumWidth = 400;

		Layout = Layout.Column();
		Layout.Margin = 0;

		var toolbar = new ToolBar( this );
		var filter = new LineEdit( toolbar ) { PlaceholderText = "Filter Properties.." };
		filter.TextEdited += OnFilterEdited;
		toolbar.AddWidget( filter );

		Layout.Add( toolbar );

		Layout.AddSeparator();

		_header = Layout.Add( new PropertiesHeader() );

		_sheetPanel = new Widget( this ) { Layout = Layout.Column() };
		_sheetPanel.Layout.Margin = 0;
		_sheetPanel.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.WidgetBackground );
			Paint.DrawRect( _sheetPanel.LocalRect, Theme.ControlRadius );
			return true;
		};

		_scroller = _sheetPanel.Layout.Add( CreateScroller( _sheetPanel ), 1 );
		_sheetCanvas = _scroller.Canvas;

		_editor = Layout.AddColumn( 1 );
		_editor.Add( _sheetPanel, 1 );
	}

	private void Rebuild()
	{
		UpdateHeader();
		RebuildSheet();
	}

	private void UpdateHeader()
	{
		_header.Color = PropertiesHeader.DefaultIconColor;

		switch ( Target )
		{
			case BlackboardParameter parameter:
				var sgp = parameter.Graph as ShaderGraphPlus;

				var typeName = sgp.IsSubgraph
					? $"Subgraph {(parameter is IBlackboardSubgraphInputParameter ? "Input" : "Output")}"
					: parameter.DisplayInfo.Name ?? parameter.GetType().Name;

				SetHeader( $"{typeName} Parameter", parameter.Name );
				_header.Icon = parameter.DisplayInfo.Icon ?? "account_tree";
				_header.Color = ShaderGraphPlusTheme.GetTypeColor( parameter );
				break;
			case GroupData:
				_header.Text = "Group";
				_header.Icon = "folder";
				_header.Color = Theme.Folder;
				break;
			case CommentNode commentNode:
				_header.Text = "Comment";
				_header.Icon = commentNode.DisplayInfo.Icon ?? "account_tree";
				break;
			case BaseNodePlus node:
				_header.Text = node.DisplayInfo.Name ?? node.GetType().Name;
				_header.Icon = node.DisplayInfo.Icon ?? "account_tree";
				_header.Color = Color.Lerp( new Color( 0.7f, 0.7f, 0.7f ), Theme.Blue, 0.1f );
				break;
			default:
				SetHeader( "Graph Settings" );
				_header.Icon = "settings";
				break;
		}

		_header.Update();
	}

	/// <summary>
	/// Set the header to the selection's type name plus optional instance detail.
	/// </summary>
	private void SetHeader( string type, string detail = null )
	{
		_header.Text = type;
		_header.Detail = string.IsNullOrWhiteSpace( detail ) || detail == type ? null : detail;
	}

	private void RebuildSheet()
	{
		if ( _scroller is null || Target is null )
			return;

		var previousCanvas = _scroller.Canvas;
		var nextCanvas = CreateSheetCanvas( _scroller );
		nextCanvas.Visible = false;
		var content = CreateSheetCanvas( nextCanvas );
		content.Layout.Margin = 4;
		nextCanvas.Layout.Add( content );
		_sheetCanvas = content;

		try
		{
			var so = Target.GetSerialized();
			so.OnPropertyChanged += p =>
			{
				UpdateHeader();
				PropertyUpdated?.Invoke( p );
			};

			_sheet = new ControlSheet();
			_sheet.AddObject( so, p => PropertyFilter( p ) );

			_sheetCanvas.Layout.Add( _sheet );

			nextCanvas.Layout.AddStretchCell();

			_sheetCanvas = nextCanvas;
		}
		catch
		{
			_sheetCanvas = previousCanvas;
			nextCanvas.Destroy();
			throw;
		}

		_scroller.UpdatesEnabled = false;
		try
		{
			nextCanvas.Visible = true;
			_scroller.Canvas = nextCanvas;
		}
		finally
		{
			_scroller.UpdatesEnabled = true;
			_scroller.Update();
		}
	}

	private void OnFilterEdited( string filter )
	{
		_filterText = filter;
		_sheet.Clear( true );
		_sheet.AddObject( Target.GetSerialized(), PropertyFilter );
		_scroller.Update();
	}

	bool PropertyFilter( SerializedProperty property )
	{
		if ( property.HasAttribute<HideAttribute>() ) return false;
		if ( string.IsNullOrEmpty( _filterText ) ) return true;
		if ( property.Name.ToLower().Contains( _filterText.ToLower() ) ) return true;
		if ( property.DisplayName.ToLower().Contains( _filterText.ToLower() ) ) return true;
		if ( property.TryGetAsObject( out var obj ) )
		{
			if ( property.TryGetAttribute<ConditionalVisibilityAttribute>( out var conditional ) )
			{
				if ( conditional.TestCondition( obj ) ) return false;
			}
			foreach ( var childProp in obj )
			{
				if ( childProp.HasAttribute<HideAttribute>() ) continue;
				if ( childProp.Name.ToLower().Contains( _filterText.ToLower() ) || childProp.DisplayName.ToLower().Contains( _filterText.ToLower() ) )
				{
					_sheet.AddRow( childProp );
				}
			}
		}
		return false;
	}

	private static ScrollArea CreateScroller( Widget parent )
	{
		var scroller = new ScrollArea( parent );
		scroller.VerticalScrollbarMode = ScrollbarMode.Auto;
		scroller.HorizontalScrollbarMode = ScrollbarMode.Auto;
		scroller.TranslucentBackground = true;
		scroller.NoSystemBackground = true;
		// Scope to the scroll area itself, otherwise the rules cascade into every
		// descendant - including the QToolTip popups shown for the rows inside it.
		scroller.Name = "AnimGraphPropertiesScroll";
		scroller.SetStyles( "#AnimGraphPropertiesScroll { border: none; background: transparent; }" );
		scroller.Canvas = CreateSheetCanvas( scroller );
		return scroller;
	}

	private static Widget CreateSheetCanvas( Widget parent )
	{
		return new Widget( parent )
		{
			Layout = Layout.Column(),
			HorizontalSizeMode = SizeMode.Flexible,
			VerticalSizeMode = SizeMode.CanGrow,
			TranslucentBackground = true,
			NoSystemBackground = true,
		};
	}
}

class PropertiesHeader : Widget
{
	public static readonly Color DefaultIconColor = global::Color.Parse( "#8E9199" ) ?? Theme.TextControl;

	public string Text { get; set; } = "Properties";
	public string Detail { get; set; }
	public string Icon { get; set; } = "settings";
	public Color Color { get; set; } = DefaultIconColor;

	public PropertiesHeader() : base( null )
	{
		FixedHeight = Theme.RowHeight + 2;

		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( 0, 1, 4, 1 );
		Layout.AddStretchCell();
	}

	protected override void OnPaint()
	{
		var rect = LocalRect;

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		Paint.ClearPen();
		Paint.SetBrush( Theme.SurfaceBackground );
		Paint.DrawRect( rect );

		// Icon
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			var iconRect = new Rect( rect.Left + 4, rect.Top, 22, rect.Height );
			Paint.SetPen( Color );
			Paint.DrawIcon( iconRect, Icon, 16, TextFlag.Center );
		}

		// Title: type tinted with the selection color, instance detail in plain text
		var textLeft = string.IsNullOrEmpty( Icon ) ? 4f : 30f;
		var textRight = rect.Width;
		var textRect = new Rect( textLeft, rect.Top, textRight - textLeft, rect.Height );
		Paint.SetHeadingFont( 11, 440, sizeInPixels: true );
		Paint.SetPen( Color == DefaultIconColor ? Theme.Text : Color );
		var drawn = Paint.DrawText( textRect, Text, TextFlag.LeftCenter );

		if ( !string.IsNullOrEmpty( Detail ) )
		{
			textRect.Left = drawn.Right + 6;
			Paint.SetPen( Theme.TextControl.WithAlpha( 0.5f ) );
			drawn = Paint.DrawText( textRect, "·", TextFlag.LeftCenter );

			textRect.Left = drawn.Right + 6;
			Paint.SetPen( Theme.Text );
			Paint.DrawText( textRect, Detail, TextFlag.LeftCenter );
		}
	}
}
