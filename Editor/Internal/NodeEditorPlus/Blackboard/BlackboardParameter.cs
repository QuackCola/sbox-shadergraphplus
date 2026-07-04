namespace NodeEditorPlus.Blackboard;

public record struct BlackboardConfig( string Name, Color Color );

public interface IBlackboardParameter
{
	Guid Identifier { get; }

	DisplayInfo DisplayInfo { get; }

	string Name { get; set; }

	public object GetValue();

	public void SetValue( object value );

	public IGraphNode ToNode();
}
