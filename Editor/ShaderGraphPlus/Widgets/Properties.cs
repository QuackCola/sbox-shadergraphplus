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

	private object _target;
	public object Target
	{
		get => _target;
		set
		{
			if ( value == _target )
				return;

			_target = value;
			Rebuild();
			TargetChanged?.Invoke();
		}
	}

	private readonly Layout Editor;

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

		_sheet = new ControlSheet();

		_scroller = new ScrollArea( this );
		_scroller.Canvas = new Widget();
		_scroller.Canvas.Layout = Layout.Column();
		_scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
		_scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;
		_scroller.Canvas.Layout.Add( _sheet );
		_scroller.Canvas.Layout.AddStretchCell();

		_sheetCanvas = _scroller.Canvas;

		Editor = Layout.AddColumn( 1 );
		Editor.Add( _scroller );

		Layout.AddStretchCell();
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public bool IsTarget<T>( out T targetValue )
	{
		targetValue = default( T );

		if ( Target is T target )
		{
			targetValue = (T)target;

			return true;
		}

		return false;
	}

	private void Rebuild()
	{
		UpdateHeader();
		RebuildSheet();
	}

	private void UpdateHeader()
	{
		switch ( _target )
		{
			case ShaderGraphPlus graph:
				_header.Text = "Graph Settings";
				_header.Icon = "settings";
				break;
			case CommentNode commentNode:
				_header.Text = "Comment";
				_header.Icon = commentNode.DisplayInfo.Icon ?? "account_tree";
				break;
			case BaseNodePlus node:
				_header.Text = node.DisplayInfo.Name ?? node.GetType().Name;
				_header.Icon = node.DisplayInfo.Icon ?? "account_tree";
				break;
			case BlackboardParameter parameter:
				_header.Text = parameter.DisplayInfo.Name ?? parameter.GetType().Name;
				_header.Icon = parameter.DisplayInfo.Icon ?? "account_tree";
				break;
			case CategoryData category:
				_header.Text = "Category";
				_header.Icon = "folder";
				break;
			default:
				_header.Text = "Properties";
				_header.Icon = "settings";
				break;
		}

		_header.Update();
	}

	private void RebuildSheet()
	{
		if ( _scroller is null || _target is null )
			return;

		var previousCanvas = _scroller.Canvas;
		var nextCanvas = new Widget( _scroller )
		{
			Layout = Layout.Column(),
			HorizontalSizeMode = SizeMode.Flexible,
			VerticalSizeMode = SizeMode.CanGrow,
			TranslucentBackground = true,
			NoSystemBackground = true,
			Visible = false
		};

		_sheetCanvas = nextCanvas;

		try
		{
			var so = _target.GetSerialized();
			so.OnPropertyChanged += p =>
			{
				PropertyUpdated?.Invoke( p );
			};

			_sheet = new ControlSheet();
			_sheet.AddObject( so, p => PropertyFilter( p ) );

			_sheetCanvas.Layout.Add( _sheet );

			_sheetCanvas.Layout.AddStretchCell();
			nextCanvas.AdjustSize();
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
		_sheet.AddObject( _target.GetSerialized(), PropertyFilter );
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
}

class PropertiesHeader : Widget
{
	public string Text { get; set; } = "Properties";
	public string Icon { get; set; } = "settings";

	public PropertiesHeader() : base( null )
	{
		FixedHeight = Theme.RowHeight + 2;
	}

	protected override void OnPaint()
	{
		var rect = LocalRect;

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		Paint.ClearPen();
		Paint.SetBrush( Theme.SurfaceBackground );
		Paint.DrawRect( rect );

		var iconColor = Color.Parse( "#8E9199" ) ?? Theme.TextControl;

		// Icon
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			var iconRect = new Rect( rect.Left + 4, rect.Top, 22, rect.Height );
			Paint.SetPen( iconColor );
			Paint.DrawIcon( iconRect, Icon, 16, TextFlag.Center );
		}

		// Title
		var textLeft = string.IsNullOrEmpty( Icon ) ? 4f : 30f;
		var textRect = new Rect( textLeft, rect.Top, rect.Width - textLeft, rect.Height );
		Paint.SetPen( Theme.Text );
		Paint.SetHeadingFont( 11, 440, sizeInPixels: true );
		Paint.DrawText( textRect, Text, TextFlag.LeftCenter );
	}
}
