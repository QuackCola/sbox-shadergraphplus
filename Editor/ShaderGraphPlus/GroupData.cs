namespace ShaderGraphPlus;

public class GroupData
{
	[Hide]
	public Guid Identifier { get; set; }

	[Browsable( false )]
	[JsonIgnore, Hide]
	public IBlackboardNodeGraph Graph { get; set; }

	/// <summary>
	/// Name of the category
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Priority of this category
	/// </summary>
	[Hide]
	public int Priority { get; set; } = 0;

	/// <summary>
	/// Parameters that belong to this category stored as guid references.
	/// </summary>
	[Hide]
	public List<Guid> ParameterReferences { get; set; }

	public GroupData( List<Guid> parameterReferences ) : this()
	{
		ParameterReferences = parameterReferences;
	}

	public GroupData()
	{
		NewIdentifier();
		ParameterReferences = new List<Guid>();
	}

	public Guid NewIdentifier()
	{
		Identifier = Guid.NewGuid();
		return Identifier;
	}

	public override int GetHashCode()
	{
		HashCode hc = new HashCode();

		hc.Add( Name );
		hc.Add( Priority );

		foreach ( var reference in ParameterReferences )
		{
			hc.Add( reference );
		}

		return hc.ToHashCode();
	}
}
