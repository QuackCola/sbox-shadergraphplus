using Editor;

namespace ShaderGraphPlus;

/// <summary>
/// Readonly representation of <see cref="ShaderTemplateResource"/>
/// </summary>
public sealed record UserShaderTemplateInfo(
	bool SupportsLitShading,
	bool SupportsUnlitShading,
	bool SupportsSurfaceDomain,
	bool SupportsRenderFaceFront,
	bool SupportsRenderFaceBack,
	bool SupportsRenderFaceBoth,
	bool SupportsOpaqueBlend,
	bool SupportsMaskedBlend,
	bool SupportsTranslucentBlend,
	bool SupportsDynamicBlend,
	bool ShowOpacityInput,
	bool ShowPositionOffset )
{

	public bool SupportsAllRenderFaceModes => SupportsRenderFaceFront && SupportsRenderFaceBack && SupportsRenderFaceBoth;

	public bool SupportsAllBlendModes => SupportsOpaqueBlend && SupportsMaskedBlend && SupportsTranslucentBlend && SupportsDynamicBlend;

	public UserShaderTemplateInfo() : this( true, true, true, true, true, true, true, true, true, true, true, true )
	{
	}

	public static implicit operator UserShaderTemplateInfo( ShaderTemplateResource template )
	{
		return new UserShaderTemplateInfo(
			template.ShadingModel == ShadingModel.Lit,
			template.ShadingModel == ShadingModel.Unlit,
			template.ShaderDomain == ShaderDomain.Surface,
			!template.EnforceRenderFace || template.RenderFace == RenderFace.Front,
			!template.EnforceRenderFace || template.RenderFace == RenderFace.Back,
			!template.EnforceRenderFace || template.RenderFace == RenderFace.Both,
			template.OpaqueBlend,
			template.MaskedBlend,
			template.TranslucentBlend,
			template.DynamicBlend,
			template.Opacity,
			template.PositionOffset
		);
	}
}

[AssetType( Name = ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName, Extension = ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension )]
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
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public ShadingModel ShadingModel { get; set; } = ShadingModel.Lit;

	[TabPage( "General" )]
	[Title( "Enforce Render Face" )]
	[ToggleGroup( "EnforceRenderFace" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public bool EnforceRenderFace { get; set; } = true;

	[TabPage( "General" )]
	[ToggleGroup( "EnforceRenderFace" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public RenderFace RenderFace { get; set; } = RenderFace.Front;

	[TabPage( "General" ), Group( "Supported Optional Material Inputs" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public bool Opacity { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Optional Material Inputs" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public bool PositionOffset { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public bool OpaqueBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public bool MaskedBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public bool TranslucentBlend { get; set; } = true;

	[TabPage( "General" ), Group( "Supported Blend Modes" )]
	[ShowIf( nameof( ShaderDomain ), ShaderDomain.Surface )]
	public bool DynamicBlend { get; set; } = true;

	[TabPage( "Code" ), TextArea]
	public string Code { get; set; } = ShaderTemplateSurface.Code;

	/// <summary>
	/// Validate the template for any issues and output any errors
	/// </summary>
	/// <param name="path">Path to the ShaderTemplate resource</param>
	/// <param name="errors">Issues found with this template</param>
	public bool Validate( string path, out IEnumerable<string> errors )
	{
		if ( string.IsNullOrWhiteSpace( Code ) )
		{
			errors = ["Shader Template has no code!!!"];
			return false;
		}

		var templateErrors = new List<string>();

		foreach ( var tag in ShaderTemplate.TemplateTagMap )
		{
			if ( !Code.Contains( tag.Key ) )
			{
				templateErrors.Add( $"Shader Template \"{path}\" Missing tag '{tag.Key}'" );
			}
		}

		errors = templateErrors;

		if ( errors.Any() )
		{
			return false;
		}

		return true;
	}
}
