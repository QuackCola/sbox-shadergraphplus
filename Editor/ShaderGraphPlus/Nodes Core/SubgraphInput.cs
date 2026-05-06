using Editor;

namespace ShaderGraphPlus;

public enum SubgraphPortType
{
	[Hide]
	Invalid,
	[Icon( "check_box" )]
	Bool,
	[Icon( "looks_one" )]
	Int,
	[Icon( "looks_one" )]
	Float,
	[Title( "Float2" ), Icon( "looks_two" )]
	Vector2,
	[Title( "Float3" ), Icon( "looks_3" )]
	Vector3,
	[Title( "Float4" ), Icon( "looks_4" )]
	Vector4,
	[Title( "Color" ), Icon( "palette" )]
	Color,
	[Title( "Float2x2" ), Icon( "apps" )]
	Float2x2,
	[Title( "Float3x3" ), Icon( "apps" )]
	Float3x3,
	[Title( "Float4x4" ), Icon( "apps" )]
	Float4x4,
	[Title( "Gradient" ), Icon( "gradient" )]
	Gradient,
	[Title( "Sampler State" ), Icon( "colorize" )]
	SamplerState,
	[Title( "Texture2D" ), Icon( "texture" )]
	Texture2DObject,
	[Title( "TextureCube" ), Icon( "view_in_ar" )]
	TextureCubeObject
}

/// <summary>
/// Input of a Subgraph.
/// </summary>
[Title( "Subgraph Input" ), Icon( "input" ), SubgraphOnly]
[InternalNode]
public sealed class SubgraphInput : ShaderNodePlus, IParameterNode, IErroringNode
{
	[Hide]
	public override string Title => string.IsNullOrWhiteSpace( Name ) ?
	$"Subgraph Input" :
	$"{Name} ({InputType})";

	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.SubgraphNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool AutoSize => true;

	[Hide, JsonIgnore]
	public override bool CanPreview => false;

	//[Hide, JsonIgnore]
	//private bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	[Hide]
	public Guid ParameterIdentifier { get; set; }

	//[Hide]
	//private bool IsPreviewInputEnabled => InputType != SubgraphPortType.Texture2DObject;

	[Input, Title( "Preview" ), Hide]
	public NodeInput PreviewInput { get; set; }

	/// <summary>
	/// The name of the input parameter
	/// </summary>
	[Hide, JsonIgnore]
	[Title( "Input Name" )]
	public string Name => GetParameter().Name;

	/// <summary>
	/// Description of what this input does
	/// </summary>
	[Hide, JsonIgnore]
	public string InputDescription => GetParameter().InputDescription;

	/// <summary>
	/// The type of the input parameter
	/// </summary>
	[Hide, JsonIgnore]
	public SubgraphPortType InputType => GetParameter().InputType;

	//[Editor( "subgraphInputDefaultValue" )]
	[Hide, JsonIgnore]
	public object DefaultValue
	{
		get => GetParameter().GetValue();
		set
		{
			if ( Graph is ShaderGraphPlus graph )
			{
				graph.UpdateParameterValue( ParameterIdentifier, value );

				Update();
				IsDirty = true;
			}
		}
	}

	/// <summary>
	/// Whether this input is required (must have a connection in order to compile)
	/// </summary>
	[Hide, JsonIgnore]
	public bool IsRequired => GetParameter().IsRequired;

	/// <summary>
	/// The order of this input port on the subgraph node
	/// </summary>
	[Hide, JsonIgnore]
	public int PortOrder => GetParameter().PortOrder;

	public SubgraphInput()
	{
	}

	[Hide, JsonIgnore]
	int _lastNameHashCode = 0;

	public override void OnFrame()
	{
		var nameHashCode = 0;
		nameHashCode += GetParameter().Name.GetHashCode();

		if ( nameHashCode != _lastNameHashCode )
		{
			_lastNameHashCode = nameHashCode;
			Update();
		}
	}

