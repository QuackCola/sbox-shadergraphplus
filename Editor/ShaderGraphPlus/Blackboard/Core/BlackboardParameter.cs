using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlus;

public record struct BlackboardConfig( string Name, Color Color );

public interface IBlackboardParameter
{
	Guid Identifier { get; }

	DisplayInfo DisplayInfo { get; }

	string Name { get; set; }

	object GetValue();

	void SetValue( object value );
}

public interface IBlackboardMaterialParameter : IBlackboardParameter
{
	bool IsAttribute { get; set; }
	IParameterUI GetParameterUI();
}

public interface IRangedBlackboardMaterialParameter : IBlackboardParameter
{
	object GetRangeMin();
	object GetRangeMax();
}

public interface IBlackboardSubgraphParameter : IBlackboardParameter
{
}

public interface IBlackboardSubgraphInputParameter : IBlackboardSubgraphParameter
{
	/// <summary>
	/// Description of what this input does
	/// </summary>
	string InputDescription { get; set; }

	/// <summary>
	/// Whether this input is required (must have a connection in order to compile)
	/// </summary>
	bool IsRequired { get; set; }

	/// <summary>
	/// The order of this input port.
	/// </summary>
	int PortOrder { get; set; }

	abstract SubgraphPortType InputType { get; }
}

public interface IBlackboardSubgraphOutputParameter : IBlackboardSubgraphParameter
{
	bool IsValid { get; }

	/// <summary>
	/// Description of what this output does
	/// </summary>
	string OutputDescription { get; set; }

	SubgraphOutputPreviewType Preview { get; set; }

	/// <summary>
	/// The order of this output port.
	/// </summary>
	int PortOrder { get; set; }

	abstract SubgraphPortType OutputType { get; }

	bool CannotPreviewOutputType { get; }
}

public interface IBlackboardParameterType
{
	public TypeDescription Type { get; }

	IBlackboardParameter CreateParameter( INodeGraph graph, string name = "" );
}

public abstract class BlackboardParameter : IBlackboardParameter, IValid
{
	[Hide, Browsable( false )]
	public Guid Identifier { get; set; }

	[JsonIgnore, Hide]
	public INodeGraph _graph;
	[Browsable( false )]
	[JsonIgnore, Hide]
	public INodeGraph Graph
	{
		get => _graph;
		set
		{
			_graph = value;
		}
	}

	[JsonIgnore, Hide, Browsable( false )]
	public DisplayInfo DisplayInfo { get; }

	[Hide, JsonIgnore, Browsable( false )]
	public virtual bool IsValid => true;

	public virtual string Name { get; set; }

	public BlackboardParameter()
	{
		DisplayInfo = DisplayInfo.For( this );
		NewIdentifier();

		Name = "";
	}

	public Guid NewIdentifier()
	{
		Identifier = Guid.NewGuid();
		return Identifier;
	}

	public abstract object GetValue();

	public abstract void SetValue( object value );

	/// <summary>
	/// Check parameter for any issues.
	/// </summary>
	/// <param name="issues">Any issues that are found.</param>
	/// <returns>False when check has failed otherwise returns true when check has passed.</returns>
	internal bool CheckParameter( out List<string> issues )
	{
		var graph = _graph as ShaderGraphPlus;
		issues = new List<string>();

		if ( string.IsNullOrWhiteSpace( Name ) )
		{
			issues.Add( $"Parameter with identifier \"{Identifier}\" must have name!" );

			return false;
		}

		var cleanedName = Name.Replace( " ", "" );

		foreach ( var parameter in graph.Parameters )
		{
			if ( parameter == this )
				continue;

			var cleanedComparsionName = parameter.Name.Replace( " ", "" );

			// Check for exact matches and matches with the spaces removed.
			if ( parameter.Name == Name || cleanedComparsionName == cleanedName )
			{
				issues.Add( $"Parameter with name \"{Name}\" already exists!" );

				return false;
			}
		}

		return true;
	}

