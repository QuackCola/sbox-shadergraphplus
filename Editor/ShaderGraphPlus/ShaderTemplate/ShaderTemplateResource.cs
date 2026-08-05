using Editor;

namespace ShaderGraphPlus;

public sealed record UserShaderTemplateInfo( bool SupportsLitShading, bool SupportsUnlitShading, bool SupportsSurfaceDomain, bool SupportsOpaqueBlend, bool SupportsMaskedBlend, bool SupportsTranslucentBlend, bool SupportsDynamicBlend, bool ShowOpacityInput, bool ShowPositionOffset )
{
	public UserShaderTemplateInfo() : this( true, true, true, true, true, true, true, true, true )
	{
	}

	public static implicit operator UserShaderTemplateInfo( ShaderTemplateResource template )
	{
		return new UserShaderTemplateInfo(
			template.ShadingModel == ShadingModel.Lit,
			template.ShadingModel == ShadingModel.Unlit,
			template.ShaderDomain == ShaderDomain.Surface,
			template.OpaqueBlend,
			template.MaskedBlend,
			template.TranslucentBlend,
			template.DynamicBlend,
			template.Opacity,
			template.PositionOffset
		);
	}
}

[AssetType( Name = "Shader Template", Extension = "shdrtpl" )]
public sealed partial class ShaderTemplateResource
{
	/// <summary>
	/// What shader type this shader template represents
	/// </summary>
	[TabPage( "General" )]
	public ShaderDomain ShaderDomain { get; set; } = ShaderDomain.Surface;

	/// <summary>
	/// What shading model this shader template supports
	/// </summary>
	[TabPage( "General" )]
	[HideIf( nameof( ShaderDomain ), ShaderDomain.PostProcess )]
	public ShadingModel ShadingModel { get; set; } = ShadingModel.Lit;

	[TabPage( "General" ), Group( "Supported Optional Material Inputs" )]
	[HideIf( nameof( ShaderDomain ), ShaderDomain.PostProcess )]
	public bool Opacity { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Optional Material Inputs" )]
	[HideIf( nameof( ShaderDomain ), ShaderDomain.PostProcess )]
	public bool PositionOffset { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[HideIf( nameof( ShaderDomain ), ShaderDomain.PostProcess )]
	public bool OpaqueBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[HideIf( nameof( ShaderDomain ), ShaderDomain.PostProcess )]
	public bool MaskedBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[HideIf( nameof( ShaderDomain ), ShaderDomain.PostProcess )]
	public bool TranslucentBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[HideIf( nameof( ShaderDomain ), ShaderDomain.PostProcess )]
	public bool DynamicBlend { get; set; } = true;

	[TabPage( "Code" ), TextArea]
	public string Code { get; set; } = DefaultCode;

	[Hide]
	public static string DefaultCode => @"
HEADER
{{
	Description = ""{sgp_shader_description}"";
}}	

FEATURES
{{
{sgp_shader_feature_defines}
}}

MODES
{{
	Forward();
	Depth();
	ToolsShadingComplexity( ""tools_shading_complexity.shader"" );
}}

COMMON
{{
{sgp_shader_common}
}}

struct VertexInput
{{
	#include ""common/vertexinput.hlsl""
	float4 vColor : COLOR0 < Semantic( Color ); >;
{sgp_vertex_input_data}
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
{sgp_pixel_input_data}
}};

VS
{{
	#include ""common/vertex.hlsl""

{sgp_vertex_globals}
{sgp_vertex_combo_rules}
{sgp_vertex_functions}

	PixelInput MainVs( VertexInput v )
	{{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
{sgp_vertex_code}
		return FinalizeVertex( i );
	}}
}}

PS
{{
	#include ""common/pixel.hlsl""

{sgp_pixel_globals}
{sgp_pixel_combo_rules}
{sgp_pixel_functions}

	float4 MainPs( PixelInput i ) : SV_Target0
	{{

{sgp_pixel_code}

		return {sgp_pixel_output};
	}}
}}
	";

	private static Dictionary<string, string> TemplateTagMap => new()
	{
		{ "{sgp_shader_description}", "{0}" },
		{ "{sgp_shader_feature_defines}", "{1}" },
		{ "{sgp_shader_common}", "{2}" },
		{ "{sgp_vertex_input_data}", "{3}" },
		{ "{sgp_pixel_input_data}", "{4}" },

		{ "{sgp_vertex_globals}", "{9}" },
		{ "{sgp_pixel_globals}", "{5}" },

		{ "{sgp_vertex_combo_rules}", "{10}" },
		{ "{sgp_pixel_combo_rules}", "{11}" },

		{ "{sgp_pixel_functions}", "{12}" },
		{ "{sgp_vertex_functions}", "{13}" },

		{ "{sgp_vertex_code}", "{8}" },

		{ "{sgp_pixel_code}", "{14}\n{6}\n{7}" },
		{ "{sgp_pixel_output}", "{15}" },
	};

	public bool Validate( string path, out IEnumerable<string> errors )
	{
		if ( string.IsNullOrWhiteSpace( Code ) )
		{
			errors = [ "Shader Template has no code!!!" ];
			return false;
		}

		var templateErrors = new List<string>();

		foreach ( var tag in TemplateTagMap )
		{
			if ( !Code.Contains( tag.Key ) )
			{
				templateErrors.Add( $"\"{path}\" Missing tag '{tag.Key}'" );
			}
		}

		errors = templateErrors;

		if ( errors.Any() )
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Convert the user defined template code to a formatable string that can be used by <seealso cref="string.Format(string, ReadOnlySpan{object?})"/>
	/// </summary>
	/// <returns>A formatable string</returns>
	public string ToFormatableString()
	{
		if ( string.IsNullOrWhiteSpace( Code ) )
			return "";

		var formatableString = Code;

		foreach ( var tag in TemplateTagMap )
		{
			formatableString = formatableString.Replace( tag.Key, tag.Value );
		}

		return formatableString;
	}
}
