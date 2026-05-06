using Editor;
using NodeEditorPlus;
using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlus;

public class ShaderGraphPlusView : GraphView
{
	private enum DragEventSource
	{
		NodePallete,
		SubgraphAsset,
		ImageFile,
		BlackboardParameter,
		Invalid
	}

	private readonly MainWindow _window;
	private readonly BlackboardView _blackboard;
	private readonly UndoStack _undoStack;

	private DragEventSource _currentDragEventSource = DragEventSource.Invalid;

	protected override string ClipboardIdent => "shadergraphplus";

	protected override string ViewCookie => _window?.AssetPath;

	private static bool? _cachedConnectionStyle;

	public static bool EnableGridAlignedWires
	{
		get => _cachedConnectionStyle ??= EditorCookie.Get( "shadergraphplus.gridwires", false );
		set => EditorCookie.Set( "shadergraphplus.gridwires", _cachedConnectionStyle = value );
	}

	private ConnectionStyle _oldConnectionStyle;

	public new ShaderGraphPlus Graph
	{
		get => (ShaderGraphPlus)base.Graph;
		set => base.Graph = value;
	}

	public Action<BaseNodePlus> OnNodeRemoved { get; set; }

	private readonly Dictionary<string, INodeType> AvailableNodes = new( StringComparer.OrdinalIgnoreCase );
	private readonly Dictionary<string, IBlackboardParameterType> AvailableParameters = new( StringComparer.OrdinalIgnoreCase );

	public override ConnectionStyle ConnectionStyle => EnableGridAlignedWires
	? GridConnectionStyle.Instance
	: ConnectionStyle.Default;

	public ShaderGraphPlusView( Widget parent, MainWindow window, BlackboardView blackboard ) : base( parent )
	{
		_window = window;
		_blackboard = blackboard;
		_undoStack = window.UndoStack;

		OnSelectionChanged += SelectionChanged;
	}

	protected override INodeType RerouteNodeType { get; } = new ClassNodeType( EditorTypeLibrary.GetType<ReroutePlus>() );
	protected override INodeType CommentNodeType { get; } = new ClassNodeType( EditorTypeLibrary.GetType<CommentNode>() );

