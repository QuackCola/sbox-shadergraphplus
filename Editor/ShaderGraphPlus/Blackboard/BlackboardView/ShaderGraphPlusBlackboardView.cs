using Editor;

namespace ShaderGraphPlus;

public class ShaderGraphPlusBlackboardView : BlackboardView
{
	private readonly MainWindow _window;
	private readonly UndoStack _undoStack;

	private readonly Dictionary<string, IBlackboardParameterType> _availableParameters = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>
	/// Called after a parameter bound node has been deleted.
	/// </summary>
	public Action OnParameterNodeDeleted { get; set; }

	public new ShaderGraphPlus Graph
	{
		get => (ShaderGraphPlus)base.Graph;
		set => base.Graph = value;
	}

	public ShaderGraphPlusBlackboardView( Widget parent, MainWindow window ) : base( parent )
	{
		_window = window;
		_undoStack = window.UndoStack;

		OnSelectionChanged += SelectionChanged;
	}

	protected override BlackboardTree InitializeTreeView()
	{
		return new ShaderGraphPlusBlackboardTree( this );
	}

	protected override void OpenTypeSelectionMenu()
	{
		void AddOption( ContextMenu contextMenu, Menu menu, IBlackboardParameterType parameterType, string icon, string description )
		{
			var option = menu.AddOption( parameterType.Type.Title, !string.IsNullOrWhiteSpace( icon ) ? icon : null, () =>
			{
				CreateNewParameter( parameterType );

				contextMenu.Update();
				contextMenu.Close();
			} );

			option.ToolTip = description;
		}

		var contextManu = new ContextMenu( _treeView );

		IBlackboardParameterType[] avalibleTypes = BlackboardParameter.GetRelevantParameters( _availableParameters, Graph.IsSubgraph ).ToArray();

		foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
		{
			var targetType = parameterType.Type.TargetType;
			var icon = parameterType.DisplayInfo.Icon;
			var description = parameterType.DisplayInfo.Description;

			Menu menu = contextManu;

			if ( !Graph.IsSubgraph )
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

		if ( !Graph.IsSubgraph )
		{
			contextManu.AddSeparator();

			contextManu.AddOption( "Group", "folder", () => { AddCategory(); } );
		}

		contextManu.OpenAtCursor( false );
	}

	public override void PushUndo( string name )
	{
		SGPLogger.Info( $"Push Undo ({name})" );
		_undoStack.PushUndo( name, Graph.UndoStackSerialize() );
		_window.OnUndoPushed();
	}

	public override void PushRedo()
	{
		SGPLogger.Info( "Push Redo" );
		_undoStack.PushRedo( Graph.UndoStackSerialize() );
		_window.SetDirty();
	}

	public void AddParameterType<T>() where T : BlackboardParameter
	{
		AddParameterType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddParameterType( TypeDescription type )
	{
		var parameterType = ClassBlackboardParameterType.HookupParameterType( type );

		_availableParameters.TryAdd( parameterType.Identifier, parameterType );
	}

	public void AddCategory()
	{
		using var undoScope = UndoScope( "Add Group" );

		var category = new CategoryData();
		category.Graph = Graph;

		category.NewName();

		Graph.AddCategoryData( category );

		OnDirty?.Invoke( true );

		_treeView.Selection.Set( category );
		SelectionChanged();

		RebuildFromGraph( true );
	}

	public IBlackboardParameter CreateNewParameter( IBlackboardParameterType type, string name = "", Action onCreated = null )
	{
		if ( type == null )
			return null;

		var parameter = type.CreateParameter( Graph, name );

		if ( parameter == null )
			return null;

		onCreated?.Invoke();

		Graph?.AddParameter( parameter );

		return parameter;
	}

	private void CreateNewParameter( IBlackboardParameterType type )
	{
		using var undoScope = UndoScope( "Add Parameter" );

		var parameter = (BlackboardParameter)type.CreateParameter( Graph );

		Graph.AddParameter( parameter );

		OnDirty?.Invoke( true );

		_treeView.Selection.Set( parameter );
		SelectionChanged();

		RebuildFromGraph( true );
	}

	protected override void RemoveParameter( IBlackboardParameter parameter )
	{
		if ( parameter is IGroupableBlackboardParameter groupable && groupable.IsGrouped )
		{
			if ( Graph.TryFindCategoryData( groupable.GroupReference, out var categoryData ) )
			{
				categoryData.ParameterReferences.Remove( groupable.Identifier );

				Graph?.UpdateCategoryData( categoryData );
			}
		}

		Graph?.RemoveParameter( (BlackboardParameter)parameter );

		var identifier = parameter.Identifier;

		foreach ( var node in Graph.Nodes )
		{
			if ( node is IParameterNode parameterNode && parameterNode.ParameterIdentifier == identifier && parameterNode is BaseNodePlus baseNode )
			{
				Graph.RemoveNode( baseNode );
				OnParameterNodeDeleted?.Invoke();
			}
		}

		OnDirty?.Invoke( true );

		RebuildFromGraph( false );
	}

	protected override void RemoveCategoryData( CategoryData categoryData )
	{
		Graph?.RemoveCategoryData( categoryData );

		foreach ( var reference in categoryData.ParameterReferences )
		{
			if ( Graph.TryFindParameter<BlackboardParameter>( reference, out var parameter ) )
			{
				var groupable = parameter as IGroupableBlackboardParameter;
				groupable.GroupReference = Guid.Empty;

				Graph?.UpdateParameter( groupable );
			}
		}

		OnDirty?.Invoke( true );
		RebuildFromGraph( false );
	}

	protected override void BuildFromParameters( IEnumerable<IBlackboardParameter> parameters, bool preserveSelection = false )
	{
		RebuildTreeView();
	}

	public void SetSelection( IBlackboardParameter parameter )
	{
		if ( parameter != null )
		{
			_treeView.Selection.Set( parameter );
		}
		else
		{
			_treeView.Selection.Clear();
		}
	}

	private void SelectionChanged()
	{
		var selection = _treeView.SelectedItems.OfType<BlackboardParameter>().FirstOrDefault();

		if ( !selection.IsValid() )
		{
			var categorySelection = _treeView.SelectedItems.OfType<CategoryData>().FirstOrDefault();

			if ( categorySelection != null )
			{
				_window.OnSelected( categorySelection );
			}
			else
			{
				_window.OnSelected( null );
			}

			return;
		}

		_window.OnSelected( selection );
		//_window.Selection.Set( selection );
	}
}
