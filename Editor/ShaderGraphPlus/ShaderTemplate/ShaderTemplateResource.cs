using Editor;

namespace ShaderGraphPlus;

public sealed record UserShaderTemplateInfo( bool SupportsLitShading, bool SupportsUnlitShading, bool SupportsOpaqueBlend, bool SupportsMaskedBlend, bool SupportsTranslucentBlend, bool SupportsDynamicBlend, bool ShowOpacityInput, bool ShowPositionOffset )
{
	public UserShaderTemplateInfo() : this( true, true, true, true, true, true, true, true )
	{
	}

	public static implicit operator UserShaderTemplateInfo( ShaderTemplateResource template )
	{
		return new UserShaderTemplateInfo(
			template.ShadingModel == ShadingModel.Lit,
			template.ShadingModel == ShadingModel.Unlit,
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
	/// What shading model this shader template supports
	/// </summary>
	[TabPage( "General" )]
	public ShadingModel ShadingModel { get; set; } = ShadingModel.Lit;

	[TabPage( "General" ), Group( "Supported Optional Material Inputs" )]
	public bool Opacity { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Optional Material Inputs" )]
	public bool PositionOffset { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	public bool OpaqueBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	public bool MaskedBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	public bool TranslucentBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
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
{sgp_vertex_input_data}
}};

struct PixelInput
{{
	#include ""common/pixelinput.hlsl""
{sgp_pixel_input_data}
}};

VS
{{
	#include ""common/vertex.hlsl""
{sgp_vertex_globals}{sgp_vertex_combo_rules}{sgp_vertex_functions}
	PixelInput MainVs( VertexInput v )
	{{
{sgp_vertex_code}
	}}
}}

PS
{{
	#include ""common/pixel.hlsl""
{sgp_pixel_globals}{sgp_pixel_combo_rules}{sgp_pixel_functions}
	float4 MainPs( PixelInput i ) : SV_Target0
	{{
{sgp_pixel_init}
{sgp_pixel_locals}
{sgp_pixel_material}
{sgp_pixel_output}
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

		{ "{sgp_pixel_init}", "{14}" },
		{ "{sgp_pixel_locals}", "{6}" },
		{ "{sgp_pixel_material}", "{7}" },
		{ "{sgp_pixel_output}", "{15}" },
	};

	public bool Validate( string path, out IEnumerable<string> errors )
	{
		var missingTagErrors = new List<string>();

		foreach ( var tag in TemplateTagMap )
		{
			if ( !Code.Contains( tag.Key ) )
			{
				missingTagErrors.Add( $"\"{path}\" Missing tag '{tag.Key}'" );
			}
		}

		errors = missingTagErrors;

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
		var formatableString = Code;

		foreach ( var tag in TemplateTagMap )
		{
			formatableString = formatableString.Replace( tag.Key, tag.Value );
		}

		return formatableString;
	}
}
