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

		public static implicit operator ShaderTypeInfo( ShaderTemplateResource userTemplate )
		{
			var templatePlugs = new Dictionary<string, TemplateInputPlugInfo>();
			var supportStrings = new List<string>();

			switch ( userTemplate.ShaderDomain )
			{
				case ShaderDomain.Surface:
					{
						Dictionary<string, TemplateInputPlugInfo> defaultInputs;

						if ( userTemplate.ShadingModel == ShadingModel.Lit )
						{
							defaultInputs = ShaderTemplateSurface.DefaultLitInputs;

							supportStrings.Add( SupportsLitShading );
						}
						else
						{
							defaultInputs = ShaderTemplateSurface.DefaultUnlitInputs;

							supportStrings.Add( SupportsUnlitShading );
						}

						if ( !userTemplate.Opacity )
						{
							defaultInputs.Remove( "Opacity" );
						}

						if ( !userTemplate.PositionOffset )
						{
							defaultInputs.Remove( "PositionOffset" );
						}

						templatePlugs = defaultInputs;

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
