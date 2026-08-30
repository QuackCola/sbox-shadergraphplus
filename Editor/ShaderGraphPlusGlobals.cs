namespace ShaderGraphPlus;

internal static class ShaderGraphPlusGlobals
{
	internal static class GraphCompiler
	{
		internal const int NoNodePreviewID = 0;
	}

	internal static class ControlWidgetCustomEditors
	{
		internal const string UIGroupEditor = "sgp_UiGroupEditor";
		internal const string ShaderFeatureEnumPreviewIndexEditor = "sgp_ShaderFeatureEnumPreviewIndexEditor";
		internal const string NamedRerouteReferenceEditor = "sgp_NamedRerouteReferenceEditor";
		internal const string PortTypeChoiceEditor = "sgp_PortTypeChoiceEditor";
		internal const string ShaderTypeDropdownEditor = "sgp_ShaderTypeDropdown";
	}
	
	internal static class EditorEvents
	{
		internal const string ShaderGraphPlusEditorCreated = "sgp_EditorCreated";
		internal const string SubgraphUpdate = "sgp_UpdateSubgraph";
		internal const string ShaderTemplateUpdate = "sgp_ShaderTemplateUpdate";
	}

	internal const string CleanName = "ShaderGraphPlus";

	internal const string AssetTypeName = "Shader Graph Plus";
	internal const string AssetTypeExtension = "sgrph";

	internal const string ShaderGraphEditorStateCookieName = "ShaderGraphPlus";

	internal const string SubgraphAssetTypeName = "Shader Graph Plus Function";
	internal const string SubgraphAssetTypeExtension = "sgpfunc";

	internal const string ShaderTemplateCleanName = "ShaderTemplate";
	internal const string ShaderTemplateAssetTypeName = "Shader Template";
	internal const string ShaderTemplateAssetTypeExtension = "shdrtpl";

	internal const string ShaderTemplateEditorStateCookieName = "ShaderTemplateEditor";
	internal const string ShaderTemplateEditorToolbarName = "ShaderTemplateEditorToolbar";
}
