using Editor;

namespace NodeEditorPlus.Blackboard;

public abstract class BlackboardTree : TreeView
{
	/// <summary>
	/// Temp until GraphEdtiorSession or similiar gets sorted.
	/// </summary>
	private SelectionSystem _internalViewSelection => new();

	public readonly BlackboardView BlackboardView;

	public IBlackboardNodeGraph Graph
	{
		get => field;
		set
		{
			if ( field == value ) return;

			field = value;
		}
	}

	public List<TreeNode> TreeNodes { get; private set; }

	protected bool HasSearchQuery => !string.IsNullOrWhiteSpace( SearchQuery );

	public string SearchQuery { get; set; }

	public Action<bool> OnRebuild { get; set; }

	public BlackboardTree( BlackboardView parent ) : base( parent )
	{
		BlackboardView = parent;

		MinimumWidth = 340;
		ExpandForSelection = true;
		SmoothScrolling = true;
		MultiSelect = false;
		BodyDropTarget = DragDropTarget.Closest;

		TreeNodes = new List<TreeNode>();
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( SetContentHash( ContentHash, 0.1f ) )
		{
			RebuildTree();
		}
	}

	protected abstract int ContentHash();

	protected override void OnPaint()
	{
		Paint.SetBrushAndPen( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, 4 );

		base.OnPaint();

		if ( Visible )
		{
			Update();
		}
	}

	internal IDisposable UndoScope( string name )
	{
		return BlackboardView?.UndoScope( name );
	}

	public void RebuildTree()
	{
		if ( Graph == null )
			return;

		// Copy the current selection as we're about to kill it
		var selection = Selection.Select( x => x );

		// treeview will clear the selection, so give it a new one to clear
		Selection = new SelectionSystem();
		TreeNodes.Clear();

		if ( HasSearchQuery )
		{
			// flat search view

			var tokens = Regex.Matches( SearchQuery, @"(\w+):(\S+)" )
			  .ToDictionary( m => m.Groups[1].Value, m => m.Groups[2].Value );

			var search = Regex.Replace( SearchQuery, @"\b\w+:\S+\b", "" ).Trim();

			OnAddItemsToTree( TreeNodes, search );
		}
		else
		{
			OnAddItemsToTree( TreeNodes, "" );
		}

		Selection = _internalViewSelection;

		SetItems( TreeNodes );

		OnSelectionRestore( selection );
	}

	/// <summary>
	/// When the TreeView is rebuilt the original selection needs to be restored.
	/// </summary>
	protected virtual void OnSelectionRestore( IEnumerable<object> selection )
	{
	}

	protected abstract void OnAddItemsToTree( List<TreeNode> nodes, string search );

	[Shortcut( "editor.delete", "DEL" )]
	public void DeleteSelection()
	{
		BlackboardView?.DeleteSelection();
	}
}
