using Editor;

namespace ShaderGraphPlus;

[AssetType( Name = ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName, Extension = ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension )]
public sealed partial class ShaderTemplateResource
{

	/// <summary>
	/// The name of the Template in the ShaderType dropdown menu of the ShaderGraphPlus graph settings. Fallsback to asset name if left empty.
	/// </summary>
	[TabPage( "General" )]
	public string Title { get; set; } = "";

	/// <summary>
	/// The icon that will be used in the ShaderType dropdown menu of the ShaderGraphPlus graph settings.
	/// </summary>
	[TabPage( "General" )]
	[IconName]
	public string Icon { get; set; } = "view_in_ar";

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

	[TabPage( "Code" ), TextArea]
	public string Code { get; set; } = ShaderTemplateSurface.Code;

	/// <summary>
	/// Check the template for any errors.
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
