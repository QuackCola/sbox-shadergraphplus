namespace ShaderGraphPlus;

/// <summary>
/// Bool subgraph input parameter
/// </summary>
[Title( "Bool" ), Icon( "check_box" ), Category( "Input" ), Order( 0 )]
[SubgraphOnly]
public sealed class BoolSubgraphInputParameter : BlackboardSubgraphInputParameter<bool>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Bool;

	public BoolSubgraphInputParameter() : base()
	{
		Value = false;
	}
}

/// <summary>
/// Bool subgraph output parameter
/// </summary>
[Title( "Bool" ), Icon( "check_box" ), Category( "Output" ), Order( 0 )]
[SubgraphOnly]
public sealed class BoolSubgraphOutputParameter : BlackboardSubgraphOutputParameter<bool>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Bool;

	public BoolSubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Int subgraph input parameter
/// </summary>
[Title( "Int" ), Icon( "looks_one" ), Category( "Input" ), Order( 1 )]
[SubgraphOnly]
public sealed class IntSubgraphInputParameter : BlackboardSubgraphInputParameter<int>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Int;

	[Group( "Range" )] public int Min { get; set; }
	[Group( "Range" )] public int Max { get; set; }

	public IntSubgraphInputParameter()
	{
		Value = 1;
		Min = 0;
		Max = 1;
	}
}

/// <summary>
/// Int subgraph output parameter
/// </summary>
[Title( "Int" ), Icon( "looks_one" ), Category( "Output" ), Order( 1 )]
[SubgraphOnly]
public sealed class IntSubgraphOutputParameter : BlackboardSubgraphOutputParameter<int>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Int;

	public IntSubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Float subgraph input parameter
/// </summary>
[Title( "Float" ), Icon( "looks_one" ), Category( "Input" ), Order( 2 )]
[SubgraphOnly]
public sealed class FloatSubgraphInputParameter : BlackboardSubgraphInputParameter<float>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float;

	[Group( "Range" )] public float Min { get; set; }
	[Group( "Range" )] public float Max { get; set; }

	public FloatSubgraphInputParameter()
	{
		Value = 1.0f;
		Min = 0.0f;
		Max = 1.0f;
	}
}

/// <summary>
/// Float subgraph output parameter
/// </summary>
[Title( "Float" ), Icon( "looks_one" ), Category( "Output" ), Order( 2 )]
[SubgraphOnly]
public sealed class FloatSubgraphOutputParameter : BlackboardSubgraphOutputParameter<float>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float;

	public FloatSubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Float2 subgraph input parameter
/// </summary>
[Title( "Float2" ), Icon( "looks_two" ), Category( "Input" ), Order( 3 )]
[SubgraphOnly]
public sealed class Float2SubgraphInputParameter : BlackboardSubgraphInputParameter<Vector2>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Vector2;

	[Group( "Range" )] public Vector2 Min { get; set; }
	[Group( "Range" )] public Vector2 Max { get; set; }

	public Float2SubgraphInputParameter()
	{
		Value = Vector2.One;
		Min = Vector2.Zero;
		Max = Vector2.One;
	}
}

/// <summary>
/// Float2 subgraph output parameter
/// </summary>
[Title( "Float2" ), Icon( "looks_two" ), Category( "Output" ), Order( 3 )]
[SubgraphOnly]
public sealed class Float2SubgraphOutputParameter : BlackboardSubgraphOutputParameter<Vector2>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Vector2;

	public Float2SubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Float3 subgraph input parameter
/// </summary>
[Title( "Float3" ), Icon( "looks_3" ), Category( "Input" ), Order( 4 )]
[SubgraphOnly]
public sealed class Float3SubgraphInputParameter : BlackboardSubgraphInputParameter<Vector3>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Vector3;

	[Group( "Range" )] public Vector3 Min { get; set; }
	[Group( "Range" )] public Vector3 Max { get; set; }

	public Float3SubgraphInputParameter()
	{
		Value = Vector3.One;
		Min = Vector3.Zero;
		Max = Vector3.One;
	}
}

