using Editor;
using System.Text;

namespace ShaderGraphPlus;

public class BlackboardView : Widget
{
	private bool _queryDirty = false;
	private Layout _header;
	private Layout _subHeader;
	private LineEdit _search;
	private ToolButton _searchClear;
	private AddButton _addButton;
	private TreeView _treeView;

	private readonly MainWindow _window;
	private readonly UndoStack _undoStack;

	private readonly Dictionary<string, IBlackboardParameterType> _availableParameters = new( StringComparer.OrdinalIgnoreCase );
	private readonly SelectionSystem _selection = new SelectionSystem();

	private BlackboardParameter _selectedParameter => _selection.OfType<BlackboardParameter>().FirstOrDefault();
	private bool _hasSelection => _selection.Any();

	/// <summary>
	/// Called after something in the blackboard has changed.
	/// </summary>
	public Action OnDirty { get; set; }

	/// <summary>
	/// Called after a parameter bound node has been deleted.
	/// </summary>
	public Action OnParameterNodeDeleted { get; set; }

	private ShaderGraphPlus _graph;
	public ShaderGraphPlus Graph
	{
		get => _graph;
		set
		{
			if ( value == null || _graph == value )
				return;

			_graph = value;
		}
	}

	public BlackboardView( Widget parent, MainWindow window ) : base( parent )
	{
		Layout = Layout.Column();

		_window = window;
		_undoStack = window.UndoStack;

		BuildUI();
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
		_addButton.MouseLeftPress = TypeSelectionMenu;

		_search = _subHeader.Add( new LineEdit(), 1 );
		_search.PlaceholderText = "⌕  Search";
		_search.Layout = Layout.Row();
		_search.Layout.AddStretchCell( 1 );
		_search.TextChanged += x => _queryDirty = true;
		_search.FixedHeight = Theme.RowHeight;

		_searchClear = _search.Layout.Add( new ToolButton( string.Empty, "clear", this ) );
		_searchClear.MouseLeftPress = () =>
		{
			_search.Text = string.Empty;
			Rebuild();

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

		_treeView = new TreeView();
		_treeView.ItemSpacing = 0;
		_treeView.MultiSelect = false;
		_treeView.Margin = 0;
		_treeView.ItemSpacing = 0;
		_treeView.BodyDropTarget = TreeView.DragDropTarget.None;
		_treeView.BodyContextMenu = OpenTreeViewContextMenu;
		_treeView.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( _treeView.LocalRect, Theme.ControlRadius );

			return false;
		};

		_selection.OnItemAdded += ( item ) =>
		{
			SetSelection( (BlackboardParameter)item );
			SelectionChanged();
		};
		_selection.OnItemRemoved += ( item ) =>
		{
			if ( !_hasSelection )
				_window.OnSelected( null );
		};

		Layout.Add( _treeView, 1 );

		CheckForChanges();
	}

	private void OpenTreeViewContextMenu()
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

	private void AddOption( ContextMenu contextMenu, Menu menu, ClassBlackboardParameterType parameterType, string icon, string description )
	{
		var option = menu.AddOption( parameterType.Type.Title, !string.IsNullOrWhiteSpace( icon ) ? icon : null, () =>
		{
			CreateNewParameter( parameterType );

			contextMenu.Update();
			contextMenu.Close();
		} );

		option.ToolTip = description;
	}

