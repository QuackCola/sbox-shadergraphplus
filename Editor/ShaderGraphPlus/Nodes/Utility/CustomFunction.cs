using System.Text;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus.Nodes;

[AttributeUsage( AttributeTargets.Property )]
internal sealed class HLSLAssetPathAttribute : Attribute
{
	internal HLSLAssetPathAttribute()
	{
	}
}

public enum CustomCodeNodeMode
{
	/// <summary>
	/// Generate a function and place it within the generated shader
	/// in whichever stage the node is used in
	/// </summary>
	Generate,
	/// <summary>
	/// Retrive the function from an external hlsl include file
	/// </summary>
	File,
}

public enum CompatableShaderStage
{
	/// <summary>
	/// Both shader stages
	/// </summary>
	Both,
	/// <summary>
	/// Just the Vetex shader stage
	/// </summary>
	Vertex,
	/// <summary>
	/// Just the Pixel shader stage
	/// </summary>
	Pixel,
}

/// <summary>
/// Inject your own custom HLSL code into ShaderGraphPlus
/// </summary>
[Title( "Custom Function" ), Category( "Utility" ), Icon( "code" )]
public sealed class CustomFunctionNode : ShaderNodePlus, IErroringNode, IWarningNode, BaseNodePlus.IInitializeNode
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool AutoSize => true;

	[Hide]
	public override string Title => string.IsNullOrEmpty( Name ) ?
	$"{DisplayInfo.For( this ).Name}" :
	$"{DisplayInfo.For( this ).Name} ({Name})";

	[Hide, JsonIgnore]
	public override bool CanPreview => false;

	public string Name { get; set; } = "CustomFunction0";

	public CustomCodeNodeMode Mode { get; set; } = CustomCodeNodeMode.Generate;

	[TextArea]
	[HideIf( nameof( Mode ), CustomCodeNodeMode.File )]
	public string Code { get; set; } = $"\nOutFloat3_0 = float3( 1.0f, 0.0f, 1.0f );";

	[HideIf( nameof( Mode ), CustomCodeNodeMode.Generate )]
	[HLSLAssetPath]
	public string Source { get; set; }

	[Hide]
	public string CodeComment { get; set; } = "";

	[Title( "Inputs" )]
	public List<CustomFunctionNodePort> FunctionInputs { get; set; } = new List<CustomFunctionNodePort>()
	{
		{
			new CustomFunctionNodePort
			{
				Name = "InFloat3_0",
				TypeName = "Vector3"
			}
		},
	};

	[Hide]
	private List<IPlugIn> InternalInputs = new();

	[Hide]
	public override IEnumerable<IPlugIn> Inputs => InternalInputs;

	[Title( "Outputs" )]
	public List<CustomFunctionNodePort> FunctionOutputs { get; set; } = new List<CustomFunctionNodePort>()
	{
		{
			new CustomFunctionNodePort
			{
				Name = "OutFloat3_0",
				TypeName = "Vector3"
			}
		},
	};

	[Hide]
	private List<IPlugOut> InternalOutputs = new();

	[Hide]
	public override IEnumerable<IPlugOut> Outputs => InternalOutputs;

	[Hide, JsonIgnore]
	int _lastHashCodeInputs = 0;

	[Hide, JsonIgnore]
	int _lastHashCodeOutputs = 0;

	public override void OnFrame()
	{
		var hashCodeInput = 0;
		var hashCodeOutput = 0;

		foreach ( var input in FunctionInputs )
		{
			hashCodeInput += input.GetHashCode();
		}

		foreach ( var output in FunctionOutputs )
		{
			hashCodeOutput += output.GetHashCode();
		}

		if ( hashCodeInput != _lastHashCodeInputs )
		{
			_lastHashCodeInputs = hashCodeInput;

			CreateInputs();
			Update();

			IsDirty = true;
		}

		if ( hashCodeOutput != _lastHashCodeOutputs )
		{
			_lastHashCodeOutputs = hashCodeOutput;

			CreateOutputs();
			Update();

			IsDirty = true;
		}
	}

	public void InitializeNode()
	{
		OnNodeCreated();
	}

	private void OnNodeCreated()
	{
		CreateInputs();
		CreateOutputs();

		Update();

		IsDirty = true;
	}

	public void CreateInputs()
	{
		var plugs = new List<IPlugIn>();

		if ( FunctionInputs == null )
		{
			InternalInputs = new();
		}
		else
		{
			foreach ( var input in FunctionInputs.OrderBy( x => x.Priority ) )
			{
				if ( !input.IsValid ) continue;

				var info = new PlugInfo()
				{
					Id = input.Id,
					Name = input.Name,
					Type = input.Type,
					DisplayInfo = new()
					{
						Name = input.Name,
						Fullname = input.Type.FullName
					}
				};

				var plug = new BasePlugIn( this, info, info.Type );
				var oldPlug = InternalInputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Id == info.Id ) as BasePlugIn;

				if ( oldPlug is not null )
				{
					oldPlug.Info.Name = info.Name;
					oldPlug.Info.Type = info.Type;
					oldPlug.Info.DisplayInfo = info.DisplayInfo;

					if ( oldPlug.Type != plug.Type )
					{
						plugs.Add( plug );
					}
					else
					{
						plugs.Add( oldPlug );
					}
				}
				else
				{
					plugs.Add( plug );
				}
			}

			InternalInputs = plugs;
		}
	}

	public void CreateOutputs()
	{
		var outPlugs = new List<IPlugOut>();

		if ( FunctionOutputs == null )
		{
			InternalOutputs = new();
		}
		else
		{
			foreach ( var output in FunctionOutputs.OrderBy( x => x.Priority ) )
			{
				if ( !output.IsValid ) continue;

				var info = new PlugInfo()
				{
					Id = output.Id,
					Name = output.Name,
					Type = output.Type,
					DisplayInfo = new()
					{
						Name = output.Name,
						Fullname = output.Type.FullName
					}
				};

				var plug = new BasePlugOut( this, info, info.Type );
				var oldPlug = InternalOutputs.FirstOrDefault( x => x is BasePlugOut plugOut && plugOut.Info.Id == info.Id ) as BasePlugOut;
				if ( oldPlug is not null )
				{
					oldPlug.Info.Name = info.Name;
					oldPlug.Info.Type = info.Type;
					oldPlug.Info.DisplayInfo = info.DisplayInfo;

					if ( oldPlug.Type != plug.Type )
					{
						outPlugs.Add( plug );
					}
					else
					{
						outPlugs.Add( oldPlug );
					}
				}
				else
				{
					outPlugs.Add( plug );
				}
			}

			InternalOutputs = outPlugs;
		}
	}

	public NodeResult GetResult( GraphCompiler compiler )
	{
		if ( Mode is CustomCodeNodeMode.File )
		{
			var functionInputs = GetFunctionInputs( compiler, out var inputErrors );

			if ( inputErrors.Any() )
			{
				return default;
			}

			compiler.RegisterInclude( Source );
			var outputResults = GetFunctionOutputs( out var outputErrors );

			if ( outputErrors.Any() )
			{
				return default;
			}

			/*
			// Update shader function with any input and output changes.
			if ( !string.IsNullOrWhiteSpace( Source ) && Editor.FileSystem.Content.FileExists( $"shaders/{Source}" ) )
			{
				string newFunctionHeader = $"	void {Name}({ConstructArguments( ExpressionInputs, false )}{(ExpressionInputs.Any() ? "," : "")}{ConstructArguments( ExpressionOutputs, true )})";
				var shaderPath = Editor.FileSystem.Content.GetFullPath( $"shaders/{Source}" );
			
				var lines = File.ReadAllLines( shaderPath );
			
				for ( int i = 0; i < lines.Length; i++ )
				{
					if ( lines[i].TrimStart().StartsWith( $"void {Name}" ) )
					{
						if ( lines[i] != newFunctionHeader )
						{
							SGPLog.Info( $"Updating function" );
							lines[i] = "";
							lines[i] = newFunctionHeader;
						}
						else
						{
							SGPLog.Info( $"No need to update function" );
						}
			
						break;
					}
				}
			
				File.WriteAllLines( shaderPath, lines );
			}
			*/

			string funcCall = compiler.RegisterCustomCode( Identifier, Name, functionInputs, outputResults, Mode );

			return new NodeResult( ResultType.VoidFunction, funcCall, true );
		}
		else if ( Mode is CustomCodeNodeMode.Generate )
		{
			var sb = new StringBuilder();

			sb.AppendLine( $"void {Name}({ConstructArguments( FunctionInputs, false )}{(FunctionInputs.Any() ? "," : "")}{ConstructArguments( FunctionOutputs, true )})" );
			sb.AppendLine( "{" );
			sb.AppendLine( GraphCompiler.IndentString( Code, 1 ) );
			sb.AppendLine( "}" );

			var funcName = compiler.RegisterHLSLFunction( sb.ToString(), Name, true );
			var funcInputs = GetFunctionInputs( compiler, out var inputErrors );

			if ( inputErrors.Any() )
			{
				return NodeResult.Error( inputErrors.ToArray() );
			}

			var funcOutputs = GetFunctionOutputs( out var outputErrors );

			if ( outputErrors.Any() )
			{
				return NodeResult.Error( outputErrors.ToArray() );
			}

			var funcCall = compiler.RegisterCustomCode( Identifier, funcName, funcInputs, funcOutputs, Mode );

			return new NodeResult( ResultType.VoidFunction, funcCall, true );
		}

		return NodeResult.Error( $"Unknown Mode '{Mode}'" );
	}

	/// <summary>
	/// Fetches the results from the user defined node inputs.
	/// </summary>
	private string GetFunctionInputs( GraphCompiler compiler, out List<string> errors )
	{
		StringBuilder sb = new StringBuilder();
		int index = 0;
		errors = new List<string>();

		foreach ( IPlugIn input in Inputs )
		{
			var result = "";
			if ( input.ConnectedOutput is null )
			{
				switch ( input.Type )
				{
					case Type t when t == typeof( bool ):
						result = $"false";
						break;
					case Type t when t == typeof( int ):
						result = $"0";
						break;
					case Type t when t == typeof( float ):
						result = $"0.0f";
						break;
					case Type t when t == typeof( Vector2 ):
						result = $"float2( 0.0f, 0.0f )";
						break;
					case Type t when t == typeof( Vector3 ):
						result = $"float3( 0.0f, 0.0f, 0.0f )";
						break;
					case Type t when t == typeof( Vector4 ) || t == typeof( Color ):
						result = $"float4( 0.0f, 0.0f, 0.0f, 0.0f )";
						break;
					case Type t when t == typeof( Float2x2 ):
						result = $"float2x2( 0.0f, 0.0f, 0.0f, 0.0f )";
						break;
					case Type t when t == typeof( Float3x3 ):
						result = $"float3x3( 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f )";
						break;
					case Type t when t == typeof( Float4x4 ):
						result = $"float4x4( 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f )";
						break;
					default:
						errors.Add( $"Required Input \"{input.DisplayInfo.Name}\" of type \"{ShaderGraphPlusTheme.NodeHandleConfigs[input.Type].Name}\" is missing" );
						continue;
				}
			}
			else
			{
				NodeInput nodeInput = new NodeInput { Identifier = input.ConnectedOutput.Node.Identifier, Output = input.ConnectedOutput.Identifier };

				var nodeResult = compiler.Result( nodeInput );
				result = nodeResult.ToString();

				if ( (nodeResult.ResultType == ResultType.Texture2D || nodeResult.ResultType == ResultType.TextureCube) && compiler.TryGetPreviewImage( result, out var imagePath ) )
				{
					var texturePath = compiler.CompileTexture( new TextureInput
					{
						DefaultTexture = imagePath,
						Type = nodeResult.ResultType == ResultType.Texture2D ? TextureType.Tex2D : TextureType.TexCube,
						ImageFormat = TextureFormat.DXT5,
						SrgbRead = true,
						DefaultColor = Color.White,
					} );

					var attributeName = result.TrimStart( "g_t" ).ToString();
					compiler.SetAttribute( attributeName, Texture.Load( texturePath ) );
				}
			}

			if ( index < Inputs.Count() - 1 )
			{
				sb.Append( $"{result}, " );
			}
			else
			{
				sb.Append( $"{result}" );
			}

			index++;
		}

		if ( errors.Any() )
			return null;

		return sb.ToString();
	}

	internal string ConstructArguments( List<CustomFunctionNodePort> ports, bool isOutputs )
	{
		var sb = new StringBuilder();

		for ( int index = 0; index < ports.Count; index++ )
		{
			var argument = ports[index];
			var keyword = isOutputs ? "out" : "";

			if ( index == ports.Count - 1 )
			{
				sb.Append( $"{(isOutputs ? " " : "")}{keyword} {argument.HLSLDataType} {argument.Name}{(isOutputs ? " " : "")}" );
			}
			else
			{
				sb.Append( $"{(isOutputs ? " " : "")}{keyword} {argument.HLSLDataType} {argument.Name},{(isOutputs ? " " : "")}" );
			}
		}

		return sb.ToString();
	}

	private Dictionary<string, string> GetFunctionOutputs( out List<string> errors )
	{
		Dictionary<string, string> result = new();
		errors = new List<string>();

		foreach ( CustomFunctionNodePort output in FunctionOutputs )
		{
			if ( string.IsNullOrWhiteSpace( output.Name ) || result.ContainsKey( output.Name ) )
			{
				continue;
			}

			result.Add( output.Name, output.HLSLDataType );
		}

		return result;
	}

	public List<string> GetErrors()
	{
		ClearError();

		var errors = new List<string>();

		var inputTypeInputError = $"Input{{0}}must have a Type.";
		var missingInputNameError = $"Input of type '{{0}}' must have a name.";
		var duplicateInputNameError = $"Duplicate input '{{0}}'.";
		var conflictingNameInputError = $"Input '{{0}}' already exists as an output.";

		var noOutputsOutputError = $"must have atleast 1 output.";
		var outputTypeOutputError = $"Output{{0}}must have a Type.";
		var missingOutputNameError = $"Output of type '{{0}}' must have a name.";
		var duplicateOutputNameError = $"Duplicate output '{{0}}'.";
		var conflictingNameOutputError = $"Output '{{0}}' already exists as an input.";

		var missingFunctionNameError = $"Must have a function name.";
		var missingSourceFilePathError = $"Source path is empty.";
		var missingSourceFileError = $"Include file `shaders/{Source}` does not exist.";

		if ( !FunctionOutputs.Any() )
		{
			errors.Add( noOutputsOutputError );
		}

		foreach ( var input in FunctionInputs )
		{
			var inputTypeInputErrorFm = string.Format( inputTypeInputError, $"{(!string.IsNullOrWhiteSpace( input.Name ) ? $" '{input.Name}' " : " ")}" );
			var missingInputNameErrorFm = string.Format( missingInputNameError, input.TypeName );
			var duplicateInputNameErrorFm = string.Format( duplicateInputNameError, input.Name );
			var conflictingNameInputErrorFm = string.Format( conflictingNameInputError, input.Name );

			if ( input.TypeName == null && !errors.Contains( inputTypeInputErrorFm ) )
			{
				errors.Add( inputTypeInputErrorFm );
			}
			else if ( string.IsNullOrWhiteSpace( input.Name ) && !errors.Contains( missingInputNameErrorFm ) )
			{
				errors.Add( missingInputNameErrorFm );
			}

			if ( !string.IsNullOrWhiteSpace( input.Name ) )
			{
				if ( FunctionInputs.Any( x => x != input && string.Equals( x.Name, input.Name, StringComparison.CurrentCultureIgnoreCase ) ) )
				{
					errors.Add( duplicateInputNameErrorFm );
				}

				if ( FunctionOutputs.Any( x => string.Equals( x.Name, input.Name, StringComparison.CurrentCultureIgnoreCase ) ) && !errors.Contains( conflictingNameInputErrorFm ) )
				{
					errors.Add( conflictingNameInputErrorFm );
				}
			}
		}

		foreach ( var output in FunctionOutputs )
		{
			var outputTypeOutputErrorFm = string.Format( outputTypeOutputError, $"{(!string.IsNullOrWhiteSpace( output.Name ) ? $" '{output.Name}' " : " ")}" );
			var missingOutputNameErrorFm = string.Format( missingOutputNameError, output.TypeName );
			var duplicateOutputNameErrorFm = string.Format( duplicateOutputNameError, output.Name );
			var conflictingNameOutputErrorFm = string.Format( conflictingNameOutputError, output.Name );

			if ( output.TypeName == null && !errors.Contains( outputTypeOutputErrorFm ) )
			{
				errors.Add( outputTypeOutputErrorFm );
			}
			else if ( string.IsNullOrWhiteSpace( output.Name ) && !errors.Contains( missingOutputNameErrorFm ) )
			{
				errors.Add( missingOutputNameErrorFm );
			}

			if ( !string.IsNullOrWhiteSpace( output.Name ) )
			{
				if ( FunctionOutputs.Any( x => x != output && string.Equals( x.Name, output.Name, StringComparison.InvariantCultureIgnoreCase ) ) )
				{
					errors.Add( duplicateOutputNameErrorFm );
				}

				if ( FunctionInputs.Any( x => string.Equals( x.Name, output.Name, StringComparison.InvariantCultureIgnoreCase ) ) && !errors.Contains( conflictingNameOutputErrorFm ) )
				{
					errors.Add( conflictingNameOutputErrorFm );
				}
			}
		}

		if ( string.IsNullOrWhiteSpace( Name ) )
		{
			errors.Add( missingFunctionNameError );
		}

		if ( Mode is CustomCodeNodeMode.File )
		{
			if ( string.IsNullOrWhiteSpace( Source ) )
			{
				errors.Add( missingSourceFilePathError );
			}
			else
			{
				if ( !Editor.FileSystem.Content.FileExists( $"shaders/{Source}" ) )
				{
					errors.Add( missingSourceFileError );
				}
			}
		}

		return errors;
	}

	public List<string> GetWarnings()
	{
		var warnings = new List<string>();

		return warnings;
	}
}

