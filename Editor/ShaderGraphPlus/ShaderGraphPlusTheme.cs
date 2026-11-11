using Editor;

namespace ShaderGraphPlus;

internal static partial class ShaderGraphPlusTheme
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

		SetupNodeTheming();
		SetupBlackboardTheming();
	}
}

