using Editor;
using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlus;

public sealed partial class GraphCompiler
{
	public struct GraphIssue
	{
		public BaseNodePlus Node;
		public string Message;
		public bool IsWarning;
	}

	/// <summary>
	/// Current graph we're compiling
	/// </summary>
	public ShaderGraphPlus Graph { get; private set; }

	/// <summary>
	/// Current SubGraph
	/// </summary>
	private ShaderGraphPlus Subgraph = null;

	/// <summary>
	/// Current SubGraph node that we are in
	/// </summary>
	private SubgraphNode SubgraphNode = null;

	/// <summary>
	/// The loaded sub-graphs
	/// </summary>
	public List<ShaderGraphPlus> Subgraphs { get; private set; }

	private List<(SubgraphNode, ShaderGraphPlus)> SubgraphStack = new();

	public Dictionary<string, string> CompiledTextures { get; private set; } = new();

	/// <summary>
	/// Pixel Shader Stage Includes
	/// </summary>
	public HashSet<string> PixelIncludes { get; private set; } = new();

	/// <summary>
	/// Vertex Shader Stage Includes
	/// </summary>
	public HashSet<string> VertexIncludes { get; private set; } = new();

	public Dictionary<string, string> PreviewImages = new();

	/// <summary>
	/// Vertex Shader stage inputs
	/// </summary>
	public Dictionary<string, string> VertexInputs { get; private set; } = new();

	/// <summary>
	/// Pixel Shader stage inputs
	/// </summary>
	public Dictionary<string, string> PixelInputs { get; private set; } = new();

	public Dictionary<string, string> ShaderDefines { get; private set; } = new();

	public int VoidLocalCount { get; set; } = 0;

	/// <summary>
	/// Is this compile for just the preview or not, preview uses attributes for constant values
	/// </summary>
	public bool IsPreview { get; private set; }

	/// <summary>
	/// Is this compile for the final generated result
	/// </summary>
	public bool IsNotPreview => !IsPreview;

	private partial class CompileResult
	{
		public List<(NodeResult localResult, NodeResult funcResult)> Results = new();
		public Dictionary<NodeInput, NodeResult> InputResults = new();

		public Dictionary<string, Sampler> SamplerStates = new();
		public Dictionary<string, TextureInput> TextureInputs = new();
		public Dictionary<string, Gradient> Gradients = new();
		public Dictionary<string, (string Options, NodeResult Result)> Parameters = new();
		public Dictionary<string, string> Globals { get; private set; } = new();
		public Dictionary<string, object> Attributes { get; private set; } = new();
		public HashSet<string> Functions { get; private set; } = new();

		public string RepresentativeTexture { get; set; }

		internal void Replace( Dictionary<string, string> globals, Dictionary<string, object> attributes, HashSet<string> functions )
		{
			Globals = globals;
			Attributes = attributes;
			Functions = functions;
		}
	}

	public enum ShaderStage
	{
		Vertex,
		Pixel,
	}

	/// <summary>
	/// Current shader stage being evaluated.
	/// </summary>
	public ShaderStage Stage { get; private set; }

	public bool IsVs => Stage == ShaderStage.Vertex;
	public bool IsPs => Stage == ShaderStage.Pixel;
	private string StageName => Stage == ShaderStage.Vertex ? "vs" : "ps";
	private string CurrentResultInput;

	private CompileResult VertexResult { get; set; } = new();
	private CompileResult PixelResult { get; set; } = new();
	private CompileResult ShaderResult => Stage == ShaderStage.Vertex ? VertexResult : PixelResult;

	public Action<string, object> OnAttribute { get; set; }

	// Init to 1, 0 is reserved.
	public int PreviewID { get; internal set; } = 1;

	private List<NodeInput> InputStack = new();

	private List<int> VoidDataHashes = new();

	private readonly Dictionary<BaseNodePlus, List<string>> NodeErrors = new();
	private readonly Dictionary<BaseNodePlus, List<string>> NodeWarnings = new();

	/// <summary>
	/// Error list.
	/// </summary>
	public IEnumerable<GraphIssue> Errors => NodeErrors
		.Select( x => new GraphIssue { Node = x.Key, Message = x.Value.FirstOrDefault(), IsWarning = false } );

	/// <summary>
	/// Warning list.
	/// </summary>
	public IEnumerable<GraphIssue> Warnings => NodeWarnings
		.Select( x => new GraphIssue { Node = x.Key, Message = x.Value.FirstOrDefault(), IsWarning = true } );

	public GraphCompiler( ShaderGraphPlus graph, Dictionary<string, ShaderFeatureBase> shaderFeatures, bool preview )
	{
		Graph = graph;
		IsPreview = preview;
		Stage = ShaderStage.Pixel;
		Subgraphs = new();
		AddSubgraphs( Graph );
		ShaderFeatures = shaderFeatures;

		// Set the Initial Vertex and Pixel stage inputs from ShaderTemplate.
		VertexInputs = ShaderTemplate.VertexInputs;
		PixelInputs = ShaderTemplate.PixelInputs;
	}

	public bool TryGetPreviewImage( string name, out string imagePath )
	{
		return PreviewImages.TryGetValue( name, out imagePath );
	}

	public void SetAttribute<T>( string name, T value )
	{
		// Dont even try to set an attribute of the type if it
		// isnt able to be set in the first place.
		if ( !ShaderAttributeTypes.Contains( value.GetType() ) )
		{
			return;
		}

		if ( !ShaderResult.Attributes.ContainsKey( name ) )
		{
			OnAttribute?.Invoke( name, value );
			ShaderResult.Attributes[name] = value;
		}
	}

	/// <summary>
	/// Generates and compiles a texture and returns the path to it.
	/// </summary>
	public string CompileTexture( TextureInput textureInput )
	{
		if ( string.IsNullOrWhiteSpace( textureInput.DefaultTexture ) )
			return "";

		var resourceText = string.Format( ShaderTemplate.TextureDefinition,
			textureInput.DefaultTexture,
			textureInput.ColorSpace,
			textureInput.ImageFormat,
			textureInput.Processor
		);

		var assetPath = $"shadergraphplus/textures/{textureInput.DefaultTexture.Replace( ".", "_" )}_shadergraphplus.generated.vtex";

		var resourcePath = Editor.FileSystem.Root.GetFullPath( "/.source2/temp" );
		resourcePath = System.IO.Path.Combine( resourcePath, assetPath );

		if ( AssetSystem.CompileResource( resourcePath, resourceText ) )
		{
			return assetPath;
		}
		else
		{
			SGPLogger.Warning( $"Failed to compile {textureInput.DefaultTexture}" );
			return "";
		}
	}

	private void AddSubgraphs( ShaderGraphPlus graph )
	{
		if ( graph != Graph )
		{
			if ( Subgraphs.Contains( graph ) )
				return;

			Subgraphs.Add( graph );
		}

		foreach ( var node in graph.Nodes )
		{
			if ( node is SubgraphNode subgraphNode )
			{
				AddSubgraphs( subgraphNode.Subgraph );
			}
		}
	}

	public void RegisterVertexInput( string type, string name, string semantic )
	{
		name = CleanName( name );

		if ( ShaderTemplate.InternalVertexInputs.ContainsKey( name ) )
		{
			SGPLogger.Error( $"VetexInput \"{name}\" is reserved by the GraphCompiler" );

			return;
		}

		var vertexInput = $"{type} {name} : {semantic};";

		if ( !VertexInputs.TryAdd( name, vertexInput ) )
		{
			SGPLogger.Warning( $"VertexInputs already contains key \"{name}\"", IsNotPreview );
		}
	}

	public void RegisterPixelInput( string type, string name, string semantic )
	{
		name = CleanName( name );

		if ( ShaderTemplate.InternalPixelInputs.ContainsKey( name ) )
		{
			SGPLogger.Error( $"PixelInput \"{name}\" is reserved by the GraphCompiler" );

			return;
		}

		var pixelInput = $"{type} {name} : {semantic};";

		if ( !PixelInputs.TryAdd( name, pixelInput ) )
		{
			SGPLogger.Warning( $"PixelInputs already contains key \"{name}\"", IsNotPreview );
		}
	}

	internal string RegisterCustomCode( string nodeID, string functionName, string functionInputs, Dictionary<string, string> outputResults, CustomCodeNodeMode mode )
	{
		if ( !ShaderResult.VoidFunctionLocals.ContainsKey( nodeID ) )
		{
			var outputData = new List<VoidFunctionResult>();
			var functionOutputsSb = new StringBuilder();

			foreach ( var output in outputResults.Index() )
			{
				var userAssignedname = output.Item.Key;
				var id = VoidLocalCount++;
				var compilerName = $"ol_{id}";

				functionOutputsSb.Append( (output.Index + 1) == outputResults.Count ? $"{compilerName}" : $" {compilerName}, " );

				VoidFunctionResult data = new(
					userAssignedname,
					compilerName,
					GetResultTypeFromHLSLDataType( output.Item.Value )
				);

				outputData.Add( data );
			}

			// Assemble the function call
			var funcCall = "";
			if ( mode == CustomCodeNodeMode.Generate )
			{
				funcCall = ResultHLSLFunction( functionName, $"{functionInputs}, {functionOutputsSb}" );
			}
			else if ( mode == CustomCodeNodeMode.File )
			{
				funcCall = $"{functionName}( {string.Join( ", ", $"{functionInputs}, {functionOutputsSb}" )} )";
			}
			else
			{
				throw new NotImplementedException( $"Unknown CustomFunction mode '{mode}'" );
			}

			var voidData = new VoidFunctionInfo(
				outputData,
				funcCall,
				SubgraphNode == null ? nodeID : SubgraphNode.Identifier,
				mode == CustomCodeNodeMode.File
			);

			ShaderResult.VoidFunctionLocals.Add( nodeID, voidData );

			return funcCall;
		}

		return ShaderResult.VoidFunctionLocals[nodeID].FunctionCall;
	}

