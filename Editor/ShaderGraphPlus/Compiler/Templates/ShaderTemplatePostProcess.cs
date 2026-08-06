#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace ShaderGraphPlus;

public static class ShaderTemplatePostProcess
{
	public static string Code => @"HEADER
{
	Description = ""[sgp_shader_description]"";
}

FEATURES
{
[sgp_shader_feature_defines]
}

MODES
{
	Forward();
	Depth();
	ToolsShadingComplexity( ""tools_shading_complexity.shader"" );
}

COMMON
{
[sgp_shader_common]
}

struct VertexInput
{
	#include ""common/vertexinput.hlsl""
	float4 vColor : COLOR0 < Semantic( Color ); >;
[sgp_vertex_input_data]
};

struct PixelInput
{
	#include ""common/pixelinput.hlsl""
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
[sgp_pixel_input_data]
};

VS
{
	#include ""common/vertex.hlsl""

[sgp_vertex_globals]
[sgp_vertex_combo_rules]
[sgp_vertex_functions]

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i;
		i.vPositionPs = float4(v.vPositionOs.xy, 0.0f, 1.0f );
		i.vPositionWs = float3(v.vTexCoord, 0.0f);
[sgp_vertex_code]
		return i;
	}
}

PS
{
	#include ""common/pixel.hlsl""
	#include ""postprocess/functions.hlsl""
	#include ""postprocess/common.hlsl""

	Texture2D g_tColorBuffer < Attribute( ""ColorBuffer"" ); SrgbRead ( true ); >;

[sgp_pixel_globals]
[sgp_pixel_combo_rules]
[sgp_pixel_functions]

	float4 MainPs( PixelInput i ) : SV_Target0
	{

[sgp_pixel_code]

		return [sgp_pixel_output];
	}
}
";
}