public class CustomFunctionNodePort : IValid
{
	[Hide]
	public Guid Id { get; } = Guid.NewGuid();

	[Hide, JsonIgnore]
	public bool IsValid => !string.IsNullOrWhiteSpace( Name ) && Type != null;

	[KeyProperty]
	public string Name { get; set; }

	[Hide, JsonIgnore]
	public Type Type
	{
		get
		{
			if ( string.IsNullOrEmpty( TypeName ) ) return null;
			var portTypeName = TypeName;

			Type type = null;
			if ( portTypeName == "bool" ) type = typeof( bool );
			if ( portTypeName == "int" ) type = typeof( int );
			if ( portTypeName == "float" ) type = typeof( float );
			if ( portTypeName == "Vector2" ) type = typeof( Vector2 );
			if ( portTypeName == "Vector3" ) type = typeof( Vector3 );
			if ( portTypeName == "Vector4" ) type = typeof( Vector4 );
			if ( portTypeName == "Color" ) type = typeof( Color );
			if ( portTypeName == "float2x2" ) type = typeof( Float2x2 );
			if ( portTypeName == "float3x3" ) type = typeof( Float3x3 );
			if ( portTypeName == "float4x4" ) type = typeof( Float4x4 );
			if ( portTypeName == "Texture2D" || portTypeName == "TextureCube" ) type = typeof( Texture );
			if ( portTypeName == "SamplerState" ) type = typeof( Sampler );

			// Try getting type from EditorTypeLibrary.
			if ( type != null && GraphCompiler.ValueTypes.TryGetValue( type, out var isEditorType ) && isEditorType )
			{
				return EditorTypeLibrary.GetType( type.FullName ).TargetType;
			}

			if ( portTypeName == "bool" ) portTypeName = typeof( bool ).FullName;
			if ( portTypeName == "int" ) portTypeName = typeof( int ).FullName;
			if ( portTypeName == "float" ) portTypeName = typeof( float ).FullName;

			type = TypeLibrary.GetType( portTypeName ).TargetType;

			return type;
		}
	}

	[KeyProperty, Editor( ControlWidgetCustomEditors.PortTypeChoiceEditor ), JsonPropertyName( "Type" )]
	public string TypeName { get; set; }

	public int Priority { get; set; }

	[Hide, JsonIgnore]
	public string HLSLDataType
	{
		get
		{
			switch ( TypeName )
			{
				case "bool":
					return "bool";
				case "int":
					return $"int";
				case "float":
					return $"float";
				case "Vector2":
					return $"float2";
				case "Vector3":
					return $"float3";
				case "Vector4":
					return $"float4";
				case "Color":
					return $"float4";
				case "float2x2":
					return "float2x2";
				case "float3x3":
					return "float3x3";
				case "float4x4":
					return "float4x4";
				case "Texture2D":
					return "Texture2D";
				case "TextureCube":
					return "TextureCube";
				case "SamplerState":
					return "SamplerState";
				default:
					return "";

			}
		}
	}

	[Hide, JsonIgnore]
	internal bool IsOutput { get; set; }

	public override int GetHashCode()
	{
		return System.HashCode.Combine( Id, Name, TypeName, Priority );
	}
}
