using Editor;
using EditorExtras;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using System.Globalization;

using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The graph's parameters, grouped: a header per group, then its parameters one row each - type in its type color,
/// name, default value, and tags for the settings worth seeing at a glance (sRGB, attribute, range...).
/// Parameters and groups reorder by dragging, and parameters rename in place.
/// </summary>
[StyleSheet.Inline( "sgp_blackboardpanel", Styles )]
public sealed partial class BlackboardPanel : Panel
{
	private TextInput _searchFilter;
	private string _filter = "";
	private readonly Panel _list;

	private readonly Dictionary<string, IBlackboardParameterType> _availableParameters = new( StringComparer.OrdinalIgnoreCase );

	private readonly HashSet<string> _collapsedGroups;
	private string CollapsedGroupsCookie => EditorCookieNames.ShaderGraphPlusBlackboardCollapsedGroupsCookie;

	private readonly List<ParameterRow> _rowPanels = [];
	private readonly List<ParameterGroupHeader> _headers = [];

	private Sandbox.UI.Menu _menu;
	private BlackboardItem _dragSource;

	// Drops, menus and graph edits change what the list should show from inside an event of a panel the
	// rebuild would delete, so the rebuild waits for the next tick
	private bool _rebuildPending;

	/// <summary>
	/// The currently selected parameter.
	/// </summary>
	public BlackboardParameter Selected { get; private set; }

	/// <summary>
	/// Called when a parmeter is selected.
	/// </summary>
	public Action<BlackboardParameter> OnSelected { get; set; }

	public Action<bool> OnDirty { get; set; }

	public Action OnParameterNodesDeleted { get; set; }

	/// <summary>
	/// Called before a user edit changes the graph, and after it. The window owns the undo stack, so
	/// it keeps one step list for the graph view and the blackboard.
	/// </summary>
	public Action<string> OnPushUndo { get; set; }

	public Action OnPushRedo { get; set; }

	int _undoDepth;

	/// <summary>
	/// <see cref="OnPushUndo"/> now and <see cref="OnPushRedo"/> on dispose. Inside another scope it
	/// does neither, so one user edit is one undo step.
	/// </summary>
	internal IDisposable UndoScope( string name )
	{
		if ( _undoDepth++ == 0 ) OnPushUndo?.Invoke( name );
		return new Sandbox.Utility.DisposeAction( EndUndoScope );
	}

	void EndUndoScope()
	{
		if ( --_undoDepth == 0 ) OnPushRedo?.Invoke();
	}

	public ShaderGraphPlus.ShaderGraphPlus Graph
	{
		get => field;
		set
		{
			if ( field == value ) return;

			field = value;

			Select( null );

			RebuildFromGraph();
		}
	}

	public BlackboardPanel( bool isSubgraph )
	{
		_collapsedGroups = EditorCookie.Get<List<string>>( CollapsedGroupsCookie, [] ).ToHashSet( StringComparer.OrdinalIgnoreCase );

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-blackboard" );

		// For F2 and Delete: a click anywhere in the blackboard lands the focus here
		AcceptsFocus = true;

		BuildToolbar();

		_list = Add.Panel( "sg-params" );

		// Pressing empty space clears the selection
		_list.AddEventListener( "onmousedown", OnListMouseDown );
	}

	private void OnListMouseDown( PanelEvent e )
	{
		if ( e.Target == _list ) Select( null );
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter..", "search" ) );
		_searchFilter.OnChange = OnFilterChanged;

		var addMenu = new Sandbox.UI.Menu();
		addMenu.AboutToShow += OnAddMenuAboutToShow;
		toolbar.AddMenu( "", "add", addMenu ).Tooltip = "Add parameter";
	}