	/// <summary>
	/// Register a hlsl shader include.
	/// </summary>
	/// <param name="path"></param>
	public void RegisterInclude( string path )
	{
		var list = IsVs ? VertexIncludes : PixelIncludes;

		if ( list.Contains( path ) )
			return;

		list.Add( path );
	}

	/// <summary>
	/// Register a hlsl define directive and then return the name of the define converted to all uppercase.
	/// </summary>
	public string RegisterDefine( string name, string value )
	{
		name = CleanName( name ).ToUpper();

		if ( ShaderDefines.ContainsKey( name ) )
			return name;

		ShaderDefines.Add( name, value );

		return name;
	}

	/// <summary>
	/// Register some generic global parameter for a node to use.
	/// </summary>
	public void RegisterGlobal( string name, string global )
	{
		var result = ShaderResult;
		if ( result.Globals.ContainsKey( name ) )
			return;

		result.Globals.Add( name, global );
	}

	/// <summary>
	/// Emit a scalar <c>float</c> expression. If <paramref name="r"/> is float2/float3/float4, uses swizzle
	/// so wiring a vector into a <c>float</c> HLSL parameter does not rely on implicit truncation (-Wconversion).
	/// </summary>
	public static string EmitScalarFloat( NodeResult r, float fallback )
	{
		if ( !r.IsValid || !r.IsFloatTypeResult() )
			return $"{fallback}";
		return r.Components > 1 ? r.Cast( 1 ) : r.Code;
	}

	public string ResultHLSLFunction( string name, params string[] args )
	{
		if ( !GraphHLSLFunctions.HasFunction( name ) )
			return null;

		var result = ShaderResult;
		if ( !result.Functions.Contains( name ) )
			result.Functions.Add( name );

		return $"{name}( {string.Join( ", ", args )} )";
	}

	public string RegisterHLSLFunction( string code, [CallerArgumentExpression( nameof( code ) )] string functionName = "", bool overrideFunction = false )
	{
		if ( !GraphHLSLFunctions.HasFunction( functionName ) || overrideFunction )
		{
			GraphHLSLFunctions.RegisterFunction( functionName, code, overrideFunction );
		}

		return functionName;
	}

	/// <summary>
	/// Loops through ShaderResult.Gradients to find the matching key then returns the corresponding Gradient.
	/// </summary>
	public Gradient GetGradient( string gradientName )
	{
		var result = ShaderResult;

		Gradient searchResult = new();

		foreach ( var gradient in result.Gradients )
		{
			if ( gradient.Key == gradientName )
			{
				searchResult = gradient.Value;
			}
		}

		return searchResult;
	}

	/// <summary>
	/// Register a gradient and return the name of the graident. A generic name is returned instead if the gradient name is empty.
	/// </summary>
	public string RegisterGradient( Gradient gradient, string gradientName )
	{
		var result = ShaderResult;

		var name = !string.IsNullOrWhiteSpace( gradientName ) ? CleanName( gradientName ) : $"Gradient_{result.Gradients.Count}";

		if ( !result.Gradients.ContainsKey( name ) )
		{
			result.Gradients.Add( name, gradient );
		}

		return name;
	}

	/// <summary>
	/// Register a sampler and return the name of it
	/// </summary>
	public string ResultSampler( Sampler sampler )
	{
		var name = !string.IsNullOrWhiteSpace( sampler.Name ) ? CleanName( sampler.Name ) : $"Sampler_{sampler.GetHashCode():X8}";

		if ( IsPreview )
		{
			return ResultValue( sampler, $"g_s{name}", previewNameOverride: true ).Code;
		}
		else 
		{
			if ( !ShaderResult.SamplerStates.ContainsKey( name ) )
			{
				ShaderResult.SamplerStates.Add( name, sampler );
			}

			return $"g_s{name}";
		}
	}

	/// <summary>
	/// Register a texture and return the global name of it
	/// </summary>
	public string ResultTexture( TextureInput input, Texture texture, bool storePreviewImage = false )
	{
		var name = CleanName( input.Name );
		var result = ShaderResult;
		var globalName = "";

		name = string.IsNullOrWhiteSpace( name ) ? $"Texture_{StageName}_{ShaderResult.TextureInputs.Count}" : name;

		if ( !result.TextureInputs.ContainsKey( name ) )
			result.TextureInputs.Add( name, input );

		if ( storePreviewImage )
		{
			if ( !PreviewImages.ContainsKey( $"g_t{name}" ) )
			{
				PreviewImages.Add( $"g_t{name}", input.DefaultTexture );
			}
		}

		if ( texture != null )
			SetAttribute( name, texture );

		globalName = $"g_t{name}";

		if ( CurrentResultInput == "Albedo" && !input.IsAttribute )
		{
			result.RepresentativeTexture = globalName;
		}

		return globalName;
	}

	/// <summary>
	/// Get result of a sampler input with an optional default sampelr value if it failed to resolve
	/// </summary>
	public string ResultSamplerOrDefault( NodeInput samplerInput, Sampler defaultSampler )
	{
		var resultSampler = Result( samplerInput );
		return resultSampler.IsValid ? resultSampler.Code : ResultSampler( defaultSampler );
	}

	/// <summary>
	/// Get result of an input with an optional default value if it failed to resolve
	/// </summary>
	public NodeResult ResultOrDefault<T>( NodeInput input, T defaultValue )
	{
		var result = Result( input );
		return result.IsValid ? result : ResultValue( defaultValue );
	}

	/// <summary>
	/// Get result of an named reroute
	/// </summary>
	internal NodeResult ResultNamedReroute( string name )
	{
		var node = Graph.FindNamedRerouteDeclarationNode( name );

		if ( node != null )
		{
			var result = node.Result.Invoke( this );

			return result;
		}

		throw new Exception( $"Could not find named reroute with name \"{name}\"" );
	}

	/// <summary>
	/// When a funcResult of a node is not valid.
	/// </summary>
	private void FuncResultError( ref BaseNodePlus node, NodeResult funcResult )
	{
		if ( funcResult.IsValid )
			return;

		if ( !NodeErrors.TryGetValue( node, out var errors ) )
		{
			errors = new();
			NodeErrors.Add( node, errors );
		}

		if ( funcResult.Errors is null || funcResult.Errors.Length == 0 )
		{
			errors.Add( $"Missing input" );
		}
		else
		{
			node.HasError = true;
			node.ErrorMessage = funcResult.Errors.FirstOrDefault();

			foreach ( var error in funcResult.Errors )
				errors.Add( error );
		}
	}

