using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	/// <summary>
	/// Replace Domain with ShaderType
	/// </summary>
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 11 )]
	internal static void Upgrader_v11( JsonObject obj )
	{
		if ( obj[JsonKeys.ParameterArray] is not JsonArray oldParameterArray )
			throw new Exception( $"Cannot find jsonArray \"{JsonKeys.ParameterArray}\"" );

		if ( obj[JsonKeys.NodeArray] is not JsonArray oldNodeArray )
			throw new Exception( $"Cannot find jsonArray \"{JsonKeys.NodeArray}\"" );

		if ( JsonUtils.GetPropertyValue( obj, "Domain", SerializerOptions(), ShaderDomain.Surface, out var shaderDomain ) )
		{
			obj.Remove( "Domain" );

			string shaderType = (shaderDomain) switch
			{
				ShaderDomain.Surface => "Surface",
				ShaderDomain.Sky => "Sky",
				ShaderDomain.PostProcess => "PostProcess",
				_ => throw new NotImplementedException(),
			};

			obj.Add( nameof( ShaderType ), shaderType );
		}
	}
}
