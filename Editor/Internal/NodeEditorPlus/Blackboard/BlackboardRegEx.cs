namespace NodeEditorPlus.Blackboard;

public static class BlackboardRegex
{
	public static Regex NonLettersAndNumbers { get; } = new Regex( @"[^\p{L}\p{N}]" );
}