	public static IEnumerable<IBlackboardParameterType> GetRelevantParameters( Dictionary<string, IBlackboardParameterType> availableParameters, bool isSubgraph )
	{
		return availableParameters.Values.Where( x =>
		{
			if ( x is ClassBlackboardParameterType classParameterType )
			{
				var targetType = classParameterType.Type.TargetType;

				// Only show material parameters when not in a subgraph
				if ( isSubgraph && targetType == typeof( BoolParameter ) ) return false;
				if ( isSubgraph && targetType == typeof( IntParameter ) ) return false;
				if ( isSubgraph && targetType == typeof( FloatParameter ) ) return false;
				if ( isSubgraph && targetType == typeof( Float2Parameter ) ) return false;
				if ( isSubgraph && targetType == typeof( Float3Parameter ) ) return false;
				if ( isSubgraph && targetType == typeof( Float4Parameter ) ) return false;
				if ( isSubgraph && targetType == typeof( ColorParameter ) ) return false;
				if ( isSubgraph && targetType == typeof( Texture2DParameter ) ) return false;
				if ( isSubgraph && targetType == typeof( TextureCubeParameter ) ) return false;
				if ( isSubgraph && targetType == typeof( ShaderFeatureBooleanParameter ) ) return false;
				if ( isSubgraph && targetType == typeof( ShaderFeatureEnumParameter ) ) return false;

				// Only show subgraph input parameters when in a subgraph
				if ( !isSubgraph && targetType == typeof( BoolSubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( IntSubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( FloatSubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float2SubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float3SubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float4SubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( ColorSubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Texture2DSubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( TextureCubeSubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float2x2SubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float3x3SubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float4x4SubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( GradientSubgraphInputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( SamplerStateSubgraphInputParameter ) ) return false;

				// Only show subgraph output parameters when in a subgraph
				if ( !isSubgraph && targetType == typeof( BoolSubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( IntSubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( FloatSubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float2SubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float3SubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float4SubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( ColorSubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Texture2DSubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( TextureCubeSubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float2x2SubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float3x3SubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( Float4x4SubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( GradientSubgraphOutputParameter ) ) return false;
				if ( !isSubgraph && targetType == typeof( SamplerStateSubgraphOutputParameter ) ) return false;
			}

			return true;
		} );
	}

	public static BaseNodePlus InitializeParameterNode( IBlackboardParameter parameter )
	{
		return parameter switch
		{
			// Material Parameters
			BoolParameter => new BoolParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			IntParameter => new IntParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			FloatParameter => new FloatParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float2Parameter => new Float2ParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float3Parameter => new Float3ParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float4Parameter => new Float4ParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			ColorParameter => new ColorParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Texture2DParameter => new Texture2DParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			TextureCubeParameter => new TextureCubeParameterNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			ShaderFeatureBooleanParameter => new BooleanFeatureSwitchNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			ShaderFeatureEnumParameter => new EnumFeatureSwitchNode()
			{
				ParameterIdentifier = parameter.Identifier,
			},

			// Subgraph Inputs
			BoolSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = false,
				ParameterIdentifier = parameter.Identifier,
			},
			IntSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = 0,
				ParameterIdentifier = parameter.Identifier,
			},
			FloatSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = 0.0f,
				ParameterIdentifier = parameter.Identifier,
			},
			Float2SubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = Vector2.Zero,
				ParameterIdentifier = parameter.Identifier,
			},
			Float3SubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = Vector3.Zero,
				ParameterIdentifier = parameter.Identifier,
			},
			Float4SubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = Vector4.Zero,
				ParameterIdentifier = parameter.Identifier,
			},
			ColorSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = Color.White,
				ParameterIdentifier = parameter.Identifier,
			},
			Float2x2SubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = new Float2x2(),
				ParameterIdentifier = parameter.Identifier,
			},
			Float3x3SubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = new Float3x3(),
				ParameterIdentifier = parameter.Identifier,
			},
			Float4x4SubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = new Float4x4(),
				ParameterIdentifier = parameter.Identifier,
			},
			Texture2DSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = new TextureInput() { Type = TextureType.Tex2D },
				ParameterIdentifier = parameter.Identifier,
			},
			TextureCubeSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = new TextureInput() { Type = TextureType.TexCube },
				ParameterIdentifier = parameter.Identifier,
			},
			SamplerStateSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = new Sampler(),
				ParameterIdentifier = parameter.Identifier,
			},
			GradientSubgraphInputParameter => new SubgraphInput()
			{
				DefaultValue = new Gradient(),
				ParameterIdentifier = parameter.Identifier,
			},

			// Subgraph Outputs
			BoolSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			IntSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			FloatSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float2SubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float3SubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float4SubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			ColorSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float2x2SubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float3x3SubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Float4x4SubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			Texture2DSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			TextureCubeSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			SamplerStateSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			GradientSubgraphOutputParameter => new SubgraphOutput()
			{
				ParameterIdentifier = parameter.Identifier,
			},
			_ => throw new NotImplementedException( $"Unknown parameter : {parameter.GetType()}" ),
		};
	}
}

