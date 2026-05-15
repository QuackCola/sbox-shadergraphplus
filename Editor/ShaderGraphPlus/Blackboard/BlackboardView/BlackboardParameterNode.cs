using Editor;
using NodeEditorPlus;
using System.Text;

namespace ShaderGraphPlus;

public class BlackboardParameterNode : TreeNode<BlackboardParameter>
{
	public BlackboardParameterNode( BlackboardParameter p ) : base( p )
	{
		Height = Theme.RowHeight;
	}

	private DisplayInfo DisplayInfo => Value.DisplayInfo;

	public override bool HasChildren => false;

	public override string Name
	{
		get => Value.Name;
		set => Value.Name = value;
	}

	///<summary>
	///Called when a blackboard parameter is deleated.
	///</summary>
	public Action<BlackboardParameter> OnParameterDeleted { get; set; }

	public override string GetTooltip()
	{
		var sb = new StringBuilder();

		sb.AppendLine( $"<h3>{Name}</h3>" );

		var usrDesc = "";

		if ( Value is IBlackboardSubgraphParameter subgraphParameter )
		{
			usrDesc = subgraphParameter.Description;
		}

		if ( !string.IsNullOrWhiteSpace( usrDesc ) )
		{
			sb.AppendLine( $"<br />" );
			sb.AppendLine( $"<i>{usrDesc}</i>" );
		}

		return sb.ToString();
	}

	public override bool CanEdit => false;

	public override int ValueHash
	{
		get
		{
			HashCode hc = new HashCode();
			hc.Add( Value.Name );

			return hc.ToHashCode();
		}
	}

	public override void OnPaint( VirtualWidget item )
	{
		var variable = Value;
		var isEven = item.Row % 2 == 0;
		var isHovered = item.Hovered;
		var selected = item.Selected || item.Pressed || item.Dragging;
		var typeColor = Color.White;
		var typeName = DisplayInfo.ForType( variable.GetType() ).Name;
		var isSubgraphoutput = variable.GetType().IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) );

		var fullSpanRect = item.Rect;
		fullSpanRect.Left = 0;
		fullSpanRect.Right = TreeView.Width;

		if ( ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( variable.GetType(), out var blackboardConfig ) )
		{
			typeColor = blackboardConfig.Color;
		}

		Paint.ClearPen();

		if ( selected )
		{
			Paint.SetBrush( Theme.Blue.WithAlpha( 0.1f ) );
			Paint.DrawRect( fullSpanRect );
		}
		else if ( isHovered )
		{
			Paint.SetBrush( Theme.SelectedBackground.WithAlpha( 0.25f ) );
			Paint.DrawRect( fullSpanRect );
		}
		else if ( isEven )
		{
			Paint.SetBrush( Theme.SurfaceLightBackground.WithAlpha( 0.1f ) );
			Paint.DrawRect( fullSpanRect );
		}

		var textAlpha = (selected ? 1.0f : 0.85f);
		var iconAlpha = selected ? 0.95f : 0.75f;

		var rect = new Rect( item.Rect.Position, new Vector2( item.Rect.Width, Height ) );
		var outerTypeRect = rect.Shrink( 4f, 0f, 4f, 0f );

		var typeNameWidth = Paint.MeasureText( typeName ).x + 24f;
		var parameterNameWidth = Paint.MeasureText( Value.Name ).x + 24f;

		outerTypeRect.Right += 4;

		outerTypeRect.Size = outerTypeRect.Size.WithX( typeNameWidth + 24 );

		Paint.SetPen( Theme.TextControl.WithAlpha( textAlpha * 1.0f ) );

		Paint.DrawText( outerTypeRect, typeName, TextFlag.Center | TextFlag.SingleLine );

		Paint.SetPen( Theme.TextControl.WithAlpha( textAlpha * 1.0f ) );

		Paint.SetPen( typeColor );

		Paint.DrawIcon( outerTypeRect.Shrink( 6 ), "circle", 12, (isSubgraphoutput ? TextFlag.RightCenter : TextFlag.LeftCenter) | TextFlag.SingleLine );

		var typeBackgroundRect = outerTypeRect.Shrink( 4 );

		Paint.SetPen( Theme.ControlBackground.Lighten( 2.5f ) );
		Paint.DrawRect( typeBackgroundRect, Theme.ControlRadius );

		var nameRect = outerTypeRect;
		nameRect.Left += typeBackgroundRect.Width + 12f;

		Paint.SetPen( Theme.TextControl.WithAlpha( textAlpha * 1.0f ) );
		Paint.DrawText( nameRect.Grow( 0f, 0f, 400, 0f ), Value.Name, TextFlag.LeftCenter | TextFlag.SingleLine );
	}

	public override bool OnDragStart()
	{
		var drag = new Drag( TreeView );

		if ( TreeView.IsSelected( Value ) )
		{
			drag.Data.Object = Value;

			drag.Execute();

			return true;
		}

		return false;
	}

	public override bool OnContextMenu()
	{
		var m = new ContextMenu( TreeView ) { Searchable = false };

		m.AddOption( "Delete", "delete", () => { OnParameterDeleted?.Invoke( Value ); }, "editor.delete" );
		//m.AddOption( "Rename", "label", TreeView.BeginRename, "editor.rename" );

		m.OpenAtCursor( false );

		return true;
	}
}

class BlackboardParameterSearchNode : BlackboardParameterNode
{
	public override bool HasChildren => false;
	public BlackboardParameterSearchNode( BlackboardParameter p ) : base( p )
	{
	}
}
