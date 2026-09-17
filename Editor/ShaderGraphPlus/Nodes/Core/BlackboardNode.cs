namespace ShaderGraphPlus;

public abstract class BlackboardNode<T> : BlackboardNode where T : BlackboardParameter
{
	protected T GetParameter()
	{
		if ( Graph is ShaderGraphPlus graph && graph.TryFindParameter<T>( ParameterIdentifier, out var foundParameter ) )
		{
			return foundParameter;
		}

		return null;
	}

	protected bool TryGetParameter( out T parameter )
	{
		parameter = GetParameter();

		return parameter != null;
	}
}

public abstract class BlackboardNode : ShaderNodePlus, IBlackboardNode
{
	[Hide, Browsable( false )]
	public Guid ParameterIdentifier { get; set; }
}
