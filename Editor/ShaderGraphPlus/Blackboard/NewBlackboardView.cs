using Editor;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

public class NewBlackboardView : Widget
{
	private readonly UndoStack _undoStack;
	private readonly MainWindow _window;
	private readonly LineEdit _filter;
	private readonly Widget _rowsCanvas;
	private readonly Layout _rows;

	private readonly List<IParameterRow> _rowWidgets = new();

	private readonly Dictionary<string, IBlackboardParameterType> _availableParameters = new( StringComparer.OrdinalIgnoreCase );

	//private readonly ToolButton _deleteUnusedButton;
	private readonly HashSet<string> _collapsedGroups;
	private bool _hasVisibleParameters;

	private string EmptyHint => (Graph?.Parameters.Count() ?? 0) > 0
	? "No matching parameters"
	: "No parameters\nClick + to add one";

	protected virtual string CollapsedGroupsCookie => EditorCookieNames.ShaderGraphPlusBlackboardCollapsedGroupsCookie;

	public ShaderGraphPlus Graph
	{
		get => field;
		set
		{
			if ( field == value ) return;

			field = value;

			RebuildFromGraph();
		}
	}

	public Action<bool> OnDirty { get; set; }

	public Action OnParameterNodesDeleted { get; set; }

	public NewBlackboardView( MainWindow window ) : base( null )
	{
		_window = window;
		_undoStack = window.UndoStack;
		_collapsedGroups = EditorCookie.Get<List<string>>( CollapsedGroupsCookie, [] ).ToHashSet( StringComparer.OrdinalIgnoreCase );

		Name = "Blackboard";
		WindowTitle = "Blackboard";
		SetWindowIcon( "tune" );

		Layout = Layout.Column();

		var bar = new Widget( this );
		bar.FixedHeight = Theme.RowHeight + 2;
		bar.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.SurfaceBackground );
			Paint.DrawRect( bar.LocalRect );
			return true;
		};

		var toolbar = bar.Layout = Layout.Row();
		toolbar.Margin = new Sandbox.UI.Margin( 0, 0, 0, 2 );
		toolbar.Spacing = 2;
		Layout.Add( bar );

		_filter = toolbar.Add( new LineEdit() { PlaceholderText = "⌕  Filter..", ClearButtonEnabled = true, FixedHeight = Theme.RowHeight }, 1 );
		_filter.TextEdited += _ => BuildFromParameters( Graph.Parameters );

		ToolButton addButton = null;
		addButton = toolbar.Add( HeaderButton( "add",
			"<b>Add Parameter</b><p>Adds a shader parameter</p>",
			() => OpenAddMenu( addButton ) ) );

		var panel = Layout.Add( new Widget( this ) { Layout = Layout.Column() }, 1 );
		panel.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.WidgetBackground.Darken( 0.1f ) );
			Paint.DrawRect( panel.LocalRect, Theme.ControlRadius );

			if ( !_hasVisibleParameters )
			{
				Paint.SetPen( Theme.TextControl.WithAlpha( 0.4f ) );
				Paint.SetDefaultFont();
				Paint.DrawText( panel.LocalRect, EmptyHint, TextFlag.Center );
			}

			return true;
		};

		var scroll = panel.Layout.Add( new ScrollArea( panel ), 1 );
		scroll.VerticalScrollbarMode = ScrollbarMode.Auto;
		scroll.HorizontalScrollbarMode = ScrollbarMode.Off;
		scroll.TranslucentBackground = true;
		scroll.NoSystemBackground = true;

		// Scope to the scroll area itself, otherwise the rules cascade into every
		// descendant - including the QToolTip popups shown for the rows inside it.
		scroll.Name = "ShaderGraphPlusParameterScroll";
		scroll.SetStyles( "#AnimGraphParameterScroll { border: none; background: transparent; }" );
		scroll.Canvas = new Widget( scroll )
		{
			Layout = Layout.Column(),
			HorizontalSizeMode = SizeMode.Flexible,
			VerticalSizeMode = SizeMode.CanGrow,
			TranslucentBackground = true,
			NoSystemBackground = true,
		};
		scroll.Canvas.Layout.Spacing = 0;

		_rowsCanvas = scroll.Canvas;
		_rows = scroll.Canvas.Layout;
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

	private void BuildFromParameters( IEnumerable<IBlackboardParameter> parameters, bool preserveSelection = false )
	{
		// Only the rows, so a filter being typed into isn't hidden along with them and loses focus
		using var _ = SuspendUpdates.For( _rowsCanvas );

		_rows.Clear( true );
		_rowWidgets.Clear();
		var groups = FilteredGroups( parameters ).ToList();
		_hasVisibleParameters = groups.Any();
		var filtering = !string.IsNullOrWhiteSpace( _filter.Text );

		//foreach ( var groupedParameter in groups.OrderBy( x => Graph.GetCategoryData( string.IsNullOrWhiteSpace( x.Key ) ? "General" : x.Key ).Priority ) )
		foreach ( var groupedParameter in groups.OrderBy( x => Graph.GetCategoryDataIndex( string.IsNullOrWhiteSpace( x.Key ) ? "General" : x.Key ) ) )
		{
			var collapsed = !filtering && _collapsedGroups.Contains( groupedParameter.Key );

			var groupName = string.IsNullOrWhiteSpace( groupedParameter.Key ) ? "General" : groupedParameter.Key;

			//SGPLogger.Info( $"Setting up group \"{groupName}\"" );

			_rows.Add( new ParameterGroupHeader( this, Graph.GetCategoryData( groupName ), groupedParameter.Count(), collapsed, !filtering ) );

			if ( collapsed )
				continue;

			var body = new Widget
			{
				Layout = Layout.Column(),
				HorizontalSizeMode = SizeMode.Flexible,
				VerticalSizeMode = SizeMode.CanShrink
			};
			body.Layout.Margin = new Sandbox.UI.Margin( 4, 4, 12, 4 );
			body.Layout.Spacing = 2;
			body.OnPaintOverride = () =>
			{
				Paint.ClearPen();
				Paint.SetBrush( Theme.WidgetBackground.Darken( 0.1f ) );
				Paint.DrawRect( body.LocalRect );
				return true;
			};
			_rows.Add( body, 0 );

			foreach ( var parameter in groupedParameter )
			{
				IParameterRow row = parameter switch
				{
					BlackboardParameter bpParameter => new ParameterRow( _window, this, bpParameter ),
					_ => throw new NotSupportedException()
				};
				_rowWidgets.Add( row );
				body.Layout.Add( (Widget)row );
			}
		}

		_rows.AddStretchCell();
	}

	public void RebuildFromGraph( bool preserveSelection = false )
	{
		if ( Graph is not null )
			BuildFromParameters( Graph.Parameters, preserveSelection );
	}

	/// <summary>
	/// Repaint rows so selection highlights stay in sync with the properties target.
	/// </summary>
	internal void UpdateSelection()
	{
		foreach ( var row in _rowWidgets )
			((Widget)row).Update();
	}

	private record GroupEntry( INewGroupableBlackboardParameter Parameter, string GroupName );

	private IEnumerable<IGrouping<string, INewGroupableBlackboardParameter>> FilteredGroups( IEnumerable<IBlackboardParameter> parameters )
	{
		var bpParameters = (parameters.Cast<INewGroupableBlackboardParameter>() ?? []);
		var filter = _filter.Text?.Trim() ?? "";

		var result = bpParameters.Where( p => string.IsNullOrEmpty( filter )
			|| p.DisplayInfo.Name.Contains( filter, StringComparison.OrdinalIgnoreCase )
			|| p.Name.Contains( filter, StringComparison.OrdinalIgnoreCase )
			|| GroupTitle( GroupName( p ) ).Contains( filter, StringComparison.OrdinalIgnoreCase ) )
			.OrderBy( p => Graph.GetParameterIndexInCategory( GroupName( p ), p.Identifier ) );

		return result
			.GroupBy( GroupName )
			.OrderBy( group => string.IsNullOrEmpty( group.Key ) ? 0 : 1 )
			.ThenBy( group => group.Key, StringComparer.OrdinalIgnoreCase );
	}

	private IEnumerable<INewGroupableBlackboardParameter> DisplayedParameters( IEnumerable<IBlackboardParameter> parameters )
	{
		var filtering = !string.IsNullOrWhiteSpace( _filter.Text );
		foreach ( var group in FilteredGroups( parameters ) )
		{
			if ( !filtering && _collapsedGroups.Contains( group.Key ) )
				continue;

			foreach ( var parameter in group )
				yield return parameter;
		}
	}

	internal static string GroupName( INewGroupableBlackboardParameter parameter ) => NormalizeGroup( parameter.Group );

	internal static string GroupTitle( string group ) => string.IsNullOrEmpty( group ) ? "General" : group;

	internal void Remove( BlackboardParameter parameter )
	{
		var parameterNodes = Graph.Nodes.OfType<BlackboardNode>()
			.Where( node => node.ParameterIdentifier == parameter.Identifier )
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

			RemoveParameterFromCategoryData( parameter );

			//OnDirty?.Invoke( true );
		}

		var referenceCount = parameterNodes.Length;
		if ( referenceCount == 0 )
		{
			Delete();
			return;
		}

		var usages = new List<string>();
		if ( parameterNodes.Length > 0 )
			usages.Add( $"{parameterNodes.Length} parameter node{(parameterNodes.Length == 1 ? "" : "s")}" );

		var confirm = new PopupWindow(
			"Delete Parameter",
			$"Delete '{parameter.Name}'?\n\nUsed by {string.Join( " and ", usages )}. These references will be cleared.",
			"Cancel",
			new Dictionary<string, Action> { ["Delete"] = Delete } );
		confirm.Show();
		confirm.Window.Position = _window.ScreenRect.Center - confirm.Window.Size * 0.5f;
	}

	internal static SerializedProperty ResolveParameterProperty( SerializedProperty property )
	{
		while ( property?.Parent?.ParentProperty is { } parent )
			property = parent;

		return property;
	}

	internal void Rename( IBlackboardParameter parameter, string name )
	{
		name = name?.Trim();

		if ( string.IsNullOrEmpty( name ) || name == parameter.Name )
			return;

		if ( Graph.Parameters.Any( p => p != parameter && p.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) )
			return;

		using var undoScope = UndoScope( "Rename Parameter" );

		Graph.RenameParameter( parameter, name );

		_window.OnParameterSelected( parameter );
	}

	internal void BeginRename( IBlackboardParameter parameter )
	=> _rowWidgets.FirstOrDefault( r => r.Parameter == parameter )?.StartRename();

	internal void ToggleGroup( string group )
	{
		if ( !string.IsNullOrWhiteSpace( _filter.Text ) )
			return;

		if ( !_collapsedGroups.Add( group ) )
			_collapsedGroups.Remove( group );

		SaveCollapsedGroups();
		BuildFromParameters( Graph.Parameters );
	}

	internal void MoveToGroup( INewGroupableBlackboardParameter parameter, string group )
	{
		group = NormalizeGroup( group );
		if ( GroupName( parameter ).Equals( group, StringComparison.OrdinalIgnoreCase ) )
			return;

		using var undoScope = UndoScope( "Move Parameter" );

		var oldGroup = parameter.Group;
		parameter.Group = group;
		ExpandGroup( group );
		PruneCollapsedGroups();

		SetCategoryData( group, oldGroup, parameter );

		_window.OnParameterSelected( parameter );
	}

	internal void ReorderGroup( CategoryData category, int priority )
	{
		using var undoScope = UndoScope( "Move Parameter Group" );

		Log.Info( $"Moving Category \"{category.Name}\" to priority \"{priority}\"" );

		Graph.ReOrderCategory( category, priority );

		RebuildFromGraph();
	}

	internal void RenameGroup( string group )
	{
		if ( string.IsNullOrEmpty( group ) )
			return;

		OpenGroupDialog( "Rename Parameter Group", group, newName =>
		{
			newName = NormalizeGroup( newName );
			if ( newName.Equals( group, StringComparison.OrdinalIgnoreCase ) )
				return;

			using var undoScope = UndoScope( "Rename Parameter Group" );

			foreach ( var parameter in Graph.Parameters.Where( p => GroupName( p ).Equals( group, StringComparison.OrdinalIgnoreCase ) ) )
			{
				parameter.Group = newName;
			}

			RenameCategoryData( group, newName );

			if ( _collapsedGroups.Remove( group ) && !string.IsNullOrEmpty( newName ) )
				_collapsedGroups.Add( newName );

			PruneCollapsedGroups();
		} );
	}

	internal void ClearGroup( string group )
	{
		if ( string.IsNullOrEmpty( group ) )
			return;

		using var undoScope = UndoScope( "Remove Parameter Group" );

		foreach ( var parameter in Graph.Parameters.Where( p => GroupName( p ).Equals( group, StringComparison.OrdinalIgnoreCase ) ) )
		{
			// Migrate each parameter to the 'General' category.
			SetCategoryData( "", parameter.Group, parameter );
			parameter.Group = "";
		}

		_collapsedGroups.Remove( group );
		SaveCollapsedGroups();
	}

	internal void AddGroupOptions( Menu menu, INewGroupableBlackboardParameter parameter )
	{
		var groups = menu.AddMenu( "Move to Group", "folder" );
		groups.AddOption( "General", string.IsNullOrEmpty( GroupName( parameter ) ) ? "check" : "", () => MoveToGroup( parameter, "" ) );

		foreach ( var group in ExistingGroups() )
		{
			groups.AddOption( group, GroupName( parameter ).Equals( group, StringComparison.OrdinalIgnoreCase ) ? "check" : "", () => MoveToGroup( parameter, group ) );
		}

		groups.AddSeparator();
		groups.AddOption( "New Group…", "create_new_folder", () => OpenGroupDialog( "New Parameter Group", "", group => MoveToGroup( parameter, group ) ) );
	}

	private IEnumerable<string> ExistingGroups() => Graph.Parameters.OfType<INewGroupableBlackboardParameter>()
	.Select( GroupName )
	.Where( group => !string.IsNullOrEmpty( group ) )
	.Distinct( StringComparer.OrdinalIgnoreCase )
	.OrderBy( group => group, StringComparer.OrdinalIgnoreCase );

	private void OpenGroupDialog( string title, string initialValue, Action<string> accepted )
	{
		Dialog.AskString(
			name => accepted( name.Trim() ),
			"Group name:",
			okay: "Apply",
			initialName: initialValue,
			title: title,
			minLength: 1
		);
	}

	private void OpenAddMenu( Widget anchor )
	{
		var menu = CreateMenu();
		AddParameterOptions( menu, "" );
		AddGroupedOptions( menu, false );
		menu.OpenAt( anchor.ScreenRect.BottomLeft );
	}

	private void AddGroupedOptions( Menu menu, bool isVirtual )
	{
		var groups = ExistingGroups().ToList();
		if ( groups.Count == 0 )
			return;

		menu.AddSeparator();
		var grouped = menu.AddMenu( "Add to Group", "folder" );
		foreach ( var group in groups )
		{
			var groupMenu = grouped.AddMenu( group, "folder" );
			AddParameterOptions( groupMenu, group );
		}
	}

	private void RenameCategoryData( string oldName, string newName )
	{
		if ( Graph is null )
			return;

		if ( Graph.TryFindCategoryData( oldName, out var existingCategory ) )
		{
			existingCategory.Name = newName;
		}
	}

	private void RemoveParameterFromCategoryData( BlackboardParameter parameter )
	{
		if ( Graph is null )
			return;

		var group = string.IsNullOrWhiteSpace( parameter.Group ) ? "General" : parameter.Group;

		if ( Graph.TryFindCategoryData( group, out var category ) )
		{
			category.ParameterReferences.Remove( parameter.Identifier );

			if ( category.ParameterReferences.Count == 0 )
			{
				Graph.RemoveCategoryData( category );
			}
		}
	}

	private void SetCategoryData( string group, string oldGroup, IBlackboardParameter parameter )
	{
		if ( Graph is null )
			return;

		oldGroup = string.IsNullOrWhiteSpace( oldGroup ) ? "General" : oldGroup;
		group = string.IsNullOrWhiteSpace( group ) ? "General" : group;

		//SGPLogger.Info( $"Moving Parameter \"{parameter.Name}\" from group \"{oldGroup}\" to group \"{group}\"" );

		if ( oldGroup != group && Graph.TryFindCategoryData( oldGroup, out var oldCategory ) )
		{
			oldCategory.ParameterReferences.Remove( parameter.Identifier );

			if ( oldCategory.ParameterReferences.Count == 0 )
			{
				Graph.RemoveCategoryData( oldCategory );
			}
		}

		if ( !Graph.HasCategoryDataWithName( group ) )
		{
			var categoryData = new CategoryData();
			categoryData.Name = group;
			categoryData.ParameterReferences.Add( parameter.Identifier );

			Graph.AddCategoryData( categoryData );
		}
		else if ( Graph.TryFindCategoryData( group, out var existingCategory ) )
		{
			existingCategory.ParameterReferences.Add( parameter.Identifier );
		}
	}

	private void CreateNewParameter( IBlackboardParameterType type, string group = "" )
	{
		group = NormalizeGroup( group );

		_filter.Text = "";
		ExpandGroup( group );

		using var undoScope = UndoScope( "Add Parameter" );

		var parameter = (BlackboardParameter)type.CreateParameter( Graph );

		if ( parameter is INewGroupableBlackboardParameter newGroupable )
		{
			newGroupable.Group = group;

			SetCategoryData( group, group, newGroupable );
		}

		Graph.AddParameter( parameter );

		OnDirty?.Invoke( true );

		_window.OnParameterSelected( parameter );

		RebuildFromGraph( true );
	}

	public IBlackboardParameter CreateNewParameter( IBlackboardParameterType type, string name = "", Action onCreated = null )
	{
		if ( type == null )
			return null;

		var group = NormalizeGroup( "" );

		_filter.Text = "";
		ExpandGroup( group );

		var parameter = type.CreateParameter( Graph, name );

		if ( parameter == null )
			return null;

		if ( parameter is INewGroupableBlackboardParameter groupable )
		{
			groupable.Group = group;
		}

		onCreated?.Invoke();

		Graph.AddParameter( parameter );

		return parameter;
	}

	internal void AddParameterOptions( Menu menu, string group )
	{
		void AddOption( Menu menu, IBlackboardParameterType parameterType, string icon, string description )
		{
			var option = menu.AddOption( parameterType.Type.Title, !string.IsNullOrWhiteSpace( icon ) ? icon : null, () =>
			{
				CreateNewParameter( parameterType, group );
			} );

			option.ToolTip = description;
		}

		IBlackboardParameterType[] avalibleTypes = BlackboardParameter.GetRelevantParameters( _availableParameters, Graph.IsSubgraph ).ToArray();

		if ( !Graph.IsSubgraph )
		{
			var materialParametersMenu = menu.AddMenu( "Parameter" );
			materialParametersMenu.Icon = "edit_attributes";

			var attributesMenu = menu.AddMenu( "Attribute" );
			attributesMenu.Icon = "edit_attributes";

			var materialCombosMenu = menu.AddMenu( "Combo" );
			materialCombosMenu.Icon = "alt_route";

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

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

				AddOption( menu, parameterType, icon, description );
			}
		}
		else
		{
			var subgraphInputsMenu = menu.AddMenu( "Input" );
			subgraphInputsMenu.Icon = "input";

			var subgraphOutputsMenu = menu.AddMenu( "Output" );
			subgraphOutputsMenu.Icon = "output";

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

				if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) )
				{
					menu = subgraphInputsMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) )
				{
					menu = subgraphOutputsMenu;
				}

				AddOption( menu, parameterType, icon, description );
			}
		}
	}

	private static string NormalizeGroup( string group )
	{
		group = group?.Trim() ?? "";
		return group.Equals( "General", StringComparison.OrdinalIgnoreCase ) ? "" : group;
	}

	private void ExpandGroup( string group )
	{
		if ( _collapsedGroups.Remove( group ) )
			SaveCollapsedGroups();
	}

	private void PruneCollapsedGroups()
	{
		var groups = Graph.Parameters.OfType<INewGroupableBlackboardParameter>().Select( GroupName ).ToHashSet( StringComparer.OrdinalIgnoreCase );
		_collapsedGroups.RemoveWhere( group => !groups.Contains( group ) );
		SaveCollapsedGroups();
	}

	private void SaveCollapsedGroups()
	=> EditorCookie.Set( CollapsedGroupsCookie, _collapsedGroups.OrderBy( x => x ).ToList() );

	internal void OnGraphDirty()
	{
		if ( Graph is null )
			return;

		var parameters = DisplayedParameters( Graph.Parameters ).ToList();
		var stale = _rowWidgets.Count != parameters.Count;

		for ( var i = 0; !stale && i < _rowWidgets.Count; i++ )
		{
			var p = parameters[i];
			stale = _rowWidgets[i].Parameter != p || _rowWidgets[i].BuiltGroup != GroupName( p );
		}

		if ( stale )
		{
			BuildFromParameters( Graph.Parameters );
			return;
		}

		//UpdateUnusedButton();
		UpdateSelection();
	}

	internal Menu CreateMenu() => new( _window );

	private static void AddDivider( Layout row )
	{
		row.AddSpacingCell( 2 );
		row.Add( new Separator( 1 ) { FixedWidth = 1, FixedHeight = Theme.RowHeight - 8, Color = Color.White.WithAlpha( 0.1f ) } );
		row.AddSpacingCell( 2 );
	}

	private ToolButton HeaderButton( string icon, string toolTip, Action onClick )
	{
		var button = new ToolButton( "", icon, this ) { ToolTip = toolTip, MouseLeftPress = onClick };
		button.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( button.Enabled && Paint.HasMouseOver ? Theme.ControlBackground.Lighten( 0.1f ) : Theme.ControlBackground );
			Paint.DrawRect( button.LocalRect, Theme.ControlRadius );

			Paint.ClearBrush();
			Paint.SetPen( button.Enabled ? Theme.Primary : Theme.TextControl.WithAlpha( 0.25f ) );
			Paint.DrawIcon( button.LocalRect, icon, 14, TextFlag.Center );
			return true;
		};
		return button;
	}
}