	private void OnFilterChanged( string value )
	{
		_filter = value?.Trim() ?? "";
		RebuildFromGraph();
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

	private void CreateNewParameter( IBlackboardParameterType type, string group = "" )
	{
		using var undoScope = UndoScope( "Add Parameter" );

		group = NormalizeGroup( group );

		_searchFilter.SetValue( "" );
		_filter = "";
		ExpandGroup( group );

		var parameter = (BlackboardParameter)type.CreateParameter( Graph );

		if ( parameter is IGroupableBlackboardParameter groupable )
		{
			SetGroupData( group, group, groupable );
		}

		Graph.AddParameter( parameter );

		RebuildParameterOrder();

		OnDirty?.Invoke( true );

		Reselect( parameter );

		QueueRebuild();
	}

	public IBlackboardParameter CreateNewParameter( IBlackboardParameterType type, string name = "", Action onCreated = null )
	{
		if ( type == null )
			return null;

		var group = NormalizeGroup( "" );

		_searchFilter.SetValue( "" );
		_filter = "";
		ExpandGroup( group );

		var parameter = type.CreateParameter( Graph, name );

		if ( parameter == null )
			return null;

		if ( parameter is IGroupableBlackboardParameter groupable )
		{
			SetGroupData( group, group, groupable );
		}

		onCreated?.Invoke();

		Graph.AddParameter( parameter );

		RebuildParameterOrder();

		return parameter;
	}

	/// <summary>
	/// The toolbar's + menu, rebuilt every time it opens: parameter types, then the same again per existing group.
	/// </summary>
	private void OnAddMenuAboutToShow( Sandbox.UI.Menu menu )
	{
		menu.Clear();

		if ( Graph is null )
			return;

		AddParameterOptions( menu, "" );
		AddGroupedOptions( menu );
	}

	/// <summary>
	/// "Add to Group": one submenu per existing group, each offering every parameter type.
	/// </summary>
	private void AddGroupedOptions( Sandbox.UI.Menu menu )
	{
		var groups = ExistingGroups().ToList();
		if ( groups.Count == 0 )
			return;

		menu.AddSeparator();
		var grouped = menu.AddMenu( "Add to Group", "folder" );
		foreach ( var group in groups )
		{
			AddParameterOptions( grouped.AddMenu( group, "folder" ), group );
		}
	}

	/// <summary>
	/// Every parameter type that fits the graph, sorted into submenus. Choosing one adds it to <paramref name="group"/>.
	/// </summary>
	private void AddParameterOptions( Sandbox.UI.Menu root, string group )
	{
		void AddOption( Sandbox.UI.Menu menu, IBlackboardParameterType parameterType, string icon, string description )
		{
			var option = menu.AddOption( parameterType.Type.Title, !string.IsNullOrWhiteSpace( icon ) ? icon : null, () =>
			{
				CreateNewParameter( parameterType, group );
			} );

			option.Tooltip = description;
		}

		IBlackboardParameterType[] avalibleTypes = BlackboardParameter.GetRelevantParameters( _availableParameters, Graph.IsSubgraph ).ToArray();

		var target = root;

		if ( !Graph.IsSubgraph )
		{
			var materialParametersMenu = root.AddMenu( "Parameter", "edit_attributes" );
			var attributesMenu = root.AddMenu( "Attribute", "edit_attributes" );
			var materialCombosMenu = root.AddMenu( "Combo", "alt_route" );

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

				if ( targetType.IsAssignableTo( typeof( IBlackboardMaterialParameter ) ) || targetType.IsAssignableTo( typeof( BlackboardTextureMaterialParameter ) ) )
				{
					target = materialParametersMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) )
				{
					target = materialCombosMenu;
				}
				else if ( targetType == typeof( SamplerStateParameter ) )
				{
					target = attributesMenu;
				}

				AddOption( target, parameterType, icon, description );
			}
		}
		else
		{
			var subgraphInputsMenu = root.AddMenu( "Input", "input" );
			var subgraphOutputsMenu = root.AddMenu( "Output", "output" );

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

				if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) )
				{
					target = subgraphInputsMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) )
				{
					target = subgraphOutputsMenu;
				}

				AddOption( target, parameterType, icon, description );
			}
		}
	}

	/// <summary>
	/// "Move to Group": General, then each existing group with a check on the parameter's own, then a new one.
	/// </summary>
	private void AddGroupOptions( Sandbox.UI.Menu menu, IGroupableBlackboardParameter parameter )
	{
		var groups = menu.AddMenu( "Move to Group", "folder" );
		groups.AddOption( BlackboardGlobals.EmptyGroupName, null, () => MoveToGroup( parameter, "" ) ).Checked = string.IsNullOrEmpty( GroupName( parameter ) );

		foreach ( var group in ExistingGroups() )
		{
			groups.AddOption( group, null, () => MoveToGroup( parameter, group ) ).Checked = GroupName( parameter ).Equals( group, StringComparison.OrdinalIgnoreCase );
		}

		groups.AddSeparator();
		groups.AddOption( "New Group…", "create_new_folder", () => OpenGroupNameDialog( "New Parameter Group", "", name => MoveToGroup( parameter, name ) ) );
	}

	/// <summary>
	/// Open a context menu, filled in by <paramref name="build"/>, under the mouse. Replaces the one already open.
	/// </summary>
	private void OpenMenu( Panel source, Action<Sandbox.UI.Menu> build )
	{
		_menu?.Delete( true );

		var menu = new Sandbox.UI.Menu();
		_menu = menu;
		build( menu );
		menu.Closed += OnMenuClosed;
		menu.Open( source, Popup.PositionMode.UnderMouse );
	}

	private void OnMenuClosed( Sandbox.UI.Menu menu )
	{
		if ( _menu == menu ) _menu = null;
		menu.Delete( true );
	}

	/// <summary>
	/// A parameter's right-click menu: rename, move to a group, delete.
	/// </summary>
	internal void OpenParameterMenu( ParameterRow row )
	{
		var parameter = row.Parameter;

		OpenMenu( row, menu =>
		{
			menu.AddOption( "Rename", "edit", row.StartRename ).Shortcut = "F2";
			AddGroupOptions( menu, parameter );

			menu.AddSeparator();
			menu.AddOption( "Delete", "delete", () => Remove( parameter ) ).Shortcut = "Del";
		} );
	}

	/// <summary>
	/// A group header's right-click menu: add a parameter to it, and for any group but General, rename or remove it.
	/// </summary>
	internal void OpenGroupMenu( ParameterGroupHeader header )
	{
		var group = header.Group;

		OpenMenu( header, menu =>
		{
			AddParameterOptions( menu.AddMenu( "Add Parameter", "add" ), group.Name );

			if ( group.Name.Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase ) )
				return;

			menu.AddSeparator();
			menu.AddOption( "Rename Group", "edit", () => RenameGroup( group.Name ) );
			menu.AddOption( "Remove Group", "folder_off", () => ClearGroup( group.Name ) );
		} );
	}

	/// <summary>
	/// Remove a parameter, its nodes and its group entry. Asks first if nodes would go.
	/// </summary>
	internal void Remove( BlackboardParameter parameter )
	{
		var parameterNodes = Graph.Nodes
			.Where( x => x is IBlackboardNode node && node.ParameterIdentifier == parameter.Identifier )
			.ToArray();

		void Delete()
		{
			using var undoScope = UndoScope( "Remove Parameter" );

			foreach ( var node in parameterNodes )
			{
				Graph.RemoveNode( node );
			}

			if ( parameterNodes.Any() )
			{
				OnParameterNodesDeleted?.Invoke();
			}

			Graph.RemoveParameter( parameter );

			if ( Selected == parameter )
				Select( null );

			RemoveParameterFromGroupData( parameter );

			RebuildParameterOrder();

			PruneCollapsedGroups();

			OnDirty?.Invoke( true );

			QueueRebuild();
		}

		if ( parameterNodes.Length == 0 )
		{
			Delete();
			return;
		}

		new Editor.PopupWindow(
			"Delete Parameter",
			$"Delete '{parameter.Name}'?\n\nIs referenced by {parameterNodes.Length} parameter node{(parameterNodes.Length == 1 ? "" : "s")}. These references will be cleared.",
			"Cancel",
			new Dictionary<string, Action> { ["Delete"] = Delete } ).Show();
	}

	/// <summary>
	/// Make <paramref name="parameter"/> the selected one, or clear the selection with null.
	/// </summary>
	public void Select( IBlackboardParameter parameter )
	{
		if ( Selected == parameter )
			return;

		ApplySelection( parameter );
	}

	/// <summary>
	/// Select <paramref name="parameter"/> and tell the listeners even when it already was, so whatever shows
	/// it picks up the rename or move.
	/// </summary>
	private void Reselect( IBlackboardParameter parameter ) => ApplySelection( parameter );

	private void ApplySelection( IBlackboardParameter parameter )
	{
		Selected = (BlackboardParameter)parameter;

		foreach ( var row in _rowPanels )
		{
			row.SetSelected( row.Parameter == parameter );
		}

		OnSelected?.Invoke( Selected );
	}

	/// <summary>
	/// F2 renames the selected parameter and Delete removes it, unless the filter has the keyboard.
	/// </summary>
	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( Selected is { } selected && !_searchFilter.HasFocus )
		{
			if ( e.Button == "f2" )
			{
				BeginRename( selected );
				e.StopPropagation = true;
				return;
			}

			if ( e.Button == "delete" )
			{
				Remove( selected );
				e.StopPropagation = true;
				return;
			}
		}

		base.OnButtonTyped( e );
	}

	public override void Tick()
	{
		base.Tick();

		if ( _rebuildPending )
			RebuildFromGraph();

		if ( _dragSource is { IsValid: true } )
			UpdateDropIndicator();
	}

	private void QueueRebuild() => _rebuildPending = true;

	/// <summary>
	/// Rename a parameter. Empty names, the same name, and names another parameter has are ignored.
	/// </summary>
	internal void Rename( IBlackboardParameter parameter, string name )
	{
		name = name?.Trim();

		if ( string.IsNullOrEmpty( name ) || name == parameter.Name )
			return;

		if ( Graph.Parameters.Any( p => p != parameter && p.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) )
			return;

		using var undoScope = UndoScope( "Rename Parameter" );

		parameter.Name = name;

		OnDirty?.Invoke( true );

		Reselect( parameter );

		QueueRebuild();
	}

	internal void BeginRename( IBlackboardParameter parameter )
		=> _rowPanels.FirstOrDefault( r => r.Parameter == parameter )?.StartRename();

	/// <summary>
	/// A parameter row dropped on another: reorder within the group, or move into the target's group, next to the target.
	/// </summary>
	internal void DropParameter( ParameterRow source, ParameterRow target, DropMode mode )
	{
		var sourceParameter = source.Parameter;
		var targetParameter = target.Parameter;

		var sourceGroupName = GroupTitle( sourceParameter.Group );
		var targetGroupName = GroupTitle( targetParameter.Group );

		if ( !Graph.TryFindGroupData( sourceGroupName, out var sourceGroupData ) )
			return;

		using var undoScope = UndoScope( "Move Parameter" );

		if ( !sourceGroupName.Equals( targetGroupName, StringComparison.OrdinalIgnoreCase ) && Graph.TryFindGroupData( targetGroupName, out var targetGroupData ) )
		{
			var targetIndex = targetGroupData.ParameterReferences.IndexOf( targetParameter.Identifier );

			if ( mode == DropMode.Below )
			{
				targetIndex++;
			}

			sourceGroupData.ParameterReferences.Remove( sourceParameter.Identifier );

			if ( sourceGroupData.ParameterReferences.Count == 0 )
			{
				Graph.RemoveGroupData( sourceGroupData );
			}

			targetGroupData.ParameterReferences.Insert( Math.Clamp( targetIndex, 0, targetGroupData.ParameterReferences.Count ), sourceParameter.Identifier );

			sourceParameter.Group = targetParameter.Group;
		}
		else
		{
			var targetIndex = sourceGroupData.ParameterReferences.IndexOf( targetParameter.Identifier );

			sourceGroupData.ParameterReferences.Remove( sourceParameter.Identifier );
			sourceGroupData.ParameterReferences.Insert( Math.Clamp( targetIndex, 0, sourceGroupData.ParameterReferences.Count ), sourceParameter.Identifier );
		}

		PruneCollapsedGroups();

		RebuildParameterOrder();

		OnDirty?.Invoke( true );

		QueueRebuild();
	}

	/// <summary>
	/// Called as <paramref name="source"/> starts being dragged.
	/// </summary>
	internal void BeginDrag( BlackboardItem source )
	{
		ClearDropIndicators();
		_dragSource = source;
	}

	internal void EndDrag( BlackboardItem source )
	{
		if ( _dragSource == source )
			_dragSource = null;

		ClearDropIndicators();
	}

	/// <summary>
	/// Show where the dragged item would land on whatever is under the mouse, and nothing on the rest. Polled
	/// each tick instead of tracking drag enter and leave, which fire for every child the mouse crosses.
	/// </summary>
	private void UpdateDropIndicator()
	{
		BlackboardItem hovered = null;

		if ( IsUnderMouse( _list ) )
		{
			hovered = _headers.FirstOrDefault( IsUnderMouse );
			hovered ??= _rowPanels.FirstOrDefault( IsUnderMouse );
		}

		foreach ( var header in _headers )
		{
			header.DropIndicator = header == hovered ? header.GetDrop( _dragSource ) : DropMode.None;
		}

		foreach ( var row in _rowPanels )
		{
			row.DropIndicator = row == hovered ? row.GetDrop( _dragSource ) : DropMode.None;
		}
	}

	private static bool IsUnderMouse( Panel panel ) => panel.IsInside( panel.MousePosition + panel.Box.Rect.Position );

	private void ClearDropIndicators()
	{
		foreach ( var header in _headers )
		{
			header.DropIndicator = DropMode.None;
		}

		foreach ( var row in _rowPanels )
		{
			row.DropIndicator = DropMode.None;
		}
	}

	/// <summary>
	/// Recreate the list from <paramref name="parameters"/>, applying the filter: a header per group in the graph's
	/// group order, then each expanded group's rows in the group's order.
	/// </summary>
	public void BuildFromParameters( IEnumerable<IBlackboardParameter> parameters, bool preserveSelection = false )
	{
		_list.DeleteChildren( true );
		_rowPanels.Clear();
		_headers.Clear();
		_dragSource = null;
		_rebuildPending = false;

		if ( Graph is null )
		{
			_list.SetClass( "is-empty", true );
			_list.Add.Label( "No graph open", "empty" );
			return;
		}

		var groups = OrderedGroups( parameters ).ToList();

		_list.SetClass( "is-empty", groups.Count == 0 );

		if ( groups.Count == 0 )
		{
			_list.Add.Label( Graph.Parameters.Any() ? "No matching parameters" : "No parameters yet - add one with +", "empty" );
			return;
		}

		var filtering = !string.IsNullOrWhiteSpace( _filter );

		foreach ( var group in groups )
		{
			var collapsed = !filtering && _collapsedGroups.Contains( group.Key );
			var groupData = GroupDataFor( group.Key );

			_headers.Add( _list.AddChild( new ParameterGroupHeader( this, groupData, group.Count(), collapsed, !filtering ) ) );

			if ( collapsed )
				continue;

			var body = _list.Add.Panel( "sg-group-body" );

			foreach ( var parameter in group )
			{
				var blackboardParameter = (BlackboardParameter)parameter;
				blackboardParameter.Graph = Graph;

				var row = body.AddChild( new ParameterRow( this, blackboardParameter, groupData ) );
				row.SetSelected( blackboardParameter == Selected );

				_rowPanels.Add( row );
			}
		}
	}

	public void RebuildFromGraph( bool preserveSelection = false )
	{
		if ( Graph is not null )
		{
			BuildFromParameters( Graph.Parameters, preserveSelection );
		}
	}

	internal void RebuildParameterOrder()
	{
		var parameterReferences = new HashSet<Guid>();

		// Group 1 : 0,1 ---> 0,1
		// Group 2 : 0,1,2,3 ---> 2,3,4,5
		// Group 3 : 0 ---> 6
		// Resulting new index's for each parameter in Graph.Parameters : 0,1,2,3,4,5,6

		foreach ( var group in Graph.GroupData )
		{
			foreach ( var parameterReference in group.ParameterReferences )
			{
				parameterReferences.Add( parameterReference );
			}
		}

		foreach ( var (parameterIndex, parameterIdentifier) in parameterReferences.Index() )
		{
			Graph.ReOrderParameter( parameterIdentifier, parameterIndex );
		}
	}

	/// <summary>
	/// The graph changed. Rebuild the list if what it shows - the parameters, their order, groups and names - no longer matches.
	/// </summary>
	internal void OnGraphDirty()
	{
		if ( Graph is null )
			return;

		var parameters = DisplayedParameters( Graph.Parameters ).ToList();
		var stale = _rowPanels.Count != parameters.Count;

		for ( var i = 0; !stale && i < _rowPanels.Count; i++ )
		{
			var row = _rowPanels[i];
			var parameter = parameters[i];

			stale = !ReferenceEquals( row.Parameter, parameter ) || row.BuiltGroup != GroupName( parameter ) || row.BuiltName != parameter.Name;
		}

		if ( stale )
			QueueRebuild();
	}

	/// <summary>
	/// The parameters that have a row: everything matching the filter outside collapsed groups, in list order.
	/// </summary>
	private IEnumerable<IGroupableBlackboardParameter> DisplayedParameters( IEnumerable<IBlackboardParameter> parameters )
	{
		var filtering = !string.IsNullOrWhiteSpace( _filter );

		foreach ( var group in OrderedGroups( parameters ) )
		{
			if ( !filtering && _collapsedGroups.Contains( group.Key ) )
				continue;

			foreach ( var parameter in group )
				yield return parameter;
		}
	}

	/// <summary>
	/// Whether the filter matches the parameter's type, name, or group.
	/// </summary>
	private bool MatchesFilter( IGroupableBlackboardParameter parameter )
	{
		if ( string.IsNullOrEmpty( _filter ) )
			return true;

		return parameter.Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| parameter.DisplayInfo.Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| ParameterRow.TypeConfig( parameter ).Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| GroupTitle( GroupName( parameter ) ).Contains( _filter, StringComparison.OrdinalIgnoreCase );
	}
}
