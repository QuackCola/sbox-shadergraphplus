using Editor;

namespace ShaderGraphPlus;

internal static partial class ShaderGraphPlusTheme
{
	static void SetupBlackboardTheming()
	{
		BlackboardConfigs = new()
		{
			{ typeof( BoolSubgraphInputParameter ), new BlackboardConfig( "bool", TypeColors[typeof( bool )] ) },
			{ typeof( IntSubgraphInputParameter ), new BlackboardConfig( "int", TypeColors[typeof( int )] ) },
			{ typeof( FloatSubgraphInputParameter ), new BlackboardConfig( "float", TypeColors[typeof( float )] ) },
			{ typeof( Float2SubgraphInputParameter ), new BlackboardConfig( "float2", TypeColors[typeof( Vector2 )] ) },
			{ typeof( Float3SubgraphInputParameter ), new BlackboardConfig( "float3", TypeColors[typeof( Vector3 )] ) },
			{ typeof( Float4SubgraphInputParameter ), new BlackboardConfig( "float4", TypeColors[typeof( Vector4 )] ) },
			{ typeof( ColorSubgraphInputParameter ), new BlackboardConfig( "Color", TypeColors[typeof( Color )] ) },
			{ typeof( Float2x2SubgraphInputParameter ), new BlackboardConfig( "float2x2", TypeColors[typeof( Float2x2 )] ) },
			{ typeof( Float3x3SubgraphInputParameter ), new BlackboardConfig( "float3x3", TypeColors[typeof( Float3x3 )] ) },
			{ typeof( Float4x4SubgraphInputParameter ), new BlackboardConfig( "float4x4", TypeColors[typeof( Float4x4 )] ) },
			{ typeof( GradientSubgraphInputParameter ), new BlackboardConfig( "Gradient", TypeColors[typeof( Gradient )] ) },
			{ typeof( SamplerStateSubgraphInputParameter ), new BlackboardConfig( "SamplerState", TypeColors[typeof( Sampler )] ) },
			{ typeof( Texture2DSubgraphInputParameter ), new BlackboardConfig( "Texture2D", TypeColors[typeof( Texture )] ) },
			{ typeof( TextureCubeSubgraphInputParameter ), new BlackboardConfig( "TextureCube", TypeColors[typeof( Texture )] ) },

			{ typeof( BoolSubgraphOutputParameter ), new BlackboardConfig( "bool", TypeColors[typeof( bool )] ) },
			{ typeof( IntSubgraphOutputParameter ), new BlackboardConfig( "int", TypeColors[typeof( int )] ) },
			{ typeof( FloatSubgraphOutputParameter ), new BlackboardConfig( "float", TypeColors[typeof( float )]) },
			{ typeof( Float2SubgraphOutputParameter ), new BlackboardConfig( "float2", TypeColors[typeof( Vector2) ] ) },
			{ typeof( Float3SubgraphOutputParameter ), new BlackboardConfig( "float3", TypeColors[typeof( Vector3 )] ) },
			{ typeof( Float4SubgraphOutputParameter ), new BlackboardConfig( "float4", TypeColors[typeof( Vector4 )] ) },
			{ typeof( ColorSubgraphOutputParameter ), new BlackboardConfig( "Color", TypeColors[typeof( Color )] ) },
			{ typeof( Float2x2SubgraphOutputParameter ), new BlackboardConfig( "float2x2", TypeColors[typeof( Float2x2 )] ) },
			{ typeof( Float3x3SubgraphOutputParameter ), new BlackboardConfig( "float3x3", TypeColors[typeof( Float3x3 )] ) },
			{ typeof( Float4x4SubgraphOutputParameter ), new BlackboardConfig( "float4x4", TypeColors[typeof( Float4x4 )] ) },
			{ typeof( GradientSubgraphOutputParameter ), new BlackboardConfig( "Gradient", TypeColors[typeof( Gradient )] ) },
			{ typeof( SamplerStateSubgraphOutputParameter ), new BlackboardConfig( "SamplerState", TypeColors[typeof( Sampler )] ) },
			{ typeof( Texture2DSubgraphOutputParameter ), new BlackboardConfig( "Texture2D", TypeColors[typeof( Texture )] ) },
			{ typeof( TextureCubeSubgraphOutputParameter ),new BlackboardConfig( "TextureCube", TypeColors[typeof( Texture )] ) },

			{ typeof( BoolParameter ), new BlackboardConfig( "bool", TypeColors[typeof( bool )] ) },
			{ typeof( IntParameter ), new BlackboardConfig( "int", TypeColors[typeof( int )] ) },
			{ typeof( FloatParameter ), new BlackboardConfig( "float", TypeColors[typeof( float )] ) },
			{ typeof( Float2Parameter ), new BlackboardConfig( "float2", TypeColors[typeof( Vector2 )] ) },
			{ typeof( Float3Parameter ), new BlackboardConfig( "float3", TypeColors[typeof( Vector3 )] ) },
			{ typeof( Float4Parameter ), new BlackboardConfig( "float4", TypeColors[typeof( Vector4 )] ) },
			{ typeof( ColorParameter ), new BlackboardConfig( "Color", TypeColors[typeof( Color )] ) },
			{ typeof( SamplerStateParameter ), new BlackboardConfig( "SamplerState", TypeColors[typeof( Sampler )] ) },
			{ typeof( Texture2DParameter ), new BlackboardConfig( "Texture2D", TypeColors[typeof( Texture )] ) },
			{ typeof( TextureCubeParameter ), new BlackboardConfig( "TextureCube", TypeColors[typeof( Texture )] ) },

			{ typeof( ShaderFeatureBooleanParameter ), new BlackboardConfig( "ShaderFeature", DefaultTypeColor ) },
			{ typeof( ShaderFeatureEnumParameter ), new BlackboardConfig( "ShaderFeature", DefaultTypeColor ) },
		};
	}

	public static Color GetTypeColor( BlackboardParameter parameter )
	{
		if ( ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( parameter.GetType(), out var blackboardConfig ) )
		{
			return blackboardConfig.Color;
		}

		return Theme.TextControl;
	}
}
