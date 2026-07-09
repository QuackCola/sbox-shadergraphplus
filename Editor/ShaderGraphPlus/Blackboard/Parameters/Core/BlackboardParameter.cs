using Editor;

namespace ShaderGraphPlus;

public enum ParameterAvailableIn
{
	Material,
	Subgraph
}

[AttributeUsage( AttributeTargets.Class )]
internal sealed class ParameterAvailableInAttribute : Attribute
{

	public ParameterAvailableIn Availability { get; private set; }

	public ParameterAvailableInAttribute( ParameterAvailableIn availability )
	{
		Availability = availability;
	}
}

public interface IGroupableBlackboardParameter : IBlackboardParameter
{
	Guid GroupReference { get; set; }

	public bool IsGrouped { get; }
}

public interface IBlackboardMaterialParameter : IGroupableBlackboardParameter
{
	bool IsAttribute { get; set; }

	public IParameterUI UI { get; set; }

	public IParameterUI GetParameterUI();
}

public interface IRangedBlackboardMaterialParameter : IBlackboardParameter
{
	public object GetRangeMin();
	public object GetRangeMax();
}

public interface IBlackboardSubgraphParameter : IGroupableBlackboardParameter
{
	string Description { get; set; }
	int PortOrder { get; }

	abstract SubgraphPortType PortType { get; }
}

public interface IBlackboardSubgraphInputParameter : IBlackboardSubgraphParameter
{
	/// <summary>
	/// Whether this input is required (must have a connection in order to compile)
	/// </summary>
	bool IsRequired { get; set; }
}

public interface IBlackboardSubgraphOutputParameter : IBlackboardSubgraphParameter
{
	bool IsValid { get; }

	SubgraphOutputPreviewType Preview { get; set; }

	bool CannotPreviewOutputType { get; }
}

public abstract class BlackboardParameter : IBlackboardParameter, IValid
{
	[Hide, Browsable( false )]
	public Guid Identifier { get; set; }

	[Browsable( false )]
	[JsonIgnore, Hide]
	public IBlackboardNodeGraph Graph { get; set; }

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

	public override int GetHashCode()
	{
		return HashCode.Combine( Name );
	}

	public Guid NewIdentifier()
	{
		Identifier = Guid.NewGuid();
		return Identifier;
	}

	public abstract object GetValue();

	public abstract void SetValue( object value );

	public virtual void NewName()
	{
	}

	/// <summary>
	/// Check parameter for any issues.
	/// </summary>
	/// <param name="issues">Any issues that are found.</param>
	/// <returns>False when check has failed otherwise returns true when check has passed.</returns>
	public virtual bool CheckParameter( out List<string> issues )
	{
		var graph = Graph as ShaderGraphPlus;
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

	public abstract IGraphNode ToNode();

	public static IEnumerable<IBlackboardParameterType> GetRelevantParameters( Dictionary<string, IBlackboardParameterType> availableParameters, bool isSubgraph )
	{
		return availableParameters.Values.Where( x =>
		{
			if ( x is ClassBlackboardParameterType classParameterType )
			{
				var attrib = classParameterType.Type.GetAttribute<ParameterAvailableInAttribute>( true );

				if ( attrib != null )
				{
					// Only show material parameters when not in a subgraph
					if ( isSubgraph && attrib.Availability == ParameterAvailableIn.Material ) return false;
					// Only show subgraph input and output parameters when in a subgraph
					if ( !isSubgraph && attrib.Availability == ParameterAvailableIn.Subgraph ) return false;
				}
				else
				{
					return false;
				}
			}

			return true;
		} );
	}
}

[ParameterAvailableIn( ParameterAvailableIn.Material )]
public abstract class BlackboardMaterialParameter<T, Y> : BlackboardParameter, IBlackboardMaterialParameter where Y : IParameterUI
{
	[InlineEditor( Label = false ), Group( "Value" )]
	public T Value { get; set; }

	[Hide, JsonIgnore]
	IParameterUI IBlackboardMaterialParameter.UI
	{
		get => UI;
		set
		{
			if ( value.GetType() != typeof( Y ) )
			{
				throw new Exception( $"Value '{value.GetType()}' is not the correct type '{typeof( Y )}'" );
			}

			UI = (Y)value;
		}
	}

