
namespace ShaderGraphPlus.Nodes;

[Title( "Oscillator" ), Category( "Utility" ), Icon( "waves" )]
public sealed class OscillatorNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[Hide]
	public string Oscillator => @"
float Oscillator( float flTime, float flFrequency, float flPhase, float flStrength )
{
	float amplitude;

	if( flFrequency > 0.0001f )
	{
		float period = 1.0f / flFrequency;
		float currentPhase = ( fmod( flTime, period ) * flFrequency ) + flPhase / 255.0f;
		amplitude = flStrength * sin( currentPhase * 3.1415926535897932f * 2.0f );
	}
	else
	{
		amplitude = flStrength;
	}

	return amplitude;
}
";

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Time { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Frequency { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Phase { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Strength { get; set; }

	[InputDefault( nameof( Frequency ) )]
	public float DefaultFrequency { get; set; } = 1.0f;

	[InputDefault( nameof( Phase ) )]
	public float DefaultPhase { get; set; } = 0.0f;

	[InputDefault( nameof( Strength ) )]
	public float DefaultStrength { get; set; } = 10.0f;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var timeResult = compiler.Result( Time );
		var frequencyResult = compiler.ResultOrDefault( Frequency, DefaultFrequency );
		var phaseResult = compiler.ResultOrDefault( Phase, DefaultPhase );
		var strengthResult = compiler.ResultOrDefault( Strength, DefaultStrength );

		var func = compiler.RegisterHLSLFunction( Oscillator, "Oscillator" );
		var funcCall = compiler.ResultHLSLFunction( func, $"{(Time.IsValid() ? timeResult.Code : "g_flTime")}, {frequencyResult}, {phaseResult}, {strengthResult}" );

		return new NodeResult( ResultType.Float, funcCall );
	};
}
