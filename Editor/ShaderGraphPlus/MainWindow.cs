using Editor;
using Sandbox.Rendering;
using ShaderGraphPlus.Internal;
using ShaderGraphPlus.Nodes;
using System.Text;

namespace ShaderGraphPlus;

[EditorForAssetType( ShaderGraphPlusGlobals.SubgraphAssetTypeExtension )]
public class MainWindowShaderFunc : MainWindow, IAssetEditor
{
	public override bool IsSubgraph => true;
	public override string FileType => $"{ShaderGraphPlusGlobals.AssetTypeName} Sub-Graph";
	public override string FileExtension => ShaderGraphPlusGlobals.SubgraphAssetTypeExtension;

	void IAssetEditor.SelectMember( string memberName )
	{
		throw new NotImplementedException();
	}
}

[EditorForAssetType( ShaderGraphPlusGlobals.AssetTypeExtension )]
[EditorApp( ShaderGraphPlusGlobals.AssetTypeName, "gradient", "edit shaders" )]
public class MainWindowShader : MainWindow, IAssetEditor
{
	public override bool IsSubgraph => false;
	public override string FileType => ShaderGraphPlusGlobals.AssetTypeName;
	public override string FileExtension => ShaderGraphPlusGlobals.AssetTypeExtension;

	void IAssetEditor.SelectMember( string memberName )
	{
		throw new NotImplementedException();
	}
}

public class MainWindow : DockWindow
{
	public virtual bool IsSubgraph => false;
	public virtual string FileType => ShaderGraphPlusGlobals.AssetTypeName;
	public virtual string FileExtension => ShaderGraphPlusGlobals.AssetTypeExtension;

	private ShaderGraphPlus _graph;
	private ShaderGraphPlusView _graphView;
	private ShaderGraphPlusBlackboardView _blackboardView;
	private Asset _asset;

	private ShaderTemplateResource _shaderTemplate;

	public string AssetPath => _asset?.Path;

	private Widget _graphCanvas;
	private Widget _blackboardCanvas;
	private Properties _properties;
	private PreviewPanel _preview;
	private Output _output;
	private UndoHistory _undoHistory;
	private PaletteWidget _palette;
	private TextView _generatedCodeTextView;

	private readonly UndoStack _undoStack = new();

	private Option _undoOption;
	private Option _redoOption;

	private Option _undoMenuOption;
	private Option _redoMenuOption;

	private Option _autoCompileOption;


	public UndoStack UndoStack => _undoStack;

	private bool _dirty = false;
	private bool _autoCompile = true;

	private string _generatedCode;

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

	//private readonly List<BaseNodePlus> _compiledNodes = new();

	private bool _isCompiling = false;
	private bool _isPendingCompile = false;
	private RealTimeSince _timeSinceCompile;

	private Menu _recentFilesMenu;
	private readonly List<string> _recentFiles = new();
	private Option _fileHistoryBack;
	private Option _fileHistoryForward;
	private Option _fileHistoryHome;

	private List<string> _fileHistory = new();
	private int _fileHistoryPosition = 0;

	public bool CanOpenMultipleAssets => true;
	public bool EnableNodePreview => _preview.Preview.EnableNodePreview;

	private ProjectCreator ProjectCreator { get; set; }

	private Dictionary<string, ShaderFeatureBase> ShaderFeatures = new();
	private List<GraphCompiler.GraphIssue> BlackboardIssues { get; set; } = new();

	public SelectionSystem Selection { get; set; }

	public MainWindow()
	{
		DeleteOnClose = true;

		Title = FileType;
		Size = new Vector2( 1700, 1050 );

		Selection = new();

		_graph = new();
		_graph.IsSubgraph = IsSubgraph;

		CreateToolBar();

		_recentFiles = Editor.FileSystem.Temporary.ReadJsonOrDefault( "shadergraphplus_recentfiles.json", _recentFiles )
			.Where( x => System.IO.File.Exists( x ) ).ToList();

		CreateUI();
		Show();
		StateCookie = "ShaderGraphPlus";
		CreateNew();

		EditorEvent.Run( "shadergraphplus.created" );

		OpenProjectCreationDialog();
	}

	private void OpenProjectCreationDialog()
	{
		ProjectCreator = new ProjectCreator
		{
			DeleteOnClose = true
		};

		var initialPath = $"{Project.Current.GetAssetsPath().Replace( "\\", "/" )}/Shaders";
		if ( !Directory.Exists( initialPath ) )
		{
			Directory.CreateDirectory( initialPath );
		}

		ProjectCreator.FolderEditPath = initialPath;
		ProjectCreator.Show();
		ProjectCreator.OnProjectCreated += OpenProject;
	}

	public void AssetOpen( Asset asset )
	{
		if ( asset == null || string.IsNullOrWhiteSpace( asset.AbsolutePath ) )
			return;

		// We dont need the project creator when opening an existing asset. So lets forceably close it.
		ProjectCreator?.Close();
		ProjectCreator = null;


		Open( asset.AbsolutePath );
	}

	private void RestoreShader()
	{
		if ( !_preview.IsValid() )
			return;

		_preview.Material = Material.Load( "materials/core/shader_editor.vmat" );
	}


	public void OnDeselected( object oldSelection )
	{
		//if ( oldSelection is BlackboardParameter || oldSelection is CategoryData )
		//{
		//	_blackboardView.ClearSelection();
		//}
	}