/// <summary>
/// Float3 subgraph output parameter
/// </summary>
[Title( "Float3" ), Icon( "looks_3" ), Category( "Output" ), Order( 4 )]
[SubgraphOnly]
public sealed class Float3SubgraphOutputParameter : BlackboardSubgraphOutputParameter<Vector3>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Vector3;

	public Float3SubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Float4 subgraph input parameter
/// </summary>
[Title( "Float4" ), Icon( "looks_4" ), Category( "Input" ), Order( 5 )]
[SubgraphOnly]
public sealed class Float4SubgraphInputParameter : BlackboardSubgraphInputParameter<Vector4>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Vector4;

	[Group( "Range" )] public Vector4 Min { get; set; }
	[Group( "Range" )] public Vector4 Max { get; set; }

	public Float4SubgraphInputParameter()
	{
		Value = Vector4.One;
		Min = Vector4.Zero;
		Max = Vector4.One;
	}
}

/// <summary>
/// Float4 subgraph output parameter
/// </summary>
[Title( "Float4" ), Icon( "looks_4" ), Category( "Output" ), Order( 5 )]
[SubgraphOnly]
public sealed class Float4SubgraphOutputParameter : BlackboardSubgraphOutputParameter<Vector4>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Vector4;

	public Float4SubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Color subgraph input parameter
/// </summary>
[Title( "Color" ), Icon( "palette" ), Category( "Input" ), Order( 6 )]
[SubgraphOnly]
public sealed class ColorSubgraphInputParameter : BlackboardSubgraphInputParameter<Color>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Color;

	public ColorSubgraphInputParameter()
	{
		Value = Color.White;
	}
}

/// <summary>
/// Color subgraph output parameter
/// </summary>
[Title( "Color" ), Icon( "palette" ), Category( "Output" ), Order( 6 )]
[SubgraphOnly]
public sealed class ColorSubgraphOutputParameter : BlackboardSubgraphOutputParameter<Color>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Color;

	public ColorSubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Float2x2 subgraph input parameter
/// </summary>
[Title( "Float2x2" ), Icon( "apps" ), Category( "Input" ), Order( 7 )]
[SubgraphOnly]
public sealed class Float2x2SubgraphInputParameter : BlackboardSubgraphInputParameter<Float2x2>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float2x2;

	public Float2x2SubgraphInputParameter() : base()
	{
		Value = new Float2x2();
	}
}

/// <summary>
/// Float2x2 subgraph output parameter
/// </summary>
[Title( "Float2x2" ), Icon( "apps" ), Category( "Output" ), Order( 7 )]
[SubgraphOnly]
public sealed class Float2x2SubgraphOutputParameter : BlackboardSubgraphOutputParameter<Float2x2>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float2x2;

	public Float2x2SubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Float3x3 subgraph input parameter
/// </summary>
[Title( "Float3x3" ), Icon( "apps" ), Order( 8 )]
[SubgraphOnly]
public sealed class Float3x3SubgraphInputParameter : BlackboardSubgraphInputParameter<Float3x3>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float3x3;

	public Float3x3SubgraphInputParameter() : base()
	{
		Value = new Float3x3();
	}
}

/// <summary>
/// Float3x3 subgraph output parameter
/// </summary>
[Title( "Float3x3" ), Icon( "apps" ), Order( 8 )]
[SubgraphOnly]
public sealed class Float3x3SubgraphOutputParameter : BlackboardSubgraphOutputParameter<Float3x3>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float3x3;

	public Float3x3SubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Float4x4 subgraph input parameter
/// </summary>
[Title( "Float4x4" ), Icon( "apps" ), Order( 9 )]
[SubgraphOnly]
public sealed class Float4x4SubgraphInputParameter : BlackboardSubgraphInputParameter<Float4x4>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float4x4;

	public Float4x4SubgraphInputParameter() : base()
	{
		Value = new Float4x4();
	}
}

