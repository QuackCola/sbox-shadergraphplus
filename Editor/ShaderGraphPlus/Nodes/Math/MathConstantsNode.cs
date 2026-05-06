
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// A container for common math constants
/// </summary>
[Title( "Math Constants" ), Category( "Constants" ), Icon( "functions" ), Order( 7 )]
public sealed class MathConstantsNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.ConstantValueNode;

	[Hide]
	public override string Title => $"{DisplayInfo.For( this ).Name} ({Constant})";

	public enum ConstantValues
	{
		PI,
		TWOPI,
		FOURPI,
		TAU,
		PHI,
		E,
		LOG2E,
		LOG10E,
		LN2,
		LN10,
		SQRT2,
		SQRT1_2
	}

	public ConstantValues Constant { get; set; }

	[Hide]
	private string ConstantResult
	{
		get
		{
			return Constant switch
			{
				ConstantValues.PI => "3.14159265359f",
				ConstantValues.TWOPI => "6.28318530718f",
				ConstantValues.FOURPI => "0.78539816339f",
				ConstantValues.TAU => "6.28318530717f",
				ConstantValues.PHI => "1.6180339887f",
				ConstantValues.E => "2.718282f",
				ConstantValues.LOG2E => "1.44269504088f",
				ConstantValues.LOG10E => "0.43429448190f",
				ConstantValues.LN2 => "0.69314718055f",
				ConstantValues.LN10 => "2.30258509299f",
				ConstantValues.SQRT2 => "1.41421356237f",
				ConstantValues.SQRT1_2 => "0.70710678118f",
				_ => throw new NotImplementedException(),
			};
		}
	}

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var define = compiler.RegisterDefine( $"SGP_M_{Constant}", ConstantResult );

		return new NodeResult( ResultType.Float, define, constant: true );
	};
}
