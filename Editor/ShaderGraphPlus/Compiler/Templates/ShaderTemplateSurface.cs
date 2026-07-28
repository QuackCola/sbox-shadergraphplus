namespace ShaderGraphPlus;

public static class ShaderTemplateSurface
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
{9}{10}{13}
	PixelInput MainVs( VertexInput v )
	{{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
{8}
		return FinalizeVertex( i );
	}}
}}

PS
{{
	#include ""common/pixel.hlsl""
{5}{11}{12}
	float4 MainPs( PixelInput i ) : SV_Target0
	{{
{14}
{6}
{7}
{15}
	}}
}}
";

	public static string Material_init => @"
Material m = Material::Init( i );
m.Albedo = float3( 1, 1, 1 );
m.Normal = float3( 0, 0, 1 );
m.Roughness = 1;
m.Metalness = 0;
m.AmbientOcclusion = 1;
m.TintMask = 1;
m.Opacity = 1;
m.Emission = float3( 0, 0, 0 );
m.Transmission = 0;";

	public static string Material_output => @"
m.AmbientOcclusion = saturate( m.AmbientOcclusion );
m.Roughness = saturate( m.Roughness );
m.Metalness = saturate( m.Metalness );
m.Opacity = saturate( m.Opacity );

// Result node takes normal as tangent space, convert it to world space now
m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

// for some toolvis shit
m.WorldTangentU = i.vTangentUWs;
m.WorldTangentV = i.vTangentVWs;
m.TextureCoords = i.vTextureCoords.xy;
		
return ShadingModelStandard::Shade( m );";
}