/// <summary>
/// Carried in drag data when dragging a parameter onto the graph or when reordering it in the blackboard.
/// </summary>
public record ParameterDragData( BlackboardParameter Parameter, CategoryData SourceCategory );

public record ParameterGroupDragData( CategoryData Category );

internal interface IParameterRow
{
	INewGroupableBlackboardParameter Parameter { get; }
	string BuiltGroup { get; }
	void StartRename();
}

internal sealed class ParameterGroupHeader : InspectorHeader
{
	private readonly NewBlackboardView _blackboardView;
	private readonly string _title;
	private readonly int _count;

	private Vector2? _dragStart;
	private bool _draggingAbove = false;
	private bool _draggingOnto = false;
	private bool _draggingBelow = false;
	private bool _draggingGroup = false;

	public CategoryData Category { get; }

	public ParameterGroupHeader( NewBlackboardView list, CategoryData category, int count, bool collapsed, bool collapsible )
	{
		_blackboardView = list;
		Category = category;
		category.Graph = list.Graph;
		_title = NewBlackboardView.GroupTitle( category.Name );
		_count = count;

		Title = "";
		Icon = "folder";
		Color = Theme.Blue;
		IsDraggable = true;
		AcceptDrops = true;
		IsCollapsable = collapsible;
		IsExpanded = !collapsed;
		BuildUI();

		Cursor = collapsible ? CursorShape.Finger : CursorShape.Arrow;
		AcceptDrops = true;
		ToolTip = collapsible
			? "Click to collapse or expand. Drag parameters here to move them into this group. Right-click for group actions."
			: "Matching groups are expanded while filtering. Drag parameters here to move them into this group. Right-click for group actions.";
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.WidgetBackground );
		Paint.DrawRect( LocalRect );

