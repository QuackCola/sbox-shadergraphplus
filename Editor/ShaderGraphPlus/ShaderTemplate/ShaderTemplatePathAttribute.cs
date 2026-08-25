namespace ShaderGraphPlus;

[AttributeUsage( AttributeTargets.Property )]
internal sealed class ShaderTemplatePathAttribute : AssetPathAttribute
{
	public override string AssetTypeExtension => "shdrtpl";
}