	/// <summary>
	/// Get result of an input
	/// </summary>
	public NodeResult Result( NodeInput input )
	{
		if ( !input.IsValid )
			return default;

		BaseNodePlus node = null;
		if ( string.IsNullOrEmpty( input.Subgraph ) )
		{
			if ( Subgraph is not null )
			{
				var nodeId = string.Join( ',', SubgraphStack.Select( x => x.Item1.Identifier ) );

				return Result( new()
				{
					Identifier = input.Identifier,
					Output = input.Output,
					Subgraph = Subgraph.Path,
					SubgraphNode = nodeId
				} );
			}

			node = Graph.FindNode( input.Identifier );
		}
		else
		{
			var subgraph = Subgraphs.FirstOrDefault( x => x.Path == input.Subgraph );
			if ( subgraph is not null )
			{
				node = subgraph.FindNode( input.Identifier );
			}
		}

		if ( ShaderResult.InputResults.TryGetValue( input, out var result ) )
		{
			return result;
		}

		if ( node == null )
		{
			return default;
		}

		var nodeType = node.GetType();
		var property = nodeType.GetProperty( input.Output );

		if ( property == null )
		{
			// Search for alias
			var allProperties = nodeType.GetProperties();
			foreach ( var prop in allProperties )
			{
				var alias = prop.GetCustomAttribute<AliasAttribute>();
				if ( alias is null ) continue;
				foreach ( var al in alias.Value )
				{
					if ( al == input.Output )
					{
						property = prop;
						break;
					}
				}
				if ( property != null )
					break;
			}
		}

		object value = null;

		if ( node is not IRerouteNode && node is not CustomFunctionNode && InputStack.Contains( input ) )
		{
			NodeErrors[node] = new List<string> { "Circular reference detected" };
			return default;
		}

		InputStack.Add( input );

		if ( Subgraph is not null && node.Graph != Subgraph )
		{
			if ( node.Graph != Graph )
			{
				Subgraph = node.Graph as ShaderGraphPlus;
			}
			else
			{
				Subgraph = null;
			}
		}

		if ( node is CustomFunctionNode customFunctionNode )
		{
			node.ClearError();

			if ( IsVs && customFunctionNode.CompatableStage == CompatableShaderStage.Pixel )
			{
				node.HasError = true;
				node.ErrorMessage = $"{(string.IsNullOrWhiteSpace( customFunctionNode.Name ) ? "" : $" '{customFunctionNode.Name}'")} is ment to be used in the pixel shader stage only!";

				NodeErrors[node] = [node.ErrorMessage];
				return default;
			}
			else if ( IsPs && customFunctionNode.CompatableStage == CompatableShaderStage.Vertex )
			{
				node.HasError = true;
				node.ErrorMessage = $"{(string.IsNullOrWhiteSpace( customFunctionNode.Name ) ? "" : $" '{customFunctionNode.Name}'")} is ment to be used in the vertex shader stage only!";

				NodeErrors[node] = [node.ErrorMessage];
				return default;
			}

			if ( customFunctionNode.Mode == CustomCodeNodeMode.Generate || customFunctionNode.Mode == CustomCodeNodeMode.File )
			{
				var funcResult = customFunctionNode.GetResult( this );

				if ( !funcResult.IsValid )
				{
					FuncResultError( ref node, funcResult );

					InputStack.Remove( input );
					return default;
				}

				if ( ShaderResult.VoidFunctionLocals.TryGetValue( node.Identifier, out VoidFunctionInfo data ) ) //&& !data.IsAlreadyPostProcessed )
				{
					funcResult.SetVoidLocalTargetID( node.Identifier );

					var userAssignedVariableName = input.Output;
					var compilerAssignedVariableName = data.GetCompilerAssignedName( userAssignedVariableName );

					var resultType = data.GetResultResultType( compilerAssignedVariableName );
					var localResult = new NodeResult( resultType, $"{compilerAssignedVariableName}", false, funcResult.Metadata );
					localResult.SetVoidLocalTargetID( funcResult.VoidLocalTargetID );

					if ( Subgraph != null )
					{
						if ( data.IsAlreadyPostProcessed )
						{
							InputStack.Remove( input );
							return localResult;
						}
					}
					else
					{
						// return the localResult if we are getting a result from a node that we have already evaluated. 
						foreach ( var inputResult in ShaderResult.InputResults )
						{
							if ( inputResult.Key.Identifier == input.Identifier )
							{
								InputStack.Remove( input );
								return localResult;
							}
						}
					}

					ShaderResult.InputResults.Add( input, localResult );
					ShaderResult.Results.Add( (localResult, funcResult) );
					ShaderResult.VoidFunctionLocals[node.Identifier] = data with { IsAlreadyPostProcessed = true };

					InputStack.Remove( input );

					return localResult;
				}


				SGPLogger.Error( "Failed to get localResult" );
			}
			else
			{
				throw new NotImplementedException( $"Unknown mode '{customFunctionNode.Mode}'" );
			}

		}

		if ( node is SubgraphNode subgraphNode )
		{
			var newStack = (subgraphNode, Subgraph);
			var lastNode = SubgraphNode;

			SubgraphStack.Add( newStack );
			Subgraph = subgraphNode.Subgraph;
			SubgraphNode = subgraphNode;

			if ( !Subgraphs.Contains( Subgraph ) )
			{
				Subgraphs.Add( Subgraph );
			}

			var resultNode = Subgraph.Nodes.OfType<SubgraphOutput>().Where( x => x.OutputName == input.Output ).FirstOrDefault();
			var resultInput = resultNode.Inputs.FirstOrDefault( x => x.Identifier == input.Output );
			if ( resultInput?.ConnectedOutput is not null )
			{
				var nodeId = string.Join( ',', SubgraphStack.Select( x => x.Item1.Identifier ) );
				var newConnection = new NodeInput()
				{
					Identifier = resultInput.ConnectedOutput.Node.Identifier,
					Output = resultInput.ConnectedOutput.Identifier,
					Subgraph = Subgraph.Path,
					SubgraphNode = nodeId
				};
				var newResult = Result( newConnection );

				if ( NodeErrors.Any() )
				{
					InputStack.Remove( input );
					return default;
				}

				InputStack.Remove( input );
				SubgraphStack.RemoveAt( SubgraphStack.Count - 1 );
				Subgraph = newStack.Item2;
				SubgraphNode = lastNode;

				return newResult;
			}
			else
			{
				//value = GetDefaultValue( subgraphNode, input.Output, resultInput.Type );

				switch ( resultNode.OutputType )
				{
					case SubgraphPortType.Bool:
						value = false;
						break;
					case SubgraphPortType.Int:
						value = 1;
						break;
					case SubgraphPortType.Float:
						value = 1;
						break;
					case SubgraphPortType.Vector2:
						value = Vector2.One;
						break;
					case SubgraphPortType.Vector3:
						value = Vector3.One;
						break;
					case SubgraphPortType.Vector4:
						value = Vector4.One;
						break;
					case SubgraphPortType.Color:
						value = Color.White;
						break;
				}

				if ( value == null )
				{
					subgraphNode.HasError = true;
					subgraphNode.ErrorMessage = $"Missing internal input \'{resultInput.DisplayInfo.Name}\' in node \'{Subgraph.Path}\'";

					SGPLogger.Error( subgraphNode.ErrorMessage );

					NodeErrors[subgraphNode] = [subgraphNode.ErrorMessage];

					return default;
				}
				else
				{
					SGPLogger.Error( $"Missing Internal Input \'{resultInput.DisplayInfo.Name}\' in node \'{Subgraph.Path}\' falling back to default value \'{value}\'" );
				}

				SubgraphStack.RemoveAt( SubgraphStack.Count - 1 );
				Subgraph = newStack.Item2;
				SubgraphNode = lastNode;
			}
		}
		else
		{
			if ( Subgraph is not null )
			{

				if ( node is SubgraphInput subgraphInput && !string.IsNullOrWhiteSpace( subgraphInput.Name ) )
				{
					var newResult = ResolveSubgraphInput( subgraphInput, ref value, out var error );

					if ( !string.IsNullOrWhiteSpace( error.ErrorString ) )
					{
						NodeErrors.Add( error.Node, new List<string> { error.ErrorString } );

						InputStack.Remove( input );
						return default;
					}

					if ( newResult.IsValid )
					{
						InputStack.Remove( input );
						return newResult;
					}
				}
			}
			else if ( Graph.IsSubgraph )
			{
				if ( node is SubgraphInput subgraphInput )
				{
					if ( subgraphInput.PreviewInput.IsValid )
					{
						var subgraphInputResult = Result( subgraphInput.PreviewInput );

						InputStack.Remove( input );
						return subgraphInputResult;
					}
				}
			}

			if ( value is null )
			{
				if ( property == null )
				{
					InputStack.Remove( input );
					return default;
				}

				value = property.GetValue( node );
			}

			if ( value == null )
			{
				InputStack.Remove( input );
				return default;
			}
		}

		if ( value is NodeResult nodeResult )
		{
			InputStack.Remove( input );
			return nodeResult;
		}
		else if ( value is NodeInput nodeInput )
		{
			if ( nodeInput == input )
			{
				InputStack.Remove( input );
				return default;
			}

			var newResult = Result( nodeInput );

			InputStack.Remove( input );
			return newResult;
		}
		else if ( value is NodeResult.Func resultFunc )
		{
			var funcResult = resultFunc.Invoke( this );

			if ( !funcResult.IsValid )
			{
				FuncResultError( ref node, funcResult );

				InputStack.Remove( input );
				return default;
			}

			funcResult.SetVoidLocalTargetID( node.Identifier );

			if ( IsPreview )
			{
				funcResult.SetPreviewID( node.PreviewID );
				funcResult.ShouldPreview = node.CanPreview;
			}
			else
			{
				funcResult.ShouldPreview = false;
			}

			//if ( subgraphResult )
			//{
			//	funcResult.SetPreviewID( SubgraphNode.PreviewID );
			//}

			// We can return this result without making it a local variable because it's constant
			if ( funcResult.Constant )
			{
				InputStack.Remove( input );

				return funcResult;
			}

			int id = ShaderResult.InputResults.Count;
			var varName = $"l_{id}";
			var localResult = new NodeResult( funcResult.ResultType, varName, false, funcResult.Metadata );
			localResult.SetPreviewID( funcResult.PreviewID );
			localResult.SetVoidLocalTargetID( funcResult.VoidLocalTargetID );
			localResult.ShouldPreview = funcResult.ShouldPreview;

			if ( !ShaderResult.InputResults.ContainsKey( input ) )
			{
				ShaderResult.InputResults.Add( input, localResult );
			}

			ShaderResult.Results.Add( (localResult, funcResult) );

			InputStack.Remove( input );

			return localResult;
		}

		var resultVal = ResultValue( value );

		InputStack.Remove( input );

		return resultVal;
	}