		base.OnPaint();

		var count = _count.ToString();
		Paint.SetDefaultFont( 8, 500 );
		var countMarginY = MathF.Floor( (LocalRect.Height - 18) * 0.5f );
		var countHeight = LocalRect.Height - countMarginY * 2;
		var countWidth = MathF.Max( countHeight, Paint.MeasureText( count ).x + 8 );
		var countRect = new Rect( LocalRect.Right - countWidth - 12, countMarginY + 1, countWidth, countHeight );

		Paint.SetPen( Theme.Text.WithAlpha( IsExpanded || !IsCollapsable ? 1f : 0.8f ) );
		Paint.SetHeadingFont( 11, 440, sizeInPixels: true );
		Paint.DrawText( LocalRect.Shrink( 48, 0, countWidth + 20, 0 ), _title, TextFlag.LeftCenter | TextFlag.SingleLine );

		Paint.ClearPen();
		Paint.SetBrush( Theme.TextControl.WithAlpha( 0.08f ) );
		Paint.DrawRect( countRect, Theme.ControlRadius );
		Paint.SetPen( Theme.TextControl.WithAlpha( 0.5f ) );
		Paint.SetDefaultFont( 8, 500 );
		Paint.DrawText( countRect, count, TextFlag.Center );

		if ( !_draggingGroup && _draggingOnto )
		{
			Paint.ClearBrush();
			Paint.SetPen( Theme.Primary, 1.5f );
			Paint.DrawRect( LocalRect.Shrink( 0.75f ) );
		}
		else
		{
			if ( _draggingAbove )
			{
				Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
				Paint.DrawLine( LocalRect.TopLeft, LocalRect.TopRight );
				_draggingAbove = false;
			}
			else if ( _draggingBelow )
			{
				Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
				Paint.DrawLine( LocalRect.BottomLeft, LocalRect.BottomRight );
				_draggingBelow = false;
			}
		}
	}

	protected override void BuildRightIcons( Layout layout )
	{
	}

	protected override void OnExpandChanged()
	{
		_blackboardView.ToggleGroup( Category.Name == "General" ? "" : Category.Name );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.LeftMouseButton )
		{
			_dragStart = e.LocalPosition;
		}

		if ( e.RightMouseButton )
			OpenContextMenu();
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		_dragStart = null;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );

		Update();

		//if ( Category.Name == "General" )
		//	return;

		if ( _dragStart is null || !e.ButtonState.HasFlag( MouseButtons.Left ) )
			return;

		if ( e.LocalPosition.Distance( _dragStart.Value ) < 10 )
			return;

		_dragStart = null;

		var drag = new Drag( this );
		drag.Data.Text = Category.Name;
		drag.Data.Object = new ParameterGroupDragData( Category );

		drag.Execute();
	}

	private bool TryDragOperation( DragEvent ev, int sourceParameterIndex, int targetParameterIndex )
	{
		if ( Category.Name == "General" )
		{
			return false;
		}

		var sourceGroupDragData = ev.Data.OfType<ParameterGroupDragData>().FirstOrDefault();

		if ( sourceGroupDragData is null || Category is null || sourceGroupDragData.Category == Category )
			return false;

		if ( sourceParameterIndex <= targetParameterIndex )
		{
			//SGPLogger.Info( $"Dragging \"{sourceGroupDragData.Category.Name}\" at index \"{sourceParameterIndex}\"  below \"{Category.Name}\" at index \"{targetParameterIndex}\"" );
			_draggingBelow = true;
		}
		else
		{
			//SGPLogger.Info( $"Dragging \"{sourceGroupDragData.Category.Name}\" at index \"{sourceParameterIndex}\" Above \"{Category.Name}\" at index \"{targetParameterIndex}\"" );
			_draggingAbove = true;
		}

		return true;
	}

	public override void OnDragHover( DragEvent ev )
	{
		void OnDrag( DragEvent ev, DropAction dropAction )
		{
			ev.Action = dropAction;
			_draggingOnto = dropAction == DropAction.Move;
		}

		if ( ev.Data.Object is ParameterDragData parameterDragData )
		{
			if ( !NewBlackboardView.GroupName( parameterDragData.Parameter ).Equals( Category.Name, StringComparison.OrdinalIgnoreCase ) )
			{
				ev.Action = DropAction.Move;
				_draggingOnto = true;
			}
			else
			{
				ev.Action = DropAction.Ignore;
				_draggingOnto = false;
			}

			_draggingGroup = false;
			Update();

			return;
		}
		else if ( ev.Data.Object is ParameterGroupDragData groupDragData )
		{
			if ( groupDragData.Category.Name == "General" )
			{
				OnDrag( ev, DropAction.Ignore );
				return;
			}

			var sourceGroupIndex = _blackboardView.Graph.GetCategoryDataIndex( groupDragData.Category );
			var targetGroupIndex = _blackboardView.Graph.GetCategoryDataIndex( Category );

			if ( !TryDragOperation( ev, sourceGroupIndex, targetGroupIndex ) )
			{
				_draggingAbove = false;
				_draggingBelow = false;

				OnDrag( ev, DropAction.Ignore );
				return;
			}
			else
			{
				OnDrag( ev, DropAction.Move );
			}

			_draggingGroup = true;
			Update();

			return;
		}

		OnDrag( ev, DropAction.Ignore );
	}

	public override void OnDragDrop( DragEvent ev )
	{
		_draggingOnto = false;
		Update();

		if ( ev.Data.Object is ParameterDragData parameterDragData )
		{
			_blackboardView.MoveToGroup( parameterDragData.Parameter, Category.Name );
		}
		else if ( ev.Data.Object is ParameterGroupDragData groupDragData )
		{
			_blackboardView.ReorderGroup( groupDragData.Category, _blackboardView.Graph.GetCategoryDataIndex( Category ) );
		}
	}

	public override void OnDragLeave()
	{
		_draggingOnto = false;
		Update();
	}

	private void OpenContextMenu()
	{
		var menu = _blackboardView.CreateMenu();
		var parameters = menu.AddMenu( "Add Parameter", "add" );
		_blackboardView.AddParameterOptions( parameters, Category.Name );

		if ( Category.Name != "General" )
		{
			menu.AddSeparator();
			menu.AddOption( "Rename Group", "edit", () => _blackboardView.RenameGroup( Category.Name ) );
			menu.AddOption( "Remove Group", "folder_off", () => _blackboardView.ClearGroup( Category.Name ) );
		}

		menu.OpenAtCursor();
	}
}

