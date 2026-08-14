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

	/// <summary>
	/// Lets <seealso cref="ShaderGraphPlus"/> know whether to fallback to the Opaque blend mode or not.
	/// </summary>
	public bool SupportsNoBlendModes => !SupportsOpaqueBlend && !SupportsMaskedBlend && !SupportsTranslucentBlend && !SupportsDynamicBlend;

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
			template.ShaderDomain == ShaderDomain.Surface && template.OpaqueBlend,
			template.ShaderDomain == ShaderDomain.Surface && template.MaskedBlend,
			template.ShaderDomain == ShaderDomain.Surface && template.TranslucentBlend,
			template.ShaderDomain == ShaderDomain.Surface && template.DynamicBlend,
			template.ShaderDomain == ShaderDomain.Surface && template.Opacity,
			template.ShaderDomain == ShaderDomain.Surface && template.PositionOffset
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

	/// <summary>
	/// Should this template enforce RenderFace to the specified option
	/// </summary>
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
	[InfoBox( "When no blend modes are toggled, the opaque blending mode will be used", "info", EditorTint.Blue )]
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
				templateErrors.Add( $"Shader Template \"{path}\" missing tag '{tag.Key}'" );

				continue;
			}

			// Detect duplicate tags
			var firstIndex = Code.IndexOf( tag.Key );

			if ( firstIndex != Code.LastIndexOf( tag.Key ) && firstIndex != -1 )
			{
				templateErrors.Add( $"Shader Template \"{path}\" contains more than 1 '{tag.Key}' tag" );
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