	/// <summary>
	/// Get result of two inputs and cast to the largest component of the two (a float2 and float3 will both become float3 results)
	/// </summary>
	public (NodeResult, NodeResult) Result( NodeInput a, NodeInput b, float defaultA = 0.0f, float defaultB = 1.0f )
	{
		var resultA = ResultOrDefault( a, defaultA );
		var resultB = ResultOrDefault( b, defaultB );

		if ( resultA.Components == resultB.Components )
			return (resultA, resultB);

		if ( resultA.Components < resultB.Components )
			return (new( resultB.ResultType, resultA.Cast( resultB.Components ) ), resultB);

		return (resultA, new( resultA.ResultType, resultB.Cast( resultA.Components ) ));
	}

	private static object GetDefaultValue( SubgraphNode node, string name, Type type )
	{
		if ( !node.DefaultValues.TryGetValue( name, out var value ) )
		{
			switch ( type )
			{
				case Type t when t == typeof( bool ):
					return false;
				case Type t when t == typeof( int ):
					return 1;
				case Type t when t == typeof( float ):
					return 1.0f;
				case Type t when t == typeof( Vector2 ):
					return Vector2.One;
				case Type t when t == typeof( Vector3 ):
					return Vector3.One;
				case Type t when t == typeof( Vector4 ):
					return Vector4.One;
				case Type t when t == typeof( Color ):
					return Color.White;
				case Type t when t == typeof( Float2x2 ):
					return new Float2x2();
				case Type t when t == typeof( Float3x3 ):
					return new Float3x3();
				case Type t when t == typeof( Float4x4 ):
					return new Float4x4();
				case Type t when t == typeof( Sampler ):
					return new Sampler();
				case Type t when t == typeof( Texture ):
					var inputType = node.InputReferences.FirstOrDefault( x => x.Value.inputNode.Name == name ).Value.inputNode.InputType;

					return new TextureInput() { Type = (inputType == SubgraphPortType.Texture2DObject ? TextureType.Tex2D : TextureType.TexCube) };
			}

		}

		if ( value is JsonElement el )
		{
			if ( type == typeof( bool ) )
			{
				value = el.GetBoolean();
			}
			else if ( type == typeof( int ) )
			{
				value = el.GetInt32();
			}
			else if ( type == typeof( float ) )
			{
				value = el.GetSingle();
			}
			else if ( type == typeof( Vector2 ) )
			{
				value = Vector2.Parse( el.GetString() );
			}
			else if ( type == typeof( Vector3 ) )
			{
				value = Vector3.Parse( el.GetString() );
			}
			else if ( type == typeof( Vector4 ) )
			{
				value = Vector4.Parse( el.GetString() );
			}
			else if ( type == typeof( Color ) )
			{
				value = Color.Parse( el.GetString() ) ?? Color.White;
			}
			else if ( type == typeof( Float2x2 ) )
			{
				value = JsonSerializer.Deserialize<Float2x2>( el, ShaderGraphPlus.SerializerOptions() );
			}
			else if ( type == typeof( Float3x3 ) )
			{
				value = JsonSerializer.Deserialize<Float3x3>( el, ShaderGraphPlus.SerializerOptions() );
			}
			else if ( type == typeof( Float4x4 ) )
			{
				value = JsonSerializer.Deserialize<Float4x4>( el, ShaderGraphPlus.SerializerOptions() );
			}
			else if ( type == typeof( Sampler ) )
			{
				value = JsonSerializer.Deserialize<Sampler>( el, ShaderGraphPlus.SerializerOptions() );
			}
			else if ( type == typeof( Texture ) )
			{
				var inputType = node.InputReferences.FirstOrDefault( x => x.Value.inputNode.Name == name ).Value.inputNode.InputType;
				var textureInput = JsonSerializer.Deserialize<TextureInput>( el, ShaderGraphPlus.SerializerOptions() );

				value = textureInput with { Type = (inputType == SubgraphPortType.Texture2DObject ? TextureType.Tex2D : TextureType.TexCube) };
			}
		}

		return value;
	}

	/// <summary>
	/// Get result of a value that can be set in material editor
	/// </summary>
	public NodeResult ResultParameter<T>( T parameter ) where T : IBlackboardParameter
	{
		var name = parameter.Name;
		var value = parameter.GetValue();
		object min = default;
		object max = default;
		var isRange = false;
		var isAttribute = false;
		IParameterUI parameterUI = default;

		if ( parameter is IBlackboardMaterialParameter materialParameter )
		{
			isAttribute = materialParameter.IsAttribute;
			parameterUI = materialParameter.GetParameterUI();
		}

		if ( parameter is IRangedBlackboardMaterialParameter rangedParameter )
		{
			min = rangedParameter.GetRangeMin();
			max = rangedParameter.GetRangeMax();
			isRange = min != max;
		}

		return ResultParameter( name, value, min, max, isRange, isAttribute, parameterUI );
	}

	/// <summary>
	/// Get result of a value that can be set in material editor
	/// </summary>
	public NodeResult ResultParameter<T>( string name, T value, T min = default, T max = default, bool isRange = false, bool isAttribute = false, IParameterUI ui = default )
	{
		if ( IsPreview || string.IsNullOrWhiteSpace( name ) || Subgraph is not null )
			return ResultValue( value );

		var attribName = name;
		name = CleanName( name );

		var prefix = value switch
		{
			bool _ => "g_b",
			int _ => "g_n",
			float _ => "g_fl",
			Vector2 _ => "g_v",
			Vector2Int _ => "g_v",
			Vector3 _ => "g_v",
			Vector3Int _ => "g_v",
			Vector4 _ => "g_v",
			Color _ => "g_v",
			Float2x2 _ => "g_m",
			Float3x3 _ => "g_m",
			Float4x4 _ => "g_m",
			Texture _ => "g_t",
			Sampler _ => "g_s",
			_ => throw new Exception( $"Unknown type \"{value.GetType()}\"" )
		};

		// Make sure the type T is can have a Default();
		bool canHaveDefualt = typeof( T ) switch
		{
			Type t when t == typeof( Float2x2 ) => false,
			Type t when t == typeof( Float3x3 ) => false,
			Type t when t == typeof( Float4x4 ) => false,
			_ => true,
		};

		if ( !name.StartsWith( prefix ) )
			name = prefix + name;

		if ( ShaderResult.Parameters.TryGetValue( name, out var parameter ) )
			return parameter.Result;

		parameter.Result = ResultValue( value, name );

		var options = new StringWriter();

		// If we're an attribute, we don't care about the UI options
		if ( isAttribute )
		{
			options.Write( $"Attribute( \"{attribName}\" ); " );

			if ( value is bool boolValue )
			{
				options.Write( $"Default( {(boolValue ? 1 : 0)} ); " );
			}
			else if ( canHaveDefualt )
			{
				options.Write( $"Default{parameter.Result.Components}( {value} );" );
			}
		}
		else if ( MaterialParameterTypes.Contains( value.GetType() ) )
		{
			if ( ui is FloatParameterUI floatParameterUI )
			{
				if ( floatParameterUI.Type != UIType.Default )
				{
					options.Write( $"UiType( {floatParameterUI.Type} ); " );
				}

				if ( floatParameterUI.Step > 0.0f )
				{
					options.Write( $"UiStep( {floatParameterUI.Step} ); " );
				}
			}
			else if ( value is int )
			{
				options.Write( $"UiType( Slider ); " );

			}
			else if ( value is bool )
			{
				options.Write( $"UiType( CheckBox ); " );
			}
			else if ( value is Color )
			{
				options.Write( $"UiType( Color ); " );
			}

			options.Write( $"UiGroup( \"{ui.UIGroup}\" ); " );

			if ( value is bool boolValue )
			{
				options.Write( $"Default( {(boolValue ? 1 : 0)} ); " );
			}
			else if ( canHaveDefualt )
			{
				options.Write( $"Default{parameter.Result.Components}( {value} ); " );
			}

			if ( value is not bool && parameter.Result.Components > 0 && isRange )
			{
				options.Write( $"Range{parameter.Result.Components}( {min}, {max} ); " );
			}
		}

		parameter.Options = options.ToString().Trim();

		// Avoid adding types that arnt exposed to the material editor.
		if ( MaterialParameterTypes.Contains( value.GetType() ) )
		{
			ShaderResult.Parameters.Add( name, parameter );
		}

		return parameter.Result;
	}

