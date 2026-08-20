namespace ShaderGraphPlus;

public static class ShaderTemplate
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


	[Flags]
	public enum SupportFlags
	{
		[Hide]
		None = 0,

		OpaqueBlend = 1 << 0,
		MaskedBlend = 1 << 1,
		TranslucentBlend = 1 << 2,

		LitShading = 1 << 3,
		UnlitShading = 1 << 4,

		RenderFaceFront = 1 << 5,
		RenderFaceBack = 1 << 6,
		RenderFaceBoth = 1 << 7,
	}

	public sealed record ShaderTypeInfo( string Title, string Icon, ShaderDomain Domian, IEnumerable<string> SupportInfo, IEnumerable<TemplateInputPlugInfo> InputPlugs ) : IValid
	{
		public bool IsValid => !string.IsNullOrWhiteSpace( Title ) && InputPlugs.Any();

		internal ShaderTypeInfo() : this( "", "", ShaderDomain.Surface, new List<string>(), new List<TemplateInputPlugInfo>() )
		{
		}

		/// <summary>
		/// Check <see cref="SupportInfo"/> to see if the provided support string exists.
		/// </summary>
		public bool Supports( string name ) => SupportInfo.Contains( name );

		public bool Supports( params string[] names ) => names.All( x => SupportInfo.Contains( x ) );
		
		public bool HasPlug( string name ) => InputPlugs.Any( x => x.Name == name );

		public static implicit operator ShaderTypeInfo( ShaderTemplateResource userTemplate )
		{
			var templatePlugs = new List<TemplateInputPlugInfo>();
			var supportInfo = new List<string>();

			switch ( userTemplate.ShaderDomain )
			{
				case ShaderDomain.Surface:
					{
						Dictionary<string, TemplateInputPlugInfo> defaultInputs;

						if ( userTemplate.ShadingModel == ShadingModel.Lit )
						{
							defaultInputs = ShaderTemplateSurface.DefaultInputsLit.ToDictionary( x => x.Name, x => x );
							supportInfo.Add( "SupportsLitShading" );
						}
						else
						{
							defaultInputs = ShaderTemplateSurface.DefaultInputsUnlit.ToDictionary( x => x.Name, x => x );
							supportInfo.Add( "SupportsUnlitShading" );
						}

						if ( !userTemplate.Opacity )
						{
							defaultInputs.Remove( "Opacity" );
						}

						if ( !userTemplate.PositionOffset )
						{
							defaultInputs.Remove( "PositionOffset" );
						}

						templatePlugs = defaultInputs.Select( x => x.Value ).ToList();

						if ( userTemplate.OpaqueBlend )
						{
							supportInfo.Add( "SupportsOpaqueBlend" );
						}
						if ( userTemplate.MaskedBlend )
						{
							supportInfo.Add( "SupportsMaskedBlend" );
						}
						if ( userTemplate.TranslucentBlend )
						{
							supportInfo.Add( "SupportsTranslucentBlend" );
						}

						break;
					}

				case ShaderDomain.Sky:
					templatePlugs = ShaderTemplateSky.DefaultInputs;
					break;

				case ShaderDomain.PostProcess:
					templatePlugs = ShaderTemplatePostProcess.DefaultInputs;
					break;
				
				default:
					throw new NotImplementedException();
			}

			if ( userTemplate.EnforceRenderFace )
			{
				switch ( userTemplate.RenderFace )
				{
					case RenderFace.Front:
						supportInfo.Add( "SupportsRenderFaceFront" );
						break;
					case RenderFace.Back:
						supportInfo.Add( "SupportsRenderFaceBack" );
						break;
					case RenderFace.Both:
						supportInfo.Add( "SupportsRenderFaceBoth" );
						break;
				}
			}
			else
			{
				supportInfo.Add( "SupportsRenderFaceFront" );
				supportInfo.Add( "SupportsRenderFaceBack" );
				supportInfo.Add( "SupportsRenderFaceBoth" );
			}

			return new ShaderTypeInfo(
				userTemplate.Title,
				userTemplate.Icon,
				userTemplate.ShaderDomain,
				supportInfo,
				templatePlugs
			);
		}
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
