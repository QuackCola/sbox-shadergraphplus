using Editor;

namespace NodeEditorPlus;

public class RerouteUI : NodeUI
{
	public class Comment : GraphicsItem
	{
		private string _text;
		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				Update();
			}
		}

		protected override void OnPaint()
		{
			if ( string.IsNullOrWhiteSpace( Text ) )
				return;

			Paint.Antialiasing = true;
			Paint.TextAntialiasing = true;

			Paint.SetDefaultFont( 10 );

			var rect = LocalRect;
			rect = Paint.MeasureText( rect, Text );
			rect.Width += 20;
			rect.Width = rect.Width.Clamp( 0, LocalRect.Width );
			rect.Position = LocalRect.Center - new Vector2( MathF.Floor( rect.Width ) * 0.5f, 0 );
			rect.Top = LocalRect.Top;
			rect.Bottom = LocalRect.Bottom - 5;

			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground.WithAlpha( 0.8f ) );
			Paint.DrawRect( rect, 2 );

			var pos = new Vector2( LocalRect.Center.x, LocalRect.Bottom - 5 );
			Paint.DrawArrow( pos, pos + Vector2.Down * 5, 10 );

			Paint.SetPen( Theme.TextControl );
			Paint.DrawText( rect, Text );
		}

		protected override void OnMousePressed( GraphicsMouseEvent e )
		{
			base.OnMousePressed( e );

			e.Accepted = false;
		}
	}

	private Comment _comment;

	public RerouteUI( GraphView graph, IGraphNode node ) : base( graph, node )
	{
		ZIndex = 0;
		Position = node.Position;
		Size = 16;
		HandlePosition = 0.5f;
		ToolTip = null;

		if ( node is IRerouteNode reroute )
		{
			_comment = new Comment
			{
				Parent = this,
				Size = new Vector2( 500, 30 ),
				Position = new Vector2( 0, -25 ),
				HandlePosition = new Vector2( 0.5f, 0.5f )
			};

			_comment.Bind( nameof( Comment.Text ) )
				.ReadOnly()
				.From( reroute, nameof( reroute.Comment ) );
		}
	}

	protected override void OnPaint()
	{
		var color = Outputs.First().HandleConfig.Color;

		if ( !Paint.HasMouseOver )
		{
			color = color.Desaturate( 0.2f ).Darken( 0.3f );
		}

		Paint.SetPen( Theme.ControlBackground, 2 );
		Paint.SetBrush( Paint.HasSelected ? SelectionOutline : color );
		Paint.DrawRect( LocalRect, 10 );
	}

	protected override void Layout()
	{
		var preferSelectingOutput = Inputs.Any( x => x.Connection is not null );

		foreach ( var input in Inputs )
		{
			input.Size = 14;
			input.Position = 14 * -0.5f;
			input.Visible = false;
			input.ZIndex = input.DefaultZIndex = preferSelectingOutput ? 0f : 2f;
		}

		foreach ( var output in Outputs )
		{
			output.Size = 14;
			output.Position = 14 * -0.5f;
			output.Visible = false;
			output.ZIndex = output.DefaultZIndex = preferSelectingOutput ? 2f : 0f;
		}
	}
}
