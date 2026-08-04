namespace ShaderGraphPlus;

public static class ShaderTemplatePostProcess
{
	public static string Code => @"
HEADER
{{
	Description = ""{0}"";
}}

FEATURES
{{
{1}
}}

MODES
{{
	Forward();
	Depth();
	ToolsShadingComplexity( ""tools_shading_complexity.shader"" );
}}

COMMON
{{
{2}
}}

struct VertexInput
{{
	#include ""common/vertexinput.hlsl""
	float4 vColor : COLOR0 < Semantic( Color ); >;
{3}
}};

struct PixelInput
{{
	#include ""common/pixelinput.hlsl""
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
{4}
}};

VS
{{
	#include ""common/vertex.hlsl""

{9}
{10}
{13}

	PixelInput MainVs( VertexInput v )
	{{
		PixelInput i;
		i.vPositionPs = float4(v.vPositionOs.xy, 0.0f, 1.0f );
		i.vPositionWs = float3(v.vTexCoord, 0.0f);
{8}
		return i;
	}}
}}

PS
{{
	#include ""common/pixel.hlsl""
	#include ""postprocess/functions.hlsl""
	#include ""postprocess/common.hlsl""

	Texture2D g_tColorBuffer < Attribute( ""ColorBuffer"" ); SrgbRead ( true ); >;

{5}
{11}
{12}

	float4 MainPs( PixelInput i ) : SV_Target0
	{{
{14}
{6}
{7}

		return {15};
	}}
}}
";
}
