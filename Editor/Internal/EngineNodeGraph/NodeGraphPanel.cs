using Sandbox.UI;

namespace EngineNodeGraph;

public enum NodeGraphGridStyle
{
	LineGrid,
	DotGrid,
}

/// <summary>
/// Shows an <see cref="IGraph"/> as nodes and wires, like the editor's node graph. Middle-drag
/// pans, the wheel zooms, left-drag on empty space box-selects, Delete removes the selection.
/// </summary>
[StyleSheet.Inline( "nodegraphpanel", ".nodegraphpanel { min-width: 0; min-height: 0; } .nodegraphpanel .canvas-selection-box { border: 1px solid #4C8DFF; background-color: rgba( 76, 141, 255, 0.1 ); border-radius: 4px; }" )]
public partial class NodeGraphPanel : CanvasPanel
{
	/// <summary>
	/// Nodes snap to this, in canvas units.
	/// </summary>
	public float GridSize { get; set; } = 12f;

	/// <summary>
	/// Widen every node to fit its whole title, as this graph used to. Off sizes nodes like the qt
	/// graph and cuts long titles of fixed-size nodes; auto-size ones always fit.
	/// </summary>
	public bool FitNodeTitles
	{
		get => _fitNodeTitles;
		set
		{
			if ( _fitNodeTitles == value ) return;
			_fitNodeTitles = value;
			foreach ( var node in _nodes ) node.MarkNodeChanged();
		}
	}

	bool _fitNodeTitles;

	/// <summary>
	/// Fade the grid out as the view zooms out.
	/// </summary>
	public virtual bool FadeOutBackground => true;

	public NodeGraphGridStyle GridStyle { get; set; } = NodeGraphGridStyle.LineGrid;

	IGraph _graph;

	/// <summary>
	/// The graph shown. Setting a different one rebuilds the view; call <see cref="RebuildFromGraph"/>
	/// to refresh the same one.
	/// </summary>
	public IGraph Graph
	{
		get => _graph;
		set
		{
			if ( ReferenceEquals( _graph, value ) ) return;

			_graph = value;
			RebuildFromGraph();
		}
	}

	readonly List<NodePanel> _nodes = new();

	public IReadOnlyList<NodePanel> Nodes => _nodes;

	public IEnumerable<NodePanel> SelectedNodes => _nodes.Where( x => x.Selected );

	/// <summary>
	/// The node selection changed.
	/// </summary>
	public event Action SelectionChanged;

	/// <summary>
	/// When a node value changes. Primarly via a <seealso cref="NodeValueEditor"/>
	/// </summary>
	public event Action OnNodeValueChanged;

	bool _viewInitialized;

	public NodeGraphPanel()
	{
		AddClass( "nodegraphpanel" );
		BoxSelectionEnabled = true;
		MinimumViewSpan = 10;
		MaximumViewSpan = 100000;

		// Named methods, not lambdas: hotload can't remap lambdas held by a live panel
		BoxSelectionStarted += OnBoxSelectionStarted;
		BoxSelectionChanged += OnBoxSelectionChanged;
		BoxSelectionFinished += OnBoxSelectionFinished;
		OnTooltip = BuildConnectionTooltip;
	}

	/// <summary>
	/// Outline and glow of selected nodes and comments, and the colour of selected wires.
	/// </summary>
	public Color SelectionColor { get; set; } = NodeTheme.Accent;

	/// <summary>
	/// Outline of a node under the cursor.
	/// </summary>
	public Color HoverBorderColor { get; set; } = NodeTheme.OutlineHover;

	/// <summary>
	/// Outline of a node that isn't selected, hovered or in error.
	/// </summary>
	public Color BorderColor { get; set; } = NodeTheme.Outline;

	/// <summary>
	/// Outline and glow of a <see cref="NodePanel.Highlighted"/> node. Apart from the selection's, so
	/// both read at once.
	/// </summary>
	public Color HighlightColor { get; set; } = Color.Parse( "#F5B942" ).Value;

	public NodeHandleConfig DefaultHandleConfig { get; } = new( null, Color.Parse( "#999" ).Value );

	readonly Dictionary<Type, NodeHandleConfig> _handleConfigCache = new();

	protected virtual NodeHandleConfig OnGetHandleConfig( Type type ) => DefaultHandleConfig;

