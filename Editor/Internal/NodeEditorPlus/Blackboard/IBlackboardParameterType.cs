namespace NodeEditorPlus.Blackboard;

public interface IBlackboardParameterType
{
	public TypeDescription Type { get; }

	IBlackboardParameter CreateParameter( IBlackboardNodeGraph graph, string name = "" );
}