/// <summary>
/// Float4x4 subgraph output parameter
/// </summary>
[Title( "Float4x4" ), Icon( "apps" ), Order( 9 )]
[SubgraphOnly]
public sealed class Float4x4SubgraphOutputParameter : BlackboardSubgraphOutputParameter<Float4x4>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Float4x4;

	public Float4x4SubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Gradient subgraph input parameter 
/// </summary>
[Title( "Gradient" ), Icon( "gradient" ), Order( 10 )]
[SubgraphOnly]
public sealed class GradientSubgraphInputParameter : BlackboardSubgraphInputParameter<Gradient>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Gradient;

	public GradientSubgraphInputParameter() : base()
	{
		Value = new Gradient();
	}
}


/// <summary>
/// Gradient subgraph output parameter 
/// </summary>
[Title( "Gradient" ), Icon( "gradient" ), Order( 10 )]
[SubgraphOnly]
public sealed class GradientSubgraphOutputParameter : BlackboardSubgraphOutputParameter<Gradient>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Gradient;

	public GradientSubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// Texture2D subgraph input parameter
/// </summary>
[Title( "Texture2D" ), Icon( "image" ), Order( 11 )]
public sealed class Texture2DSubgraphInputParameter : BlackboardSubgraphInputParameter<TextureInput>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Texture2DObject;

	[Hide, JsonIgnore]
	public override bool IsRequired { get; set; } = true;

	public Texture2DSubgraphInputParameter() : base()
	{
		Value = new TextureInput()
		{
			ImageFormat = TextureFormat.DXT5,
			Type = TextureType.Tex2D,
			SrgbRead = true,
			DefaultColor = Color.White,
		};
		IsRequired = true;
	}
}


/// <summary>
/// Texture2D subgraph output parameter
/// </summary>
[Title( "Texture2D" ), Icon( "image" ), Order( 11 )]
[SubgraphOnly]
public sealed class Texture2DSubgraphOutputParameter : BlackboardSubgraphOutputParameter<TextureInput>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.Texture2DObject;

	public Texture2DSubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// TextureCube subgraph input parameter
/// </summary>
[Title( "TextureCube" ), Icon( "image" ), Order( 12 )]
public sealed class TextureCubeSubgraphInputParameter : BlackboardSubgraphInputParameter<TextureInput>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.TextureCubeObject;

	[Hide, JsonIgnore]
	public override bool IsRequired { get; set; } = true;

	public TextureCubeSubgraphInputParameter() : base()
	{
		Value = new TextureInput()
		{
			ImageFormat = TextureFormat.DXT5,
			Type = TextureType.TexCube,
			SrgbRead = true,
			DefaultColor = Color.White,
		};
		IsRequired = true;
	}
}

/// <summary>
/// TextureCube subgraph output parameter
/// </summary>
[Title( "TextureCube" ), Icon( "image" ), Order( 12 )]
[SubgraphOnly]
public sealed class TextureCubeSubgraphOutputParameter : BlackboardSubgraphOutputParameter<TextureInput>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.TextureCubeObject;

	public TextureCubeSubgraphOutputParameter() : base()
	{
	}
}

/// <summary>
/// SamplerState subgraph input parameter
/// </summary>
[Title( "Sampler State" ), Icon( "colorize" ), Order( 13 )]
[SubgraphOnly]
public sealed class SamplerStateSubgraphInputParameter : BlackboardSubgraphInputParameter<Sampler>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.SamplerState;

	public SamplerStateSubgraphInputParameter() : base()
	{
		Value = new Sampler();
	}
}

/// <summary>
/// SamplerState subgraph output parameter
/// </summary>
[Title( "Sampler State" ), Icon( "colorize" ), Order( 13 )]
[SubgraphOnly]
public sealed class SamplerStateSubgraphOutputParameter : BlackboardSubgraphOutputParameter<Sampler>
{
	[Hide, JsonIgnore]
	public override SubgraphPortType PortType => SubgraphPortType.SamplerState;

	public SamplerStateSubgraphOutputParameter() : base()
	{
	}
}