public abstract class BlackboardMaterialParameter<T, Y> : BlackboardParameter, IBlackboardMaterialParameter where Y : IParameterUI
{
	[InlineEditor( Label = false ), Group( "Value" )]
	public T Value { get; set; }

	[InlineEditor( Label = false ), Group( "UI" )]
	public Y UI { get; set; }

	public bool IsAttribute { get; set; }

	public BlackboardMaterialParameter() : base()
	{
		IsAttribute = false;
	}

	public override object GetValue()
	{
		return Value;
	}

	public IParameterUI GetParameterUI()
	{
		return UI;
	}

	public override void SetValue( object value )
	{
		if ( value.GetType() != typeof( T ) )
		{
			throw new InvalidCastException( $"Cannot cast {value.GetType()} to {typeof( T )}" );
		}

		Value = (T)value;
	}
}

public abstract class BlackboardSubgraphInputParameter<T> : BlackboardParameter, IBlackboardSubgraphInputParameter
{
	[Title( "Input Name" )]
	public override string Name { get; set; }

	/// <summary>
	/// Description of what this input does
	/// </summary>
	[TextArea]
	public string InputDescription { get; set; } = "";

	[InlineEditor( Label = false ), Group( "Value" )]
	public virtual T Value { get; set; }

	/// <summary>
	/// Whether this input is required (must have a connection in order to compile)
	/// </summary>
	public virtual bool IsRequired { get; set; } = false;

	/// <summary>
	/// The order of this input port.
	/// </summary>
	[Title( "Order" )]
	public int PortOrder { get; set; } = 0;

	[Hide, JsonIgnore]
	public abstract SubgraphPortType InputType { get; }

	public BlackboardSubgraphInputParameter() : base()
	{
	}

	public override object GetValue()
	{
		return Value;
	}

	public override void SetValue( object value )
	{
		if ( value.GetType() != typeof( T ) )
		{
			throw new InvalidCastException( $"Cannot cast {value.GetType()} to {typeof( T )}" );
		}

		Value = (T)value;
	}
}

public abstract class BlackboardSubgraphOutputParameter<T> : BlackboardParameter, IBlackboardSubgraphOutputParameter
{
	[Title( "Output Name" )]
	public override string Name { get; set; }

	/// <summary>
	/// Description of what this output does
	/// </summary>
	[TextArea]
	public string OutputDescription { get; set; } = "";

	/// <summary>
	/// The order of this output port
	/// </summary>
	[Title( "Order" )]
	public int PortOrder { get; set; } = 0;

	[Hide, JsonIgnore]
	public abstract SubgraphPortType OutputType { get; }

	[HideIf( nameof( CannotPreviewOutputType ), true )]
	public SubgraphOutputPreviewType Preview { get; set; }

	[JsonIgnore, Hide, Browsable( false )]
	public bool CannotPreviewOutputType
	{
		get
		{
			return (OutputType == SubgraphPortType.Bool ||
				OutputType == SubgraphPortType.Float2x2 ||
				OutputType == SubgraphPortType.Float3x3 ||
				OutputType == SubgraphPortType.Float4x4 ||
				OutputType == SubgraphPortType.Gradient ||
				OutputType == SubgraphPortType.Texture2DObject ||
				OutputType == SubgraphPortType.TextureCubeObject ||
				OutputType == SubgraphPortType.SamplerState);
		}
	}

	public BlackboardSubgraphOutputParameter() : base()
	{
		Preview = SubgraphOutputPreviewType.None;
	}

	public override object GetValue()
	{
		return null;
	}

	public override void SetValue( object value )
	{
	}
}

public abstract class BlackboardTextureMaterialParameter : BlackboardParameter
{
	[Hide]
	private TextureInput _value;
	[InlineEditor( Label = false ), Group( "Value" )]
	public TextureInput Value
	{
		get => _value with { Name = Name };
		set
		{
			_value = value;
		}
	}

	public BlackboardTextureMaterialParameter() : base()
	{
	}

	public override object GetValue()
	{
		return Value;
	}

	public override void SetValue( object value )
	{
		if ( value.GetType() != typeof( TextureInput ) )
		{
			throw new InvalidCastException( $"Cannot cast {value.GetType()} to {typeof( TextureInput )}" );
		}

		Value = (TextureInput)value;
	}

	public IParameterUI GetParameterUI()
	{
		throw new NotImplementedException();
	}
}
