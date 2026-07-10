namespace ShaderGraphPlus;

public class CategoryData
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

	public CategoryData( List<Guid> parameterReferences ) : this()
	{
		ParameterReferences = parameterReferences;
	}

	public CategoryData()
	{
		NewIdentifier();
		ParameterReferences = new List<Guid>();
	}

	public Guid NewIdentifier()
	{
		Identifier = Guid.NewGuid();
		return Identifier;
	}

	public void NewName()
	{
		if ( Graph is ShaderGraphPlus graph )
		{
			var name = $"Group";
			int count = 0;
			var id = $"{name}{count}";

			while ( graph.HasCategoryDataWithName( id ) )
			{
				id = $"{name}{count++}";
			}

			Name = id;
		}
	}

	public bool ReOrderParameter( IBlackboardParameter parameter, int newIndex )
	{
		if ( newIndex <= -1 )
		{
			//throw new IndexOutOfRangeException( $"New Index Invalid '{newIndex}'" );

			SGPLogger.Error( $"New Index Invalid '{newIndex}'" );

			return false;
		}

		ParameterReferences.Remove( parameter.Identifier );

		if ( newIndex > ParameterReferences.Count )
		{
			ParameterReferences.Add( parameter.Identifier );
		}
		else
		{
			ParameterReferences.Insert( newIndex, parameter.Identifier );
		}

		return true;
	}

	public IEnumerable<BlackboardParameter> GetReferencedParameters()
	{
		var referencedParameters = new List<BlackboardParameter>();
		var graph = Graph as ShaderGraphPlus;

		foreach ( var parameterReference in ParameterReferences )
		{
			if ( graph.TryFindParameter( parameterReference, out var referencedParameter ) )
			{
				referencedParameters.Add( referencedParameter );
			}
		}

		return referencedParameters;
	}

	/// <summary>
	/// Is the provided parameter a descendant of this group?
	/// </summary>
	public bool IsDescendant( IGroupableBlackboardParameter parameter )
	{
		var graph = Graph as ShaderGraphPlus;
		foreach ( var reference in ParameterReferences )
		{
			var parameterReference = graph.FindParameter( reference ) as IGroupableBlackboardParameter;

			if ( parameter == parameterReference )
			{
				return true;
			}
		}

		return false;
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
