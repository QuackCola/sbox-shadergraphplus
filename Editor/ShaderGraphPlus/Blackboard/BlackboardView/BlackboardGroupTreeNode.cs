using Editor;
using System.Text;
using static Editor.BaseItemWidget;

namespace ShaderGraphPlus;

public class BlackboardGroupTreeNode : BlackboardTreeNode
{
	private IEnumerable<BlackboardParameter> _referencedParameters;

	protected override bool CanHaveChildren => true;

	public override string Name
	{
		get => Value.Name;
		set => Value.Name = value;
	}

	public new CategoryData Value
	{
		get => base.Value as CategoryData;
		set => base.Value = value;
	}

	public new ShaderGraphPlusBlackboardTree TreeView
	{
		get => base.TreeView as ShaderGraphPlusBlackboardTree;
	}

	public override bool HasChildren => Value.ParameterReferences.Any();

	public override int ValueHash
	{
		get
		{
			if ( Value?.ParameterReferences is null )
				return 0;

			HashCode hc = new HashCode();

			foreach ( var reference in Value.ParameterReferences )
			{
				hc.Add( reference );
			}

			return hc.ToHashCode();
		}
	}

	public BlackboardGroupTreeNode( CategoryData categoryData ) : base( categoryData )
	{
	}

	public override string GetTooltip()
	{
		var baseTooltip = base.GetTooltip();

		var sb = new StringBuilder();

		sb.AppendLine( baseTooltip );

		sb.AppendLine( $"<hr />" );
		sb.AppendLine( $"<b>Parameters:</b>" );

		if ( _referencedParameters != null )
		{
			foreach ( var p in _referencedParameters )
			{
				sb.AppendLine( $"<br />" );
				sb.AppendLine( $"- {p.Name}" );
			}
		}

		return sb.ToString();
	}

	protected override void BuildChildren()
	{
		if ( HasChildren )
		{
			_referencedParameters = Value.GetReferencedParameters();
			SetChildren( _referencedParameters, x => new ShaderGraphPlusParameterTreeNode( x ) );
		}
		else
		{
			_referencedParameters = null;
			ClearChildren();
		}
	}

	protected override bool HasDescendant( object obj )
	{
		return Value.IsDescendant( obj as IGroupableBlackboardParameter );
	}

	protected override void OnPaintItemDropping( VirtualWidget item, Rect dropRect )
	{
		var dragEvent = TreeView.CurrentItemDragEvent;
		var sourceNode = dragEvent.Data.Object as BlackboardTreeNode;
		var targetNode = this;

		Paint.ClearPen();
		Paint.SetBrush( Theme.Blue );

		if ( dragEvent.DropEdge.HasFlag( ItemEdge.Top ) )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( dropRect.TopLeft, dropRect.TopRight );
		}
		else if ( dragEvent.DropEdge.HasFlag( ItemEdge.Bottom ) )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( dropRect.BottomLeft, dropRect.BottomRight );
		}
		else if ( sourceNode is not BlackboardGroupTreeNode && targetNode is BlackboardGroupTreeNode )
		{
			Paint.SetBrushAndPen( Theme.Blue.WithAlpha( 0.2f ), Theme.Blue );
			Paint.PenSize = 2;
			Paint.DrawRect( item.Rect, 4 );
		}
		else if ( sourceNode is BlackboardGroupTreeNode && targetNode is BlackboardGroupTreeNode )
		{
			var groupRect = item.Rect;
			groupRect.Left = 0;
			groupRect.Right = TreeView.Width;

			Paint.SetBrushAndPen( Theme.Blue.WithAlpha( 0.2f ), Theme.Blue );
			Paint.PenSize = 2;
			Paint.DrawRect( groupRect, 4 );
		}
	}

	protected override void PaintSpecial( Rect item )
	{
		float opacity = 0.9f;

		var r = item;
		r.Left += 4;

		Paint.SetDefaultFont();

		Paint.Pen = Color.White;
		Paint.DrawIcon( r, "folder", 12, TextFlag.LeftCenter | TextFlag.SingleLine );

		r.Left += 22;

		Paint.Pen = Theme.TextControl.WithAlphaMultiplied( opacity );

		r.Left += Paint.DrawText( r, Name, TextFlag.LeftCenter | TextFlag.SingleLine ).Width + 4;
	}

	protected override void InsertNodeAtEdge( BlackboardTreeNode source, BaseItemWidget.ItemEdge edge )
	{
		var graph = TreeView.Graph;

		if ( source.Value is IGroupableBlackboardParameter parameter && edge == ItemEdge.None )
		{
			//using var undoScope = TreeView?.UndoScope( "Move Parameter To Group" );

			if ( parameter.IsGrouped )
			{
				RemoveFromGroup( parameter );
			}

			parameter.GroupReference = Value.Identifier;
			Value.ParameterReferences.Add( parameter.Identifier );

			TreeView.TreeNodes.Remove( source );
		}
		else if ( source.Value is CategoryData sourceCategory && edge == ItemEdge.None )
		{
			var sourcePriority = sourceCategory.Priority;
			var targetPriority = Value.Priority;

			if ( sourcePriority > targetPriority )
			{
				targetPriority++;
			}

			if ( targetPriority > TreeView.TreeNodes.Count() - 1 )
			{
				targetPriority = TreeView.TreeNodes.Count() - 1;
			}

			TreeView?.Graph?.UpdateCategoryPriority( sourceCategory, targetPriority );
		}
	}

	private int RemoveFromGroup( IGroupableBlackboardParameter sourceParameter )
	{
		if ( TreeView.Graph is ShaderGraphPlus graph )
		{
			if ( graph.TryFindCategoryData( sourceParameter.GroupReference, out var referencedGroup ) )
			{
				sourceParameter.GroupReference = Guid.Empty;

				referencedGroup.ParameterReferences.Remove( sourceParameter.Identifier );

				graph.UpdateParameter( sourceParameter );
				graph.UpdateCategoryData( referencedGroup );

				return referencedGroup.Priority;
			}
		}

		return -1;
	}
}

class BlackboardGroupSearchTreeNode : BlackboardGroupTreeNode
{
	public override bool HasChildren => false;

	public BlackboardGroupSearchTreeNode( CategoryData categoryData ) : base( categoryData )
	{
	}
}
