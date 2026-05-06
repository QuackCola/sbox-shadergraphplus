using System.Text;
using ShaderGraphPlus.Internal;

namespace ShaderGraphPlus;

public struct FeatureRule : IValid
{
	/// <summary>
	/// Features bound to this rule.
	/// </summary>
	[InlineEditor( Label = false )]
	public List<string> Features { get; set; }

	/// <summary>
	/// Text hint when hovering over features
	/// </summary>
	public string HoverHint { get; set; }

	[Hide, JsonIgnore]
	public bool IsValid
	{
		get
		{
			if ( Features.Any() )
			{
				foreach ( var feature in Features )
				{
					if ( string.IsNullOrWhiteSpace( feature ) )
					{
						return false;
					}
					else
					{
						continue;
					}
				}

				return true;
			}
			else
			{
				return false;
			}
		}
	}

	public FeatureRule()
	{
		Features = new();
		HoverHint = string.Empty;
	}
}

public sealed partial class GraphCompiler
{
	private partial class CompileResult
	{
		public List<string> ShaderFeatureResultStrings { get; set; } = new();
	}

	/// <summary>
	/// Registerd ShaderFeatures
	/// </summary>
	public Dictionary<string, ShaderFeatureBase> ShaderFeatures = new();

	struct SwitchBlockResultHolder
	{
		public string GeneratedLocals { get; private set; }
		public NodeResult Result { get; private set; }

		public SwitchBlockResultHolder( string locals, NodeResult nodeResult )
		{
			GeneratedLocals = locals;
			Result = nodeResult;
		}
	}

	public void SetComboPreview( string comboName, int preview )
	{
		if ( IsNotPreview )
		{
			SGPLogger.Warning( $"{nameof( SetComboPreview )} was called when IsPreview is false!" );
			return;
		}

		Assert.True( comboName.StartsWith( $"D_" ), $"Cannot set a static combo \"{comboName}\"" );

		var dynamicComboWrapper = new DynamicComboWrapper( comboName, preview );
		OnAttribute?.Invoke( dynamicComboWrapper.ComboName, dynamicComboWrapper );
	}

	public NodeResult ResultFeatureSwitch( IEnumerable<NodeInput> inputs, ShaderFeatureBase shaderFeature, int previewInt )
	{
		var sb = new StringBuilder();
		var blockResults = new List<SwitchBlockResultHolder>();

		foreach ( var input in inputs )
		{
			if ( !input.IsValid )
			{
				blockResults.Add( new SwitchBlockResultHolder( $"float l_0 = 1.0f;", new NodeResult( ResultType.Float, "l_0" ) ) );
			}
			else
			{
				blockResults.Add( GenerateSwitchBlockCode( input, input.Identifier, shaderFeature ) );
			}
		}

		if ( !blockResults.Any() )
			return default;

		var resultType = blockResults.Select( x => x.Result.ResultType ).Where( x => x != ResultType.Invalid && !((int)x > 6) ).Max();
		var id = 0;

		while ( ShaderResult.ShaderFeatureResultStrings.Contains( $"{shaderFeature.Name}_result{id}" ) )
		{
			id++;
		}

		var resultAssignmentLocal = $"{shaderFeature.Name}_result{id}";
		var resultDataType = resultType switch
		{
			ResultType.Bool => "bool",
			ResultType.Int => "int",
			ResultType.Float => "float",
			ResultType.Vector2 => "float2",
			ResultType.Vector3 => "float3",
			ResultType.Vector4 => "float4",
			ResultType.Float2x2 => "float2x2",
			ResultType.Float3x3 => "float3x3",
			ResultType.Float4x4 => "float4x4",
			ResultType.Sampler => "SamplerState",
			ResultType.Texture2D => "Texture2D",
			ResultType.TextureCube => "TextureCube",
			ResultType.Gradient => "Gradient",
			_ => throw new Exception( $"Unsupported ResultType `{resultType}`" ),
		};
		var resultTypeComponents = blockResults.Select( x => x.Result.Components ).Max();

		foreach ( var (index, result) in blockResults.Index() )
		{
			var lastResult = new NodeResult( resultType, result.Result.Cast( resultTypeComponents ) );

			if ( index == 0 )
			{
				sb.AppendLine( InitializeVariable( resultType, resultAssignmentLocal ) ); //$"{resultDataType} {resultLocal};" );

				if ( shaderFeature is ShaderFeatureBoolean boolFeature )
				{
					sb.AppendLine( $"#if ( {(IsPreview ? "D" : "S")}_{shaderFeature.Name.ToUpper()} == SWITCH_TRUE )" );
				}
				else
				{
					sb.AppendLine( $"#if ( {(IsPreview ? "D" : "S")}_{shaderFeature.Name.ToUpper()} == {index} )" );
				}

				BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
			}

			if ( shaderFeature is ShaderFeatureBoolean )
			{
				if ( index == blockResults.Count - 1 )
				{
					sb.AppendLine( $"#else" );
					BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
					sb.AppendLine( $"#endif" );
				}
			}
			else
			{
				if ( index != 0 && index != blockResults.Count - 1 )
				{
					sb.AppendLine( $"#elif ( {(IsPreview ? "D" : "S")}_{shaderFeature.Name.ToUpper()} == {index} )" );
					BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
				}
				else if ( index == blockResults.Count - 1 )
				{
					sb.AppendLine( $"#elif ( {(IsPreview ? "D" : "S")}_{shaderFeature.Name.ToUpper()} == {index} )" );
					BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
					sb.AppendLine( $"#endif" );
				}
			}
		}

		//SGPLog.Info( $"Generated Switch D_{shaderFeature.Name.ToUpper()}: \n {sb.ToString()}" );

		// TODO : Once SceneObject.Attributes.SetFeature is added. Replace SetComboPreview with something like SetFeaturePreview.
		if ( IsPreview )
		{
			SetComboPreview( shaderFeature.GetDynamicComboString(), previewInt );
		}

		var finalResult = new NodeResult( resultType, resultAssignmentLocal );
		finalResult.AddMetadataEntry( nameof( MetadataType.ComboSwitchBody ), sb.ToString() );

		if ( !ShaderResult.ShaderFeatureResultStrings.Contains( resultAssignmentLocal ) )
		{
			ShaderResult.ShaderFeatureResultStrings.Add( resultAssignmentLocal );
		}

		return finalResult;
	}

