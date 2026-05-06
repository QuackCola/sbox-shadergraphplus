using Sandbox.Rendering;
using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public struct Sampler : ISGPJsonUpgradeable
{
	[Hide, JsonPropertyName( ShaderGraphPlus.JsonKeys.Version )]
	public readonly int Version => 2;

	/// <summary>
	/// The name of this Sampler.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// If true, this parameter can be modified with <see cref="RenderAttributes"/>.
	/// </summary>
	public bool IsAttribute { get; set; }

	/// <summary>
	/// The texture filtering mode used for sampling (e.g., point, bilinear, trilinear).
	/// </summary>
	public FilterMode Filter { get; init; }

	/// <summary>
	/// The addressing mode used for the U (X) texture coordinate.
	/// </summary>
	public TextureAddressMode AddressModeU { get; init; }

	/// <summary>
	/// The addressing mode used for the V texture coordinate.
	/// </summary>
	public TextureAddressMode AddressModeV { get; init; }

	/// <summary>
	/// The addressing mode used for the W texture coordinate.
	/// </summary>
	public TextureAddressMode AddressModeW { get; init; }

	/// <summary>
	/// The bias applied to the calculated mip level during texture sampling. Positive
	/// values make textures appear blurrier; negative values sharpen.
	/// </summary>
	public float MipLodBias { get; init; }

	/// <summary>
	/// The maximum anisotropy level used for anisotropic filtering. Higher values improve
	/// texture quality at oblique viewing angles.
	/// </summary>
	public int MaxAnisotropy { get; init; }

	/// <summary>
	/// Border color to use if TextureAddressMode.Border is specified
	/// for AddressU, AddressV, or AddressW.
	/// </summary>
	public Color BorderColor { get; init; }

	public Sampler()
	{
		Name = "";
		Filter = FilterMode.Bilinear;
		AddressModeU = TextureAddressMode.Wrap;
		AddressModeV = TextureAddressMode.Wrap;
		AddressModeW = TextureAddressMode.Wrap;
		MipLodBias = 0f;
		MaxAnisotropy = 8;
		BorderColor = Color.Transparent;
	}

	public override readonly int GetHashCode()
	{
		var h = new HashCode();
		h.Add( Filter );
		h.Add( AddressModeU );
		h.Add( AddressModeV );
		h.Add( AddressModeW );
		h.Add( MaxAnisotropy );
		h.Add( MipLodBias );
		h.Add( IsAttribute );
		h.Add( BorderColor );
		return h.ToHashCode();
	}

	public override readonly bool Equals( object obj ) => obj is Sampler other && this == other;

	public static bool operator ==( Sampler a, Sampler b )
	{
		return a.Filter == b.Filter
			&& a.AddressModeU == b.AddressModeU
			&& a.AddressModeV == b.AddressModeV
			&& a.AddressModeW == b.AddressModeW
			&& a.MaxAnisotropy == b.MaxAnisotropy
			&& a.MipLodBias == b.MipLodBias
			&& a.IsAttribute == b.IsAttribute
			&& a.BorderColor == b.BorderColor;
	}

	public static bool operator !=( Sampler a, Sampler b ) => !( a == b );

	public static explicit operator SamplerState( Sampler sampler )
	{
		return new SamplerState()
		{
			Filter = sampler.Filter,
			AddressModeU = sampler.AddressModeU,
			AddressModeV = sampler.AddressModeV,
			AddressModeW = sampler.AddressModeW,
			MipLodBias = sampler.MipLodBias,
			MaxAnisotropy = sampler.MaxAnisotropy,
			BorderColor = sampler.BorderColor,
		};
	}

	[SGPJsonUpgrader( typeof( Sampler ), 2 )]
	public static void Upgrader_v2( JsonObject json )
	{
		if ( !json.ContainsKey( "Filter" ) )
		{
			return;
		}
		if ( !json.ContainsKey( "AddressU" ) )
		{
			return;
		}
		if ( !json.ContainsKey( "AddressV" ) )
		{
			return;
		}

		try
		{
			if ( json["Filter"].ToString() == "Aniso" )
			{
				json["Filter"] = JsonSerializer.SerializeToNode( FilterMode.Anisotropic, ShaderGraphPlus.SerializerOptions() );
			}

			if ( json["AddressU"].ToString() == "Mirror_Once" )
			{
				json["AddressModeU"] = JsonSerializer.SerializeToNode( TextureAddressMode.MirrorOnce, ShaderGraphPlus.SerializerOptions() );
			}

			if ( json["AddressV"].ToString() == "Mirror_Once" )
			{
				json["AddressModeV"] = JsonSerializer.SerializeToNode( TextureAddressMode.MirrorOnce, ShaderGraphPlus.SerializerOptions() );
			}

			json.Remove( "AddressU" );
			json.Remove( "AddressV" );
		}
		catch
		{
		}
	}
}