	/// <summary>
	/// Get result of a value, in preview mode an attribute will be registered and returned
	/// </summary>
	public NodeResult ResultValue<T>( T value, string name = null, bool previewOverride = false, bool previewNameOverride = false )
	{
		if ( value is NodeInput nodeInput ) return Result( nodeInput );

		bool isConstant = IsPreview && !previewOverride;
		bool isNamed = isConstant || !string.IsNullOrWhiteSpace( name );
		name = isConstant && !previewNameOverride ? $"g_{StageName}_{ShaderResult.Attributes.Count}" : name;

		if ( isConstant )
		{
			SetAttribute( name, value );
		}

		return value switch
		{
			bool v => isNamed ? new NodeResult( ResultType.Bool, $"{name}" ) : new NodeResult( ResultType.Bool, $"{v.ToString().ToLower()}" ) { },
			int v => isNamed ? new NodeResult( ResultType.Int, $"{name}" ) : new NodeResult( ResultType.Int, $"{v}", true ),
			float v => isNamed ? new NodeResult( ResultType.Float, $"{name}" ) : new NodeResult( ResultType.Float, $"{v}", true ),
			Vector2 v => isNamed ? new NodeResult( ResultType.Vector2, $"{name}" ) : new NodeResult( ResultType.Vector2, $"float2( {v.x}, {v.y} )" ),
			Vector3 v => isNamed ? new NodeResult( ResultType.Vector3, $"{name}" ) : new NodeResult( ResultType.Vector3, $"float3( {v.x}, {v.y}, {v.z} )" ),
			Vector4 v => isNamed ? new NodeResult( ResultType.Vector4, $"{name}" ) : new NodeResult( ResultType.Vector4, $"float4( {v.x}, {v.y}, {v.z}, {v.w} )" ),
			Color v => isNamed ? new NodeResult( ResultType.Vector4, $"{name}" ) : new NodeResult( ResultType.Vector4, $"float4( {v.r}, {v.g}, {v.b}, {v.a} )" ),
			Float2x2 v => isNamed ? new NodeResult( ResultType.Float2x2, $"{value}" ) : new NodeResult( ResultType.Float2x2, $"float2x2( {v.M11}, {v.M12}, {v.M21}, {v.M22} )" ),
			Float3x3 v => isNamed ? new NodeResult( ResultType.Float3x3, $"{value}" ) : new NodeResult( ResultType.Float3x3, $"float3x3( {v.M11}, {v.M12}, {v.M13}, {v.M21}, {v.M22}, {v.M23}, {v.M31}, {v.M32}, {v.M33} )" ),
			Float4x4 v => isNamed ? new NodeResult( ResultType.Float4x4, $"{value}" ) : new NodeResult( ResultType.Float4x4, $"float4x4( {v.M11}, {v.M12}, {v.M13}, {v.M14}, {v.M21}, {v.M22}, {v.M23}, {v.M24}, {v.M31}, {v.M32}, {v.M33}, {v.M34}, {v.M41}, {v.M42}, {v.M43}, {v.M44} )" ),
			Sampler v => new NodeResult( ResultType.Sampler, $"{name}", true ),
			_ => throw new ArgumentException( $"Unsupported attribute type `{value.GetType()}`" )
		};
	}

	private NodeResult ResolveSubgraphInput( SubgraphInput inputNode, ref object value, out (SubgraphNode Node, string ErrorString) error )
	{
		var lastStack = SubgraphStack.LastOrDefault();
		var lastNodeEntered = lastStack.Item1;
		error = new();

		if ( lastNodeEntered != null )
		{
			var parentInput = lastNodeEntered.InputReferences.FirstOrDefault( x => x.Key.Identifier == inputNode.Name );
			if ( parentInput.Key is not null )
			{
				var lastSubgraph = Subgraph;
				var lastNode = SubgraphNode;
				Subgraph = lastStack.Item2;
				SubgraphNode = (Subgraph is null) ? null : lastNodeEntered;
				SubgraphStack.RemoveAt( SubgraphStack.Count - 1 );

				var connectedPlug = parentInput.Key.ConnectedOutput;
				if ( connectedPlug is not null )
				{
					var nodeId = string.Join( ',', SubgraphStack.Select( x => x.Item1.Identifier ) );
					var newResult = Result( new()
					{
						Identifier = connectedPlug.Node.Identifier,
						Output = connectedPlug.Identifier,
						Subgraph = Subgraph?.Path,
						SubgraphNode = nodeId
					} );
					SubgraphStack.Add( lastStack );
					Subgraph = lastSubgraph;
					SubgraphNode = lastNode;
					return newResult;
				}
				else
				{
					value = GetDefaultValue( lastNodeEntered, inputNode.Name, parentInput.Value.inputNodeValueType );

					SubgraphStack.Add( lastStack );
					Subgraph = lastSubgraph;
					SubgraphNode = lastNode;

					var inputNodeInputType = parentInput.Value.inputNode.InputType;
					if ( inputNodeInputType == SubgraphPortType.Texture2DObject || inputNodeInputType == SubgraphPortType.TextureCubeObject )
					{
						var resultType = (inputNodeInputType == SubgraphPortType.Texture2DObject ? ResultType.Texture2D : ResultType.TextureCube);
						var textureInput = ((TextureInput)value) with { Type = (inputNodeInputType == SubgraphPortType.Texture2DObject ? TextureType.Tex2D : TextureType.TexCube) };
						var texurePath = CompileTexture( textureInput );
						var textureGlobal = ResultTexture( textureInput, Texture.Load( texurePath ), true );

						return new NodeResult( resultType, textureGlobal, true );
					}
					else if ( value is Sampler sampler )
					{
						var samplerResult = ResultSampler( sampler );
						return new NodeResult( ResultType.Sampler, samplerResult, constant: true );
					}
				}
			}
		}

		return new();
	}

	// TODO : Fix this up
	/*
	public string GeneratePostProcessingComponent( PostProcessingComponentInfo postProcessiComponentInfo, string className, string shaderPath )
	{
		var ppcb = new PostProcessingComponentBuilder( postProcessiComponentInfo );
		var type = "";

		foreach ( var parameter in ShaderResult.Parameters )
		{
			type = parameter.Value.Result.ComponentType.ToSimpleString();

			if ( type is "System.Boolean" )
			{
				ppcb.AddBoolProperty( parameter.Key, parameter.Value.Options );
			}
			if ( type is "float" )
			{
				ppcb.AddFloatProperty( type, parameter.Key, parameter.Value.Options );
			}
			if ( type is "Vector2" )
			{
				ppcb.AddVector2Property( type, parameter.Key, parameter.Value.Options );
			}
			if ( type is "Vector3" )
			{
				ppcb.AddVector3Property( type, parameter.Key, parameter.Value.Options );
			}
			if ( type is "Vector4" )
			{
				ppcb.AddVector4Property( type, parameter.Key, parameter.Value.Options );
			}
			if ( type is "Color" )
			{
				ppcb.AddVector4Property( type, parameter.Key, parameter.Value.Options );
			}
		}

		return ppcb.Finish( className, shaderPath );
	}
	*/

	/// <summary>
	/// Generate shader code, will evaluate the graph if it hasn't already.
	/// Different code is generated for preview and not preview.
	/// </summary>
	public string Generate()
	{
		if ( Graph.IsSubgraph )
		{
			if ( !Graph.Nodes.OfType<SubgraphOutput>().Any() )
			{
				NodeErrors.Add( new DummyNode(), [$"There must be atleast one Subgraph Output node!"] );
			}
		}

		// May have already evaluated and there's errors
		if ( Errors.Any() )
			return null;

		/*
		if ( Graph.MaterialDomain == MaterialDomain.BlendingSurface )
		{
			VertexInputs.Add( "vColorBlendValues", "float4 vColorBlendValues : TEXCOORD4 < Semantic( VertexPaintBlendParams ); >;" );
			VertexInputs.Add( "vColorPaintValues", "float4 vColorPaintValues : TEXCOORD5 < Semantic( VertexPaintTintColor ); >;" );
		
			PixelInputs.Add( "vBlendValues", "float4 vBlendValues : TEXCOORD14;" );
			PixelInputs.Add( "vPaintValues", "float4 vPaintValues : TEXCOORD15;" );
		}
		*/

		// Pre-Register anything before we Generate anything. Shouldn't cause any issues i hope.
		foreach ( var node in Graph.Nodes.OfType<IPreRegisterNodeData>() )
		{
			node.PreRegister( this );
		}

		var material = GenerateMaterial();
		var pixelOutput = GeneratePixelOutput();

		// If we have any errors after evaluating, no point going further
		if ( Errors.Any() )
			return null;

		var shaderTemplate = ShaderTemplate.Code;

		if ( Graph.Domain is ShaderDomain.BlendingSurface )
		{
			shaderTemplate = ShaderTemplateBlending.Code;
		}

		return string.Format( shaderTemplate,
			Graph.Description, // {0}
			IndentString( GenerateFeatures(), 1 ), // {1}
			IndentString( GenerateCommon(), 1 ), // {2}
			IndentString( GenerateStageInputs( ShaderStage.Vertex ), 1 ), // {3}
			IndentString( GenerateStageInputs( ShaderStage.Pixel ), 1 ), // {4}
			IndentString( GenerateGlobals(), 1 ), // {5}
			IndentString( GenerateLocals(), 2 ), // {6}
			IndentString( material, 2 ), // {7}
			IndentString( GenerateVertex(), 2 ), // {8}
			IndentString( GenerateGlobals(), 1 ), // {9}
			IndentString( GenerateVertexComboRules(), 1 ), // {10}
			IndentString( GeneratePixelComboRules(), 1 ),  // {11}
			IndentString( GenerateFunctions( PixelResult ), 1 ),  // {12}
			IndentString( GenerateFunctions( VertexResult ), 1 ),  // {13}
			IndentString( GeneratePixelInit(), 2 ), // {14}
			IndentString( pixelOutput, 2 ) // {15}
		);
	}

