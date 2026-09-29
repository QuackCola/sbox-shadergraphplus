using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	/// <summary>
	/// Changes : <br/>
	/// - Replace Domain with ShaderType.
	/// </summary>
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 11 )]
	internal static void Upgrader_v11( JsonObject obj )
	{
		if ( obj[JsonKeys.ParameterArray] is not JsonArray oldParameterArray )
			throw new Exception( $"Cannot find jsonArray \"{JsonKeys.ParameterArray}\"" );

		if ( obj[JsonKeys.NodeArray] is not JsonArray oldNodeArray )
			throw new Exception( $"Cannot find jsonArray \"{JsonKeys.NodeArray}\"" );

		JsonUtils.UpdatePropertyKey( obj, "Domain", "ShaderType" );
	}
}
