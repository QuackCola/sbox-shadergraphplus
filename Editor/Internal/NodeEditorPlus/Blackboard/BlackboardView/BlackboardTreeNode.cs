using Editor;
using static Editor.BaseItemWidget;

namespace NodeEditorPlus.Blackboard;

public abstract class BlackboardTreeNode : TreeNode
{
	public override bool HasChildren => false;

	protected virtual bool CanHaveChildren => false;

	public virtual bool IsGrouped => false;

	public override bool CanEdit => true;

	public new BlackboardTree TreeView
	{
		get => base.TreeView as BlackboardTree;
	}

	public BlackboardTreeNode( object value ) : base( value )
	{
		Height = Theme.RowHeight;
	}

	public BlackboardTreeNode() : this( null )
	{
	}

	public override void OnPaint( VirtualWidget item )
	{
		var targetValue = Value;
		var isEven = item.Row % 2 == 0;
		var isHovered = item.Hovered;
		var selected = item.Selected || item.Pressed || item.Dragging;

		var fullSpanRect = item.Rect;
		fullSpanRect.Left = 0;
		fullSpanRect.Right = TreeView.Width;

		float opacity = 0.9f;

		if ( item.Dropping )
		{
			var dropRect = item.Rect;
			dropRect.Left = 0;
			dropRect.Right = TreeView.Width;

			OnPaintItemDropping( item, dropRect );
		}

		if ( selected )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.SelectedBackground.WithAlpha( opacity ) );
			Paint.DrawRect( fullSpanRect );
		}
		else if ( isHovered )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.SelectedBackground.WithAlpha( 0.25f ) );
			Paint.DrawRect( fullSpanRect );
		}
		if ( isEven )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.SurfaceLightBackground.WithAlpha( 0.1f ) );
			Paint.DrawRect( fullSpanRect );
		}

		PaintSpecial( item.Rect );
	}

	protected virtual void OnPaintItemDropping( VirtualWidget item, Rect dropRect )
	{
		var dragEvent = TreeView.CurrentItemDragEvent;

		Paint.ClearPen();
		Paint.SetBrush( Theme.Blue );

		if ( dragEvent.DropEdge.HasFlag( ItemEdge.Top ) )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( dropRect.TopLeft, dropRect.TopRight );
		}
		else if ( dragEvent.DropEdge.HasFlag( ItemEdge.Bottom ) || dragEvent.DropEdge.HasFlag( ItemEdge.None ) )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( dropRect.BottomLeft, dropRect.BottomRight );
		}
	}

	protected abstract void PaintSpecial( Rect item );

	public override bool OnDragStart()
	{
		var drag = new Drag( TreeView );
		drag.Data.Object = this;
		drag.Execute();

		return true;
	}

	public override DropAction OnDragDrop( BaseItemWidget.ItemDragEvent e )
	{
		var targetNode = this;

		if ( e.Data.Object is BlackboardTreeNode sourceNode )
		{
			if ( sourceNode == targetNode || sourceNode.Value == Value )
				return DropAction.Ignore;

			if ( sourceNode.CanHaveChildren && targetNode.IsGrouped )
				return DropAction.Ignore;

			if ( sourceNode.IsDescendantOf( targetNode ) )
				return DropAction.Ignore;

			//Log.Info( $"Source '{sourceNode.Name}' Target '{Name}' DropEdge '{e.DropEdge}" );

			if ( e.IsDrop )
			{
				using var undoScope = TreeView?.UndoScope( "Change Parameter Order" );

				InsertNodeAtEdge( sourceNode, e.DropEdge );
			}

			return DropAction.Move;
		}

		return DropAction.Ignore;
	}

	protected abstract void InsertNodeAtEdge( BlackboardTreeNode source, ItemEdge edge );

	public override bool OnContextMenu()
	{
		var contextMenu = new ContextMenu( TreeView ) { Searchable = false };

		contextMenu.AddOption( "Rename", "label", TreeView.BeginRename, "editor.rename" );
		contextMenu.AddOption( "Delete", "delete", TreeView.DeleteSelection, "editor.delete" );

		contextMenu.AddSeparator();

		OnPopulateContextMenuSpecialOptions( contextMenu );

		contextMenu.OpenAtCursor( false );

		return true;
	}

	protected virtual void OnPopulateContextMenuSpecialOptions( ContextMenu m )
	{
	}

	protected bool TryGetValueAs<T>( out T value )
	{
		value = default;

		if ( Value.GetType() == typeof( T ) )
		{
			value = (T)Value;
			return true;
		}

		return false;
	}

	/// <summary>
	/// If the provided TreeNode is an ancestor of this TreeNode instance.
	/// </summary>
	/// <param name="potentialAncestor"></param>
	/// <returns></returns>
	protected bool IsDescendantOf( BlackboardTreeNode potentialAncestor )
	{
		var current = Parent as BlackboardTreeNode;
		while ( current != null )
		{
			if ( current == potentialAncestor )
				return true;
			current = current.Parent as BlackboardTreeNode;
		}
		return false;
	}
}

/// <summary>
/// A small wrapper that changes Value into the T type and TreeView into the Y Type.
/// </summary>
public abstract class BlackboardTreeNode<T, Y> : BlackboardTreeNode where T : class where Y : BlackboardTree
{
	public new T Value
	{
		get => base.Value as T;
		set => base.Value = value;
	}

	public new Y TreeView
	{
		get => base.TreeView as Y;
	}

	public BlackboardTreeNode( T value ) : base( value )
	{
	}

	public BlackboardTreeNode()
	{
	}
}