	public void AddNodeType<T>()
		where T : BaseNodePlus
	{
		AddNodeType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddNodeType( TypeDescription type )
	{
		var nodeType = new ClassNodeType( type );

		AvailableNodes.TryAdd( nodeType.Identifier, nodeType );
	}

	public void AddNodeType( string subgraphPath )
	{
		var subgraphTxt = Editor.FileSystem.Content.ReadAllText( subgraphPath );
		var subgraph = new ShaderGraphPlus();
		subgraph.Deserialize( subgraphTxt );
		if ( !subgraph.AddToNodeLibrary ) return;
		var nodeType = new SubgraphNodeType( subgraphPath, EditorTypeLibrary.GetType<SubgraphNode>() );
		nodeType.SetDisplayInfo( subgraph );
		AvailableNodes.TryAdd( nodeType.Identifier, nodeType );
	}

	public INodeType FindNodeType( Type type )
	{
		return AvailableNodes.TryGetValue( type.FullName!, out var nodeType ) ? nodeType : null;
	}

	public IBlackboardParameterType FindParameterType( Type type )
	{
		return AvailableParameters.TryGetValue( type.FullName!, out var parameterType ) ? parameterType : null;
	}

	public void AddParameterType<T>() where T : BlackboardParameter
	{
		AddParameterType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddParameterType( TypeDescription type )
	{
		var parameterType = new ClassBlackboardParameterType( type );

		AvailableParameters.TryAdd( parameterType.Identifier, parameterType );
	}

	protected override INodeType NodeTypeFromDragEvent( DragEvent ev )
	{
		if ( ev.Data.Assets.FirstOrDefault() is { } asset )
		{
			if ( asset.IsInstalled )
			{
				if ( string.Equals( Path.GetExtension( asset.AssetPath ), ".shdrfunc", StringComparison.OrdinalIgnoreCase ) )
				{
					_currentDragEventSource = DragEventSource.SubgraphAsset;

					return new SubgraphNodeType( asset.AssetPath, EditorTypeLibrary.GetType<SubgraphNode>() );
				}
				else
				{
					var realAsset = asset.GetAssetAsync().Result;
					if ( realAsset.AssetType == AssetType.ImageFile )
					{
						_currentDragEventSource = DragEventSource.ImageFile;

						return new ParameterNodeType( EditorTypeLibrary.GetType<Texture2DParameterNode>(), asset.AssetPath, () =>
						{
							_blackboard.RebuildFromGraph( true );
						}
						);
					}
				}
			}
		}

		if ( ev.Data.Object is BlackboardParameter blackboardParameter )
		{
			_currentDragEventSource = DragEventSource.BlackboardParameter;

			return new ParameterNodeType( blackboardParameter );
		}

		_currentDragEventSource = DragEventSource.NodePallete;

		return AvailableNodes.TryGetValue( ev.Data.Text, out var type )
			? type
			: null;
	}

	protected override IEnumerable<INodeType> GetRelevantNodes( NodeQuery query )
	{
		return AvailableNodes.Values.Filter( query ).Where( x =>
		{
			if ( x is ClassNodeType classNodeType )
			{
				var targetType = classNodeType.Type.TargetType;
				if ( classNodeType.Type.HasAttribute<HideAttribute>() ) return false;
				if ( classNodeType.Type.HasAttribute<InternalNodeAttribute>() ) return false;
				if ( Graph.IsSubgraph && targetType == typeof( Result ) ) return false;
				if ( targetType == typeof( SubgraphNode ) && classNodeType.DisplayInfo.Name == targetType.Name.ToTitleCase() ) return false;
			}
			return true;
		} );
	}

	private static bool TryGetHandleConfig( Type type, out Type matchingType, out NodeHandleConfig config )
	{
		if ( ShaderGraphPlusTheme.NodeHandleConfigs.TryGetValue( type, out config ) )
		{
			matchingType = type;
			return true;
		}

		matchingType = null;
		return false;
	}

	protected override NodeHandleConfig OnGetHandleConfig( Type type )
	{
		if ( TryGetHandleConfig( type, out var matchingType, out var config ) )
		{
			return config with { Name = type == matchingType ? config.Name : null };
		}

		return base.OnGetHandleConfig( type );
	}

	protected override void OnPopulateNodeMenuSpecialOptions( Menu menu, Vector2 clickPos, NodePlug targetPlug, string filter )
	{
		void NewParameterMenuOption( Menu menu, string baseParameterName, IBlackboardParameterType classType, string undoScopeName )
		{
			var option = menu.AddOption( classType.Type.Title, classType.Type.Icon, () =>
			{
				using var undoScope = UndoScope( undoScopeName );

				var baseName = baseParameterName;
				var id = 0;
				while ( Graph.HasParameterWithName( $"{baseName}{id}" ) )
				{
					id++;
				}

				var parameter = CreateNewParameter( classType );
				parameter.Name = $"{baseName}{id}";

				var node = CreateNewParameterNode( parameter, clickPos );

				SelectNode( node );
				_window.OnSelected( parameter );
			} );
		}

		base.OnPopulateNodeMenuSpecialOptions( menu, clickPos, targetPlug, filter );
		var isSubgraph = Graph.IsSubgraph;

		if ( !targetPlug.IsValid() )
		{
			if ( !isSubgraph )
			{
				var newMaterialParameterMenu = menu.AddMenu( $"Create Parameter", "add" );

				foreach ( var classType in BlackboardParameter.GetRelevantParameters( AvailableParameters, false ).OrderBy( x =>
						x.Type.GetAttribute<OrderAttribute>().Value ) )
				{

					var baseName = !classType.Type.TargetType.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) ? "MaterialParameter" : "ShaderFeature";
					NewParameterMenuOption( newMaterialParameterMenu, baseName, classType, "Add Material Parameter" );
				}
			}
			else
			{
				var newSubgraphInputParameterMenu = menu.AddMenu( $"Create Subgraph Input", "add" );

				foreach ( var classType in BlackboardParameter.GetRelevantParameters( AvailableParameters, true ).Where( x =>
					x.Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) ).OrderBy( x =>
						x.Type.GetAttribute<OrderAttribute>().Value ) )
				{
					NewParameterMenuOption( newSubgraphInputParameterMenu, "SubgraphInput", classType, "Add Subgraph Input Parameter" );
				}

				var newSubgraphOutputParameterMenu = menu.AddMenu( $"Create Subgraph Output", "add" );

				foreach ( var classType in BlackboardParameter.GetRelevantParameters( AvailableParameters, true ).Where( x =>
					x.Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) ).OrderBy( x =>
						x.Type.GetAttribute<OrderAttribute>().Value ) )
				{
					NewParameterMenuOption( newSubgraphOutputParameterMenu, "SubgraphOutput", classType, "Add Subgraph Output Parameter" );
				}
			}

			//menu.AddOption( "Add Named Reroute Declaration", "route", () =>
			//{
			//	var nodeType = new NamedRerouteDeclarationNodeType( EditorTypeLibrary.GetType<NamedRerouteDeclarationNode>() );
			//
			//	CreateNewNode( nodeType, clickPos, targetPlug );
			//} );

			var namedRerouteDeclarations = Graph.Nodes.OfType<NamedRerouteDeclarationNode>();

			if ( namedRerouteDeclarations.Any() )
			{
				var optionsMenu = menu.AddMenu( "Named Reroutes", "route" );

				foreach ( var namedReroute in namedRerouteDeclarations )
				{
					optionsMenu.AddOption( namedReroute.Name, "route", () =>
					{
						CreateNewNamedReroute( namedReroute.Name, clickPos );
					} );
				}
			}
		}
		else if ( targetPlug is PlugIn )
		{
			var namedRerouteDeclarations = Graph.Nodes.OfType<NamedRerouteDeclarationNode>();

			if ( namedRerouteDeclarations.Any() )
			{
				var optionsMenu = menu.AddMenu( "Named Reroutes", "route" );

				foreach ( var namedRerouteDeclaration in namedRerouteDeclarations )
				{
					optionsMenu.AddOption( namedRerouteDeclaration.Name, "route", () =>
					{
						var nodeType = new NamedRerouteNodeType( EditorTypeLibrary.GetType<NamedRerouteNode>(), namedRerouteDeclaration.Name );

						CreateNewNode( nodeType, clickPos, targetPlug );
					} );
				}
			}
		}
		else if ( targetPlug is PlugOut )
		{
			menu.AddOption( "Add Named Reroute Declaration", "route", () =>
			{
				Dialog.AskString( ( string namedRerouteName ) =>
				{
					CreateNewNamedRerouteDeclaration( namedRerouteName, clickPos, targetPlug );
				},
				"Specify a Named Reroute name" );
			} );
		}