	private void TypeSelectionMenu()
	{
		var contextManu = new ContextMenu( _treeView );

		IBlackboardParameterType[] avalibleTypes = BlackboardParameter.GetRelevantParameters( _availableParameters, Graph.IsSubgraph ).ToArray();

		foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
		{
			var targetType = parameterType.Type.TargetType;
			var icon = parameterType.DisplayInfo.Icon;
			var description = parameterType.DisplayInfo.Description;

			Menu menu = contextManu;

			if ( !_graph.IsSubgraph )
			{
				var materialParametersMenu = contextManu.FindOrCreateMenu( "Parameter" );
				materialParametersMenu.Icon = "edit_attributes";

				var attributesMenu = contextManu.FindOrCreateMenu( "Attribute" );
				attributesMenu.Icon = "edit_attributes";

				var materialCombosMenu = contextManu.FindOrCreateMenu( "Combo" );
				materialCombosMenu.Icon = "alt_route";

				if ( targetType.IsAssignableTo( typeof( IBlackboardMaterialParameter ) ) || targetType.IsAssignableTo( typeof( BlackboardTextureMaterialParameter ) ) )
				{
					menu = materialParametersMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) )
				{
					menu = materialCombosMenu;
				}
				else if ( targetType == typeof( SamplerStateParameter ) )
				{
					menu = attributesMenu;
				}

				AddOption( contextManu, menu, parameterType, icon, description );
			}
			else
			{
				var subgraphInputsMenu = contextManu.FindOrCreateMenu( "Input" );
				subgraphInputsMenu.Icon = "input";

				var subgraphOutputsMenu = contextManu.FindOrCreateMenu( "Output" );
				subgraphOutputsMenu.Icon = "output";

				if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) )
				{
					menu = subgraphInputsMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) )
				{
					menu = subgraphOutputsMenu;
				}

				AddOption( contextManu, menu, parameterType, icon, description );
			}
		}

		contextManu.OpenAtCursor( false );
	}

	[EditorEvent.Frame]
	private void CheckForChanges()
	{
		if ( !_queryDirty )
			return;

		_queryDirty = false;
		Rebuild();
	}

	internal IDisposable UndoScope( string name )
	{
		PushUndo( name );
		return new Sandbox.Utility.DisposeAction( () => PushRedo() );
	}

	public void PushUndo( string name )
	{
		SGPLogger.Info( $"Push Undo ({name})" );
		_undoStack.PushUndo( name, Graph.UndoStackSerialize() );
		_window.OnUndoPushed();
	}

	public void PushRedo()
	{
		SGPLogger.Info( "Push Redo" );
		_undoStack.PushRedo( Graph.UndoStackSerialize() );
		_window.SetDirty();
	}

	public void Rebuild()
	{
		if ( Graph == null )
			return;

		// Copy the current selection as we're about to kill it
		var selection = _treeView.Selection.Select( x => x as BlackboardParameter );

		// treeview will clear the selection, so give it a new one to clear
		_treeView.Selection = new SelectionSystem();
		_treeView.Clear();

		bool hasSearch = !string.IsNullOrEmpty( _search.Text );
		_searchClear.Visible = hasSearch;

		var parameters = Graph.Parameters;
		if ( hasSearch )
		{
			// flat search view

			var tokens = Regex.Matches( _search.Text, @"(\w+):(\S+)" )
			  .ToDictionary( m => m.Groups[1].Value, m => m.Groups[2].Value );

			var search = Regex.Replace( _search.Text, @"\b\w+:\S+\b", "" ).Trim();

			foreach ( var parameter in parameters )
			{
				if ( !parameter.Name.Contains( search, StringComparison.OrdinalIgnoreCase ) )
					continue;

				var treeNode = new BlackboardParameterSearchNode( parameter );
				treeNode.OnParameterDeleted += ( p ) =>
				{
					if ( _hasSelection )
					{
						ClearSelection();
						SelectionChanged();
						DeleteParameter( parameter );
					}
				};

				_treeView.AddItem( treeNode );
			}

			_treeView.Selection = _selection;
		}
		else
		{
			_treeView.Selection = _selection;

			foreach ( var parameter in parameters )
			{
				var treeNode = new BlackboardParameterNode( parameter );
				treeNode.OnParameterDeleted += ( p ) =>
				{
					if ( _hasSelection )
					{
						ClearSelection();
						SelectionChanged();
						DeleteParameter( parameter );
					}
				};

				_treeView.AddItem( treeNode );
				_treeView.Open( treeNode );
			}
		}
	}

	public void AddParameterType<T>() where T : BlackboardParameter
	{
		AddParameterType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddParameterType( TypeDescription type )
	{
		var parameterType = new ClassBlackboardParameterType( type );

		_availableParameters.TryAdd( parameterType.Identifier, parameterType );
	}

	public IBlackboardParameter CreateNewParameter( IBlackboardParameterType type, Action onCreated = null )
	{
		if ( type == null )
			return null;

		var parameter = type.CreateParameter( Graph );

		if ( parameter == null )
			return null;

		onCreated?.Invoke();

		Graph?.AddParameter( parameter );

		return parameter;
	}

	private void CreateNewParameter( IBlackboardParameterType type )
	{
		using var undoScope = UndoScope( "Add Parameter" );

		var parameterInstance = (BlackboardParameter)type.CreateParameter( Graph );

		Graph.AddParameter( parameterInstance );

		OnDirty?.Invoke();

		SetSelection( parameterInstance );

		SelectionChanged();

		RebuildFromGraph( true );
	}

	private void DeleteParameter( BlackboardParameter parameter )
	{
		using var undoScope = UndoScope( "Delete Parameter" );

		_graph?.RemoveParameter( parameter );

		var identifier = parameter.Identifier;

		foreach ( var node in _graph.Nodes )
		{
			if ( node is IParameterNode parameterNode && parameterNode.ParameterIdentifier == identifier && parameterNode is BaseNodePlus baseNode )
			{
				_graph.RemoveNode( baseNode );
				OnParameterNodeDeleted?.Invoke();
			}
		}

		_window.SetDirty();
		RebuildFromGraph( false );
	}

	private void BuildFromParameters( IEnumerable<BlackboardParameter> parameters, bool preserveSelection = false )
	{
		Rebuild();

		if ( !preserveSelection && _hasSelection )
		{
			_selection.Clear();
			SelectionChanged();
			return;
		}
		else if ( _hasSelection )
		{
			var parameter = Graph.FindParameter( _selectedParameter.Identifier );
			SetSelection( parameter );
			SelectionChanged();
		}
	}

	public void RebuildFromGraph( bool preserveSelection = false )
	{
		Rebuild();

		if ( _graph is not null )
			BuildFromParameters( _graph.Parameters, preserveSelection );
	}

	public void SetSelection( IBlackboardParameter parameter )
	{
		_selection.Set( parameter );
	}

	public void ClearSelection()
	{
		_treeView.Selection.Clear();
	}

	private void SelectionChanged()
	{
		if ( !_hasSelection )
		{
			_window.OnSelected( null );
			return;
		}

		_window.OnSelected( _selectedParameter );
	}
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

file class ParameterTypeEntry : Widget
{
	public string Text { get; set; } = "Test";
	public string Icon { get; set; } = "note_add";

	public IBlackboardParameterType Type { get; init; }

	internal ParameterTypeEntry( Widget parent, IBlackboardParameterType type = null ) : base( parent )
	{
		FixedHeight = 24;
		Type = type;

		if ( type is not null )
		{
			Text = type.Type.Title;
			Icon = type.Type.Icon;
			ToolTip = $"<b>{type.Type.Title}</b><br/>{type.Type.Description}";
		}
	}

	protected override void OnPaint()
	{
		var r = LocalRect.Shrink( 12, 2 );
		var hovered = IsUnderMouse;
		var opacity = hovered ? 1.0f : 0.7f;
		var typeColor = Color.White;
		var textColor = Theme.TextControl.WithAlpha( hovered ? 1.0f : 0.5f );
		var isSubgraphoutput = Type.Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) );

		if ( ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( Type.Type.TargetType, out var blackboardConfig ) )
		{
			typeColor = blackboardConfig.Color;
		}

		if ( hovered )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.Primary.Lighten( 0.1f ).Desaturate( 0.3f ).WithAlpha( 0.4f * 0.6f ) );
			Paint.DrawRect( LocalRect );
		}

		Paint.SetPen( typeColor );
		Paint.DrawIcon( r.Shrink( 4f ), "circle", 12f, TextFlag.LeftCenter );

		r.Left += r.Height + 6;

		Paint.SetDefaultFont( 8 );
		Paint.SetPen( textColor );
		Paint.DrawText( r, $"{Text} {(isSubgraphoutput ? $"Output" : $"Input")}", TextFlag.LeftCenter );
	}
}

