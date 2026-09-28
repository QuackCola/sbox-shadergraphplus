namespace NodeEditorPlus.Blackboard;

public static class BlackboardRegex
{
	public static Regex InvalidCharacters { get; } = new Regex( @"[^\p{L}\p{N}_-]" );
}