	public void OnSelected( object selection )
	{
		void SetDefaultSelection()
		{
			Selection.Set( _graph );
		}

		if ( selection != null )
		{
			if ( selection is BaseNodePlus node )
			{

				if ( node is IBlackboardNode blackboardNode )
				{
					// For now only select a blackboard parameter when _graphView only has 1 selection.
					if ( _graphView.SelectedItems.Count() == 1 )
					{
						if ( blackboardNode.ParameterIdentifier != default )
						{
							var blackboardParameter = _graph.FindParameter( blackboardNode.ParameterIdentifier );

							if ( blackboardParameter != null )
							{
								_blackboardView.SetSelection( blackboardParameter );
								_properties.Target = blackboardParameter;
							}
							else
							{
								_blackboardView.SetSelection( null );
								SetDefaultSelection();
							}

						}
					}
					else
					{
						SetDefaultSelection();
					}
				}
				else
				{
					Selection.Set( node );
				}

				if ( EnableNodePreview && (node != null && node.CanPreview) )
				{
					_preview.SetStage( node.PreviewID );
				}
				else
				{
					_preview.SetStage( ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
				}

				return;
			}

			Selection.Set( selection );
		}
		else
		{
			SetDefaultSelection();
			return;
		}
	}

	internal void OnGraphViewClicked()
	{
		// Fixes not being able to select the graph in the GraphView when the latest target was a BlackboardParameter.
		if ( _properties.Target is BlackboardParameter || _properties.Target is CategoryData )
		{
			OnSelected( null );
			_blackboardView.ClearSelection();
		}
	}

	private void OpenGeneratedShader()
	{
		if ( _asset is null )
		{
			Save();
		}
		else
		{
			var path = System.IO.Path.ChangeExtension( _asset.AbsolutePath, ".shader" );
			var asset = AssetSystem.FindByPath( path );

			asset?.OpenInEditor();
		}
	}

	private void OpenTempGeneratedShader()
	{
		if ( !_dirty )
		{
			SetDirty();
		}

		var assetPath = $"shadergraphplus/{_asset?.Name ?? "untitled"}_shadergraphplus.generated.shader";
		var resourcePath = System.IO.Path.Combine( Editor.FileSystem.Temporary.GetFullPath( "/temp" ), assetPath );
		var asset = AssetSystem.FindByPath( resourcePath );

		asset?.OpenInEditor();
	}

	private void OpenShaderGraphProjectTxt()
	{
		if ( _asset is null )
		{
			Save();
		}
		else
		{
			var path = _asset.AbsolutePath;
			Utilities.Path.OpenInNotepad( path );
		}
	}

	private void Screenshot()
	{
		if ( _asset is null )
			return;

		var path = Editor.FileSystem.Root.GetFullPath( $"/screenshots/shadergraphplus/{_asset.Name}.png" );
		System.IO.Directory.CreateDirectory( System.IO.Path.GetDirectoryName( path ) );

		_graphView.Capture( $"screenshots/shadergraphplus/{_asset.Name}.png" );

		EditorUtility.OpenFileFolder( path );
	}

	protected virtual void Compile()
	{
		_shaderCompileErrors.Clear();

		if ( string.IsNullOrWhiteSpace( _generatedCode ) )
		{
			RestoreShader();

			return;
		}

		if ( _isCompiling )
		{
			_isPendingCompile = true;

			return;
		}

		var assetPath = $"shadergraphplus/{_asset?.Name ?? "untitled"}_shadergraphplus.generated.shader";
		var resourcePath = System.IO.Path.Combine( ".source2/temp", assetPath );

		Editor.FileSystem.Root.CreateDirectory( ".source2/temp/shadergraphplus" );
		Editor.FileSystem.Root.WriteAllText( resourcePath, _generatedCode );

		_isCompiling = true;
		_preview.IsCompiling = _isCompiling;

		RestoreShader();

		_timeSinceCompile = 0;

		CompileAsync( resourcePath );
	}

	private async void CompileAsync( string path )
	{
		var options = new Sandbox.Engine.Shaders.ShaderCompileOptions
		{
			ConsoleOutput = true
		};

		var result = await EditorUtility.CompileShader( Editor.FileSystem.Root, path, options );

		if ( result.Success )
		{
			var asset = AssetSystem.RegisterFile( Editor.FileSystem.Root.GetFullPath( path ) );

			while ( !asset.IsCompiledAndUpToDate )
			{
				await Task.Yield();
			}
		}

		MainThread.Queue( () => OnCompileFinished( result.Success ? 0 : 1 ) );
	}

	private readonly List<string> _shaderCompileErrors = new();

	private struct StatusMessage
	{
		public string Status { get; set; }
		public string Message { get; set; }
	}

	internal void OnOutputData( object sender, DataReceivedEventArgs e )
	{
		var str = e.Data;
		if ( str == null )
			return;

		MainThread.Queue( () =>
		{
			var trimmed = str.Trim();
			if ( trimmed.StartsWith( '{' ) && trimmed.EndsWith( '}' ) )
			{
				var status = Json.Deserialize<StatusMessage>( trimmed );
				if ( status.Status == "Error" )
				{
					if ( !string.IsNullOrWhiteSpace( status.Message ) )
					{
						var lines = status.Message.Split( '\n' );
						foreach ( var line in lines.Where( x => !string.IsNullOrWhiteSpace( x ) ) )
						{
							_shaderCompileErrors.Add( line );
						}
					}
				}
			}
		} );
	}

	internal void OnErrorData( object sender, DataReceivedEventArgs e )
	{
		if ( e == null || e.Data == null )
			return;

		MainThread.Queue( () =>
		{
			var error = $"{e.Data}";
			if ( !string.IsNullOrWhiteSpace( error ) )
				_shaderCompileErrors.Add( error );
		} );
	}

	private void OnCompileFinished( int exitCode )
	{
		_isCompiling = false;

		if ( _isPendingCompile )
		{
			_isPendingCompile = false;

			Compile();

			return;
		}

		if ( exitCode == 0 && _shaderCompileErrors.Count == 0 )
		{
			SGPLogger.Info( $"Compile finished in {_timeSinceCompile}" );

			var shaderPath = $"shadergraphplus/{_asset?.Name ?? "untitled"}_shadergraphplus.generated.shader";

			// Reload the shader otherwise it's gonna be the old wank
			// Alternatively Material.Create could be made to force reload the shader
			Editor.ConsoleSystem.Run( $"mat_reloadshaders {shaderPath}" );

			var material = Material.Create( $"{_asset?.Name ?? "untitled"}_shadergraphplus_generated", shaderPath );

			_preview.Material = material;
		}
		else
		{
			SGPLogger.Error( $"Compile failed in {_timeSinceCompile}" );

			_output.GraphIssues = _shaderCompileErrors.Select( x => new GraphCompiler.GraphIssue { Message = x } ).ToList();

			DockManager.RaiseDock( "Output" );

			RestoreShader();
			ClearAttributes();
		}

		_preview.IsCompiling = _isCompiling;
		_preview.PostProcessing = _graph.Domain == ShaderDomain.PostProcess;

		_shaderCompileErrors.Clear();
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

	private void RegisterShaderFeatures( out List<GraphCompiler.GraphIssue> registrationIssues )
	{
		ShaderFeatures.Clear();
		registrationIssues = new();

		var features = _graph.Parameters.OfType<IBlackboardShaderFeatureParameter>();

		foreach ( var feature in features )
		{
			ShaderFeatureBase shaderFeature = default;

			if ( feature is ShaderFeatureBooleanParameter boolFeatureParam )
			{
				shaderFeature = new ShaderFeatureBoolean()
				{
					Name = boolFeatureParam.Name,
					HeaderName = boolFeatureParam.HeaderName,
					Description = boolFeatureParam.Description,
				};
			}
			else if ( feature is ShaderFeatureEnumParameter enumFeatureParam )
			{
				shaderFeature = new ShaderFeatureEnum()
				{
					Name = enumFeatureParam.Name,
					HeaderName = enumFeatureParam.HeaderName,
					Description = enumFeatureParam.Description,
					Options = enumFeatureParam.Options,
				};
			}

			if ( shaderFeature.IsValid && !ShaderFeatures.ContainsKey( shaderFeature.Name ) )
			{
				ShaderFeatures.Add( shaderFeature.Name, shaderFeature );
			}
		}
	}

	private string GeneratePreviewCode()
	{
		if ( BlackboardIssues.Any() )
		{
			_output.GraphIssues = BlackboardIssues;
			DockManager.RaiseDock( "Output" );

			_generatedCode = null;
			_generatedCodeTextView.SetTextContents( "" );

			RestoreShader();
			return null;
		}

		if ( _shaderTemplate != null )
		{
			// Validate the template
			if ( !_shaderTemplate.Validate( _graph.ShaderTemplate, out var tagErrors ) )
			{
				var graphIssues = tagErrors.Select( x => new GraphCompiler.GraphIssue() { Node = null, Message = x, IsWarning = false } );

				_output.GraphIssues = graphIssues.ToList();
				DockManager.RaiseDock( "Output" );

				_generatedCode = null;
				_generatedCodeTextView.SetTextContents( "" );

				RestoreShader();
				return null;
			}
		}

		if ( _autoCompile )
		{
			ClearAttributes();
		}

		// Go ahead preregister anything before iterating over all the nodes in the graph.
		RegisterShaderFeatures( out List<GraphCompiler.GraphIssue> registrationIssues );

		if ( registrationIssues.Any() )
		{
			_output.GraphIssues = registrationIssues;

			DockManager.RaiseDock( "Output" );

			_generatedCode = null;
			_generatedCodeTextView.SetTextContents( "" );

			RestoreShader();

			return null;
		}

		var resultNode = _graph.Nodes.OfType<BaseResult>().FirstOrDefault();
		var compiler = new GraphCompiler( _graph, _shaderTemplate, ShaderFeatures, true );
		var nodeErrors = new List<GraphCompiler.GraphIssue>();
		var nodeWarnings = new List<GraphCompiler.GraphIssue>();
		var evaluatedCustomFunctions = new List<string>();

		if ( _autoCompile )
		{
			compiler.OnAttribute = OnAttribute;
		}

		foreach ( var node in _graph.Nodes.OfType<BaseNodePlus>() )
		{
			node.ClearError();

			// Assign a PreviewID to any Previewable node.
			if ( node.CanPreview )
			{
				node.PreviewID = compiler.PreviewID++;
			}

			if ( node is IWarningNode warningNode )
			{
				node.HasWarning = false;

				var warnings = warningNode.GetWarnings();
				if ( warnings.Count > 0 )
				{
					node.HasWarning = true;
					foreach ( var warning in warnings )
					{
						nodeWarnings.Add( new() { Message = warning, Node = node, IsWarning = true } );
					}
				}
			}

			if ( node is IErroringNode erroringNode )
			{
				node.ClearError();

				var errors = erroringNode.GetErrors();
				if ( errors.Count > 0 )
				{
					node.HasError = true;

					var errorSb = new StringBuilder();
					foreach ( var error in errors )
					{
						nodeErrors.Add( new() { Message = error, Node = node, IsWarning = false } );

						errorSb.AppendLine( error );
					}

					node.ErrorMessage = errorSb.ToString();
				}
			}

			if ( node is PreviewableNode previewableNode )
			{
				previewableNode.EvaluatePreview( compiler );
			}

			if ( node is CustomFunctionNode customFunctionNode )
			{
				if ( !string.IsNullOrWhiteSpace( customFunctionNode.Name ) )
				{
					node.ClearError();

					if ( evaluatedCustomFunctions.Contains( customFunctionNode.Name ) )
					{
						node.HasError = true;
						node.ErrorMessage = "Duplicate Custom Function node";
						nodeErrors.Add( new() { Message = node.ErrorMessage, Node = node, IsWarning = false } );
					}
					else
					{
						evaluatedCustomFunctions.Add( customFunctionNode.Name );
					}
				}
			}

			var property = node.GetType().GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static )
				.FirstOrDefault( x => x.GetGetMethod() != null && x.PropertyType == typeof( NodeResult.Func ) );

			if ( property == null )
				continue;

			var output = property.GetCustomAttribute<BaseNodePlus.OutputAttribute>();
			if ( output == null )
				continue;

			var result = compiler.Result( new NodeInput { Identifier = node.Identifier, Output = property.Name } );
			if ( !result.IsValid() )
				continue;

			Type componentType = null;
			if ( result.ResultType == ResultType.VoidFunction )
			{
				componentType = result.Components switch
				{
					int r when r == 1 => typeof( float ),
					int r when r == 2 => typeof( Vector2 ),
					int r when r == 3 => typeof( Vector3 ),
					int r when r == 4 => typeof( Vector4 ),
					_ => throw new NotImplementedException(),
				};
			}
			else
			{
				componentType = result.ResultType switch
				{
					ResultType.Bool => typeof( bool ),
					ResultType.Int => typeof( int ),
					ResultType.Float => typeof( float ),
					ResultType.Vector2 => typeof( Vector2 ),
					ResultType.Vector3 => typeof( Vector3 ),
					ResultType.Vector4 => typeof( Vector4 ),
					ResultType.Float2x2 => typeof( Float2x2 ),
					ResultType.Float3x3 => typeof( Float3x3 ),
					ResultType.Float4x4 => typeof( Float4x4 ),
					ResultType.Sampler => typeof( Sampler ),
					ResultType.Texture2D => typeof( Texture ),
					ResultType.TextureCube => typeof( Texture ),
					ResultType.Gradient => typeof( Gradient ),
					_ => throw new Exception( $"Unsupported ResultType \"{result.ResultType}\"" ),
				};
			}

			if ( componentType == null )
				continue;

			// While we're here, let's check the output plugs and update their handle configs to the result type
			var nodeUI = _graphView.FindNode( node );
			if ( !nodeUI.IsValid() )
				continue;

			var plugOut = nodeUI.Outputs.FirstOrDefault( x => ((BasePlug)x.Inner).Info.Property == property );
			if ( !plugOut.IsValid() )
				continue;

			plugOut.PropertyType = componentType;

			// We also have to update everything so they get repainted

			nodeUI.Update();

			foreach ( var input in nodeUI.Inputs )
			{
				if ( !input.IsValid() || !input.Connection.IsValid() )
					continue;

				input.Connection.Update();
			}
		}

		if ( _properties.IsValid() && _properties.Target is BaseNodePlus targetNode && targetNode.CanPreview )
		{
			_preview.SetStage( targetNode.PreviewID );
		}
		else
		{
			_preview.SetStage( ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
		}

		if ( resultNode != null && !IsSubgraph )
		{
			UpdateNodeUI( resultNode );
		}
		else if ( IsSubgraph )
		{
			foreach ( var subgraphOutput in _graph.Nodes.OfType<SubgraphOutput>() )
			{
				UpdateNodeUI( subgraphOutput );
			}
		}

		var code = compiler.Generate();

		#region Errors & Warnings

		nodeErrors.AddRange( compiler.Errors );
		nodeWarnings.AddRange( compiler.Warnings );

		if ( nodeWarnings.Any() ) //&& iErroringNodeErrors.Any() )
		{
			// Add any iErroringNodeErrors to the end of the iWarningNodeWarning list.
			if ( nodeErrors.Any() )
			{
				nodeWarnings.AddRange( nodeErrors );
				_output.GraphIssues = nodeWarnings;

				DockManager.RaiseDock( "Output" );

				_generatedCode = null;
				_generatedCodeTextView.SetTextContents( "" );

				RestoreShader();

				return null;
			}
			else // No Errors to add :) not great not terrible...
			{
				_output.GraphIssues = nodeWarnings;

				DockManager.RaiseDock( "Output" );
			}
		}
		else // No warnings? clear em.
		{
			_output.ClearWarnings();
		}

		if ( nodeErrors.Any() )
		{
			_output.GraphIssues = nodeErrors;

			DockManager.RaiseDock( "Output" );

			_generatedCode = null;
			//_generatedCodeTextView.SetTextContents( "" );

			RestoreShader();

			return null;
		}
		else // No errors :o? clear em :D.
		{
			_output.ClearErrors();
		}
		#endregion Errors & Warnings

		if ( _generatedCode != code )
		{
			_generatedCode = code;
			//_generatedCodeTextView.SetTextContents( code );

			if ( _autoCompile )
			{
				Compile();
			}
		}

		return code;
	}

	private void UpdateNodeUI( BaseNodePlus node )
	{
		var nodeUI = _graphView.FindNode( node );

		if ( nodeUI.IsValid() )
		{
			nodeUI.Update();

			foreach ( var input in nodeUI.Inputs )
			{
				if ( !input.IsValid() || !input.Connection.IsValid() )
					continue;

				input.Connection.Update();
			}
		}
	}

	private string GenerateShaderCode()
	{
		// Go ahead preregister anything before iterating over all the nodes in the graph.
		RegisterShaderFeatures( out _ );

		var compiler = new GraphCompiler( _graph, _shaderTemplate, ShaderFeatures, false );
		return compiler.Generate();
	}

	public void OnUndoPushed()
	{
		_undoHistory.History = _undoStack.Names;
	}

	public void SetDirty( bool evaluate = true )
	{
		Update();

		_dirty = true;
		_graphCanvas.WindowTitle = $"{_asset?.Name ?? "untitled"}*";

		if ( evaluate )
			GeneratePreviewCode();
	}

	[EditorEvent.Frame]
	protected void Frame()
	{
		_undoOption.Enabled = _undoStack.CanUndo;
		_redoOption.Enabled = _undoStack.CanRedo;
		_undoMenuOption.Enabled = _undoStack.CanUndo;
		_redoMenuOption.Enabled = _undoStack.CanRedo;

		_undoOption.Text = _undoStack.UndoName;
		_redoOption.Text = _undoStack.RedoName;
		_undoMenuOption.Text = _undoStack.UndoName ?? "Undo";
		_redoMenuOption.Text = _undoStack.RedoName ?? "Redo";

		_undoOption.StatusTip = _undoStack.UndoName;
		_redoOption.StatusTip = _undoStack.RedoName;
		_undoMenuOption.StatusTip = _undoStack.UndoName;
		_redoMenuOption.StatusTip = _undoStack.RedoName;

		if ( _undoHistory is not null )
		{
			_undoHistory.UndoLevel = _undoStack.UndoLevel;
		}

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
			_graphView.ChildValuesChanged( null );
		}
	}

	[Shortcut( "editor.undo", "CTRL+Z", ShortcutType.Window )]
	private void Undo()
	{
		if ( _undoStack.Undo() is UndoOp op )
		{
			SGPLogger.Info( $"Undo ({op.name})" );

			_redoOption.Enabled = _undoStack.CanUndo;

			_graph.ClearNodes();
			_graph.ClearCategoryData();
			_graph.ClearParameters();

			_graph.DeserializeNodes( op.undoBuffer, true );
			_graph.DeserializeCategoryData( op.undoBuffer );
			_graph.DeserializeParameters( op.undoBuffer );

			_graphView.RebuildFromGraph();
			_blackboardView.RebuildFromGraph();

			SetDirty();
		}
	}

	[Shortcut( "editor.redo", "CTRL+Y", ShortcutType.Window )]
	private void Redo()
	{
		if ( _undoStack.Redo() is UndoOp op )
		{
			SGPLogger.Info( $"Redo ({op.name})" );

			_redoOption.Enabled = _undoStack.CanRedo;

			_graph.ClearNodes();
			_graph.ClearCategoryData();
			_graph.ClearParameters();

			_graph.DeserializeNodes( op.redoBuffer, true );
			_graph.DeserializeCategoryData( op.redoBuffer );
			_graph.DeserializeParameters( op.redoBuffer );

			_graphView.RebuildFromGraph();
			_blackboardView.RebuildFromGraph();

			SetDirty();
		}
	}

	private void SetUndoLevel( int level )
	{
		if ( _undoStack.SetUndoLevel( level ) is UndoOp op )
		{
			SGPLogger.Info( $"SetUndoLevel ({op.name})" );

			_graph.ClearNodes();
			_graph.ClearCategoryData();
			_graph.ClearParameters();

			_graph.DeserializeNodes( op.redoBuffer, true );
			_graph.DeserializeCategoryData( op.redoBuffer );
			_graph.DeserializeParameters( op.redoBuffer );

			_graphView.RebuildFromGraph();
			_blackboardView.RebuildFromGraph();

			SetDirty();
		}
	}

	[Shortcut( "editor.cut", "CTRL+X", ShortcutType.Window )]
	private void CutSelection()
	{
		_graphView.CutSelection();
	}

	[Shortcut( "editor.copy", "CTRL+C", ShortcutType.Window )]
	private void CopySelection()
	{
		_graphView.CopySelection();
	}

	[Shortcut( "editor.paste", "CTRL+V", ShortcutType.Window )]
	private void PasteSelection()
	{
		_graphView.PasteSelection();
	}

	[Shortcut( "editor.duplicate", "CTRL+D", ShortcutType.Window )]
	private void DuplicateSelection()
	{
		_graphView.DuplicateSelection();
	}

	[Shortcut( "editor.select-all", "CTRL+A", ShortcutType.Window )]
	private void SelectAll()
	{
		_graphView.SelectAll();
	}

	[Shortcut( "editor.clear-selection", "ESC", ShortcutType.Window )]
	private void ClearSelection()
	{
		_graphView.ClearSelection();
	}

	[Shortcut( "gameObject.frame", "F", ShortcutType.Window )]
	private void CenterOnSelection()
	{
		_graphView.CenterOnSelection();
	}

	private void CreateToolBar()
	{
		var toolBar = new ToolBar( this, "ShaderGraphPlusToolbar" );
		AddToolBar( toolBar, ToolbarPosition.Top );

		toolBar.AddOption( "New", "common/new.png", New ).StatusTip = "New Graph";
		toolBar.AddOption( "Open", "common/open.png", Open ).StatusTip = "Open Graph";
		toolBar.AddOption( "Save", "common/save.png", () => Save() ).StatusTip = "Save Graph";

		toolBar.AddSeparator();

		_undoOption = toolBar.AddOption( "Undo", "undo", Undo );
		_redoOption = toolBar.AddOption( "Redo", "redo", Redo );

		toolBar.AddSeparator();

		toolBar.AddOption( "Cut", "common/cut.png", CutSelection );
		toolBar.AddOption( "Copy", "common/copy.png", CopySelection );
		toolBar.AddOption( "Paste", "common/paste.png", PasteSelection );
		toolBar.AddOption( "Select All", "select_all", SelectAll );

		toolBar.AddSeparator();

		toolBar.AddOption( "Compile", "refresh", () => Compile() ).StatusTip = "Compile Graph";
		_autoCompileOption = toolBar.AddOption( "Toggle Auto Compile", "model_editor/auto_recompile.png", () =>
		{
			_autoCompile = !_autoCompile;

			if ( _autoCompile )
			{
				Compile();
				//SetDirty();
			}

			_autoCompileOption.Icon = $"{(_autoCompile ? "model_editor/auto_recompile.png" : "model_editor/supress_auto_recompile.png")}";
		} );
		_autoCompileOption.StatusTip = "Enable or Disable graph auto compile.";

		toolBar.AddOption( "Open Generated Shader", "common/edit.png", () => OpenGeneratedShader() ).StatusTip = "Open Generated Shader";
		toolBar.AddOption( "Take Screenshot", "photo_camera", Screenshot ).StatusTip = "Take Screenshot";

		_undoOption.Enabled = false;
		_redoOption.Enabled = false;
	}

	public void BuildMenuBar()
	{
		var file = MenuBar.AddMenu( "File" );
		file.AddOption( "New", "common/new.png", New, "editor.new" ).StatusTip = "New Graph";
		file.AddOption( "Open", "common/open.png", Open, "editor.open" ).StatusTip = "Open Graph";
		file.AddOption( "Save", "common/save.png", Save, "editor.save" ).StatusTip = "Save Graph";
		file.AddOption( "Save As...", "common/save.png", SaveAs, "editor.save-as" ).StatusTip = "Save Graph As...";

		file.AddSeparator();

		file.AddOption( "Reload Shader Template", "common/reload.png", LoadShaderTemplate );

		file.AddSeparator();

		_recentFilesMenu = file.AddMenu( "Recent Files" );

		file.AddSeparator();

		file.AddOption( "Quit", null, Quit, "editor.quit" ).StatusTip = "Quit";

		var edit = MenuBar.AddMenu( "Edit" );
		_undoMenuOption = edit.AddOption( "Undo", "undo", Undo, "editor.undo" );
		_redoMenuOption = edit.AddOption( "Redo", "redo", Redo, "editor.redo" );
		_undoMenuOption.Enabled = _undoStack.CanUndo;
		_redoMenuOption.Enabled = _undoStack.CanRedo;

		edit.AddSeparator();
		edit.AddOption( "Cut", "common/cut.png", CutSelection, "editor.cut" );
		edit.AddOption( "Copy", "common/copy.png", CopySelection, "editor.copy" );
		edit.AddOption( "Paste", "common/paste.png", PasteSelection, "editor.paste" );
		edit.AddOption( "Select All", "select_all", SelectAll, "editor.select-all" );

		var debug = MenuBar.AddMenu( "Debug" );
		debug.AddSeparator();
		debug.AddOption( "Open Temp Shader", "common/edit.png", OpenTempGeneratedShader );
		debug.AddOption( "Open ShaderGraph Project in text editor", "common/edit.png", OpenShaderGraphProjectTxt );
		debug.AddSeparator();

		RefreshRecentFiles();

		var view = MenuBar.AddMenu( "View" );
		view.AboutToShow += () => OnViewMenu( view );
	}

	[Shortcut( "editor.quit", "CTRL+Q" )]
	void Quit()
	{
		Close();
	}

	void RefreshRecentFiles()
	{
		_recentFilesMenu.Enabled = _recentFiles.Count > 0;

		_recentFilesMenu.Clear();

		_recentFilesMenu.AddOption( "Clear recent files", null, ClearRecentFiles )
			.StatusTip = "Clear recent files";

		_recentFilesMenu.AddSeparator();

		const int maxFilesToDisplay = 10;
		int fileCount = 0;

		for ( int i = _recentFiles.Count - 1; i >= 0; i-- )
		{
			if ( fileCount >= maxFilesToDisplay )
				break;

			var filePath = _recentFiles[i];

			_recentFilesMenu.AddOption( $"{++fileCount} - {filePath}", null, () => PromptSave( () => Open( filePath ) ) )
				.StatusTip = $"Open {filePath}";
		}
	}

	private void OnViewMenu( Menu view )
	{
		view.Clear();
		view.AddOption( "Restore To Default", "settings_backup_restore", ResetLayout );
		view.AddSeparator();

		foreach ( var dock in DockManager.DockTypes )
		{
			var o = view.AddOption( dock.Title, dock.Icon );
			o.Checkable = true;
			o.Checked = DockManager.IsDockOpen( dock.Title );
			o.Toggled += ( b ) => DockManager.SetDockState( dock.Title, b );
		}

		view.AddSeparator();

		var style = view.AddOption( "Grid-Aligned Wires", "turn_sharp_right" );
		style.Checkable = true;
		style.Checked = ShaderGraphPlusView.EnableGridAlignedWires;
		style.Toggled += b => ShaderGraphPlusView.EnableGridAlignedWires = b;

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

	[Shortcut( "editor.new", "CTRL+N" )]
	public void New()
	{
		PromptSave( CreateNew );
	}

	public void CreateNew()
	{
		Selection.Clear();

		_asset = null;
		_graph = new();
		_dirty = false;
		_graphView.Graph = _graph;
		_blackboardView.Graph = _graph;
		_graphCanvas.WindowTitle = "untitled";
		_preview.Model = null;
		_preview.Tint = Color.White;
		_undoStack.Clear();
		_undoHistory.History = _undoStack.Names;
		_generatedCode = "";
		_generatedCodeTextView.SetTextContents( "" );
		Selection.Set( _graph );

		_output.ClearErrors();
		_output.ClearWarnings();

		if ( !IsSubgraph )
		{
			var result = _graphView.CreateNewNode( _graphView.FindNodeType( typeof( Result ) ), 0 );
			_graphView.Scale = 1;
			_graphView.CenterOn( result.Size * 0.5f );
		}
		else
		{
			var result = _graphView.CreateNewNode( _graphView.FindNodeType( typeof( SubgraphOutput ) ), 0 );
			var parameter = _blackboardView.CreateNewParameter( _graphView.FindParameterType( typeof( Float3SubgraphOutputParameter ) ) ) as Float3SubgraphOutputParameter;

			parameter.Preview = SubgraphOutputPreviewType.Albedo;

			var subgraphOutput = result.Node as SubgraphOutput;
			subgraphOutput.ParameterIdentifier = parameter.Identifier;

			_graphView.Scale = 1;
			_graphView.CenterOn( result.Size * 0.5f );
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

		//_compiledNodes.Clear();

		_preview?.ClearAttributes();
	}

	public void Open()
	{
		var fd = new FileDialog( null )
		{
			Title = $"Open {FileType}",
			DefaultSuffix = $".{FileExtension}"
		};

		fd.SetNameFilter( $"{FileType} ( *.{FileExtension})" );

		if ( !fd.Execute() )
			return;

		PromptSave( () => Open( fd.SelectedFile ) );
	}

	private void OpenProject( string path )
	{
		Open( path );
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

		var graph = new ShaderGraphPlus();
		graph.Deserialize( System.IO.File.ReadAllText( path ), null, Path.GetFileName( path ) );
		graph.Path = asset.RelativePath;
		graph.IsSubgraph = IsSubgraph;

		_preview.Model = string.IsNullOrWhiteSpace( graph.Model ) ? null : Model.Load( graph.Model );
		_preview.LoadSettings( graph.PreviewSettings );

		Selection.Clear();

		_asset = asset;
		_graph = graph;
		_dirty = false;
		_graphView.Graph = _graph;

		LoadShaderTemplate();

		_blackboardView.Graph = _graph;
		_graphCanvas.WindowTitle = _asset.Name;
		_undoStack.Clear();
		_undoHistory.History = _undoStack.Names;
		_generatedCode = "";
		_generatedCodeTextView.SetTextContents( "" );
		Selection.Set( _graph );

		_blackboardView.RebuildTreeView();

		if ( addToPath )
			AddFileHistory( path );

		_output.ClearErrors();
		_output.ClearWarnings();

		var center = Vector2.Zero;
		var resultNode = graph.Nodes.OfType<BaseResult>().FirstOrDefault();
		if ( resultNode != null )
		{
			var nodeUI = _graphView.FindNode( resultNode );
			if ( nodeUI.IsValid() )
			{
				center = nodeUI.Position + nodeUI.Size * 0.5f;
			}
		}

		_graphView.Scale = 1;
		_graphView.CenterOn( center );
		_graphView.RestoreViewFromCookie();

		ClearAttributes();

		AddToRecentFiles( path );

		GeneratePreviewCode();

		var generatedShaderPath = System.IO.Path.ChangeExtension( _asset.AbsolutePath, ".shader" );

		if ( System.IO.Path.Exists( generatedShaderPath ) )
		{
			_generatedCodeTextView.SetTextContents( System.IO.File.ReadAllText( generatedShaderPath ) );
		}
		else
		{
			DockManager.RaiseDock( "Output" );
		}

	}

	private void LoadShaderTemplate()
	{
		if ( _graph is null ) return;

		if ( !string.IsNullOrWhiteSpace( _graph.ShaderTemplate ) )
		{
			var templateAsset = AssetSystem.FindByPath( _graph.ShaderTemplate );

			_shaderTemplate = new ShaderTemplateResource();
			_shaderTemplate.Deserialize( System.IO.File.ReadAllText( templateAsset.AbsolutePath ), System.IO.Path.GetFileName( templateAsset.AbsolutePath ) );

			_graph.UserTemplateInfo = _shaderTemplate;
		}
		else
		{
			_graph.UserTemplateInfo = new UserShaderTemplateInfo();
		}

		_graph.ValidateTemplateSettings();
	}

	[Shortcut( "editor.save-as", "CTRL+SHIFT+S" )]
	public void SaveAs()
	{
		SaveInternal( true );
	}

	[Shortcut( "editor.save", "CTRL+S" )]
	public void Save()
	{
		SaveInternal( false );
	}

	private void AddFileHistory( string path )
	{
		var lastFileHistory = _fileHistory.LastOrDefault();
		if ( _fileHistoryPosition < _fileHistory.Count - 1 )
		{
			_fileHistory.RemoveRange( _fileHistoryPosition + 1, _fileHistory.Count - _fileHistoryPosition - 1 );
		}
		if ( path != lastFileHistory )
			_fileHistory.Add( path );
		_fileHistoryPosition = _fileHistory.Count - 1;

		UpdateFileHistoryButtons();
	}

	private void FileHistoryForward()
	{
		if ( _fileHistoryPosition < _fileHistory.Count - 1 )
		{
			_fileHistoryPosition++;
			PromptSave( () =>
			{
				Open( _fileHistory[_fileHistoryPosition], false );
				UpdateFileHistoryButtons();
			} );
		}
	}

	private void FileHistoryBack()
	{
		if ( _fileHistoryPosition > 0 )
		{
			_fileHistoryPosition--;
			PromptSave( () =>
			{
				Open( _fileHistory[_fileHistoryPosition], false );
				UpdateFileHistoryButtons();
			} );
		}
	}

	private void FileHistoryHome()
	{
		if ( _fileHistory.Count == 0 ) return;
		PromptSave( () =>
		{
			Open( _fileHistory.First() );
			UpdateFileHistoryButtons();
		} );
	}

	private void UpdateFileHistoryButtons()
	{
		_fileHistoryForward.Enabled = _fileHistoryPosition < _fileHistory.Count - 1;
		_fileHistoryBack.Enabled = _fileHistoryPosition > 0;
		_fileHistoryHome.Enabled = _asset.Path != _fileHistory.FirstOrDefault();
	}

	private bool SaveInternal( bool saveAs )
	{
		var savePath = _asset == null || saveAs ? GetSavePath() : _asset.AbsolutePath;
		if ( string.IsNullOrWhiteSpace( savePath ) )
			return false;

		_preview.SaveSettings( _graph.PreviewSettings );

		// Write serialized graph to asset file
		System.IO.File.WriteAllText( savePath, _graph.Serialize() );

		if ( saveAs )
		{
			// If we're saving as, we want to register the new asset
			_asset = null;
		}

		// Register asset if we haven't already
		_asset ??= AssetSystem.RegisterFile( savePath );

		if ( _asset == null )
		{
			SGPLogger.Warning( $"Unable to register asset {savePath}" );

			return false;
		}

		MainAssetBrowser.Instance?.Local.UpdateAssetList();

		_dirty = false;
		_graphCanvas.WindowTitle = _asset.Name;

		if ( IsSubgraph )
		{
			Compile();

			_generatedCodeTextView.SetTextContents( _generatedCode );
		}
		else
		{
			var shaderPath = System.IO.Path.ChangeExtension( savePath, ".shader" );

			var code = GenerateShaderCode();

			if ( string.IsNullOrWhiteSpace( code ) )
				return false;

			_generatedCodeTextView.SetTextContents( code );

			// Write generated shader to file
			if ( System.IO.File.Exists( shaderPath ) )
			{
				FileInfo fileInfo = new FileInfo( shaderPath );

				if ( !fileInfo.IsReadOnly )
				{
					System.IO.File.WriteAllText( shaderPath, code );
				}
				else
				{
					SGPLogger.Warning( $"Asset at path \"{_asset.Path}\" is read only!" );
				}
			}
			else
			{
				System.IO.File.WriteAllText( shaderPath, code );
			}

		}


		// Write generated post processing class to file within the current projects code folder.
		//if (_graph.MaterialDomain is MaterialDomain.PostProcess)
		//{
		//	// If the post processing class code is blank, dont bother generating the class.
		//	if ( !string.IsNullOrWhiteSpace( code.Item2 ) )
		//	{
		//        WritePostProcessingShaderClass( code.Item2 );
		//    }
		//	
		//}
		//

		AddToRecentFiles( savePath );

		EditorEvent.Run( "shadergraphplus.update.subgraph", _asset.RelativePath );

		return true;
	}

	private void WritePostProcessingShaderClass( string classCode )
	{
		var path = System.IO.Directory.CreateDirectory( $"{Utilities.Path.GetProjectCodePath()}/Components/PostProcessing" );

		File.WriteAllText( Path.Combine( path.FullName, $"{_asset.Name}_PostProcessing.cs" ), classCode );
	}

	private string GetSavePath()
	{
		var fd = new FileDialog( null )
		{
			Title = $"Save {FileType}",
			DefaultSuffix = $".{FileExtension}"
		};

		fd.SelectFile( $"untitled.{FileExtension}" );
		fd.SetFindFile();
		fd.SetModeSave();
		fd.SetNameFilter( $"{FileType} (*.{FileExtension})" );
		if ( !fd.Execute() )
			return null;

		return fd.SelectedFile;
	}

	public void CreateUI()
	{
		BuildMenuBar();

		_graphCanvas = new Widget( this ) { WindowTitle = $"{(_asset != null ? _asset.Name : "untitled")}{(_dirty ? "*" : "")}" };
		_graphCanvas.Name = "Graph";
		_graphCanvas.SetWindowIcon( "account_tree" );
		_graphCanvas.Layout = Layout.Column();

		var graphToolBar = new ToolBar( _graphCanvas, "GraphToolBar" );
		graphToolBar.SetIconSize( 16 );
		_fileHistoryBack = graphToolBar.AddOption( null, "arrow_back" );
		_fileHistoryForward = graphToolBar.AddOption( null, "arrow_forward" );
		graphToolBar.AddSeparator();
		_fileHistoryHome = graphToolBar.AddOption( null, "common/home.png" );
		_fileHistoryBack.Triggered += FileHistoryBack;
		_fileHistoryForward.Triggered += FileHistoryForward;
		_fileHistoryHome.Triggered += FileHistoryHome;
		_fileHistoryBack.Enabled = false;
		_fileHistoryForward.Enabled = false;
		_fileHistoryHome.Enabled = false;

		var stretcher = new Widget( graphToolBar );
		stretcher.Layout = Layout.Row();
		stretcher.Layout.AddStretchCell( 1 );
		graphToolBar.AddWidget( stretcher );

		graphToolBar.AddWidget( new GamePerformanceBar( () => (1000.0f / PerformanceStats.LastSecond.FrameAvg).ToString( "n0" ) + "fps" ) { ToolTip = "Frames Per Second Average" } );
		graphToolBar.AddWidget( new GamePerformanceBar( () => PerformanceStats.LastSecond.FrameAvg.ToString( "0.00" ) + "ms" ) { ToolTip = "Frame Time Average (milliseconds)" } );
		graphToolBar.AddWidget( new GamePerformanceBar( () => PerformanceStats.ApproximateProcessMemoryUsage.FormatBytes() ) { ToolTip = "Approximate Memory Usage" } );

		_graphCanvas.Layout.Add( graphToolBar );

		_blackboardCanvas = new Widget( this ) { WindowTitle = $"Blackboard" };
		_blackboardCanvas.Name = "Blackboard";
		_blackboardCanvas.SetWindowIcon( "list" );
		_blackboardCanvas.Layout = Layout.Row();
		_blackboardCanvas.Layout.Spacing = 8;
		_blackboardCanvas.Layout.Margin = 4;

		_blackboardView = new ShaderGraphPlusBlackboardView( _blackboardCanvas, this );
		_blackboardView.Graph = _graph;
		_blackboardView.OnDirty += ( evaluate ) => SetDirty( evaluate );
		_blackboardView.OnParameterNodeDeleted += () =>
		{
			_graphView.RebuildFromGraph();
		};

		_blackboardCanvas.Layout.Add( _blackboardView, 1 );

		_graphView = new ShaderGraphPlusView( _graphCanvas, this, _blackboardView );
		_graphView.BilinearFiltering = false;

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
			_blackboardView.AddParameterType( type );
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

		_graphView.Graph = _graph;
		_graphView.OnChildValuesChanged += ( w ) => SetDirty();
		_graphCanvas.Layout.Add( _graphView, 1 );

		_output = new Output( this );
		_output.OnNodeSelected += ( node ) =>
		{
			var nodeUI = _graphView.SelectNode( node );

			_graphView.Scale = 1;
			_graphView.CenterOn( nodeUI.Center );
		};

		_preview = new PreviewPanel( this, _graph.Model )
		{
			OnModelChanged = ( model ) => _graph.Model = model?.Name
		};

		foreach ( var value in _samplerStateAttributes )
		{
			_preview.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _textureAttributes )
		{
			_preview.SetAttribute( value.Key, value.Value );
		}


		foreach ( var value in _float4Attributes )
		{
			_preview.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _float3Attributes )
		{
			_preview.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _float2Attributes )
		{
			_preview.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _floatAttributes )
		{
			_preview.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _boolAttributes )
		{
			_preview.SetAttribute( value.Key, value.Value );
		}

		foreach ( var value in _dynamicComboIntAttributes )
		{
			_preview.SetDynamicCombo( value.Key, value.Value );
		}

		foreach ( var value in _shaderFeatures )
		{
			_preview.SetFeature( value.Key, value.Value );
		}

		Selection.OnItemAdded += ( x ) =>
		{
			_properties.Target = x;
		};
		Selection.OnItemRemoved += OnDeselected;

		_properties = new Properties( this );
		_properties.Target = _graph;
		_properties.PropertyUpdated += OnPropertyUpdated;

		_undoHistory = new UndoHistory( this, _undoStack );
		_undoHistory.OnUndo = Undo;
		_undoHistory.OnRedo = Redo;
		_undoHistory.OnHistorySelected = SetUndoLevel;

		_palette = new PaletteWidget( this, IsSubgraph );
		_generatedCodeTextView = new TextView( this, "Generated Code", "" );

		var graph = DockManager.AddDock( "Graph", "account_tree", _graphCanvas, DockArea.Center );
		var preview = DockManager.AddDock( "Preview", "photo", _preview, DockArea.Left );

		DockManager.AddDock( "Properties", "edit", _properties, DockArea.Bottom, relativeTo: preview );

		var output = DockManager.AddDock( "Output", "notes", _output, DockArea.Bottom, relativeTo: graph );

		DockManager.AddDock( "Blackboard", "format_list_bulleted", _blackboardCanvas, DockArea.Right, relativeTo: output );

		if ( EditorTypeLibrary.Create( "ConsoleWidget", typeof( Widget ), new[] { this } ) is Widget console )
			DockManager.AddDock( "Console", "text_snippet", console, DockArea.Center, relativeTo: output );

		DockManager.AddDock( "Undo History", "history", _undoHistory, DockArea.Center, relativeTo: output );
		DockManager.AddDock( "Palette", "palette", _palette, DockArea.Center, relativeTo: output );
		DockManager.AddDock( "Generated Code", "text_snippet", _generatedCodeTextView, DockArea.Center, relativeTo: output );

		foreach ( var info in DockManager.DockTypes )
		{
			if ( DockManager.FindDockWidget( info.Title ) is { } dock )
				dock.MinimumSizeFromContent = true;
		}

		DockManager.RaiseDock( "Output" );

		Compile();
	}

	private void CheckParameterTarget( BlackboardParameter parameter )
	{
		BlackboardIssues.Clear();

		if ( !parameter.CheckParameter( out var parameterIssues ) )
		{
			foreach ( var issue in parameterIssues )
			{
				BlackboardIssues.Add( new() { Node = null, Message = issue, IsWarning = false } );
			}
		}
	}

	protected override void BuildDefaultLayout()
	{
		var graph = DockManager.OpenDock( "Graph", DockArea.Center );
		var preview = DockManager.OpenDock( "Preview", DockArea.Left );

		DockManager.SetSplitterProportions( preview, 0.28f, 0.72f );

		var properties = DockManager.OpenDock( "Properties", DockArea.Bottom, preview );
		var output = DockManager.OpenDock( "Output", DockArea.Bottom, graph );

		DockManager.OpenDock( "Blackboard", DockArea.Right, output );
		DockManager.OpenDock( "Console", DockArea.Center, output );
		DockManager.OpenDock( "Undo History", DockArea.Center, output );
		DockManager.OpenDock( "Palette", DockArea.Center, output );
		DockManager.OpenDock( "Generated Code", DockArea.Center, output );

		DockManager.SetSplitterProportions( properties, 0.68f, 0.32f );
		DockManager.SetSplitterProportions( output, 0.68f, 0.32f );
		DockManager.RaiseDock( "Output" );
	}

	private void OnPropertyUpdated( SerializedProperty serializedProperty )
	{
		BlackboardIssues.Clear();
		_preview.PostProcessing = _graphView.Graph.Domain == ShaderDomain.PostProcess;

		if ( _properties.Target is BlackboardParameter parameter )
		{
			CheckParameterTarget( parameter );
		}

		if ( _properties.Target is BaseNodePlus node )
		{
			_graphView.UpdateNode( node );
		}

		// Reload shader template
		if ( serializedProperty.Name == nameof( ShaderGraphPlus.ShaderTemplate ) )
		{
			LoadShaderTemplate();
		}

		var shouldEvaluate = _properties.Target is not CommentNode;
		SetDirty( shouldEvaluate );
	}

	protected override bool OnClose()
	{
		if ( !_dirty )
		{
			return true;
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

		return false;
	}

	// For Development iteration only
	/*
	[EditorEvent.Hotload]
	public void OnHotload()
	{
		DockManager.Clear();
		MenuBar.Clear();

		CreateUI();
		Compile();

		_preview3D.LoadSettings( _graph.PreviewSettings );
	}
	*/

	[Event( "shadergraphplus.update.subgraph" )]
	public void OnSubgraphUpdate( string updatedPath )
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
}
