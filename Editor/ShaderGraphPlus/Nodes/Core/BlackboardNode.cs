namespace ShaderGraphPlus;

public abstract class BlackboardNode<T> : ShaderNodePlus, IBlackboardNode where T : BlackboardParameter
{
	[Hide, Browsable( false )]
	public Guid ParameterIdentifier { get; set; }

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
