using ShaderGraphPlus.Nodes;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

/// <summary>
/// Bool material parameter
/// </summary>
[Title( "Bool" ), Icon( "check_box" ), Order( 0 )]
public sealed class BoolParameter : BlackboardMaterialParameter<bool, GenericParameterUI>
{
	public BoolParameter() : base()
	{
		Value = false;
		UI = new GenericParameterUI();
	}

	public override IGraphNode ToNode()
	{
		return new BoolParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// Int material parameter
/// </summary>
[Title( "Int" ), Icon( "looks_one" ), Order( 1 )]
public sealed class IntParameter : BlackboardMaterialParameter<int, GenericParameterUI>, IRangedBlackboardMaterialParameter
{
	[Group( "Range" )] public int Min { get; set; }
	[Group( "Range" )] public int Max { get; set; }

	public IntParameter() : base()
	{
		Value = 1;
		Min = 0;
		Max = 1;
		UI = new GenericParameterUI();
	}

	public object GetRangeMin()
	{
		return Min;
	}

	public object GetRangeMax()
	{
		return Max;
	}

	public override IGraphNode ToNode()
	{
		return new IntParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// Float material parameter
/// </summary>
[Title( "Float" ), Icon( "looks_one" ), Order( 2 )]
public sealed class FloatParameter : BlackboardMaterialParameter<float, FloatParameterUI>, IRangedBlackboardMaterialParameter
{
	[Group( "Range" )] public float Min { get; set; }
	[Group( "Range" )] public float Max { get; set; }

	public FloatParameter() : base()
	{
		Value = 1.0f;
		Min = 0.0f;
		Max = 1.0f;
		UI = new FloatParameterUI { Type = UIType.Default };
	}

	public object GetRangeMin()
	{
		return Min;
	}

	public object GetRangeMax()
	{
		return Max;
	}

	public override IGraphNode ToNode()
	{
		return new FloatParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// Float2 material parameter
/// </summary>
[Title( "Float2" ), Icon( "looks_two" ), Order( 3 )]
public sealed class Float2Parameter : BlackboardMaterialParameter<Vector2, FloatParameterUI>, IRangedBlackboardMaterialParameter
{
	[Group( "Range" )] public Vector2 Min { get; set; }
	[Group( "Range" )] public Vector2 Max { get; set; }

	public Float2Parameter() : base()
	{
		Value = Vector2.One;
		Min = Vector2.Zero;
		Max = Vector2.One;
		UI = new FloatParameterUI { Type = UIType.Default };
	}

	public object GetRangeMin()
	{
		return Min;
	}

	public object GetRangeMax()
	{
		return Max;
	}

	public override IGraphNode ToNode()
	{
		return new Float2ParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// Float3 material parameter
/// </summary>
[Title( "Float3" ), Icon( "looks_3" ), Order( 4 )]
public sealed class Float3Parameter : BlackboardMaterialParameter<Vector3, FloatParameterUI>, IRangedBlackboardMaterialParameter
{
	[Group( "Range" )] public Vector3 Min { get; set; }
	[Group( "Range" )] public Vector3 Max { get; set; }

	public Float3Parameter() : base()
	{
		Value = Vector3.One;
		Min = Vector3.Zero;
		Max = Vector3.One;
		UI = new FloatParameterUI { Type = UIType.Default };
	}

	public object GetRangeMin()
	{
		return Min;
	}

	public object GetRangeMax()
	{
		return Max;
	}

	public override IGraphNode ToNode()
	{
		return new Float3ParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// Float4 material parameter
/// </summary>
[Title( "Float4" ), Icon( "looks_4" ), Order( 5 )]
public sealed class Float4Parameter : BlackboardMaterialParameter<Vector4, FloatParameterUI>, IRangedBlackboardMaterialParameter
{
	[Group( "Range" )] public Vector4 Min { get; set; }
	[Group( "Range" )] public Vector4 Max { get; set; }

	public Float4Parameter() : base()
	{
		Value = Vector4.One;
		Min = Vector4.Zero;
		Max = Vector4.One;
		UI = new FloatParameterUI { Type = UIType.Default };
	}

	public object GetRangeMin()
	{
		return Min;
	}

	public object GetRangeMax()
	{
		return Max;
	}

	public override IGraphNode ToNode()
	{
		return new Float4ParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// Color material parameter
/// </summary>
[Title( "Color" ), Icon( "palette" ), Order( 6 )]
public sealed class ColorParameter : BlackboardMaterialParameter<Color, GenericParameterUI>
{
	public ColorParameter() : base()
	{
		Value = Color.White;
		UI = new GenericParameterUI();
	}

	public override IGraphNode ToNode()
	{
		return new ColorParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// Texture2D material parameter
/// </summary>
[Title( "Texture2D" ), Icon( "image" ), Order( 7 )]
public sealed class Texture2DParameter : BlackboardTextureMaterialParameter
{
	public Texture2DParameter() : base()
	{
		Value = new TextureInput()
		{
			ImageFormat = TextureFormat.DXT5,
			Type = TextureType.Tex2D,
			SrgbRead = true,
			DefaultColor = Color.White,
		};
	}

	public override IGraphNode ToNode()
	{
		return new Texture2DParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// TextureCube material parameter
/// </summary>
[Title( "TextureCube" ), Icon( "image" ), Order( 8 )]
public sealed class TextureCubeParameter : BlackboardTextureMaterialParameter
{
	public TextureCubeParameter() : base()
	{
		Value = new TextureInput()
		{
			ImageFormat = TextureFormat.DXT5,
			Type = TextureType.TexCube,
			SrgbRead = true,
			DefaultColor = Color.White,
		};
	}

	public override IGraphNode ToNode()
	{
		return new TextureCubeParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
/// SamplerState material parameter
/// </summary>
[Title( "Sampler State" ), Icon( "colorize" ), Order( 8 ), ParameterAvailableIn( ParameterAvailableIn.Material )]
public sealed class SamplerStateParameter : BlackboardParameter, IGroupableBlackboardParameter
{
	[InlineEditor( Label = false ), Group( "Value" )]
	public Sampler Value { get; set; }

	[Hide]
	public Guid GroupReference { get; set; } = Guid.Empty;

	[Hide]
	public bool IsGrouped => GroupReference != default || GroupReference != Guid.Empty;

	public SamplerStateParameter() : base()
	{
		Value = new Sampler();
	}

	public override int GetHashCode()
	{
		return HashCode.Combine( Name, GroupReference );
	}

	public override object GetValue()
	{
		return Value;
	}

	public override void SetValue( object value )
	{
		if ( value.GetType() != typeof( Sampler ) )
		{
			throw new InvalidCastException( $"Cannot cast {value.GetType()} to {typeof( Sampler )}" );
		}

		Value = (Sampler)value;
	}

	public override IGraphNode ToNode()
	{
		return new SamplerStateParameterNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
///
/// </summary>
[Title( "Shader Feature Boolean" ), Icon( "tune" ), Order( 9 ), ParameterAvailableIn( ParameterAvailableIn.Material )]
public sealed class ShaderFeatureBooleanParameter : BlackboardParameter, IBlackboardShaderFeatureParameter, IGroupableBlackboardParameter
{
	[Hide, JsonIgnore, Browsable( false )]
	public override bool IsValid => !string.IsNullOrWhiteSpace( Name );

	/// <summary>
	/// Name of this feature.
	/// </summary>
	[Title( "Feature Name" )]
	public override string Name { get; set; }

	/// <summary>
	/// What this feature does.
	/// </summary>
	[Hide]
	public string Description { get; set; }

	/// <summary>
	/// Header Name of this Feature that shows up in the Material Editor.
	/// </summary>
	public string HeaderName { get; set; }

	[Title( "Preview" )]
	public bool Preview { get; set; } = false;

	[Hide]
	public Guid GroupReference { get; set; } = Guid.Empty;

	[Hide]
	public bool IsGrouped => GroupReference != default || GroupReference != Guid.Empty;

	public ShaderFeatureBooleanParameter() : base()
	{
	}

	public override int GetHashCode()
	{
		return HashCode.Combine( Name, GroupReference );
	}

	public override object GetValue()
	{
		throw new NotImplementedException();
	}

	public override void SetValue( object value )
	{
		throw new NotImplementedException();
	}

	public override IGraphNode ToNode()
	{
		return new BooleanFeatureSwitchNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

/// <summary>
///
/// </summary>
[Title( "Shader Feature Enum" ), Icon( "tune" ), Order( 10 ), ParameterAvailableIn( ParameterAvailableIn.Material )]
public sealed class ShaderFeatureEnumParameter : BlackboardParameter, IBlackboardShaderFeatureParameter, IGroupableBlackboardParameter
{
	[Hide, JsonIgnore, Browsable( false )]
	public override bool IsValid => !string.IsNullOrWhiteSpace( Name ) && Options.All( x => !string.IsNullOrWhiteSpace( x.Name ) );

	/// <summary>
	/// Name of this feature.
	/// </summary>
	[Title( "Feature Name" )]
	public override string Name { get; set; }

	/// <summary>
	/// What this feature does.
	/// </summary>
	[Hide]
	public string Description { get; set; }

	/// <summary>
	/// Header Name of this Feature that shows up in the Material Editor.
	/// </summary>
	public string HeaderName { get; set; }

	/// <summary>
	/// Options of your feature. Must have no special characters. Note : all lowercase letters will be converted to uppercase.
	/// </summary>
	public List<ShaderFeatureEnumOption> Options { get; set; }

	[global::Editor( ControlWidgetCustomEditors.ShaderFeatureEnumPreviewIndexEditor )]
	[Title( "Preview" )]
	public int PreviewIndex { get; set; } = 0;

	[Hide]
	public Guid GroupReference { get; set; } = Guid.Empty;

	[Hide]
	public bool IsGrouped => GroupReference != default || GroupReference != Guid.Empty;

	public ShaderFeatureEnumParameter() : base()
	{
		Options = new List<ShaderFeatureEnumOption>();
	}

	public override int GetHashCode()
	{
		HashCode hc = new HashCode();
		hc.Add( Name );
		hc.Add( Description );
		hc.Add( HeaderName );

		foreach ( var option in Options )
		{
			hc.Add( option );
		}

		return hc.ToHashCode();
	}

	public override object GetValue()
	{
		throw new NotImplementedException();
	}

	public override void SetValue( object value )
	{
		throw new NotImplementedException();
	}

	public override bool CheckParameter( out List<string> issues )
	{
		base.CheckParameter( out issues );

		var takenOptions = new List<string>();
		foreach ( var option in Options )
		{
			var index = Options.IndexOf( option );

			if ( string.IsNullOrWhiteSpace( option.Name ) )
			{
				issues.Add( $"Shader Feature{(!string.IsNullOrWhiteSpace( Name ) ? $" {Name}" : " Enum")} Option at index \"{index}\" must have a name!" );

				continue;
			}

			if ( !takenOptions.Contains( option.Name ) )
			{
				takenOptions.Add( option.Name );
			}
			else
			{
				issues.Add( $"Shader Feature{(!string.IsNullOrWhiteSpace( Name ) ? $" {Name}" : " Enum")} duplicate option \"{option}\"" );
			}
		}

		return !issues.Any();
	}

	public override IGraphNode ToNode()
	{
		return new EnumFeatureSwitchNode()
		{
			ParameterIdentifier = Identifier,
		};
	}
}
