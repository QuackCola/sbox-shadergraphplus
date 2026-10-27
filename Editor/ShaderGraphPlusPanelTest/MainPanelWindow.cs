using Editor;
using NodeEditorPlusTest;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using ShaderGraphPlus.Internal;

namespace ShaderGraphPlusPanelTest;

public sealed partial class ShaderGraphPlusPanelTest : PanelDockWindow
{
	private bool _dirty = false;
	private bool _autoCompile = true;
	private string _generatedCode;

	private Sandbox.UI.Menu _recentFilesMenu;
	private readonly List<string> _recentFiles = new();

	private readonly UndoStack _undoStack = new();

	private ShaderGraphPlusViewPanel _graphView;
	private PreviewPanel _preview;
	private PropertiesPanel _properties;
	private OutputPanel _output;
	private UndoHistoryPanel _undoHistory;
	private PalettePanel _palette;
	private GeneratedCodePanel _generatedCodeView;
	private BlackboardPanel _blackboard;
	private StatusBarPanel _statusBar;

	private ShaderGraphPlus.ShaderGraphPlus _graph;
	private ShaderTemplateResource _shaderTemplate;
	private Asset _asset;

	private readonly Dictionary<string, bool> _boolAttributes = new();
	private readonly Dictionary<string, int> _intAttributes = new();
	private readonly Dictionary<string, float> _floatAttributes = new();
	private readonly Dictionary<string, Vector2> _float2Attributes = new();
	private readonly Dictionary<string, Vector3> _float3Attributes = new();
	private readonly Dictionary<string, Color> _float4Attributes = new();
	private readonly Dictionary<string, SamplerState> _samplerStateAttributes = new();
	private readonly Dictionary<string, Texture> _textureAttributes = new();
	private readonly Dictionary<string, int> _dynamicComboIntAttributes = new();
	private readonly Dictionary<string, int> _shaderFeatures = new();

	public bool IsSubgraph => false;
	public string FileType => ShaderGraphPlusGlobals.AssetTypeName;
	public string FileExtension => ShaderGraphPlusGlobals.AssetTypeExtension;

	[Menu( "Editor", "Shader Graph Plus/NewEditorTest", "schema" )]
	public static void OpenNewEditorTest()
	{
		// Find the open editor instead of a static - statics don't survive hotload
		foreach ( var existing in PanelWindow.All.Where( x => x.Root?.Children.OfType<ShaderGraphPlusPanelTest>().Any() == true ).ToArray() )
		{
			existing.Dispose();
		}

#pragma warning disable CA2000 // Dispose objects before losing scope
		// The window owns itself until closed; Quit or the next open disposes it
		var window = new PanelWindow( "Shader Graph Plus", new Vector2( 1700, 1050 ), null, true ) { MinSize = new Vector2( 800, 480 ) };
#pragma warning restore CA2000 // Dispose objects before losing scope
		window.Root.AddChild( new ShaderGraphPlusPanelTest() );
	}

	public ShaderGraphPlusPanelTest() : base()
	{
		StyleSheet.AddInline( ShaderGraphPlusStyleSheets.ShaderGraphPlusScss, "shadergraphplus_style" );

		Style.FlexDirection = FlexDirection.Column;

		_graph = new ShaderGraphPlus.ShaderGraphPlus()
		{
			Title = "untitled",
			IsSubgraph = IsSubgraph
		};

		_recentFiles = Editor.FileSystem.Temporary.ReadJsonOrDefault( "shadergraphplus_recentfiles.json", _recentFiles )
			.Where( x => System.IO.File.Exists( x ) ).ToList();

		BuildMenuBar();
		CreateToolBar();
		CreateUI();

		CreateNew();

		EditorEvent.Run( ShaderGraphPlusGlobals.EditorEvents.ShaderGraphPlusEditorCreated );
	}

