namespace ShaderGraphPlus;

public static partial class ShaderTemplate
{
	/// <summary>
	/// Maps user freindly tags to their internal representation to be used in <see cref="ToFormattableString"/>
	/// </summary>
	internal static Dictionary<string, string> TemplateTagMap => new()
	{
		{ "/*sgp_shader_description*/", "{0}" },
		{ "/*sgp_shader_feature_defines*/", "{1}" },
		{ "/*sgp_shader_common*/", "{2}" },
		{ "/*sgp_vertex_input_data*/", "{3}" },
		{ "/*sgp_pixel_input_data*/", "{4}" },

		{ "/*sgp_vertex_globals*/", "{9}" },
		{ "/*sgp_pixel_globals*/", "{5}" },

		{ "/*sgp_vertex_combo_rules*/", "{10}" },
		{ "/*sgp_pixel_combo_rules*/", "{11}" },

		{ "/*sgp_vertex_functions*/", "{13}" },
		{ "/*sgp_pixel_functions*/", "{12}" },

		{ "/*sgp_vertex_code*/", "{8}" },

		{ "/*sgp_pixel_code*/", "{14}\n{6}\n{7}" },
		{ "/*sgp_pixel_output*/", "{15}" },
	};

	public enum TemplateInputPlugType
	{
		[Icon( "check_box" )]
		Bool,
		[Icon( "looks_one" )]
		Int,
		[Icon( "looks_one" )]
		Float,
		[Title( "Float2" ), Icon( "looks_two" )]
		Vector2,
		[Title( "Float3" ), Icon( "looks_3" )]
		Vector3,
		[Title( "Float4" ), Icon( "looks_4" )]
		Vector4,
		[Title( "Color" ), Icon( "palette" )]
		Color,
	}

	public sealed record TemplateInputPlugInfo( TemplateInputPlugType Plugtype, string Name, string FriendlyName, GraphCompiler.ShaderStage Stage )
	{
		public TemplateInputPlugInfo( TemplateInputPlugType plugtype, string name, GraphCompiler.ShaderStage stage ) : this( plugtype, name, name, stage )
		{
		}
	}

	public sealed record TemplateEntry( string Name, string Path, string Icon )
	{
		internal TemplateEntry( string name, string icon ) : this( name, "", icon )
		{
		}
	}

	internal static Dictionary<string, TemplateEntry> BuiltInTemplateEntries => new()
	{
		{ "Surface", new ( "Surface", "view_in_ar" ) },
		{ "Sky", new ( "Sky", "nights_stay" ) },
		{ "PostProcess", new ( "PostProcess", "desktop_windows" ) },
	};

	/// <summary>
	/// Convert the user defined template code to a formatable string that can be used by <seealso cref="string.Format(string, ReadOnlySpan{object?})"/>
	/// </summary>
	/// <returns>A formatable string</returns>
	public static string ToFormattableString( string code )
	{
		if ( string.IsNullOrWhiteSpace( code ) )
			return "";

		var formatableString = code;

		// double up all { and }
		formatableString = formatableString.Replace( "{", "{{" ).Replace( "}", "}}" );

		// Replace all friendly named tags with the ones used by string.Format()
		foreach ( var tag in TemplateTagMap )
		{
			formatableString = formatableString.Replace( tag.Key, tag.Value );
		}

		return formatableString;
	}
}