	private SwitchBlockResultHolder GenerateSwitchBlockCode( NodeInput input, string blockName, ShaderFeatureBase shaderFeature )
	{
		var outerResult = ShaderResult;
		var outerInputStack = InputStack;

		if ( IsVs )
		{
			VertexResult = new();
			VertexResult.Replace( outerResult.Globals, outerResult.Attributes, outerResult.Functions );
		}
		else
		{
			PixelResult = new();
			PixelResult.Replace( outerResult.Globals, outerResult.Attributes, outerResult.Functions );
		}

		InputStack = new();

		var result = Result( input );
		var codeBlock = GenerateLocals( true );

		foreach ( var samplerState in ShaderResult.SamplerStates )
		{
			outerResult.SamplerStates[samplerState.Key] = samplerState.Value;
		}

		foreach ( var textureInput in ShaderResult.TextureInputs )
		{
			outerResult.TextureInputs[textureInput.Key] = textureInput.Value;
		}

		foreach ( var gradient in ShaderResult.Gradients )
		{
			outerResult.Gradients[gradient.Key] = gradient.Value;
		}

		foreach ( var parameter in ShaderResult.Parameters )
		{
			outerResult.Parameters[parameter.Key] = parameter.Value;
		}

		foreach ( var attribute in ShaderResult.Attributes )
		{
			outerResult.Attributes[attribute.Key] = attribute.Value;
		}

		if ( IsVs )
		{
			VertexResult = outerResult;
		}
		else
		{
			PixelResult = outerResult;
		}
		InputStack = outerInputStack;

		//SGPLogger.Info( $"GeneratedBlock {blockName} : \n {{ {IndentString( codeBlock, 1)} \n }}" );

		return new( codeBlock, result );
	}

	private static StringBuilder BuildSwitchBlock( StringBuilder sb, string generatedLocals, string resultAssignmentLocal, NodeResult lastResult )
	{
		sb.AppendLine( $"{{" );
		sb.AppendLine( $"{IndentString( generatedLocals, 1 )}" );
		sb.AppendLine( $"{IndentString( $"{resultAssignmentLocal} = {lastResult}", 1 )};" );
		sb.AppendLine( $"}}" );

		return sb;
	}

	private static string BuildFeatureOptionsBody( List<ShaderFeatureEnumOption> options )
	{
		var options_body = "";
		int count = 0;

		foreach ( var option in options )
		{
			if ( count == 0 ) // first option starts at 0 :)
			{
				options_body += $"0=\"{option.Name}\", ";
				count++;
			}
			else if ( count != (options.Count - 1) )  // These options dont get the privilege of being the first >:)
			{
				options_body += $"{count}=\"{option.Name}\", ";
				count++;
			}
			else // Last option in the list oh well...:(
			{
				options_body += $"{count}=\"{option.Name}\"";
			}
		}

		return options_body;
	}
}
