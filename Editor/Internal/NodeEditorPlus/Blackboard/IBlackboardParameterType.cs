namespace NodeEditorPlus.Blackboard;

public interface IBlackboardParameterType
{
	public TypeDescription Type { get; }

	IBlackboardParameter CreateParameter( INodeGraph graph, string name = "" );
}