	private void CreateToolBar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "main-toolbar" );

		toolbar.AddButton( "New", Texture.Load( "/tools/images/common/new.png" ), New );
		toolbar.AddButton( "Open", Texture.Load( "/tools/images/common/open.png" ), Open );
		toolbar.AddButton( "Save", Texture.Load( "/tools/images/common/save.png" ), Save );

		toolbar.AddSeparator();

		toolbar.AddButton( "Compile", Texture.Load( "/tools/images/common/reload.png" ), Compile );

		toolbar.AddButton( "Auto Compile",
			Texture.Load( "/tools/images/model_editor/auto_recompile.png" ),
			Texture.Load( "/tools/images/model_editor/supress_auto_recompile.png" ),
		() =>
		{
			_autoCompile = !_autoCompile;

			if ( _autoCompile )
			{
				SGPLogger.Info( "Enabled AutoCompile" );
				Compile();
			}
			else
			{
				SGPLogger.Info( "Disabled AutoCompile" );
			}
		} );

		toolbar.AddSpacer();
	}

	private void BuildMenuBar()
	{
		var bar = Add.Panel( "titlebar window-drag" );

		bar.Add.Label( "s&", "brand" );

		// Menus live in the title bar, same as the editor's
		var menus = bar.AddChild( new Sandbox.UI.MenuBar() );
		menus.AddClass( "window-nodrag" );

		var file = menus.AddMenu( "File" );

		Option( file, "New", "note_add", "Ctrl+N", New );
		Option( file, "Open", "file_open", "Ctrl+O", Open );
		Option( file, "Save", "save", "Ctrl+S", Save );
		Option( file, "Save As...", "save_as", "Ctrl+Shift+S", SaveAs );

		file.AddSeparator();

		Option( file, "Reload Shader Template", "common/reload.png", "", () =>
		{
			LoadShaderTypeInfo();
			SetDirty();
		} );

		file.AddSeparator();

		_recentFilesMenu = file.AddMenu( "Recent Files" );

		file.AddSeparator();

		Option( file, "Quit", "logout", "Alt+F4", Quit );

		var edit = menus.AddMenu( "Edit" );

		Option( edit, "Undo", "undo", "Ctrl+Z", Undo );
		Option( edit, "Redo", "redo", "Ctrl+Y", Redo );

		edit.AddSeparator();

		Option( edit, "Cut", "content_cut", "Ctrl+X", CutSelection );
		Option( edit, "Copy", "content_copy", "Ctrl+C", CopySelection );
		Option( edit, "Paste", "content_paste", "Ctrl+V", PasteSelection );
		Option( edit, "Select All", "select_all", "Ctrl+A", SelectAll );

		var view = menus.AddMenu( "View" );

		Option( view, "Graph", "account_tree", action: () => ShowDock( "graph" ) );
		Option( view, "Palette", "palette", action: () => ShowDock( "palette" ) );
		Option( view, "Blackboard", "format_list_bulleted", action: () => ShowDock( "blackboard" ) );
		Option( view, "Preview", "photo", action: () => ShowDock( "preview" ) );
		Option( view, "Properties", "tune", action: () => ShowDock( "properties" ) );
		Option( view, "Console", "text_snippet", action: () => ShowDock( "console" ) );

		view.AddSeparator();

		Option( view, "Save Layout", "playlist_add", action: () => _savedLayout = DockWindows.State );
		Option( view, "Restore Layout", "playlist_add_check", action: () => DockWindows.RestoreState( _savedLayout ) );
		Option( view, "Reset Layout", "restart_alt", action: () => DockWindows.RestoreState( _defaultLayout ) );

		view.AddSeparator();

		Option( view, "Full Screen", "F11", "", () => { Window?.ToggleMaximized(); UpdateMaximizeIcon(); } );

		RefreshRecentFiles();

		bar.Add.Panel( "grow" );

		// Centred over the bar, so the menus on the left don't push it off centre
		var titleWrap = bar.Add.Panel( "titlewrap" );
		WindowTitle = titleWrap.Add.Label( "", "title" );

		WindowButton( bar, "remove", null, () => Window?.Minimize() );
		_maximizeIcon = WindowButton( bar, "crop_square", null, () =>
		{
			Window?.ToggleMaximized();
			UpdateMaximizeIcon();
		} );
		WindowButton( bar, "close", "close", Quit );

		Add.Panel( "accentline" );

		UpdateTitle();
	}

	protected override void CreateUI()
	{
		base.CreateUI();

		ConsolePanel.Hook();

		_blackboard = new BlackboardPanel( IsSubgraph );
		_blackboard.Graph = _graph;
		_blackboard.OnDirty += ( evaluate ) => SetDirty( evaluate );
		_blackboard.OnPushUndo = PushUndo;
		_blackboard.OnPushRedo = PushRedo;
		_blackboard.OnParameterNodesDeleted += () =>
		{
			_graphView.RebuildFromGraph();
		};
		// After the graph view's handler: it clears the graph selection, which shows the graph,
		// then the picked parameter takes over. Unpicking leaves whatever the graph selection shows.
		_blackboard.OnSelected += parameter =>
		{
			if ( parameter is not null )
				_properties.Target = parameter;
		};

		_graphView = new ShaderGraphPlusViewPanel( this, _blackboard );
		_graphView.Graph = _graph;
		_graphView.OnPushUndo = PushUndo;
		_graphView.OnPushRedo = PushRedo;
		_graphView.OnNodeValueChanged += () => SetDirty();

		var nodeTypes = EditorTypeLibrary.GetTypes<ShaderNodePlus>()
			.Where( x => !x.IsAbstract && !x.HasAttribute<HideAttribute>() ).OrderBy( x => x.Name );

		var blackboardParameterTypes = EditorTypeLibrary.GetTypes<BlackboardParameter>()
			.Where( x => !x.IsAbstract ).OrderBy( x => x.Name );

		foreach ( var type in nodeTypes )
		{
			_graphView.AddNodeType( type );
		}

		foreach ( var type in blackboardParameterTypes )
		{
			_graphView.AddParameterType( type );
			_blackboard.AddParameterType( type );
		}

		var subgraphs = AssetSystem.All.Where( x => x.Path.EndsWith( $".{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension}", StringComparison.OrdinalIgnoreCase ) );
		foreach ( var subgraph in subgraphs )
		{
			// Skip any _c compiled subgraph files.
			if ( subgraph.CanRecompile )
			{
				_graphView.AddNodeType( subgraph.Path );
			}
		}

		_preview = new PreviewPanel();
		_preview.OnModelChanged = ( model ) => _graph.Model = model?.Name;

		_properties = new PropertiesPanel();
		_properties.Target = _graph;
		_properties.PropertyUpdated += OnPropertyUpdated;

		_undoHistory = new UndoHistoryPanel();

		_output = new OutputPanel();
		_palette = new PalettePanel( IsSubgraph );
		_generatedCodeView = new GeneratedCodePanel();

		_statusBar = AddChild( new StatusBarPanel() );

		SetAllAttributes();

		DockHost.Register( "graph", "Graph", _graphView, canClose: false, icon: "account_tree" );

		DockHost.Register( "preview", "Preview", _preview, icon: "photo" );
		DockHost.Register( "properties", "Properties", _properties, icon: "tune" );

		DockHost.Register( "output", "Output", _output, icon: "notes" );
		DockHost.Register( "console", "Console", () => new ConsolePanel(), icon: "text_snippet" );
		DockHost.Register( "undohistory", "Undo History", _undoHistory, icon: "history" );
		DockHost.Register( "palette", "Palette", _palette, icon: "palette" );
		DockHost.Register( "generatedcode", "Generated Code", _generatedCodeView, icon: "text_snippet" );

		DockHost.Register( "blackboard", "Blackboard", _blackboard, icon: "format_list_bulleted" );

		DockHost.Dock( "graph" );

		DockHost.Dock( "preview", position: DockPosition.Left, fraction: 0.26f );
		DockHost.Dock( "properties", "preview", DockPosition.Bottom, 0.55f );

		DockHost.Dock( "output", "graph", position: DockPosition.Bottom, fraction: 0.26f );
		DockHost.Dock( "console", "output", position: DockPosition.Center, fraction: 0.26f );
		DockHost.Dock( "undohistory", "output", position: DockPosition.Center, fraction: 0.26f );
		DockHost.Dock( "palette", "output", position: DockPosition.Center, fraction: 0.26f );
		DockHost.Dock( "generatedcode", "output", position: DockPosition.Center, fraction: 0.26f );

		DockHost.Dock( "blackboard", "output", position: DockPosition.Right, fraction: 0.36f );

		DockHost.Activate( "output" );

		_defaultLayout = DockWindows.State;
		_savedLayout = _defaultLayout;

		Compile();
	}

	private void SetAllAttributes()
	{

		foreach ( var value in _boolAttributes )
		{
			_preview?.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _floatAttributes )
		{
			_preview?.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _float2Attributes )
		{
			_preview?.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _float3Attributes )
		{
			_preview?.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _float4Attributes )
		{
			_preview?.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _samplerStateAttributes )
		{
			_preview?.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _textureAttributes )
		{
			_preview?.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _dynamicComboIntAttributes )
		{
			_preview?.SetDynamicCombo( value.Key, value.Value );
		}

		foreach ( var value in _shaderFeatures )
		{
			_preview?.SetFeature( value.Key, value.Value );
		}
	}

	private void OnPropertyUpdated( SerializedProperty serializedProperty )
	{
		_preview.EnablePostProcessing = _graph.Domain == ShaderDomain.PostProcess;

		if ( _properties.Target is BaseNodePlus node )
		{
			_graphView.UpdateNode( node );
		}

		// Reload shader template
		if ( _properties.Target is ShaderGraphPlus.ShaderGraphPlus && serializedProperty.IsPropertyName( nameof( ShaderGraphPlus.ShaderGraphPlus.ShaderType ) ) )
		{
			LoadShaderTypeInfo();
		}

		var shouldEvaluate = _properties.Target is not CommentNode;
		SetDirty( shouldEvaluate );
	}

	public void OnNodeSelected( BaseNodePlus selectedNode )
	{
		if ( selectedNode == null )
		{
			_properties.Target = _graph;
			return;
		}

		// A parameter node has nothing of its own to edit - show its parameter, like the Qt editor
		if ( selectedNode is IBlackboardNode { ParameterIdentifier: var id } && id != default && _graph.TryFindParameter( id, out var parameter ) )
		{
			_properties.Target = parameter;
			return;
		}

		_properties.Target = selectedNode;
	}

	private void PromptSave( Action action )
	{
		if ( !_dirty )
		{
			action?.Invoke();
			return;
		}

		var confirm = new PopupWindow(
			"Save Current Graph", "The open graph has unsaved changes. Would you like to save now?", "Cancel",
			new Dictionary<string, Action>()
			{
				{ "No", () => action?.Invoke() },
				{ "Yes", () => { if ( SaveInternal( false ) ) action?.Invoke(); } }
			}
		);

		confirm.Show();
	}

	public void New()
	{
		PromptSave( CreateNew );
	}

	public void CreateNew()
	{
		_asset = null;
		_graph = new()
		{
			Title = "untitled"
		};
		_dirty = false;

		// Must be done before setting _graphView.Graph and _blackboardView.Graph
		// or else some nodes, namely the 'Result' node may break valid inputs upon it getting initialized via GraphView.RebuildFromGraph().
		LoadShaderTypeInfo();

		_graphView.Graph = _graph;
		_blackboard.Graph = _graph;

		UpdateTitle();

		_preview.Model = null;
		_preview.Tint = Color.White;
		_undoStack.Clear();
		_generatedCode = "";
		_generatedCodeView.Code = "";

		// TODO
		//_output.ClearErrors();
		//_output.ClearWarnings();

		if ( !IsSubgraph )
		{
			var result = _graphView.CreateNewNode( _graphView.FindNodeType( typeof( Result ) ), 0 );
		}

		ClearAttributes();
		RestoreShader();
	}

	private void ClearAttributes()
	{
		_boolAttributes.Clear();
		_intAttributes.Clear();
		_floatAttributes.Clear();
		_float2Attributes.Clear();
		_float3Attributes.Clear();
		_float4Attributes.Clear();
		_textureAttributes.Clear();
		_samplerStateAttributes.Clear();
		_dynamicComboIntAttributes.Clear();
		_shaderFeatures.Clear();

		_preview?.ClearAllAttributes();
	}

	public void Open()
	{
		var fd = new FileDialog( null )
		{
			Title = $"Open {FileType}",
			DefaultSuffix = $".{FileExtension}"
		};

		fd.SetNameFilter( $"{FileType} ( *.{ShaderGraphPlus.ShaderGraphPlusGlobals.AssetTypeExtension};*.{ShaderGraphPlus.ShaderGraphPlusGlobals.SubgraphAssetTypeExtension})" );

		if ( !fd.Execute() )
			return;

		PromptSave( () => Open( fd.SelectedFile ) );
	}

	public void Open( string path, bool addToPath = true )
	{
		var asset = AssetSystem.FindByPath( path );

		if ( asset == null )
			return;

		if ( asset == _asset )
		{
			Focus();
			return;
		}

		var graph = JsonSerializer.Deserialize<ShaderGraphPlus.ShaderGraphPlus>( System.IO.File.ReadAllText( path ), ShaderGraphPlus.ShaderGraphPlus.SerializerOptions() );
		graph.Path = asset.RelativePath;
		//graph.IsSubgraph = IsSubgraph;

		if ( !graph.IsSubgraph )
		{
			graph.Title = asset.Name;
		}

		_preview.Model = string.IsNullOrWhiteSpace( graph.Model ) ? null : Model.Load( graph.Model );
		_preview.LoadSettings( _graph.PreviewSettings );

		_asset = asset;
		_graph = graph;
		_dirty = false;

		// Must be done before setting _graphView.Graph and _blackboardView.Graph
		// or else some nodes, namely the 'Result' node may break valid inputs upon it getting initialized via GraphView.RebuildFromGraph().
		LoadShaderTypeInfo();

		_graphView.Graph = _graph;
		_blackboard.Graph = _graph;

		UpdateTitle();

		_undoStack.Clear();
		_generatedCode = "";
		_generatedCodeView.Code = "";
		//Selection.Set( _graph );

		//_output.ClearErrors();
		//_output.ClearWarnings();

		ClearAttributes();

		AddToRecentFiles( path );

		// Just generate it twice so that some attributes actually get set.
		GeneratePreviewCode();
		GeneratePreviewCode();

		var generatedShaderPath = System.IO.Path.ChangeExtension( _asset.AbsolutePath, ".shader" );

		if ( System.IO.Path.Exists( generatedShaderPath ) )
		{
			_generatedCodeView.Code = System.IO.File.ReadAllText( generatedShaderPath );
		}
		else
		{
			DockHost.Activate( "output" );
		}
	}

	private void LoadShaderTypeInfo()
	{
		if ( _graph is null ) return;

		if ( _graph.HasTemplate )
		{
			var templateAsset = AssetSystem.FindByPath( _graph.ShaderType );

			_shaderTemplate = new ShaderTemplateResource();

			if ( _shaderTemplate.Deserialize( System.IO.File.ReadAllText( templateAsset.AbsolutePath ), System.IO.Path.GetFileName( templateAsset.AbsolutePath ) ) )
			{
				_graph.ShaderTypeInfo = _shaderTemplate;
				_graph.ValidateShaderTypeInfo();
			}
			else
			{
				_shaderTemplate = null;
				_graph.ShaderTypeInfo = new ShaderGraphPlus.ShaderTemplate.ShaderTypeInfo();
			}
		}
		else
		{
			_shaderTemplate = null;

			_graph.ShaderTypeInfo = _graph.ShaderType switch
			{
				"Surface" => ShaderTemplateSurface.ShaderTypeInfo,
				"Sky" => ShaderTemplateSky.ShaderTypeInfo,
				"PostProcess" => ShaderTemplatePostProcess.ShaderTypeInfo,
				_ => ShaderTemplateSurface.ShaderTypeInfo,
			};

			_graph.ValidateShaderTypeInfo();
		}
	}

	public void SaveAs()
	{
		SaveInternal( true );
	}

	public void Save()
	{
		SaveInternal( false );
	}

	private bool SaveInternal( bool saveAs )
	{
		SGPLogger.Info( $"TODO : Save Graph{(saveAs ? " As" : "")}" );

		//AddToRecentFiles( savePath );
		//EditorEvent.Run( ShaderGraphPlusGlobals.EditorEvents.SubgraphUpdate, _asset.RelativePath );

		return true;
	}

	private void Quit()
	{
		if ( !_dirty )
		{
			Close();
			return;
		}

		var confirm = new PopupWindow(
			"Save Current Graph", "The open graph has unsaved changes. Would you like to save now?", "Cancel",
			new Dictionary<string, Action>()
			{
				{ "No", () => { _dirty = false; Close(); } },
				{ "Yes", () => { if ( SaveInternal( false ) ) Close(); } }
			}
		);

		confirm.Show();

		return;
	}

	private void PushUndo( string name )
	{
		SGPLogger.Info( $"Push Undo ({name})" );
		_undoStack.PushUndo( name, _graph.UndoStackSerialize() );
	}

	private void PushRedo()
	{
		SGPLogger.Info( "Push Redo" );
		_undoStack.PushRedo( _graph.UndoStackSerialize() );
		SetDirty();
	}

	private void Undo()
	{
		if ( _undoStack.Undo() is not UndoOp op ) return;

		SGPLogger.Info( $"Undo ({op.name})" );
		RestoreGraph( op.undoBuffer );
	}

	private void Redo()
	{
		if ( _undoStack.Redo() is not UndoOp op ) return;

		SGPLogger.Info( $"Redo ({op.name})" );
		RestoreGraph( op.redoBuffer );
	}

	/// <summary>
	/// Put the graph back to a snapshot from <see cref="ShaderGraphPlus.ShaderGraphPlus.UndoStackSerialize"/>,
	/// Nodes and parameters are new objects afterwards, so both views rebuild
	/// and the properties stop showing what was replaced.
	/// </summary>
	private void RestoreGraph( string buffer )
	{
		_graph.ClearNodes();
		_graph.ClearGroupData();
		_graph.ClearParameters();

		_graph.DeserializeNodes( buffer, true );
		_graph.DeserializeGroupData( buffer );
		_graph.DeserializeParameters( buffer );

		_graphView.RebuildFromGraph();
		_blackboard.RebuildFromGraph();
		_properties.Target = _graph;

		SetDirty();
	}

	/// <summary>
	/// Ctrl+Z undoes, Ctrl+Y and Ctrl+Shift+Z redo - unless a text field has the keys, which undoes its own typing.
	/// </summary>
	protected internal override bool HandleShortcut( ButtonEvent e, Panel scope )
	{
		if ( !e.HasCtrl || scope.Descendants.OfType<Sandbox.UI.TextEntry>().Any( x => x.HasFocus ) )
			return false;

		switch ( e.Button )
		{
			case "z" when e.HasShift:
			case "y":
				Redo();
				return true;
			case "z":
				Undo();
				return true;
		}

		return false;
	}

	private void CutSelection()
	{
		_graphView.CutSelection();
	}

	private void CopySelection()
	{
		_graphView.CopySelection();
	}

	private void PasteSelection()
	{
		_graphView.Paste( EditorUtility.Clipboard.Paste() );
	}

	private void SelectAll()
	{
		SGPLogger.Info( $"TODO : Select All" );
	}

	private void OnAttribute( string name, object value )
	{
		if ( value == null )
			return;


		switch ( value )
		{
			case Color v:
				if ( _float4Attributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, v );
				}
				break;
			case Vector4 v:
				if ( _float4Attributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, (Color)v );
				}
				break;
			case Vector3 v:
				if ( _float3Attributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, v );
				}
				break;
			case Vector2 v:
				if ( _float2Attributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, v );
				}
				break;
			case float v:
				if ( _floatAttributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, v );
				}
				break;
			case int v:
				if ( _intAttributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, v );
				}
				break;
			case bool v:
				if ( _boolAttributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, v );
				}
				break;
			case Texture v:
				if ( _textureAttributes.TryAdd( name, v ) )
				{
					_preview?.SetAttribute( name, v );
				}
				break;
			case Sampler v:
				if ( _samplerStateAttributes.TryAdd( name, (SamplerState)v ) )
				{
					_preview?.SetAttribute( name, (SamplerState)v );
				}
				break;
			case ShaderFeatureWrapper v:
				if ( _shaderFeatures.TryAdd( v.FeatureName, v.Value ) )
				{
					_preview?.SetFeature( v.FeatureName, v.Value );
				}
				break;
			case DynamicComboWrapper v:
				if ( _dynamicComboIntAttributes.TryAdd( v.ComboName, v.Value ) )
				{
					_preview?.SetDynamicCombo( v.ComboName, v.Value );
				}
				break;
			default:
				throw new InvalidOperationException( $"Unsupported attribute type: \"{value.GetType()}\"" );
		}
	}

	private void SetDirty( bool evaluate = true )
	{
		_dirty = true;
		_blackboard.OnGraphDirty();

		UpdateTitle();

		if ( evaluate )
			GeneratePreviewCode();
	}

	private void RestoreShader()
	{
		if ( !_preview.IsValid() )
			return;

		_preview.Material = ShaderGraphPlusGlobals.Preview.DefaultMaterial;
	}

	private void UpdateTitle()
	{
		WindowTitle.Text = $"Shader Graph Plus - {_asset?.Path ?? "untitled"}{(_dirty ? "*" : "")}";
	}

	public void ShowStatusBarMessage( string text, float seconds = 5f )
	{
		if ( _statusBar == null )
			return;

		_statusBar.ShowMessage( text, seconds );
	}

	public void ClearStatusBarMessage()
	{
		if ( _statusBar == null )
			return;

		_statusBar.ClearMessage();
	}

	private void StatusTip( Panel panel, string text, float seconds = 5f )
	{
		panel.AddEventListener( "onmouseover", () => ShowStatusBarMessage( text, seconds ) );
		panel.AddEventListener( "onmouseout", () => ClearStatusBarMessage() );
	}

	private void RefreshRecentFiles()
	{
		_recentFilesMenu.Enabled = _recentFiles.Count > 0;

		_recentFilesMenu.Clear();

		var clearRecentFiles = _recentFilesMenu.AddOption( "Clear recent files", null, () =>
		{
			_recentFilesMenu.ParentMenu?.Close();
			ClearRecentFiles();
		} );
		StatusTip( clearRecentFiles, "Clear recent files", -1f );

		_recentFilesMenu.AddSeparator();

		const int maxFilesToDisplay = 10;
		int fileCount = 0;

		for ( int i = _recentFiles.Count - 1; i >= 0; i-- )
		{
			if ( fileCount >= maxFilesToDisplay )
				break;

			var filePath = _recentFiles[i];

			var option = _recentFilesMenu.AddOption( $"{++fileCount} - {filePath}", null, () =>
			{
				_recentFilesMenu.ParentMenu?.Close();
				PromptSave( () => Open( filePath ) );
			} );

			StatusTip( option, $"Open {filePath}", -1f );
		}
	}

	private void ClearRecentFiles()
	{
		if ( _recentFiles.Count == 0 )
			return;

		_recentFiles.Clear();

		RefreshRecentFiles();

		SaveRecentFiles();
	}

	private void AddToRecentFiles( string filePath )
	{
		filePath = filePath.ToLower();

		// If file is already recent, remove it so it'll become the most recent
		if ( _recentFiles.Contains( filePath ) )
		{
			_recentFiles.RemoveAll( x => x == filePath );
		}

		_recentFiles.Add( filePath );

		RefreshRecentFiles();
		SaveRecentFiles();
	}

	private void SaveRecentFiles()
	{
		Editor.FileSystem.Temporary.WriteJson( "shadergraphplus_recentfiles.json", _recentFiles );
	}

	// For Development only
	[EditorEvent.Hotload]
	private void OnHotload()
	{
		// A closed window can still be registered - one closed by the old code, before this hotload
		if ( !IsValid || IsDeleting ) return;

		StyleSheet.AddInline( ShaderGraphPlusStyleSheets.ShaderGraphPlusScss, "shadergraphplus_style" );

		// Rebuilding adds a new title bar, toolbar and dock host, so drop the old ones and their floating windows first
		DockWindows?.Dispose();
		DeleteChildren( true );

		BuildMenuBar();
		CreateToolBar();
		CreateUI();

		Compile();

		_preview.LoadSettings( _graph.PreviewSettings );

		Log.Info( "ShaderGraphPlus hotloaded!" );
	}

	public override void Tick()
	{
		base.Tick();

		CheckForChanges();
	}

	private void CheckForChanges()
	{
		bool wasDirty = false;
		foreach ( var node in _graph.Nodes )
		{
			if ( node is ShaderNodePlus shaderNode && shaderNode.IsDirty )
			{
				shaderNode.IsDirty = false;
				wasDirty = true;
			}
		}

		if ( wasDirty )
		{
			_graphView.NodeValueChanged();
		}
	}

	[Event( ShaderGraphPlusGlobals.EditorEvents.SubgraphUpdate )]
	internal void OnSubgraphUpdate( string updatedPath )
	{
		foreach ( var node in _graph.Nodes )
		{
			if ( node is SubgraphNode subgraphNode )
			{
				subgraphNode.Subgraph = null;
				subgraphNode.OnNodeCreated();
			}
		}

		GeneratePreviewCode();
	}

	[Event( ShaderGraphPlusGlobals.EditorEvents.ShaderTemplateUpdate )]
	internal void OnShaderTemplateUpdate( string templatePath )
	{
		if ( _graph.HasTemplate && _graph.ShaderType == templatePath )
		{
			LoadShaderTypeInfo();
			SetDirty();
		}
	}
}
