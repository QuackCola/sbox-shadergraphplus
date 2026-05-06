namespace ShaderGraphPlus;

public sealed partial class GraphCompiler
{
	/// <summary>
	/// Avalible value types that are passed between <seealso cref="ShaderNodePlus"/> nodes.
	/// Value represents if the key type is defined in editor code or not.
	/// </summary>
	internal static Dictionary<Type, bool> ValueTypes => new()
	{
		{ typeof( bool ), false },
		{ typeof( int ), false },
		{ typeof( float ), false },
		{ typeof( Vector2 ),false },
		{ typeof( Vector3 ), false },
		{ typeof( Vector4 ), false },
		{ typeof( Color ), false },
		{ typeof( Float2x2 ), true },
		{ typeof( Float3x3 ), true },
		{ typeof( Float4x4 ), true },
		{ typeof( Texture ), true },
		{ typeof( Sampler ), true },
	};

	/// <summary>
	/// Value types that are exposed to the material editor.
	/// </summary>
	internal static List<Type> MaterialParameterTypes => new()
	{
		{ typeof( bool ) },
		{ typeof( int ) },
		{ typeof( float ) },
		{ typeof( Vector2 ) },
		{ typeof( Vector3 ) },
		{ typeof( Vector4 ) },
		{ typeof( Color ) },
		{ typeof( Texture ) },
	};

	/// <summary>
	/// Value types that can be set on <seealso cref="RenderAttributes"/>
	/// </summary>
	internal static List<Type> ShaderAttributeTypes => new()
	{
		{ typeof( bool ) },
		{ typeof( int ) },
		{ typeof( Vector2Int ) },
		{ typeof( Vector3Int ) },
		{ typeof( float ) },
		{ typeof( Double ) },
		{ typeof( Angles ) },
		{ typeof( Vector2 ) },
		{ typeof( Vector3 ) },
		{ typeof( Vector4 ) },
		{ typeof( Color ) },
		{ typeof( Texture ) },
		{ typeof( Sampler ) },
	};

	/// <summary>
	/// Value types that have no defaults.
	/// </summary>
	internal static HashSet<Type> ValueTypesNoDefault => new()
	{
		{ typeof( Sampler ) },
		{ typeof( Texture ) },
	};
}