	/// <summary>
	/// Output for the input value
	/// </summary>
	[Output, Title( "Value" ), Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		// In subgraphs, check if preview input is connected
		if ( compiler.Graph.IsSubgraph && PreviewInput.IsValid )
		{
			return compiler.Result( PreviewInput );
		}

		// Use the appropriate default value based on input type
		var outputValue = DefaultValue;
		var resultType = InputType switch
		{
			SubgraphPortType.Bool => ResultType.Bool,
			SubgraphPortType.Int => ResultType.Int,
			SubgraphPortType.Float => ResultType.Float,
			SubgraphPortType.Vector2 => ResultType.Vector2,
			SubgraphPortType.Vector3 => ResultType.Vector3,
			SubgraphPortType.Vector4 => ResultType.Vector4,
			SubgraphPortType.Color => ResultType.Vector4,
			SubgraphPortType.Float2x2 => ResultType.Float2x2,
			SubgraphPortType.Float3x3 => ResultType.Float3x3,
			SubgraphPortType.Float4x4 => ResultType.Float4x4,
			SubgraphPortType.Gradient => ResultType.Gradient,
			SubgraphPortType.SamplerState => ResultType.Sampler,
			SubgraphPortType.Texture2DObject => ResultType.Texture2D,
			SubgraphPortType.TextureCubeObject => ResultType.TextureCube,
			_ => throw new NotImplementedException( $"Unknown InputType \"{InputType}\"" ),
		};

		// If we're in a subgraph context, just return the value directly
		if ( compiler.Graph.IsSubgraph )
		{
			if ( InputType == SubgraphPortType.Texture2DObject || InputType == SubgraphPortType.TextureCubeObject )
			{
				return new NodeResult( resultType, ProcessTexture2D( compiler, (TextureInput)outputValue, InputType == SubgraphPortType.Texture2DObject ), true );
			}

			if ( InputType == SubgraphPortType.Gradient )
			{
				var gradientValue = (Gradient)outputValue;

				if ( gradientValue.Colors.Count > 8 )
				{
					return NodeResult.Error( $"{DisplayInfo.Name} has {gradientValue.Colors.Count} color keys which is greater than the maximum amount of 8 allowed color keys." );
				}
				else
				{
					// Register gradient with the compiler.
					var result = compiler.RegisterGradient( gradientValue, Name );

					return new NodeResult( ResultType.Gradient, result, constant: true );
				}
			}

			return compiler.ResultValue( outputValue );
		}
		else
		{
			if ( InputType == SubgraphPortType.Texture2DObject || InputType == SubgraphPortType.TextureCubeObject )
			{
				return new NodeResult( resultType, ProcessTexture2D( compiler, (TextureInput)outputValue, InputType == SubgraphPortType.Texture2DObject ), true );
			}

			if ( InputType == SubgraphPortType.Gradient )
			{
				var gradientValue = (Gradient)outputValue;

				if ( gradientValue.Colors.Count > 8 )
				{
					return NodeResult.Error( $"{DisplayInfo.Name} has {gradientValue.Colors.Count} color keys which is greater than the maximum amount of 8 allowed color keys." );
				}
				else
				{
					// Register gradient with the compiler.
					var result = compiler.RegisterGradient( gradientValue, Name );

					return new NodeResult( ResultType.Gradient, result, constant: true );
				}
			}

			// For normal graphs, use ResultParameter to create a material parameter
			return compiler.ResultParameter( Name, outputValue, default, default, false, IsRequired, default );
		}
	};

	private string ProcessTexture2D( GraphCompiler compiler, TextureInput input, bool isTexture2D )
	{
		input.Type = isTexture2D ? TextureType.Tex2D : TextureType.TexCube;

		return compiler.ResultTexture( input, null, true );
	}

	private IBlackboardSubgraphInputParameter GetParameter()
	{
		if ( Graph is ShaderGraphPlus graph )
		{
			var parameter = graph.FindParameter( ParameterIdentifier );

			if ( parameter is IBlackboardSubgraphInputParameter subgraphInputParameter )
			{
				return subgraphInputParameter;
			}

			return default;
		}

		return default;
	}

	public NodeResult GetResult( GraphCompiler compiler )
	{
		return Result.Invoke( compiler );
	}

	public List<string> GetWarnings()
	{
		var warnings = new List<string>();

		return warnings;
	}

	public List<string> GetErrors()
	{
		List<string> errors = new List<string>();

		return errors;
	}
}
