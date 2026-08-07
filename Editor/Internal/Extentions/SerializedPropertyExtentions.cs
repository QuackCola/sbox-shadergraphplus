using System.Runtime.CompilerServices;

internal static class SerializedPropertyExtentions
{
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	internal static bool IsPropertyName( this SerializedProperty serializedProperty, string name )
	{
		return serializedProperty.Name == name;
	}
}
