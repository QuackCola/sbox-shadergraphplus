namespace ShaderGraphPlus;

public interface IBlackboardNode
{
	Guid ParameterIdentifier { get; set; }
}

public interface IBlackboardNode<T> : IBlackboardNode
{
	protected T GetParameter();
}
