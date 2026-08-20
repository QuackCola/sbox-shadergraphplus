using static ShaderGraphPlus.ShaderTemplate;

namespace ShaderGraphPlus;

public static class ShaderTemplateSky
{
	public static string Code { get; set; } = @"HEADER
{
	Description = ""/*sgp_shader_description*/"";
}

FEATURES
{
/*sgp_shader_feature_defines*/
}

MODES
{
	Forward();
}

COMMON
{
/*sgp_shader_common*/
}

struct VertexInput
{
	float4 vPositionOs : POSITION < Semantic( PosXyz ); >;
/*sgp_vertex_input_data*/
};

struct PixelInput
{
	// Graph nodes (World Position, view vector, triplanar, ...) emit
	// ""i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz"",
	// so this field has to exist even though the sky builds its own position.
	float3 vPositionWithOffsetWs : TEXCOORD1;
	float3 vRayWs : TEXCOORD2;

	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs : SV_Position;
	#endif
	#if ( PROGRAM == VFX_PROGRAM_PS )
		float4 vPositionSs : SV_Position;
	#endif
/*sgp_pixel_input_data*/
};

VS
{
	// NOTE: no ""common/vertex.hlsl"" here -- it defines ProcessVertex/FinalizeVertex
	// in terms of VS_CommonProcessing, which only exists for the standard vertex
	// input path. The sky box builds its own position, so it isn't needed.
	#include ""system.fxc""

/*sgp_vertex_globals*/
/*sgp_vertex_combo_rules*/
/*sgp_vertex_functions*/

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i;

		// Push the box out to the far plane and keep it centred on the camera
		// so the sky never intersects world geometry.
		float flSkyboxScale = g_flNearPlane + g_flFarPlane;
		float3 vPositionWs = g_vCameraPositionWs.xyz + v.vPositionOs.xyz * flSkyboxScale;

		i.vPositionPs = Position3WsToPs( vPositionWs );

		// Camera-relative, matching the standard vertex path -- graph nodes add
		// g_vHighPrecisionLightingOffsetWs back on to recover absolute world space.
		i.vPositionWithOffsetWs = vPositionWs - g_vHighPrecisionLightingOffsetWs.xyz;
		i.vRayWs = normalize( v.vPositionOs.xyz );
/*sgp_vertex_code*/
		return i;
	}
}

PS
{
	// PixelInput in this shader has none of the standard mesh fields (normals, tangents,
	// lightmap UVs), so Material::Init( PixelInput ) can't read them. This
	// switches it to the empty Material::Init() -- see common/material.hlsl.
	#define CUSTOM_MATERIAL_INPUTS 1

	#include ""common/pixel.hlsl""

/*sgp_pixel_globals*/
/*sgp_pixel_combo_rules*/
/*sgp_pixel_functions*/

	// Sky renders behind everything: no depth write, reversed-Z far plane test.
	RenderState( DepthWriteEnable, false );
	RenderState( DepthEnable, true );
	RenderState( DepthFunc, GREATER_EQUAL );

	// Let the MaterialSystem know this is a sky shader
	BoolAttribute( sky, true );

	float4 MainPs( PixelInput i ) : SV_Target0
	{

/*sgp_pixel_code*/

		return /*sgp_pixel_output*/;
	}
}
";

	internal static ShaderTypeInfo ShaderTypeInfo => new( "Sky", "nights_stay", ShaderDomain.Sky, SupportFlags.None, DefaultInputs );

	internal static List<TemplateInputPlugInfo> DefaultInputs => new()
	{
		{ new( TemplateInputPlugType.Vector3, "Albedo", GraphCompiler.ShaderStage.Pixel ) }
	};
}