internal class ParameterRow : Widget, IParameterRow
{
	INewGroupableBlackboardParameter IParameterRow.Parameter => _parameter;

	private readonly BlackboardParameter _parameter;
	private readonly CategoryData _category;

	private readonly MainWindow _window;
	private readonly NewBlackboardView _blackboardView;
	private readonly Widget _nameCell;
	private float _headerHeight = Theme.RowHeight;
	private Vector2? _dragStart;
	private LineEdit _renameEdit;

	private const float NameX = 3;

	private Rect PillRect
	{
		get
		{
			var rect = LocalRect;
			rect.Height = _headerHeight + 6;
			rect = rect.Shrink( 0, 1 );
			rect.Right = _nameCell.Position.x + _nameCell.Width;

			return rect;
		}
	}

	private bool _draggingAbove = false;
	private bool _draggingBelow = false;

	public string BuiltGroup { get; }

	public ParameterRow( MainWindow window, NewBlackboardView list, BlackboardParameter parameter ) : base( list )
	{
		_window = window;
		_blackboardView = list;
		parameter.Graph = list.Graph;
		_parameter = parameter;
		_category = _blackboardView.Graph.GetCategoryData( string.IsNullOrWhiteSpace( _parameter.Group ) ? "General" : _parameter.Group );

		BuiltGroup = NewBlackboardView.GroupName( parameter );

		FixedHeight = Theme.RowHeight + 6;
		Cursor = CursorShape.None;
		MouseTracking = true;
		FocusMode = FocusMode.Click;

		IsDraggable = true;
		AcceptDrops = true;

		ToolTip = $"{_parameter.DisplayInfo.Name} parameter\nDrag onto the graph to create a node\nDouble-click the parameter pill to rename\nClick the usage count to find references";

		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( NameX, 3, NameX, 3 );

		Layout.Spacing = 4f;
		Layout.Alignment = TextFlag.LeftTop;

		_renameEdit = new LineEdit( this ) { Visible = false, FixedHeight = Theme.RowHeight };
		_renameEdit.SetStyles( $"background-color: {Theme.ControlBackground.Hex}; selection-background-color: {Theme.Primary.Hex}; selection-color: white; border: none;" );
		_renameEdit.EditingFinished += FinishRename;

		// The rename edit lives in the name area cell so the layout owns its geometry
		_nameCell = Layout.Add( new Widget( this ) { Layout = Layout.Column(), TransparentForMouseEvents = true, FixedHeight = Theme.RowHeight }, 2 );
		_nameCell.Layout.Margin = new Sandbox.UI.Margin( 0, 0, 2, 0 );
		_nameCell.Layout.AddStretchCell( 1 );
		_nameCell.Layout.Add( _renameEdit );
		_nameCell.Layout.AddStretchCell( 1 );

		var so = parameter.GetSerialized();
		so.OnPropertyChanged += p =>
		{
			p = NewBlackboardView.ResolveParameterProperty( p );

			// Transient preview scrubs don't touch the document
			if ( p?.HasAttribute<JsonIgnoreAttribute>() != true )
				_window.SetDirty();
		};

		AddActions();
	}