	private string GenerateMaterial()
	{
		Stage = ShaderStage.Pixel;
		Subgraph = null;
		SubgraphStack.Clear();

		if ( Graph.ShadingModel != ShadingModel.Lit || Graph.Domain == ShaderDomain.PostProcess ) return "";

		var resultNode = Graph.Nodes.OfType<BaseResult>().FirstOrDefault();

		if ( resultNode == null )
			return null;

		var sb = new StringBuilder();
		var visited = new HashSet<string>();

		foreach ( var property in GetNodeInputProperties( resultNode.GetType() ) )
		{
			if ( property.Name == "PositionOffset" )
				continue;

			CurrentResultInput = property.Name;
			visited.Add( property.Name );

			NodeResult result;

			if ( property.GetValue( resultNode ) is NodeInput connection && connection.IsValid() )
			{
				result = Result( connection );
			}
			else
			{
				var editorAttribute = property.GetCustomAttribute<BaseNodePlus.NodeValueEditorAttribute>();
				if ( editorAttribute == null )
					continue;

				var valueProperty = resultNode.GetType().GetProperty( editorAttribute.ValueName );
				if ( valueProperty == null )
					continue;

				result = ResultValue( valueProperty.GetValue( resultNode ) );

			}

			if ( Errors.Any() )
				return null;

			if ( !result.IsValid() )
				continue;

			if ( string.IsNullOrWhiteSpace( result.Code ) )
				continue;

			var inputAttribute = property.GetCustomAttribute<BaseNodePlus.InputAttribute>();
			var componentCount = GetComponentCount( inputAttribute.Type );

			sb.AppendLine( $"m.{property.Name} = {result.Cast( componentCount )};" );
		}

		if ( Graph.IsSubgraph )
		{
			var subgraphOutputs = Graph.Nodes.OfType<SubgraphOutput>();
			var reservedPreview = new Dictionary<SubgraphOutputPreviewType, SubgraphOutput>();

			foreach ( var subgraphOutput in subgraphOutputs )
			{
				subgraphOutput.ClearError();

				if ( reservedPreview.ContainsKey( subgraphOutput.Preview ) )
				{
					subgraphOutput.HasError = true;
					subgraphOutput.ErrorMessage = $"SubgraphOutput \"{reservedPreview[subgraphOutput.Preview].OutputName}\" has already claimed the \"{subgraphOutput.Preview}\" preview type";

					NodeErrors.Add( subgraphOutput, [subgraphOutput.ErrorMessage] );
					continue;
				}

				if ( subgraphOutput.Preview == SubgraphOutputPreviewType.None )
					continue;

				reservedPreview.Add( subgraphOutput.Preview, subgraphOutput );

				subgraphOutput.AddMaterialOutput( this, sb, subgraphOutput.Preview, out var errors );

				if ( errors.Any() )
				{
					NodeErrors.Add( new DummyNode(), errors );

					return "";
				}
			}

			reservedPreview.Clear();
		}

		visited.Clear();
		CurrentResultInput = null;

		return sb.ToString();
	}

