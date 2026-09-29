using Editor;

namespace ShaderGraphPlus;

internal static class ShaderGraphPlusTheme
{
	public static Dictionary<Type, Color> TypeColors { get; private set; }
	public static Dictionary<Type, NodeHandleConfig> NodeHandleConfigs { get; private set; }
	public static Dictionary<Type, BlackboardConfig> BlackboardConfigs { get; private set; }

	public static Color DefaultTypeColor => Color.Parse( "#dddddd" )!.Value;

	/// <summary>
	/// Storing the Node Header Primary Colors here so that they are all in one place.
	/// </summary>
	public static class NodeHeaderColors
	{
		public static Color SubgraphNode => Color.Parse( "#e05b0a" )!.Value;
		public static Color GraphResultNode => Color.Parse( "#84705e" )!.Value;
		public static Color MathNode => Color.Parse( "#394d62" )!.Value;
		public static Color UnaryNode => MathNode;
		public static Color BinaryNode => MathNode;
		public static Color ConstantValueNode => Color.Parse( "#736024" )!.Value;
		public static Color ParameterNode => Color.Parse( "#5d9b31" )!.Value;
		public static Color MatrixNode => Color.Parse( "#5d9b31" )!.Value;
		public static Color StageInputNode => Color.Parse( "#803334" )!.Value;
		public static Color GlobalVariableNode => Color.Parse( "#803334" )!.Value;
		public static Color FunctionNode => Color.Parse( "#1d53ac" )!.Value;
		public static Color TransformNode => Color.Parse( "#6c3baa" )!.Value;
		public static Color LogicNode => Color.Parse( "#006b54" )!.Value;
		public static Color ChannelNode => Color.Parse( "#2e2a60" )!.Value;
	}


	static ShaderGraphPlusTheme()
	{
		Update();
	}

	[Event( "hotloaded" )]
	static void Update()
	{
		TypeColors = new()
		{
			{ typeof( bool ), Theme.Blue.AdjustHue( -80 ) },
			{ typeof( int ), Color.Parse( "#ce67e0" )!.Value.AdjustHue( -80 ) },
			{ typeof( float ), Color.Parse( "#8ec07c" )!.Value },
			{ typeof( Vector2 ), Color.Parse( "#ce67e0" )!.Value },
			{ typeof( Vector3 ), Color.Parse( "#7177e1" )!.Value },
			{ typeof( Vector4 ), Color.Parse( "#c7ae32" )!.Value },
			{ typeof( Color ), Color.Parse( "#c7ae32" )!.Value  },
			{ typeof( Float2x2 ), Color.Parse( "#b83385" )!.Value },
			{ typeof( Float3x3 ), Color.Parse( "#b83385" )!.Value },
			{ typeof( Float4x4 ), Color.Parse( "#b83385" )!.Value },
			{ typeof( Gradient ), DefaultTypeColor },
			{ typeof( Sampler ), DefaultTypeColor },
			{ typeof( Texture ), Color.Parse( "#ffb3a7" )!.Value },
		};

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

		BlackboardConfigs = new()
		{
			{ typeof( BoolSubgraphInputParameter ), new BlackboardConfig( "bool", TypeColors[typeof( bool )] ) },
			{ typeof( IntSubgraphInputParameter ), new BlackboardConfig( "int", TypeColors[typeof( int )] ) },
			{ typeof( FloatSubgraphInputParameter ), new BlackboardConfig( "float", TypeColors[typeof( float )] ) },
			{ typeof( Float2SubgraphInputParameter ), new BlackboardConfig( "float2", TypeColors[typeof( Vector2 )] ) },
			{ typeof( Float3SubgraphInputParameter ), new BlackboardConfig( "float3", TypeColors[typeof( Vector3 )] ) },
			{ typeof( Float4SubgraphInputParameter ), new BlackboardConfig( "float4", TypeColors[typeof( Vector4 )] ) },
			{ typeof( ColorSubgraphInputParameter ), new BlackboardConfig( "float4", TypeColors[typeof( Color )] ) },
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
			{ typeof( ColorSubgraphOutputParameter ), new BlackboardConfig( "float4", TypeColors[typeof( Color )] ) },
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
			{ typeof( ColorParameter ), new BlackboardConfig( "float4", TypeColors[typeof( Color )] ) },
			{ typeof( SamplerStateParameter ), new BlackboardConfig( "SamplerState", TypeColors[typeof( Sampler )] ) },
			{ typeof( Texture2DParameter ), new BlackboardConfig( "Texture2D", TypeColors[typeof( Texture )] ) },
			{ typeof( TextureCubeParameter ), new BlackboardConfig( "TextureCube", TypeColors[typeof( Texture )] ) },

			{ typeof( ShaderFeatureBooleanParameter ), new BlackboardConfig( "ShaderFeature", DefaultTypeColor ) },
			{ typeof( ShaderFeatureEnumParameter ), new BlackboardConfig( "ShaderFeature", DefaultTypeColor ) },
		};
	}
}