	/// <summary>
	/// How plugs and wires of <paramref name="type"/> look.
	/// </summary>
	public NodeHandleConfig GetHandleConfig( Type type )
	{
		if ( _handleConfigCache.TryGetValue( type, out var config ) )
		{
			return config;
		}

		config = OnGetHandleConfig( type );

		return _handleConfigCache[type] = config with { Name = config.Name ?? type.ToSimpleString( false ).HtmlEncode() };
	}

	/// <summary>
	/// The panel for <paramref name="node"/>: a <see cref="ReroutePanel"/> for reroutes, a
	/// <see cref="CommentPanel"/> for comments.
	/// </summary>
	protected virtual NodePanel CreateNodePanel( INode node, bool isPreview = false ) => node switch
	{
		IRerouteNode => new ReroutePanel( this, node, isPreview ),
		ICommentNode => new CommentPanel( this, node, isPreview ),
		_ => new NodePanel( this, node, isPreview ),
	};

	/// <summary>
	/// A see-through panel for <paramref name="node"/> that isn't in the graph, such as a drop preview.
	/// Delete it when done.
	/// </summary>
	public NodePanel CreatePreview( INode node ) => Content.AddChild( CreateNodePanel( node, true ) );

	public NodePanel FindNode( INode node ) => _nodes.FirstOrDefault( x => x.Node == node );

	public PlugPanel FindPlug( IPlug plug )
	{
		if ( plug is null || FindNode( plug.Node ) is not { } node ) return null;

		return plug is IPlugIn
			? node.Inputs.FirstOrDefault( x => x.Inner == plug )
			: node.Outputs.FirstOrDefault( x => x.Inner == plug );
	}

	/// <summary>
	/// The plug whose row contains <paramref name="position"/>, in canvas units. Comments are skipped:
	/// they have no plugs and sit behind what they hold, so they mustn't hide their members' plugs.
	/// </summary>
	protected PlugPanel GetPlugAt( Vector2 position )
	{
		for ( var i = _nodes.Count - 1; i >= 0; i-- )
		{
			var node = _nodes[i];
			if ( node is CommentPanel || !new Rect( node.Position, node.Size ).IsInside( position ) ) continue;

			return node.GetPlugAt( position );
		}

		return null;
	}

	/// <summary>
	/// The node at <paramref name="position"/>, in canvas units: the one owning the plug there,
	/// else the topmost node, else the innermost comment.
	/// </summary>
	protected NodePanel GetNodeAt( Vector2 position )
	{
		if ( GetPlugAt( position ) is { } plug ) return plug.Node;

		NodePanel comment = null;

		for ( var i = _nodes.Count - 1; i >= 0; i-- )
		{
			var node = _nodes[i];
			if ( !new Rect( node.Position, node.Size ).IsInside( position ) ) continue;

			if ( node is not CommentPanel ) return node;
			comment ??= node;
		}

		return comment;
	}

	/// <summary>
	/// When a node value changes. Primarly via a <seealso cref="NodeValueEditor"/>
	/// </summary>
	public virtual void NodeValueChanged()
	{
		OnNodeValueChanged?.Invoke();
	}

	internal Vector2 MouseCanvas => ScreenToCanvas( MousePosition );

	public override void Tick()
	{
		if ( !_viewInitialized && Viewport.Width > 0 && Viewport.Height > 0 )
		{
			_viewInitialized = true;
			SetView( Vector2.Zero, 1f );
			RestoreViewFromCookie();
		}

		if ( _viewInitialized )
		{
			KeepZoomOnResize();
			AutoPan();
			SaveViewCookie();
		}

		if ( _nodePreview.IsValid() && _previewFollowsMouse )
		{
			_nodePreview.MoveOriginTo( MouseCanvas );
		}

		base.Tick();
	}

	Vector2 _viewportSize;
	float _viewZoom;

	/// <summary>
	/// The view is kept in canvas units, so a resized viewport would rescale the graph - such as when
	/// a new panel's dock settles a few frames after the cookie view was restored. Keep the zoom instead.
	/// </summary>
	void KeepZoomOnResize()
	{
		var size = Viewport.Size;
		if ( size.x <= 0 || size.y <= 0 ) return;

		if ( size != _viewportSize && _viewZoom > 0 )
		{
			SetView( (ViewMin + ViewMax) * 0.5f, _viewZoom );
		}

		_viewportSize = size;
		_viewZoom = Zoom;
	}

