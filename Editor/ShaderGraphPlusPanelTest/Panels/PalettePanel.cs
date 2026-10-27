using Editor;
using EditorExtras;
using Sandbox.UI;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

[StyleSheet.Inline( "sgp_palettepanel", Styles )]
public sealed partial class PalettePanel : Panel
{
	private TextInput _searchFilter;
	private readonly Sandbox.UI.TreeView<PaletteTreeNode> _tree;

	private bool _wasDragging;
	private bool _isSubgraph { get; }

	public PalettePanel( bool isSubgraph ) : base()
	{
		_isSubgraph = isSubgraph;

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );

		BuildToolbar();

		_tree = new Sandbox.UI.TreeView<PaletteTreeNode>();
		_tree.AddClass( "palette-tree" );
		_tree.GetChildren = treeNode => treeNode.Children;
		_tree.GetParent = treeNode => treeNode.Parent;
		_tree.CanExpand = treeNode => treeNode.CanHaveChildren;
		_tree.GetHeight = treeNode => treeNode is PaletteCategoryNode ? 30 : Theme.RowHeight;
		_tree.GetTooltip = treeNode => _tree.IsDragging ? null : treeNode.ToolTip;
		_tree.OnRow = OnRow;
		_tree.OnSelect = treeNode => SGPLogger.Info( $"selected {treeNode.Name}" );
		_tree.OnActivate = treeNode => SGPLogger.Info( $"activated {treeNode.Name}" );
		_tree.OnRename = ( treeNode, text ) => { treeNode.Name = text; SGPLogger.Info( $"renamed to {text}" ); };
		_tree.CanDrag = treeNode => treeNode is not PaletteCategoryNode;

		AddChild( _tree );

		UpdateTree();
	}

	public override void Tick()
	{
		base.Tick();

		// Tooltips are read on bind, so rebind to drop them while dragging
		if ( _tree.IsDragging == _wasDragging )
			return;

		_wasDragging = _tree.IsDragging;
		_tree.RebindRows();
	}

	private void OnRow( Sandbox.UI.TreeRow row, PaletteTreeNode node )
	{
		row.Text = node.Name;
		row.IconName = node.Icon;

		if ( node is PaletteTypeNode typeNode )
		{
			row.UserData = typeNode.DisplayInfo;
		}
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter Nodes..", "search" ) );
		_searchFilter.OnChange = ( value ) =>
		{
			UpdateTree();
		};
	}

	private IEnumerable<IGrouping<string, (TypeDescription Type, DisplayInfo Info)>> GetItems()
	{
		var types = EditorTypeLibrary.GetTypes<ShaderNodePlus>()
			.Where( x =>
			{
				if ( x.IsAbstract ) return false;
				if ( x.HasAttribute<HideAttribute>() ) return false;
				if ( x.HasAttribute<InternalNodeAttribute>() ) return false;
				if ( _isSubgraph && x.TargetType == typeof( Result ) )
				{
					return false;
				}
				if ( !_isSubgraph && x.HasAttribute<SubgraphOnlyAttribute>() ) return false;
				if ( x.TargetType == typeof( SubgraphNode ) )
				{
					return false;
				}
				return true;
			} )
			.Select( x => (Type: x, Info: DisplayInfo.ForType( x.TargetType )) );

		if ( !string.IsNullOrEmpty( _searchFilter.Value ) )
		{
			types = types.Where( e => e.Info.Name.Contains( _searchFilter.Value, StringComparison.OrdinalIgnoreCase ) );
		}

		return types.GroupBy( x => x.Info.Group?.ToLower() )
			.OrderBy( p => p.Key == null )
			.ThenBy( p => p.Key );
	}

	public void UpdateTree()
	{
		var roots = new List<PaletteTreeNode>();
		foreach ( var category in GetItems() )
		{
			var categoryNode = new PaletteCategoryNode( category.Key ?? "Uncategorized" );

			foreach ( var (type, info) in category.OrderBy( x => x.Info.Name ) )
			{
				categoryNode.AddItem( new PaletteTypeNode( info ) );
			}

			roots.Add( categoryNode );
		}

		_tree.Roots = roots;
		_tree.Refresh();
	}
}

class PaletteCategoryNode : PaletteTreeNode
{
	public override bool CanHaveChildren => true;

	public PaletteCategoryNode( string name ) : base( name )
	{
		Children = new List<PaletteTreeNode>();
	}

	public void AddItem( PaletteTreeNode item )
	{
		if ( item is null )
			return;

		if ( !CanHaveChildren )
			return;

		Children.Add( item );
	}
}

class PaletteTypeNode : PaletteTreeNode
{
	public DisplayInfo DisplayInfo { get; private set; }

	public PaletteTypeNode( DisplayInfo info )
	{
		DisplayInfo = info;
		Name = info.Name;
		Icon = info.Icon;
		ToolTip = info.Description;
	}
}

abstract class PaletteTreeNode
{
	public string Name { get; set; }

	public string Icon { get; init; }

	public string ToolTip { get; init; }

	public PaletteTreeNode Parent { get; set; }

	public PaletteTreeNode( string name, string icon = "", string tooltip = "" )
	{
		Name = name;
		Icon = icon;
		ToolTip = tooltip;
	}

	public PaletteTreeNode()
	{
		Name = "";
		Icon = "";
		ToolTip = "";
	}

	public virtual bool CanHaveChildren => false;

	private List<PaletteTreeNode> _children;
	public List<PaletteTreeNode> Children
	{
		get
		{
			if ( _children is not null ) return _children;

			_children = new List<PaletteTreeNode>();
			if ( !CanHaveChildren ) return _children;

			return _children;
		}
		set
		{
			_children = value;
		}
	}

	public bool IsDescendantOf( PaletteTreeNode other )
	{
		for ( var p = Parent; p is not null; p = p.Parent )
		{
			if ( p == other ) return true;
		}

		return false;
	}
}
