namespace NodeEditorPlus.Blackboard;

public static class BlackboardRegex
{
	public static Regex AllowedCharacters { get; } = new Regex( @"^[\p{L}\p{N}.,_-]+$" );
}