	public override void OnDraw( Painter painter )
	{
		var viewport = Viewport;

		if ( viewport.Width <= 0 || viewport.Height <= 0 )
			return;

		using var scope = painter.Scope();
		painter.Clip( viewport );

		painter.Fill = NodeTheme.Canvas;
		painter.Stroke = Stroke.None;
		painter.Rect( viewport );
		
		var alpha = FadeOutBackground ? Math.Clamp( ( Zoom - 0.3f ) / 0.3f, 0f, 1f ) : 1f;
		
		if ( alpha > 0f )
		{
			switch ( GridStyle )
			{
				case NodeGraphGridStyle.LineGrid:
					DrawLineGrid( painter, viewport, alpha );
					break;
				case NodeGraphGridStyle.DotGrid:
					DrawDotGrid( painter, viewport, alpha );
					break;
				default:
					DrawLineGrid( painter, viewport, alpha );
					break;
			}
		}

		// Before the nodes draw: they paint the wires laid out here
		UpdateConnections();
	}

	static Texture _dot;

	/// <summary>
	/// One soft white dot in the middle of a tile.
	/// </summary>
	private static Texture DotTexture()
	{
		if ( _dot is not null ) return _dot;

		const int size = 32;
		var data = new byte[size * size * 4];

		for ( var y = 0; y < size; y++ )
		{
			for ( var x = 0; x < size; x++ )
			{
				var distance = new Vector2( x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f ).Length;
				var i = (y * size + x) * 4;

				data[i] = data[i + 1] = data[i + 2] = 255;
				data[i + 3] = (byte)(Math.Clamp( 2f - distance, 0f, 1f ) * 255);
			}
		}

		return _dot = Texture.Create( size, size ).WithData( data ).Finish();
	}

	/// <summary>
	/// A dot every two grid steps, fading out as the view zooms out.
	/// </summary>
	private void DrawDotGrid( Painter painter, Rect viewport, float alpha )
	{
		var step = GridSize * 2f;
		var tile = step * ( CanvasToScreen( Vector2.One ) - CanvasToScreen( Vector2.Zero ) ).x;
		var origin = CanvasToScreen( Vector2.Zero ) - viewport.Position - new Vector2( tile * 0.5f );
		var offset = new Vector2( ( origin.x % tile + tile ) % tile, ( origin.y % tile + tile ) % tile );

		painter.Fill = Fill.Image( DotTexture(), Color.White.WithAlpha( 0.1f * alpha ), Length.Pixels( tile ), Length.Pixels( tile ),
			Length.Pixels( offset.x ), Length.Pixels( offset.y ), BackgroundRepeat.Repeat );
		painter.Rect( viewport );
	}

	private void DrawLineGrid( Painter painter, Rect viewport, float alpha )
	{
		var min = ScreenToCanvas( viewport.Position );
		var max = ScreenToCanvas( viewport.Position + viewport.Size );
		var pixelsPerUnit = viewport.Width / (max.x - min.x);

		var step = GridSize;
		var majorEvery = GridSize / 2;

		while ( step * pixelsPerUnit < majorEvery )
		{
			step *= 2;
		}
			
		var major = Math.Max( step, GridSize * majorEvery );
		var minorColor = Color.White.WithAlpha( 0.04f * alpha );
		var majorColor = Color.White.WithAlpha( 0.09f * alpha );

		if ( step < major )
		{
			DrawLines( painter, min, max, step, major, minorColor );
		}
			
		DrawLines( painter, min, max, major, 0, majorColor );
	}

	private void DrawLines( Painter painter, Vector2 min, Vector2 max, float step, float skipEvery, Color color )
	{
		painter.Stroke = Stroke.Solid( color, 1 );

		for ( var x = MathF.Ceiling( min.x / step ) * step; x <= max.x; x += step )
		{
			if ( skipEvery > 0 && IsMultiple( x, skipEvery ) )
				continue;
			
			painter.Line( CanvasToScreen( new Vector2( x, min.y ) ), CanvasToScreen( new Vector2( x, max.y ) ) );
		}

		for ( var y = MathF.Ceiling( min.y / step ) * step; y <= max.y; y += step )
		{
			if ( skipEvery > 0 && IsMultiple( y, skipEvery ) )
				continue;

			painter.Line( CanvasToScreen( new Vector2( min.x, y ) ), CanvasToScreen( new Vector2( max.x, y ) ) );
		}
	}

	private static bool IsMultiple( float value, float of )
	{
		var r = MathF.Abs( value % of );
		return r < 0.001f || of - r < 0.001f;
	}
}
