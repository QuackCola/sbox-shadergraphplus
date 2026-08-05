namespace ShaderGraphPlus;

public static class ShaderTemplate
{
	/// <summary>
	/// Maps user freindly tags to their internal representation to be used in <see cref="ToFormattableString"/>
	/// </summary>
	internal static Dictionary<string, string> TemplateTagMap => new()
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

	/// <summary>
	/// Convert the user defined template code to a formatable string that can be used by <seealso cref="string.Format(string, ReadOnlySpan{object?})"/>
	/// </summary>
	/// <returns>A formatable string</returns>
	public static string ToFormattableString( string code )
	{
		if ( string.IsNullOrWhiteSpace( code ) )
			return "";

		var formatableString = code;

		foreach ( var tag in TemplateTagMap )
		{
			formatableString = formatableString.Replace( tag.Key, tag.Value );
		}

		return formatableString;
	}
}