	protected static void PaintTypeLabel( Rect row, string typeName, Color typeColor )
	{
		Color tint = "#48494c";

		var c = tint.ToHsv();
		var bg = c;

		if ( Paint.HasMouseOver )
		{
			bg = c with { Value = (c.Value + 0.1f) };
		}
		else
		{
			bg = c = Theme.SurfaceLightBackground;
		}

		if ( bg.Alpha > 0 )
		{
			float radius = 3;
			Paint.Antialiasing = true;

			Paint.ClearPen();
			Paint.SetBrush( bg with { Value = (bg.Value + 0.04f), Saturation = (c.Saturation * 0.8f) } );
			Paint.DrawRect( row, radius );

			Paint.SetBrushLinear( row.TopLeft, row.BottomRight, bg, bg with { Value = (bg.Value - 0.03f) } );
			Paint.DrawRect( row.Shrink( 1, 1, 1, 1 ), radius );
			Paint.SetPen( typeColor, 1 );
			Paint.DrawRect( row.Shrink( 1, 1, 1, 1 ), radius );

			var r2 = row.Grow( 1.25f );

			Paint.DrawRect( r2.Grow( 12, 0, 0, 0 ), radius );
		}
		else
		{
			c = Color.White.WithAlpha( 0.5f );
		}

		Paint.SetDefaultFont();
		Paint.SetPen( c with { Value = 0.99f, Saturation = c.Saturation * 0.20f } );
		Paint.DrawText( row, typeName );

		var iconRect = row;
		iconRect.Left -= 10;

		Paint.Pen = typeColor;
		Paint.DrawIcon( iconRect, "circle", 12, TextFlag.LeftCenter );
	}