	[InlineEditor( Label = false ), Group( "UI" )]
	public Y UI { get; set; }

	[Hide]
	public Guid GroupReference { get; set; } = Guid.Empty;

	[Hide]
	public bool IsGrouped => GroupReference != default || GroupReference != Guid.Empty;

	public bool IsAttribute { get; set; }

	public BlackboardMaterialParameter() : base()
	{
		IsAttribute = false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine( Name, GroupReference );
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

[ParameterAvailableIn( ParameterAvailableIn.Subgraph )]
public abstract class BlackboardSubgraphInputParameter<T> : BlackboardParameter, IBlackboardSubgraphInputParameter
{
	[Title( "Input Name" )]
	public override string Name { get; set; }

	/// <summary>
	/// Description of what this input does
	/// </summary>
	[Title( "Input Description" )]
	[TextArea]
	public string Description { get; set; } = "";

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
	[Hide, JsonIgnore]
	public int PortOrder => Graph is ShaderGraphPlus graph ? graph.GetParameterIndex( this ) : 0;

	[Hide]
	public Guid GroupReference { get; set; } = Guid.Empty;

	[Hide]
	public bool IsGrouped => GroupReference != default || GroupReference != Guid.Empty;

	[Hide, JsonIgnore]
	public abstract SubgraphPortType PortType { get; }

	public BlackboardSubgraphInputParameter() : base()
	{
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
		if ( value.GetType() != typeof( T ) )
		{
			throw new InvalidCastException( $"Cannot cast {value.GetType()} to {typeof( T )}" );
		}

		Value = (T)value;
	}

	public override IGraphNode ToNode()
	{
		return new SubgraphInput()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

[ParameterAvailableIn( ParameterAvailableIn.Subgraph )]
public abstract class BlackboardSubgraphOutputParameter<T> : BlackboardParameter, IBlackboardSubgraphOutputParameter
{
	[Title( "Output Name" )]
	public override string Name { get; set; }

	/// <summary>
	/// Description of what this output does
	/// </summary>
	[Title( "Output Description" )]
	[TextArea]
	public string Description { get; set; } = "";

	/// <summary>
	/// The order of this output port
	/// </summary>
	[Title( "Order" )]
	[Hide, JsonIgnore]
	public int PortOrder => Graph is ShaderGraphPlus graph ? graph.GetParameterIndex( this ) : 0;

	[Hide, JsonIgnore]
	public abstract SubgraphPortType PortType { get; }

	[Hide]
	public Guid GroupReference { get; set; } = Guid.Empty;

	[Hide]
	public bool IsGrouped => GroupReference != default || GroupReference != Guid.Empty;

	[HideIf( nameof( CannotPreviewOutputType ), true )]
	public SubgraphOutputPreviewType Preview { get; set; }

	[JsonIgnore, Hide, Browsable( false )]
	public bool CannotPreviewOutputType
	{
		get
		{
			return (PortType == SubgraphPortType.Bool ||
				PortType == SubgraphPortType.Float2x2 ||
				PortType == SubgraphPortType.Float3x3 ||
				PortType == SubgraphPortType.Float4x4 ||
				PortType == SubgraphPortType.Gradient ||
				PortType == SubgraphPortType.Texture2DObject ||
				PortType == SubgraphPortType.TextureCubeObject ||
				PortType == SubgraphPortType.SamplerState);
		}
	}

	public BlackboardSubgraphOutputParameter() : base()
	{
		Preview = SubgraphOutputPreviewType.None;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine( Name, GroupReference );
	}

	public override object GetValue()
	{
		return null;
	}

	public override void SetValue( object value )
	{
	}

	public override IGraphNode ToNode()
	{
		return new SubgraphOutput()
		{
			ParameterIdentifier = Identifier,
		};
	}
}

[ParameterAvailableIn( ParameterAvailableIn.Material )]
public abstract class BlackboardTextureMaterialParameter : BlackboardParameter, IGroupableBlackboardParameter
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

	[Hide]
	public Guid GroupReference { get; set; } = Guid.Empty;

	[Hide]
	public bool IsGrouped => GroupReference != default || GroupReference != Guid.Empty;

	public BlackboardTextureMaterialParameter() : base()
	{
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