		menu.AddSeparator();
	}

	protected override void OnDoubleClickNodeSpecial( NodeUI node )
	{
		base.OnDoubleClickNodeSpecial( node );

		if ( node.Node is NamedRerouteNode namedRerouteNode )
		{
			var namedRerouteDeclaration = Graph.FindNamedRerouteDeclarationNode( namedRerouteNode.Name );

			if ( namedRerouteDeclaration != null )
			{
				CenterOn( namedRerouteDeclaration.Position );
				//SelectNode( namedRerouteDeclaration );
				//_window.SetPropertiesTarget( namedRerouteDeclaration );
			}
		}
	}

	private void CreateNewNamedReroute( string name, Vector2 position )
	{
		using var undoScope = UndoScope( "Add Named Reroute" );

		var nodeType = new NamedRerouteNodeType( EditorTypeLibrary.GetType<NamedRerouteNode>(), name );

		CreateNewNode( nodeType, position );
	}

	private void CreateNewNamedRerouteDeclaration( string name, Vector2 position, NodePlug targetPlug )
	{
		var nodeType = new NamedRerouteDeclarationNodeType( EditorTypeLibrary.GetType<NamedRerouteDeclarationNode>(), name );

		CreateNewNode( nodeType, position, targetPlug );
	}

	public override void ChildValuesChanged( Widget source )
	{
		BindSystem.Flush();

		base.ChildValuesChanged( source );

		BindSystem.Flush();
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


	protected override void OnOpenContextMenu( Menu menu, NodePlug targetPlug )
	{
		base.OnOpenContextMenu( menu, targetPlug );

		var selectedNodes = SelectedItems.OfType<NodeUI>().ToArray();
		// TODO : FIX CreateSubgraphFromSelection();
		//if ( selectedNodes.Length > 1 && !selectedNodes.Any( x => x.Node is BaseResult ) )
		//{
		//	menu.AddOption( "Create Custom Node...", "add_box", () =>
		//	{
		//		var fd = new FileDialog( null );
		//		fd.Title = "Create Shader Graph Function";
		//		fd.Directory = Project.Current.RootDirectory.FullName;
		//		fd.DefaultSuffix = $".{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension}";
		//		fd.SelectFile( $"untitled.{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension}" );
		//		fd.SetFindFile();
		//		fd.SetModeSave();
		//		fd.SetNameFilter( $"ShaderGraph Function (*.{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension})" );
		//		if ( !fd.Execute() ) return;
		//
		//		CreateSubgraphFromSelection( fd.SelectedFile );
		//	} );
		//}

		//if ( selectedNodes.Length > 1 && selectedNodes.All( x => x.Node is IConstantNode && x.Node is not IConstantMatrixNode ) )

		if ( selectedNodes.Length > 1 && selectedNodes.All( x => x.Node is IConstantNode && x.Node is not IConstantMatrixNode ) )
		{
			var optionName = $"Convert {selectedNodes.Count()} constants to {(Graph.IsSubgraph ? "Subgraph Inputs" : "Material Parameters")}";
			var convertOption = menu.AddOption( optionName, "swap_horiz", () =>
			{
				using var undoScope = UndoScope( optionName );

				var lastNode = selectedNodes.First().Node as BaseNodePlus;

				foreach ( var selectedNode in selectedNodes )
				{
					var baseNode = selectedNode.Node as BaseNodePlus;
					var constantNode = baseNode as IConstantNode;
					Dictionary<IPlugIn, IPlugOut> oldOutputConnections = new();

					if ( !Graph.IsSubgraph )
					{
						oldOutputConnections = GatherConnectedOutputs( baseNode );
					}

					Graph.RemoveNode( baseNode );

					var baseName = $"{(Graph.IsSubgraph ? "SubgraphInput" : "MaterialParameter")}";
					var id = 0;
					while ( Graph.HasParameterWithName( $"{baseName}{id}" ) )
					{
						id++;
					}

					lastNode = ConvertConstantNodeToParameter( constantNode, $"{baseName}{id}", selectedNode.Position, oldOutputConnections );
				}

				RebuildFromGraph();

				// Select the last node in the list.
				_window.OnSelected( lastNode );
				SelectNode( lastNode );
			} );
		}

		if ( selectedNodes.Length == 1 )
		{
			var item = selectedNodes.FirstOrDefault();

			if ( item is null )
				return;

			if ( item.Node is BaseNodePlus baseNode && baseNode is IConstantNode constantNode )
			{
				string nodeTypeTitle = constantNode.GetType() switch
				{
					Type t when t == typeof( BoolConstantNode ) => "Bool",
					Type t when t == typeof( IntConstantNode ) => "Int",
					Type t when t == typeof( FloatConstantNode ) => "Float",
					Type t when t == typeof( Float2ConstantNode ) => "Float2",
					Type t when t == typeof( Float3ConstantNode ) => "Float3",
					Type t when t == typeof( Float4ConstantNode ) => "Float4",
					Type t when t == typeof( Color ) => "Color",
					_ => ""
				};

				if ( !string.IsNullOrWhiteSpace( nodeTypeTitle ) )
				{
					var convertOption = menu.AddOption( $"Convert constant to {(Graph.IsSubgraph ? "Subgraph Input" : "Material Parameter")}", "swap_horiz", () =>
					{
						using var undoScope = UndoScope( $"Convert constant to {(Graph.IsSubgraph ? "Subgraph Input" : "Material Parameter")}" );

						Dictionary<IPlugIn, IPlugOut> oldOutputConnections = new();

						if ( !Graph.IsSubgraph )
						{
							oldOutputConnections = GatherConnectedOutputs( baseNode );
						}

						Graph.RemoveNode( baseNode );

						var baseName = $"{(Graph.IsSubgraph ? "SubgraphInput" : "MaterialParameter")}";
						var id = 0;
						while ( Graph.HasParameterWithName( $"{baseName}{id}" ) )
						{
							id++;
						}

						var parameterNode = ConvertConstantNodeToParameter( constantNode, $"{baseName}{id}", item.Node.Position, oldOutputConnections );

						RebuildFromGraph();

						_window.OnSelected( parameterNode );
						SelectNode( parameterNode );
					} );
				}
			}
		}
	}

	private Dictionary<IPlugIn, IPlugOut> GatherConnectedOutputs( BaseNodePlus targetNode )
	{
		var oldConnections = new Dictionary<IPlugIn, IPlugOut>();

		foreach ( var node in Graph.Nodes )
		{
			foreach ( var input in node.Inputs )
			{
				if ( input.ConnectedOutput is null )
					continue;

				if ( input.ConnectedOutput.Node == targetNode )
				{
					oldConnections[input] = input.ConnectedOutput;

					continue;
				}
			}
		}

		return oldConnections;
	}

	private BaseNodePlus ConvertConstantNodeToParameter( IConstantNode constantNode, string parameterName, Vector2 nodePosition, Dictionary<IPlugIn, IPlugOut> oldOutputConnections )
	{
		var parameterFullTypeName = "";

		if ( !Graph.IsSubgraph )
		{
			parameterFullTypeName = constantNode switch
			{
				BoolConstantNode => DisplayInfo.ForType( typeof( BoolParameter ) ).Fullname,
				IntConstantNode => DisplayInfo.ForType( typeof( IntParameter ) ).Fullname,
				FloatConstantNode => DisplayInfo.ForType( typeof( FloatParameter ) ).Fullname,
				Float2ConstantNode => DisplayInfo.ForType( typeof( Float2Parameter ) ).Fullname,
				Float3ConstantNode => DisplayInfo.ForType( typeof( Float3Parameter ) ).Fullname,
				Float4ConstantNode => DisplayInfo.ForType( typeof( Float4Parameter ) ).Fullname,
				ColorConstantNode => DisplayInfo.ForType( typeof( ColorParameter ) ).Fullname,
				_ => throw new NotImplementedException( $"Unknown type : {constantNode.GetType()}" ),
			};
		}
		else
		{
			parameterFullTypeName = constantNode switch
			{
				BoolConstantNode => DisplayInfo.ForType( typeof( BoolSubgraphInputParameter ) ).Fullname,
				IntConstantNode => DisplayInfo.ForType( typeof( IntSubgraphInputParameter ) ).Fullname,
				FloatConstantNode => DisplayInfo.ForType( typeof( FloatSubgraphInputParameter ) ).Fullname,
				Float2ConstantNode => DisplayInfo.ForType( typeof( Float2SubgraphInputParameter ) ).Fullname,
				Float3ConstantNode => DisplayInfo.ForType( typeof( Float3SubgraphInputParameter ) ).Fullname,
				Float4ConstantNode => DisplayInfo.ForType( typeof( Float4SubgraphInputParameter ) ).Fullname,
				ColorConstantNode => DisplayInfo.ForType( typeof( ColorSubgraphInputParameter ) ).Fullname,
				_ => throw new NotImplementedException( $"Unknown type : {constantNode.GetType()}" ),
			};
		}

		if ( AvailableParameters.TryGetValue( parameterFullTypeName, out var parameterType ) )
		{
			var parameter = CreateNewParameter( parameterType );
			parameter.Name = parameterName;
			parameter.SetValue( constantNode.GetValue() );

			var parameterNode = CreateNewParameterNode( parameter, nodePosition );

			if ( !Graph.IsSubgraph && oldOutputConnections.Any() )
			{
				// fixup any valid output connections
				foreach ( var node in Graph.Nodes )
				{
					foreach ( var input in node.Inputs )
					{
						if ( input.ConnectedOutput is null && oldOutputConnections.TryGetValue( input, out var correspondingOutput ) )
						{
							node.ConnectNode( input.Identifier, correspondingOutput.Identifier, parameterNode.Identifier );

							continue;
						}
					}
				}
			}

			if ( parameterNode != null )
			{
				return parameterNode;
			}
		}

		throw new Exception( $"Unable to convert constant node \"{constantNode.GetType()}\" to {(Graph.IsSubgraph ? "subgraph input" : "material")} parameter" );
	}

	private IBlackboardParameter CreateNewParameter( IBlackboardParameterType type )
	{
		return _blackboard.CreateNewParameter( type );
	}

	private T CreatenNewParameter<T>( ShaderGraphPlus graph ) where T : IBlackboardParameter
	{
		return (T)_blackboard.CreateNewParameter( FindParameterType( typeof( T ) ) );
	}

	private BaseNodePlus CreateNewParameterNode( IBlackboardParameter parameter, Vector2 position )
	{
		var node = BlackboardParameter.InitializeParameterNode( parameter );
		node.Graph = Graph;
		node.Position = position.SnapToGrid( GridSize );

		Graph?.AddNode( node );

		OnNodeCreated( node );

		var nodeUI = node.CreateUI( this );

		Add( nodeUI );

		_blackboard.RebuildFromGraph( true );

		return node;
	}

	/// <summary>
	/// TODO : FIXME!!!
	/// </summary>
	private void CreateSubgraphFromSelection( string filePath )
	{
		if ( string.IsNullOrWhiteSpace( filePath ) ) return;

		var fileName = Path.GetFileNameWithoutExtension( filePath );
		var subgraph = new ShaderGraphPlus();
		subgraph.Title = fileName.ToTitleCase();
		subgraph.IsSubgraph = true;

		// Grab all selected nodes
		Vector2 rightmostPos = new Vector2( -9999, 0 );
		var selectedNodes = SelectedItems.OfType<NodeUI>();
		var selectedParameters = new List<Guid>();
		Dictionary<IPlugIn, IPlugOut> oldConnections = new();
		foreach ( var node in selectedNodes )
		{
			if ( node.Node is not BaseNodePlus baseNode ) continue;

			foreach ( var input in baseNode.Inputs )
			{
				oldConnections[input] = input.ConnectedOutput;
			}

			subgraph.AddNode( baseNode );

			rightmostPos.y += baseNode.Position.y;
			if ( baseNode.Position.x > rightmostPos.x )
			{
				rightmostPos = rightmostPos.WithX( baseNode.Position.x );
			}
		}
		rightmostPos.y /= selectedNodes.Count();

		// Create Inputs/Constants
		var nodesToAdd = new List<BaseNodePlus>();
		var previousOutputs = new Dictionary<string, IPlugOut>();
		foreach ( var node in subgraph.Nodes )
		{
			foreach ( var input in node.Inputs )
			{
				var correspondingOutput = oldConnections[input];

				var correspondingNode = subgraph.Nodes.FirstOrDefault( x => x.Identifier == correspondingOutput?.Node?.Identifier );
				if ( correspondingOutput is not null && correspondingNode is null )
				{
					var inputName = $"{input.Identifier}_{correspondingOutput?.Node?.Identifier}";
					var existingParameterNode = nodesToAdd.OfType<IParameterNode>().FirstOrDefault( x => x.Name == inputName );
					if ( input.ConnectedOutput is not null )
					{
						previousOutputs[inputName] = input.ConnectedOutput;
					}
					if ( existingParameterNode is not null )
					{
						input.ConnectedOutput = (existingParameterNode as BaseNodePlus).Outputs.FirstOrDefault();
						continue;
					}

					BlackboardParameter parameter = null;

					if ( input.Type == typeof( bool ) )
					{
						parameter = CreatenNewParameter<BoolSubgraphInputParameter>( subgraph );
					}
					if ( input.Type == typeof( int ) )
					{
						parameter = CreatenNewParameter<IntSubgraphInputParameter>( subgraph );
					}
					if ( input.Type == typeof( float ) )
					{
						Log.Info( $"input.Type == typeof( float )" );
						parameter = CreatenNewParameter<FloatSubgraphInputParameter>( subgraph );
					}
					else if ( input.Type == typeof( Vector2 ) )
					{
						parameter = CreatenNewParameter<Float2SubgraphInputParameter>( subgraph );
					}
					else if ( input.Type == typeof( Vector3 ) )
					{
						parameter = CreatenNewParameter<Float3SubgraphInputParameter>( subgraph );
					}
					else if ( input.Type == typeof( Vector4 ) )
					{
						parameter = CreatenNewParameter<Float4SubgraphInputParameter>( subgraph );
					}
					else if ( input.Type == typeof( Color ) )
					{
						parameter = CreatenNewParameter<ColorSubgraphInputParameter>( subgraph );
					}

					if ( parameter != null )
					{
						if ( parameter is IBlackboardSubgraphInputParameter subgraphParameter )
						{
							subgraphParameter.PortOrder = nodesToAdd.Count;
						}

						subgraph.AddParameter( parameter );

						var subgraphInput = FindNodeType( typeof( SubgraphInput ) ).CreateNode( subgraph );
						subgraphInput.Position = node.Position - new Vector2( 240, 0 );
						if ( subgraphInput is SubgraphInput subgraphInputNode )
						{
							subgraphInputNode.ParameterIdentifier = parameter.Identifier;
							subgraphInputNode.OnFrame(); // Trigger update to create outputs
							input.ConnectedOutput = subgraphInputNode.Outputs.FirstOrDefault();
							nodesToAdd.Add( subgraphInputNode );
						}
					}
					else
					{
						var defaultparameter = CreatenNewParameter<FloatSubgraphInputParameter>( subgraph );
						defaultparameter.Name = inputName;
						defaultparameter.PortOrder = nodesToAdd.Count;

						subgraph.AddParameter( defaultparameter );

						// Default to float for unknown types
						var subgraphInput = FindNodeType( typeof( SubgraphInput ) ).CreateNode( subgraph );
						subgraphInput.Position = node.Position - new Vector2( 240, 0 );
						if ( subgraphInput is SubgraphInput subgraphInputNode )
						{
							subgraphInputNode.ParameterIdentifier = defaultparameter.Identifier;
							subgraphInputNode.OnFrame(); // Trigger update to create outputs
							input.ConnectedOutput = subgraphInputNode.Outputs.FirstOrDefault();
							nodesToAdd.Add( subgraphInputNode );
						}
					}
				}
			}
		}

		// Create Output/Result node
		//var frNode = FindNodeType( typeof( FunctionResult ) ).CreateNode( subgraph );
		//if ( frNode is FunctionResult resultNode )
		//{
		//	resultNode.Position = rightmostPos + new Vector2( 240, 0 );
		//	resultNode.FunctionOutputs = new();
		//	foreach ( var node in subgraph.Nodes )
		//	{
		//		foreach ( var output in node.Outputs )
		//		{
		//			var correspondingNode = Graph.Nodes.FirstOrDefault( x => !subgraph.Nodes.Contains( x ) && x.Inputs.Any( x => x.ConnectedOutput == output ) );
		//			if ( correspondingNode is null ) continue;
		//			var inputName = $"{output.Identifier}_{output.Node.Identifier}";
		//			resultNode.FunctionOutputs.Add( new FunctionOutput
		//			{
		//				Name = inputName,
		//				TypeName = output.Type.FullName
		//			} );
		//			resultNode.CreateInputs();
		//
		//			var input = resultNode.Inputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Name == inputName );
		//			input.ConnectedOutput = output;
		//			break;
		//		}
		//	}
		//	nodesToAdd.Add( resultNode );
		//}
		//
		//// Add all the newly created nodes
		//foreach ( var node in nodesToAdd )
		//{
		//	subgraph.AddNode( node );
		//}
		//
		//// Save the newly created sub-graph
		//System.IO.File.WriteAllText( filePath, subgraph.Serialize() );
		//var asset = AssetSystem.RegisterFile( filePath );
		//MainAssetBrowser.Instance?.Local.UpdateAssetList();
		//
		//PushUndo( "Create Subgraph from Selection" );
		//
		//// Create the new subgraph node centered on the selected nodes
		//Vector2 centerPos = Vector2.Zero;
		//foreach ( var node in selectedNodes )
		//{
		//	centerPos += node.Position;
		//}
		//centerPos /= selectedNodes.Count();
		//var subgraphNode = CreateNewNode( new SubgraphNodeType( asset.RelativePath, EditorTypeLibrary.GetType<SubgraphNode>() ) ).Node as SubgraphNode;
		//subgraphNode.Position = centerPos;
		//
		//// Get all the collected inputs/outputs and connect them to the new subgraph node
		//foreach ( var node in Graph.Nodes )
		//{
		//	if ( node == subgraphNode ) continue;
		//
		//	if ( selectedNodes.Any( x => x.Node == node ) )
		//	{
		//		foreach ( var input in node.Inputs )
		//		{
		//			var correspondingOutput = oldConnections[input];
		//			if ( correspondingOutput is not null && !selectedNodes.Any( x => x.Node == correspondingOutput.Node ) )
		//			{
		//				var inputName = $"{input.Identifier}_{correspondingOutput.Node.Identifier}";
		//				var newInput = subgraphNode.Inputs.FirstOrDefault( x => x.Identifier == inputName );
		//				if ( previousOutputs.TryGetValue( inputName, out var previousOutput ) )
		//				{
		//					newInput.ConnectedOutput = previousOutput;
		//				}
		//			}
		//		}
		//	}
		//	else
		//	{
		//		foreach ( var input in node.Inputs )
		//		{
		//			var correspondingOutput = input.ConnectedOutput;
		//			if ( correspondingOutput is not null && selectedNodes.Any( x => x.Node == correspondingOutput.Node ) )
		//			{
		//				var inputName = $"{correspondingOutput.Identifier}_{correspondingOutput.Node.Identifier}";
		//				var newOutput = subgraphNode.Outputs.FirstOrDefault( x => x.Identifier == inputName );
		//				if ( newOutput is not null )
		//				{
		//					input.ConnectedOutput = newOutput;
		//				}
		//			}
		//		}
		//	}
		//}
		//
		//PushRedo();
		//DeleteSelection();
		//
		// Delete all previously selected nodes
		//UpdateConnections( Graph.Nodes );
	}

	private void SelectionChanged()
	{
		var item = SelectedItems
			.OfType<NodeUI>()
			.OrderByDescending( n => n is CommentUI )
			.FirstOrDefault();

		if ( !item.IsValid() )
		{
			_window.OnSelected( null );
			return;
		}

		_window.OnSelected( (BaseNodePlus)item.Node );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		var item = SelectedItems
			.OfType<NodeUI>()
			.OrderByDescending( n => n is CommentUI )
			.FirstOrDefault();

		if ( !item.IsValid() )
		{
			_window.OnGraphViewClicked();
		}
	}

	protected override void OnNodeCreated( IGraphNode node )
	{
		if ( node is SubgraphNode subgraphNode )
		{
			subgraphNode.OnNodeCreated();
		}
	}

	protected override void OnNodePreviewPreRemove( NodeUI nodePreview )
	{
		var node = nodePreview.Node as BaseNodePlus;

		if ( node is IParameterNode parameterNode )
		{
			if ( _currentDragEventSource == DragEventSource.ImageFile )
			{
				Graph.RemoveParameter( parameterNode.ParameterIdentifier );

				_blackboard.RebuildFromGraph( true );
			}
		}

		_currentDragEventSource = DragEventSource.Invalid;
	}

	protected override void OnDragDropFinish()
	{
		_currentDragEventSource = DragEventSource.Invalid;
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		foreach ( var node in Items )
		{
			if ( node is NodeUI nodeUI && nodeUI.Node is BaseNodePlus baseNode )
			{
				baseNode.OnFrame();
			}
		}

		if ( _oldConnectionStyle != ConnectionStyle )
		{
			_oldConnectionStyle = ConnectionStyle;

			foreach ( var connection in Items.OfType<NodeEditorPlus.Connection>() )
			{
				connection.Layout();
			}
		}
	}
}