	protected override void OnPaint()
	{
		var selected = _window.IsSelected( _parameter );
		var hovered = PillRect.IsInside( FromScreen( Editor.Application.CursorPosition ) );
		var typeColor = Color.White;

		if ( ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( _parameter.GetType(), out var blackboardConfig ) )
		{
			typeColor = blackboardConfig.Color;
		}

		var chip = PillRect;

		Paint.Antialiasing = true;
		Paint.ClearPen();
		Paint.SetBrush( blackboardConfig.Color.WithAlpha( selected ? 0.25f : hovered ? 0.18f : 0.1f ) );
		Paint.DrawRect( chip, Theme.ControlRadius );

		if ( selected )
		{
			Paint.ClearBrush();
			Paint.SetPen( blackboardConfig.Color.WithAlpha( 0.8f ) );
			Paint.DrawRect( chip.Shrink( 0.5f ), Theme.ControlRadius );
		}

		if ( _renameEdit.Visible )
			return;

		var typeName = _parameter.DisplayInfo.Name;
		var typeRectOffset = 24;
		var typeRect = Paint.MeasureText( chip.Shrink( chip.Left + typeRectOffset, 0, 0, 0 ), typeName, TextFlag.LeftCenter | TextFlag.SingleLine ).Grow( 4, 0, 4, 0 );

		PaintTypeLabel( typeRect, typeName, typeColor );

		var nameRectOffset = 8;
		var nameRect = chip.Shrink( typeRect.Right + nameRectOffset, 0, 0, 0 );

		Paint.SetPen( Theme.TextControl.WithAlpha( selected || hovered ? 0.9f : 0.8f ) );
		Paint.SetDefaultFont();
		Paint.DrawText( nameRect, $"{_parameter.Name}", TextFlag.LeftCenter | TextFlag.SingleLine );

		if ( _draggingAbove )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( LocalRect.TopLeft, LocalRect.TopRight );
			_draggingAbove = false;
		}
		else if ( _draggingBelow )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( LocalRect.BottomLeft, LocalRect.BottomRight );
			_draggingBelow = false;
		}
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( !PillRect.IsInside( e.LocalPosition ) )
			return;

		if ( e.LeftMouseButton )
		{
			_dragStart = e.LocalPosition;
			_window.OnParameterSelected( _parameter );
		}
		else if ( e.RightMouseButton )
		{
			_window.OnParameterSelected( _parameter );
			OpenContextMenu();
		}
	}

	protected override void OnMouseReleased( MouseEvent e ) => _dragStart = null;

	protected override void OnMouseMove( MouseEvent e )
	{
		Cursor = PillRect.IsInside( e.LocalPosition ) ? CursorShape.Finger : CursorShape.None;
		Update();

		if ( _dragStart is null || !e.ButtonState.HasFlag( MouseButtons.Left ) )
			return;

		if ( e.LocalPosition.Distance( _dragStart.Value ) < 10 )
			return;

		_dragStart = null;

		var drag = new Drag( this );
		drag.Data.Text = _parameter.Name;
		drag.Data.Object = new ParameterDragData( _parameter, _category );
		drag.Execute();
	}

	protected override void OnMouseLeave()
	{
		Cursor = CursorShape.None;
		Update();
		base.OnMouseLeave();
	}

	protected override void OnDoubleClick( MouseEvent e )
	{
		if ( !e.LeftMouseButton || !PillRect.IsInside( e.LocalPosition ) )
			return;

		_dragStart = null;
		StartRename();
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		switch ( e.Key )
		{
			case KeyCode.F2:
				StartRename();
				break;
			case KeyCode.Delete:
				_blackboardView.Remove( _parameter );
				break;
			case KeyCode.Escape when _renameEdit.Visible:
				_renameEdit.Text = _parameter.Name;
				_renameEdit.Blur();
				break;
			default:
				base.OnKeyPress( e );
				break;
		}
	}

	private bool TryDragOperation( ParameterDragData parameterDragData )
	{
		if ( parameterDragData is null || _parameter is null || parameterDragData.Parameter == _parameter )
			return false;

		void TryDragInGroup( CategoryData sharedCategory )
		{
			var sourceIndex = sharedCategory.ParameterReferences.IndexOf( parameterDragData.Parameter.Identifier );
			var targetIndex = sharedCategory.ParameterReferences.IndexOf( _parameter.Identifier );

			if ( sourceIndex < targetIndex )
			{
				_draggingBelow = true;
			}
			else
			{
				_draggingAbove = true;
			}
		}

		void TryDragAcrossGroup( CategoryData sourceCategory, CategoryData targetCategory )
		{
			var sourceGroupIndex = _blackboardView.Graph.GetCategoryDataIndex( sourceCategory );
			var targetGroupIndex = _blackboardView.Graph.GetCategoryDataIndex( targetCategory );

			if ( targetGroupIndex > sourceGroupIndex )
			{
				_draggingBelow = true;
			}
			else
			{
				_draggingAbove = true;
			}
		}

		if ( _category.Name == parameterDragData.SourceCategory.Name )
		{
			TryDragInGroup( parameterDragData.SourceCategory );
		}
		else if ( _category.Name != parameterDragData.SourceCategory.Name )
		{
			TryDragAcrossGroup( parameterDragData.SourceCategory, _category );
		}
		else
		{
			return false;
		}

		return true;
	}

	public override void OnDragHover( DragEvent ev )
	{
		base.OnDragHover( ev );

		if ( ev.Data.Object is ParameterDragData parameterDragData )
		{
			if ( !TryDragOperation( parameterDragData ) )
			{
				_draggingAbove = false;
				_draggingBelow = false;
				ev.Action = DropAction.Ignore;

				return;
			}
		}
		else
		{
			ev.Action = DropAction.Ignore;
		}
	}

	public override void OnDragDrop( DragEvent ev )
	{
		base.OnDragDrop( ev );

		if ( ev.Data.Object is not ParameterDragData parameterDragData )
			return;

		var sourceParameter = parameterDragData.Parameter;
		var targetParameter = _parameter;

		var sourceGroup = string.IsNullOrWhiteSpace( sourceParameter.Group ) ? "General" : sourceParameter.Group;
		var targetGroup = string.IsNullOrWhiteSpace( _parameter.Group ) ? "General" : _parameter.Group;

		var globalSourceIndex = _blackboardView.Graph.GetParameterIndex( sourceParameter );
		var globalTargetIndex = _blackboardView.Graph.GetParameterIndex( targetParameter );

		_blackboardView.Graph.TryFindCategoryData( sourceGroup, out var sourceCategory );
		_blackboardView.Graph.TryFindCategoryData( targetGroup, out var targetCategory );

		if ( !TryDragOperation( parameterDragData ) )
			return;

		if ( sourceCategory != null )
		{
			using var undoScope = _blackboardView.UndoScope( "Reorder Parameter" );

			if ( sourceGroup != targetGroup && targetCategory != null )
			{
				var targetIndex = targetCategory.ParameterReferences.IndexOf( targetParameter.Identifier );

				if ( _draggingBelow )
				{
					targetIndex++;
				}

				sourceCategory.ParameterReferences.Remove( sourceParameter.Identifier );

				_blackboardView.Graph.ReOrderParameter( sourceParameter, globalTargetIndex );
				targetCategory.ParameterReferences.Insert( targetIndex, sourceParameter.Identifier );

				sourceParameter.Group = targetGroup == "General" ? "" : targetGroup;
			}
			else
			{
				var targetIndex = sourceCategory.ParameterReferences.IndexOf( _parameter.Identifier );

				sourceCategory.ParameterReferences.Remove( sourceParameter.Identifier );

				_blackboardView.Graph.ReOrderParameter( sourceParameter, globalTargetIndex );
				sourceCategory.ParameterReferences.Insert( targetIndex, sourceParameter.Identifier );
			}
		}

		_blackboardView.RebuildFromGraph();
	}

	private void AddActions()
	{
		var actionsWidget = Layout.Add( new Widget( this ) { Layout = Layout.Row(), FixedHeight = _headerHeight } );
		var actions = actionsWidget.Layout;
		actions.Alignment = TextFlag.LeftTop;
		AddActions( actions );
	}

	private void AddActionColumn()
	{
		var column = Layout.AddColumn();
		AddActions( column.AddRow() );
		column.AddStretchCell();
	}

	private void AddActions( Layout actions )
	{
		actions.Add( new IconButton( "delete", () => _blackboardView.Remove( _parameter ), this )
		{
			ToolTip = "Delete parameter",
			IconSize = 16,
			Foreground = Theme.Red
		} );
	}

	public void StartRename()
	{
		if ( _renameEdit.Visible )
			return;

		_nameCell.TransparentForMouseEvents = false;
		_renameEdit.Text = _parameter.Name;
		_renameEdit.Visible = true;
		_renameEdit.SelectAll();
		_renameEdit.Focus();
	}

	private void FinishRename()
	{
		if ( !_renameEdit.Visible )
			return;

		_renameEdit.Visible = false;
		_nameCell.TransparentForMouseEvents = true;
		_blackboardView.Rename( _parameter, _renameEdit.Text );
		Update();
	}

	private void OpenContextMenu()
	{
		var menu = _blackboardView.CreateMenu();
		menu.AddOption( "Rename", "edit", StartRename, "F2" );
		_blackboardView.AddGroupOptions( menu, _parameter );

		menu.AddSeparator();
		menu.AddOption( "Delete", "delete", () => _blackboardView.Remove( _parameter ), "Del" );
		menu.OpenAtCursor();
	}
}
