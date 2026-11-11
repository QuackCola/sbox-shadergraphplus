using Editor;

namespace ShaderGraphPlus;

internal static partial class ShaderGraphPlusTheme
{
	static void SetupNodeTheming()
	{
		NodeHandleConfigs = new()
		{
			{ typeof( bool ), new NodeHandleConfig( "bool", TypeColors[typeof( bool )] ) },
			{ typeof( int ), new NodeHandleConfig( "int", TypeColors[typeof( int )] ) },
			{ typeof( float ), new NodeHandleConfig( "Float", TypeColors[typeof( float )] ) },
			{ typeof( Vector2 ), new NodeHandleConfig( "Vector2", TypeColors[typeof( Vector2 )] ) },
			{ typeof( Vector3 ), new NodeHandleConfig( "Vector3", TypeColors[typeof( Vector3 )] ) },
			{ typeof( Vector4 ), new NodeHandleConfig( "Vector4", TypeColors[typeof( Vector4 )] ) },
			{ typeof( Color ), new NodeHandleConfig( "Color", TypeColors[typeof( Color )] ) },
			{ typeof( Float2x2 ), new NodeHandleConfig( "Float2x2", TypeColors[typeof( Float2x2 )] ) },
			{ typeof( Float3x3 ), new NodeHandleConfig( "Float3x3", TypeColors[typeof( Float3x3 )] ) },
			{ typeof( Float4x4 ), new NodeHandleConfig( "Float4x4", TypeColors[typeof( Float4x4 )] ) },
			{ typeof( Gradient ), new NodeHandleConfig( "Gradient", TypeColors[typeof( Gradient )] ) },
			{ typeof( Sampler ), new NodeHandleConfig( "Sampler", TypeColors[typeof( Sampler )] ) },
			{ typeof( Texture ), new NodeHandleConfig( "Texture", TypeColors[typeof( Texture )] ) },
		};
	}
}
