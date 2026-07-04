using Editor;

namespace ShaderGraphPlus;

public abstract class BlackboardView : Widget
{
	private bool _queryDirty = false;
	private Layout _header;
	private Layout _subHeader;
	private LineEdit _search;
	private ToolButton _searchClear;
	private AddButton _addButton;

	protected BlackboardTree _treeView;

	protected bool _hasSelection => _treeView.Selection.Any();

	public IBlackboardNodeGraph Graph
	{
		get => field;
		set
		{
			if ( field == value ) return;

			field = value;
			_treeView.Graph = field;

			RebuildFromGraph();
		}
	}

	/// <summary>
	/// Called after something in the blackboard has changed.
	/// </summary>
	public Action<bool> OnDirty { get; set; }

	public Action OnSelectionChanged { get; set; }

	public BlackboardView( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();

		BuildUI();
	}

	[EditorEvent.Frame]
	protected void Frame()
	{
		if ( !_queryDirty )
			return;

		_queryDirty = false;
		_searchClear.Visible = !string.IsNullOrWhiteSpace( _search.Text );
	}

	private void BuildUI()
	{
		Layout.Clear( true );
		_header = Layout.AddColumn();

		_subHeader = Layout.AddRow();
		_subHeader.Spacing = 2;
		_subHeader.Margin = new Sandbox.UI.Margin( 0, 2 );
		_subHeader.Alignment = TextFlag.LeftCenter;

		_addButton = _subHeader.Add( new AddButton() );
		_addButton.MouseLeftPress = OpenTypeSelectionMenu;

		_search = _subHeader.Add( new LineEdit(), 1 );
		_search.PlaceholderText = "⌕  Search";
		_search.Layout = Layout.Row();
		_search.Layout.AddStretchCell( 1 );
		_search.TextChanged += x =>
		{
			_queryDirty = true;
			_treeView.SearchQuery = _search.Text;
			RebuildTreeView();
		};
		_search.FixedHeight = Theme.RowHeight;

		_searchClear = _search.Layout.Add( new ToolButton( string.Empty, "clear", this ) );
		_searchClear.MouseLeftPress = () =>
		{
			_search.Text = string.Empty;
			_treeView.SearchQuery = string.Empty;
			_treeView.RebuildTree();

			// make sure we're open to the stuff we picked from search
			foreach ( var item in _treeView.Selection )
			{
				_treeView.ExpandPathTo( item );
			}
			_treeView.UpdateIfDirty();

			var scrollTarget = _treeView.Selection.FirstOrDefault();
			if ( scrollTarget is not null )
			{
				_treeView.ScrollTo( scrollTarget );
			}
		};
		_searchClear.Visible = false;

		_treeView = InitializeTreeView();
		_treeView.OnSelectionChanged += ( selection ) =>
		{
			if ( selection.Any() )
			{
				OnSelectionChanged?.Invoke();
			}
		};
		_treeView.OnRebuild += ( hasSelection ) =>
		{
			RebuildFromGraph( hasSelection );
			OnDirty?.Invoke( false );
		};
		_treeView.BodyContextMenu = OpenTreeViewContextMenu;

		Layout.Add( _treeView, 1 );

		Frame();
	}

	protected abstract BlackboardTree InitializeTreeView();

	protected abstract void OpenTypeSelectionMenu();

	protected void OpenTreeViewContextMenu()
	{
		if ( !_hasSelection )
			return;

		var rootItem = _treeView.Items.FirstOrDefault();
		if ( rootItem is null ) return;

		if ( rootItem is TreeNode node )
		{
			node.OnContextMenu();
		}
	}

	internal IDisposable UndoScope( string name )
	{
		PushUndo( name );
		return new Sandbox.Utility.DisposeAction( () => PushRedo() );
	}

	public virtual void PushUndo( string name )
	{
	}

	public virtual void PushRedo()
	{
	}

	public void RebuildTreeView()
	{
		_treeView?.RebuildTree();
	}

	public void RebuildFromGraph( bool preserveSelection = false )
	{
		if ( Graph is not null )
			BuildFromParameters( Graph.Parameters, preserveSelection );
	}

	protected abstract void BuildFromParameters( IEnumerable<IBlackboardParameter> parameters, bool preserveSelection = false );

	public void DeleteParameter( IBlackboardParameter parameter, bool notifySelectionChange = false )
	{
		if ( parameter != null )
			return;

		using var undoScope = UndoScope( "Delete Parameter" );

		RemoveParameter( parameter );
		ClearSelection( notifySelectionChange );
	}

	public void DeleteSelection()
	{
		if ( !_treeView.SelectedItems.Any() )
			return;

		using var undoScope = UndoScope( "Delete Selection" );

		foreach ( var item in _treeView.SelectedItems.OfType<IBlackboardParameter>() )
		{
			RemoveParameter( item );
		}

		foreach ( var item in _treeView.SelectedItems.OfType<CategoryData>() )
		{
			RemoveCategoryData( item );
		}

		ClearSelection( true );
	}

	public void ClearSelection( bool notifyChange = false )
	{
		_treeView.Selection.Clear();

		if ( notifyChange )
		{
			OnSelectionChanged?.Invoke();
		}
	}

	protected virtual void RemoveParameter( IBlackboardParameter parameter )
	{
		Graph?.RemoveParameter( parameter );
	}

	protected abstract void RemoveCategoryData( CategoryData categoryData );
}

class AddButton : Button
{
	public AddButton() : base( null )
	{
		Icon = "add";

		Cursor = CursorShape.Finger;
		FixedHeight = Theme.RowHeight;
	}

	protected override Vector2 SizeHint()
	{
		return new Vector2( Theme.RowHeight );
	}

	protected override void OnPaint()
	{
		Paint.ClearBrush();
		Paint.ClearPen();

		var color = Enabled ? Theme.ControlBackground : Theme.SurfaceBackground;

		if ( Enabled && Paint.HasMouseOver )
		{
			color = color.Lighten( 0.1f );
		}

		Paint.ClearPen();
		Paint.SetBrush( color );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );

		Paint.ClearBrush();
		Paint.ClearPen();
		Paint.SetPen( Theme.Primary );

		Paint.DrawIcon( LocalRect, Icon, 14, TextFlag.Center );
	}
}