	private string GenerateVertex()
	{
		Stage = ShaderStage.Vertex;

		var resultNode = Graph.Nodes.OfType<BaseResult>().FirstOrDefault();
		if ( resultNode == null )
			return null;

		var positionOffsetInput = resultNode.GetPositionOffset();

		var sb = new StringBuilder();
		var sb2 = new StringBuilder();

		foreach ( var vertexInput in VertexInputs )
		{
			sb2.AppendLine( $"i.{vertexInput.Key} = v.{vertexInput.Key};" );
		}

		switch ( Graph.Domain )
		{
			case ShaderDomain.Surface:
				sb.AppendLine( $@"
PixelInput i = ProcessVertex( v );
i.vPositionOs = v.vPositionOs.xyz;

{sb2.ToString()}
ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
i.vTintColor = extraShaderData.vTint;

VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		" );
				break;
			case ShaderDomain.BlendingSurface:
				sb.AppendLine( $@"
PixelInput i = ProcessVertex( v );

{sb2.ToString()}
i.vBlendValues = v.vColorBlendValues;
i.vPaintValues = v.vColorPaintValues;
" );
				break;
			case ShaderDomain.PostProcess:
				sb.AppendLine( $@"
PixelInput i;

{sb2.ToString()}
i.vPositionPs = float4( v.vPositionOs.xy, 0.0f, 1.0f );
i.vPositionWs = float3( v.vTexCoord, 0.0f );
" );
				break;
		}

		NodeResult result;

		if ( positionOffsetInput is NodeInput connection && connection.IsValid() )
		{
			result = Result( connection );

			if ( !Errors.Any() && result.IsValid() && !string.IsNullOrWhiteSpace( result.Code ) )
			{
				var componentCount = GetComponentCount( typeof( Vector3 ) );

				GenerateLocalResults( ref sb, ShaderResult.Results, out _, true, 0 );

				sb.AppendLine( $"i.vPositionWs.xyz += {result.Cast( componentCount )};" );
				sb.AppendLine( "i.vPositionPs.xyzw = Position3WsToPs( i.vPositionWs.xyz );" );
			}
		}

		switch ( Graph.Domain )
		{
			case ShaderDomain.Surface:
				sb.AppendLine( "return FinalizeVertex( i );" );
				break;
			case ShaderDomain.BlendingSurface:
				sb.AppendLine( "return FinalizeVertex( i );" );
				break;
			case ShaderDomain.PostProcess:
				sb.AppendLine( "return i;" );
				break;
		}
		return sb.ToString();
	}

	private string GeneratePixelOutput()
	{
		Stage = ShaderStage.Pixel;

		Subgraph = null;
		SubgraphStack.Clear();

		if ( Graph.ShadingModel == ShadingModel.Unlit || Graph.Domain == ShaderDomain.PostProcess )
		{
			var resultNode = Graph.Nodes.OfType<BaseResult>().FirstOrDefault();
			if ( resultNode == null )
				return null;

			var albedoResult = resultNode.GetAlbedoResult( this );
			string albedo = "float3( 1.0f, 1.0f, 1.0f )";
			if ( albedoResult.IsValid )
			{
				albedo = albedoResult.Cast( GetComponentCount( typeof( Vector3 ) ) ) ?? "float3( 1.0f, 1.0f, 1.0f )";
			}

			var opacityResult = resultNode.GetOpacityResult( this );
			string opacity = "1.0f";
			if ( opacityResult.IsValid )
			{
				opacity = opacityResult.Cast( 1 ) ?? "1.0f";
			}

			return $"return float4( {albedo}, {opacity} );";
		}
		else if ( Graph.ShadingModel == ShadingModel.Lit )
		{
			return ShaderTemplate.Material_output;
		}

		return null;
	}

	private string GenerateFeatures()
	{
		var sb = new StringBuilder();
		var result = ShaderResult;

		sb.AppendLine( "#include \"common/features.hlsl\"" );

		// Register any Graph level Shader Features...
		//RegisterShaderFeatures( Graph.shaderFeatureNodeResults );

		if ( Graph.Domain is ShaderDomain.BlendingSurface )
		{
			sb.AppendLine( "Feature( F_MULTIBLEND, 0..3 ( 0=\"1 Layers\", 1=\"2 Layers\", 2=\"3 Layers\", 3=\"4 Layers\", 4=\"5 Layers\" ), \"Number Of Blendable Layers\" );" );
		}

		foreach ( var feature in ShaderFeatures )
		{
			if ( feature.Value is ShaderFeatureBoolean boolFeature )
			{
				sb.AppendLine( $"Feature( {boolFeature.GetFeatureString()}, 0..1, \"{boolFeature.HeaderName}\" );" );
			}
			else if ( feature.Value is ShaderFeatureEnum enumFeature )
			{
				var optionsBody = BuildFeatureOptionsBody( enumFeature.Options );
				sb.AppendLine( $"Feature( {enumFeature.GetFeatureString()}, 0..{enumFeature.Options.Count - 1} ( {optionsBody} ), \"{enumFeature.HeaderName}\" );" );
			}
		}

		// TODO :
		//if ( Graph.FeatureRules.Any() )
		//{
		//	foreach ( var rule in Graph.FeatureRules )
		//	{
		//		if ( rule.IsValid )
		//		{
		//			sb.AppendLine( $"FeatureRule(Allow1( {String.Join( ", ", rule.Features )} ), \"{rule.HoverHint}\");" );
		//		}
		//	}
		//}

		return sb.ToString();
	}

	private string GenerateCommon()
	{
		var sb = new StringBuilder();

		if ( IsPreview )
		{
			sb.AppendLine( $"#ifndef SHADERGRAPHPLUS_PREVIEW" );
			sb.AppendLine( IndentString( $"#define SHADERGRAPHPLUS_PREVIEW", 1 ) );
			sb.AppendLine( $"#endif" );
		}

		if ( ShaderFeatures.Any() )
		{
			sb.AppendLine( $"#ifndef SWITCH_TRUE" );
			sb.AppendLine( IndentString( $"#define SWITCH_TRUE 1", 1 ) );
			sb.AppendLine( $"#endif" );

			sb.AppendLine( $"#ifndef SWITCH_FALSE" );
			sb.AppendLine( IndentString( $"#define SWITCH_FALSE 0", 1 ) );
			sb.AppendLine( $"#endif" );

			sb.AppendLine();
		}

		var blendMode = Graph.BlendMode;
		var alphaTest = blendMode == BlendMode.Masked ? 1 : 0;
		var translucent = blendMode == BlendMode.Translucent ? 1 : 0;

		sb.AppendLine( $"#ifndef S_ALPHA_TEST" );
		sb.AppendLine( IndentString( $"#define S_ALPHA_TEST {alphaTest}", 1 ) );
		sb.AppendLine( $"#endif" );

		sb.AppendLine( $"#ifndef S_TRANSLUCENT" );
		sb.AppendLine( IndentString( $"#define S_TRANSLUCENT {translucent}", 1 ) );
		sb.AppendLine( $"#endif" );

		sb.AppendLine();

		sb.AppendLine( $"#include \"common/shared.hlsl\"" );
		sb.AppendLine( $"#include \"common/gradient.hlsl\"" );
		sb.AppendLine( $"#include \"procedural.hlsl\"" );

		sb.AppendLine();

		sb.Append( $"#define S_UV2 1" );

		if ( ShaderDefines.Any() )
		{
			sb.AppendLine();
			sb.AppendLine();

			foreach ( var define in ShaderDefines )
			{
				sb.AppendLine( $"#ifndef {define.Key}" );
				sb.AppendLine( IndentString( $"#define {define.Key} {define.Value}", 1 ) );
				sb.AppendLine( $"#endif" );
			}
		}

		return sb.ToString();
	}

	private string GenerateStageInputs( ShaderStage shaderStage )
	{
		var sb = new StringBuilder();

		if ( shaderStage == ShaderStage.Vertex )
		{
			sb.AppendLine();

			foreach ( var vertexInput in VertexInputs )
			{
				if ( !ShaderTemplate.InternalVertexInputs.ContainsValue( vertexInput.Value ) )
				{
					sb.AppendLine( vertexInput.Value );
				}
			}
		}
		else
		{
			sb.AppendLine();

			foreach ( var pixelInput in PixelInputs )
			{
				if ( !ShaderTemplate.InternalPixelInputs.ContainsValue( pixelInput.Value ) )
				{
					sb.AppendLine( pixelInput.Value );
				}
			}
		}

		return sb.ToString();
	}

	private string GenerateGlobals()
	{
		var sb = new StringBuilder();

		var includes = IsVs ? VertexIncludes : PixelIncludes;

		if ( IsPs && Graph.Domain == ShaderDomain.PostProcess )
		{
			sb.AppendLine( "#include \"postprocess/functions.hlsl\"" );
			sb.AppendLine( "#include \"postprocess/common.hlsl\"" );
		}

		foreach ( var include in includes )
		{
			sb.AppendLine( $"#include \"{include}\"" );
		}

		if ( includes.Any() )
		{
			sb.AppendLine();
		}

		foreach ( var global in ShaderResult.Globals )
		{
			sb.AppendLine( global.Value );
		}

		foreach ( var feature in ShaderFeatures )
		{
			if ( IsPreview )
			{
				sb.AppendLine( $"DynamicCombo( {feature.Value.GetDynamicComboString()}, {feature.Value.GetOptionRangeString()}, Sys( ALL ) );" );
			}
			else
			{
				sb.AppendLine( $"StaticCombo( {feature.Value.GetStaticComboString()}, {feature.Value.GetFeatureString()}, Sys( ALL ) );" );
			}

			sb.AppendLine();
		}

		// Support for color buffer in post-process shaders
		if ( IsPs && Graph.Domain is ShaderDomain.PostProcess )
		{
			sb.AppendLine( "Texture2D g_tColorBuffer < Attribute( \"ColorBuffer\" ); SrgbRead ( true ); >;" );
		}

		if ( IsPreview )
		{
			foreach ( var result in ShaderResult.TextureInputs )
			{
				sb.Append( $"{result.Value.CreateTexture( result.Key )} <" )
				  .Append( $" Attribute( \"{result.Key}\" );" )
				  .Append( $" SrgbRead( {result.Value.SrgbRead} ); >;" )
				  .AppendLine();
			}

			foreach ( var result in ShaderResult.Attributes )
			{
				if ( result.Value is Texture || result.Value == null || !ShaderAttributeTypes.Contains( result.Value.GetType() ) )
					continue;

				var typeName = result.Value switch
				{
					bool _ => "bool",
					int _ => "int",
					float _ => "float",
					Vector2 _ => "float2",
					Vector3 _ => "float3",
					Vector4 _ or Color _ => "float4",
					Float2x2 _ => "float2x2",
					Float3x3 _ => "float3x3",
					Float4x4 _ => "float4x4",
					Sampler _ => "SamplerState",
					_ => null
				};

				sb.AppendLine( $"{typeName} {result.Key} < Attribute( \"{result.Key}\" ); >;" );
			}

			sb.AppendLine( "float g_flPreviewTime < Attribute( \"g_flPreviewTime\" ); >;" );
			sb.AppendLine( $"int g_iStageId < Attribute( \"g_iStageId\" ); >;" );
		}
		else
		{
			foreach ( var sampler in ShaderResult.SamplerStates )
			{
				if ( sampler.Value.IsAttribute )
				{
					sb.AppendLine( $"SamplerState g_s{sampler.Key} < Attribute( \"{sampler.Key}\" ); >;" );
				}
				else
				{
					sb.Append( $"SamplerState g_s{sampler.Key} <" )
					  .Append( $" Filter( {sampler.Value.Filter.ToString().ToUpper()} );" )
					  .Append( $" AddressU( {sampler.Value.AddressModeU.ToString().ToUpper()} );" )
					  .Append( $" AddressV( {sampler.Value.AddressModeV.ToString().ToUpper()} );" )
					  .Append( $" AddressW( {sampler.Value.AddressModeW.ToString().ToUpper()} );" )
					  .Append( $" MaxAniso( {sampler.Value.MaxAnisotropy.ToString()} ); >;" )
					  .AppendLine();
				}
			}

			foreach ( var result in ShaderResult.TextureInputs )
			{
				// If we're an attribute, we don't care about texture inputs
				if ( result.Value.IsAttribute )
					continue;

				var defaultTex = result.Value.DefaultTexture;
				sb.Append( $"{result.Value.CreateInput}( {result.Key}, {result.Value.ColorSpace}, 8," )
				  .Append( $" \"{result.Value.Processor.ToString()}\"," )
				  .Append( $" \"_{result.Value.ExtensionString.ToLower()}\"," )
				  .Append( $" \"{result.Value.UIGroup}\"," )
				  .Append( string.IsNullOrEmpty( defaultTex )
							? $" Default4( {result.Value.DefaultColor} ) );"
							: $" DefaultFile( \"{defaultTex}\" ) );" )
				  .AppendLine();
			}

			foreach ( var result in ShaderResult.TextureInputs )
			{
				// If we're an attribute, we don't care about the UI options
				if ( result.Value.IsAttribute )
				{
					sb.AppendLine( $"{result.Value.CreateTexture( result.Key )} < Attribute( \"{result.Key}\" ); >;" );
				}
				else
				{
					sb.Append( $"{result.Value.CreateTexture( result.Key )} < Channel( RGBA, Box( {result.Key} ), {(result.Value.SrgbRead ? "Srgb" : "Linear")} );" )
					  .Append( $" OutputFormat( {result.Value.ImageFormat} );" )
					  .Append( $" SrgbRead( {result.Value.SrgbRead} ); >;" )
					  .AppendLine();
				}
			}

			if ( !string.IsNullOrWhiteSpace( ShaderResult.RepresentativeTexture ) )
			{
				sb.AppendLine( $"TextureAttribute( LightSim_DiffuseAlbedoTexture, {ShaderResult.RepresentativeTexture} )" );
				sb.AppendLine( $"TextureAttribute( RepresentativeTexture, {ShaderResult.RepresentativeTexture} )" );
			}

			foreach ( var parameter in ShaderResult.Parameters )
			{
				sb.AppendLine( $"{parameter.Value.Result.TypeName} {parameter.Key} < {parameter.Value.Options} >;" );
			}
		}

		if ( sb.Length > 0 )
		{
			sb.Insert( 0, "\n" );
		}

		return sb.ToString();
	}

	private string GenerateLocals( bool noPreviewOverride = false )
	{
		var sb = new StringBuilder();

		if ( ShaderResult.Results.Any() )
		{
			sb.AppendLine();
		}

		foreach ( var gradient in ShaderResult.Gradients )
		{
			sb.AppendLine( $"Gradient {gradient.Key} = Gradient::Init();" );
			sb.AppendLine();

			var colorindex = 0;
			var alphaindex = 0;

			sb.AppendLine( $"{gradient.Key}.colorsLength = {gradient.Value.Colors.Count};" );
			sb.AppendLine( $"{gradient.Key}.alphasLength = {gradient.Value.Alphas.Count};" );

			foreach ( var color in gradient.Value.Colors )
			{
				if ( ConCommands.VerboseDebgging )
				{
					SGPLogger.Info( $"{gradient.Key} Gradient Color {colorindex} : {color.Value} Time : {color.Time}" );
				}

				// All good with time as the 4th component?
				sb.AppendLine( $"{gradient.Key}.colors[{colorindex++}] = float4( {color.Value.r}, {color.Value.g}, {color.Value.b}, {color.Time} );" );
			}

			foreach ( var alpha in gradient.Value.Alphas )
			{
				sb.AppendLine( $"{gradient.Key}.alphas[{alphaindex++}] = float( {alpha.Value} );" );
			}

			sb.AppendLine();
		}

		GenerateLocalResults( ref sb, ShaderResult.Results, out _, noPreviewOverride, 0 );

		return sb.ToString();
	}

	private void GenerateLocalResults( ref StringBuilder sb, IEnumerable<(NodeResult localResult, NodeResult funcResult)> shaderResults, out (NodeResult localResult, NodeResult funcResult) lastResult, bool noPreviewOverride = false, int indentLevel = 0 )
	{
		lastResult = (new NodeResult(), new NodeResult());

		foreach ( var result in shaderResults )
		{
			lastResult = result;
			var comboBodyStr = "";

			if ( result.funcResult.TryGetMetaData<string>( nameof( MetadataType.ComboSwitchBody ), out var comboSwitchBody ) )
			{
				comboBodyStr = comboSwitchBody;

				if ( !string.IsNullOrWhiteSpace( comboSwitchBody ) )
				{
					sb.AppendLine( IndentString( comboSwitchBody, indentLevel ) );
				}
			}

			// CustomFunction node uses this primarly.
			if ( ShaderResult.VoidFunctionLocals.Any() && ShaderResult.VoidFunctionLocals.ContainsKey( result.funcResult.VoidLocalTargetID ) )
			{
				var data = ShaderResult.VoidFunctionLocals[result.funcResult.VoidLocalTargetID];
				sb.AppendLine();

				if ( !VoidDataHashes.Contains( data.GetHashCode() ) )
				{
					VoidDataHashes.Add( data.GetHashCode() );

					// Init all the output results.
					foreach ( var outResult in data.TargetResults )
					{
						sb.AppendLine( IndentString( InitializeVariable( outResult.ResultType, outResult.CompilerAssignedName ), indentLevel ) );
					}

					sb.AppendLine( IndentString( $"{data.FunctionCall};", indentLevel ) );
					sb.AppendLine();
				}
			}

			if ( result.funcResult.ResultType != ResultType.VoidFunction )
			{
				if ( result.funcResult.ResultType == ResultType.Float2x2 ||
					 result.funcResult.ResultType == ResultType.Float3x3 ||
					 result.funcResult.ResultType == ResultType.Float4x4 )
				{
					if ( IsPreview )
					{
						sb.AppendLine( IndentString( $"{result.funcResult.TypeName} {result.localResult} = {result.funcResult.TypeName}( {result.funcResult.Code} );", indentLevel ) );
					}
					else
					{
						sb.AppendLine( IndentString( $"{result.funcResult.TypeName} {result.localResult} = {result.funcResult.Code};", indentLevel ) );
					}
				}
				else
				{
					sb.AppendLine( IndentString( $"{result.funcResult.TypeName} {result.localResult} = {result.funcResult.Code};", indentLevel ) );
				}
			}

			if ( !noPreviewOverride )
			{
				if ( IsPs && IsPreview && string.IsNullOrWhiteSpace( comboBodyStr ) )
				{
					if ( result.localResult.ResultType == ResultType.Bool )
					{
						// TODO
					}
					else if ( result.localResult.CanPreview && result.localResult.ShouldPreview && result.localResult.PreviewID != ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID )
					{
						sb.AppendLine( IndentString( $"if ( g_iStageId == {result.localResult.PreviewID} ) return {result.localResult.Cast( 4, 1.0f )};", indentLevel ) );
					}
				}
			}
		}
	}

	private string GenerateVertexComboRules()
	{
		var sb = new StringBuilder();

		if ( IsNotPreview )
			return sb.ToString();

		sb.AppendLine();
		return sb.ToString();
	}

	private string GeneratePixelComboRules()
	{
		var sb = new StringBuilder();

		if ( !IsNotPreview )
		{
			sb.AppendLine();
			sb.AppendLine( "DynamicCombo( D_RENDER_BACKFACES, 0..1, Sys( ALL ) );" );
			sb.AppendLine( "RenderState( CullMode, D_RENDER_BACKFACES ? NONE : BACK );" );
		}
		else
		{
			sb.AppendLine();
			sb.AppendLine( "RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );" );
		}

		return sb.ToString();
	}

	private string GeneratePixelInit()
	{
		Stage = ShaderStage.Pixel;
		if ( Graph.ShadingModel == ShadingModel.Lit && Graph.Domain != ShaderDomain.PostProcess )
			return ShaderTemplate.Material_init;
		return "";
	}

	public static string IndentString( string input, int tabCount )
	{
		if ( tabCount == 0 ) return input;

		if ( string.IsNullOrWhiteSpace( input ) )
			return input;

		var tabs = new string( '\t', tabCount );
		var lines = input.Split( '\n' );

		for ( int i = 0; i < lines.Length; i++ )
		{
			lines[i] = tabs + lines[i];
		}

		return string.Join( "\n", lines );
	}

	public static string CleanName( string name )
	{
		if ( string.IsNullOrWhiteSpace( name ) )
			return "";

		name = name.Trim();
		name = new string( name.Where( x => char.IsLetter( x ) || char.IsNumber( x ) || x == '_' ).ToArray() );

		return name;
	}

	private static int GetComponentCount( Type inputType )
	{
		return inputType switch
		{
			Type t when t == typeof( int ) => 1,
			Type t when t == typeof( float ) => 1,
			Type t when t == typeof( Vector2 ) => 2,
			Type t when t == typeof( Vector3 ) => 3,
			Type t when t == typeof( Vector4 ) || t == typeof( Color ) => 4,
			Type t when t == typeof( Float2x2 ) => 4,
			Type t when t == typeof( Float3x3 ) => 6,
			Type t when t == typeof( Float4x4 ) => 16,
			_ => 0
		};
	}

	private static string InitializeVariable( ResultType resultType, string name )
	{
		switch ( resultType )
		{
			case ResultType.Bool:
				return $"bool {name} = false;";
			case ResultType.Int:
				return $"int {name} = 0;";
			case ResultType.Float:
				return $"float {name} = 0.0f;";
			case ResultType.Vector2:
				return $"float2 {name} = float2( 0.0f, 0.0f );";
			case ResultType.Vector3:
				return $"float3 {name} = float3( 0.0f, 0.0f, 0.0f );";
			case ResultType.Vector4:
				return $"float4 {name} = float4( 0.0f, 0.0f, 0.0f, 0.0f );";
			case ResultType.Float2x2:
				return $"float2x2 {name} = float2x2( 0.0f, 0.0f, 0.0f, 0.0f );";
			case ResultType.Float3x3:
				return $"float3x3 {name} = float3x3( 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f );";
			case ResultType.Float4x4:
				return $"float4x4 {name} = float4x4( 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f );";
			default:
				throw new NotImplementedException( $"Unknown ResultType \"{resultType}\"" );
		}
	}

	private static ResultType GetResultTypeFromHLSLDataType( string DataType )
	{
		return DataType switch
		{
			"bool" => ResultType.Bool,
			"int" => ResultType.Int,
			"float" => ResultType.Float,
			"float2" => ResultType.Vector2,
			"float3" => ResultType.Vector3,
			"float4" => ResultType.Vector4,
			"float2x2" => ResultType.Float2x2,
			"float3x3" => ResultType.Float3x3,
			"float4x4" => ResultType.Float4x4,
			"Texture2D" => ResultType.Texture2D,
			"TextureCube" => ResultType.TextureCube,
			"SamplerState" => ResultType.Sampler,
			_ => throw new ArgumentException( $"Unknown DataType `{DataType}`" )
		};
	}

	private static IEnumerable<PropertyInfo> GetNodeInputProperties( Type type )
	{
		return type.GetProperties( BindingFlags.Instance | BindingFlags.Public )
			.Where( property => property.GetSetMethod() != null &&
			property.PropertyType == typeof( NodeInput ) &&
			property.IsDefined( typeof( BaseNodePlus.InputAttribute ), false ) );
	}

	private static string GenerateFunctions( CompileResult result )
	{
		if ( !result.Functions.Any() )
			return null;

		var sb = new StringBuilder();
		foreach ( var function in result.Functions )
		{
			if ( GraphHLSLFunctions.TryGetFunction( function, out var code ) )
			{
				sb.AppendLine();
				sb.Append( code );
			}
		}

		return sb.ToString();
	}
}
