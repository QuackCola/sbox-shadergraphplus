namespace ShaderGraphPlus;

public interface IBlackboardNodeGraph : INodeGraph
{
	IEnumerable<IBlackboardParameter> Parameters { get; }

	void AddParameter( IBlackboardParameter parameter );
	void RemoveParameter( IBlackboardParameter parameter );

	IBlackboardParameter FindParameter( Guid identifier );

	string SerializeParameters( IEnumerable<IBlackboardParameter> parameters );
	IEnumerable<IBlackboardParameter> DeserializeParameters( string serialized );
}
