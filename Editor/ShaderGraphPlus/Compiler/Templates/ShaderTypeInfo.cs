namespace ShaderGraphPlus;

public static partial class ShaderTemplate
{
	public sealed record ShaderTypeInfo( string Title, string Icon, ShaderDomain Domian, IEnumerable<string> SupportStrings, Dictionary<string, TemplateInputPlugInfo> InputPlugs ) : IValid
	{
		public bool IsValid => !string.IsNullOrWhiteSpace( Title ) && InputPlugs.Any();

		internal ShaderTypeInfo() : this( "", "", ShaderDomain.Surface, new List<string>(), new Dictionary<string, TemplateInputPlugInfo>() )
		{
		}

		/// <summary>
		/// Check to see if the provided support string exists.
		/// </summary>
		public bool HasSupport( string supportString ) => SupportStrings.Contains( supportString );

		/// <summary>
		/// Check to see if an input plug with the provided name exists.
		/// </summary>
		public bool HasInputPlug( string name ) => InputPlugs.ContainsKey( name );

		/// <summary>
		/// Try to get the named input plug.
		/// </summary>
		public bool TryGetInputPlug( string name, out TemplateInputPlugInfo plug )
		{
			return InputPlugs.TryGetValue( name, out plug );
		}

		public static implicit operator ShaderTypeInfo( ShaderTemplateResource userTemplate )
		{
			var templatePlugs = new Dictionary<string, TemplateInputPlugInfo>();
			var supportStrings = new List<string>();

			switch ( userTemplate.ShaderDomain )
			{
				case ShaderDomain.Surface:
					{
						if ( userTemplate.ShadingModel == ShadingModel.Lit )
						{
							templatePlugs = ShaderTemplateSurface.DefaultLitInputs;

							supportStrings.Add( SupportsLitShading );
						}
						else
						{
							templatePlugs = ShaderTemplateSurface.DefaultUnlitInputs;

							supportStrings.Add( SupportsUnlitShading );
						}

						if ( !userTemplate.Opacity )
						{
							templatePlugs.Remove( "Opacity" );
						}

						if ( !userTemplate.PositionOffset )
						{
							templatePlugs.Remove( "PositionOffset" );
						}

						if ( userTemplate.OpaqueBlend && userTemplate.MaskedBlend && userTemplate.TranslucentBlend )
						{
							supportStrings.Add( SupportsAllBlend );
						}
						else
						{
							if ( userTemplate.OpaqueBlend )
							{
								supportStrings.Add( SupportsOpaqueBlend );
							}
							if ( userTemplate.MaskedBlend )
							{
								supportStrings.Add( SupportsMaskedBlend );
							}
							if ( userTemplate.TranslucentBlend )
							{
								supportStrings.Add( SupportsTranslucentBlend );
							}
						}

						break;
					}
				case ShaderDomain.Sky:
					{
						templatePlugs = ShaderTemplateSky.DefaultInputs;
						break;
					}
				case ShaderDomain.PostProcess:
					{
						templatePlugs = ShaderTemplatePostProcess.DefaultInputs;
						break;
					}
				default:
					throw new NotImplementedException( $"Unknown ShaderDomain '{userTemplate.ShaderDomain}'" );
			}

			if ( userTemplate.EnforceRenderFace )
			{
				switch ( userTemplate.RenderFace )
				{
					case RenderFace.Front:
						supportStrings.Add( SupportsRenderFaceFront );
						break;
					case RenderFace.Back:
						supportStrings.Add( SupportsRenderFaceBack );
						break;
					case RenderFace.Both:
						supportStrings.Add( SupportsRenderFaceBoth );
						break;
				}
			}
			else
			{
				supportStrings.Add( SupportsAllRenderFace );
			}

			return new ShaderTypeInfo(
				userTemplate.Title,
				userTemplate.Icon,
				userTemplate.ShaderDomain,
				supportStrings,
				templatePlugs
			);
		}
	}
}
