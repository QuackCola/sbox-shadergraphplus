using Editor;
using EditorExtras;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using System.Globalization;

using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// How a dragged item lands on the item it's over.
/// </summary>
internal enum DropMode
{
	None,
	Above,
	Below,
	Onto,
}

/// <summary>
/// A parameter row or a group header: something that can be dragged within the blackboard and dropped onto.
/// </summary>
internal abstract class BlackboardItem : Panel
{
	protected BlackboardPanel Blackboard { get; }

	/// <summary>
	/// The drop indicator shown on this item: a line above or below it, or an outline around it.
	/// </summary>
	internal DropMode DropIndicator
	{
		get;
		set
		{
			if ( field == value ) return;

			field = value;
			SetClass( "drop-above", value == DropMode.Above );
			SetClass( "drop-below", value == DropMode.Below );
			SetClass( "drop-onto", value == DropMode.Onto );
		}
	}

	protected BlackboardItem( BlackboardPanel blackboard )
	{
		Blackboard = blackboard;

		// The line or outline itself, shown by .drop-above, .drop-below and .drop-onto
		Add.Panel( "drop" );
	}

	/// <summary>
	/// Where <paramref name="source"/> would land if it were dropped here, or None if it can't be.
	/// </summary>
	internal abstract DropMode GetDrop( BlackboardItem source );

	/// <summary>
	/// Carry out the drop <see cref="GetDrop"/> described.
	/// </summary>
	protected abstract void ApplyDrop( BlackboardItem source, DropMode mode );

	protected override void OnDragStart( DragEvent e )
	{
		AddClass( "dragging" );
		Blackboard.BeginDrag( this );
		e.StopPropagation();
	}

	/// <summary>
	/// The drag is ours, not a scroll of the list.
	/// </summary>
	protected override void OnDrag( DragEvent e ) => e.StopPropagation();

	protected override void OnDragEnd( DragEvent e )
	{
		RemoveClass( "dragging" );
		Blackboard.EndDrag( this );
		e.StopPropagation();
	}

	protected override void OnDragCancel( DragEvent e ) => OnDragEnd( e );

	/// <summary>
	/// Released over this item or one of its children, the event bubbling up to here. The source is the event's target.
	/// </summary>
	protected override void OnDrop( PanelEvent e )
	{
		// Files and text from outside the app aren't ours
		if ( e is DropEvent || e.Target is not BlackboardItem source ) return;

		e.StopPropagation();

		var mode = GetDrop( source );
		DropIndicator = DropMode.None;

		if ( mode != DropMode.None )
			ApplyDrop( source, mode );
	}
}

/// <summary>
/// A group's title in the list: collapse arrow, folder, name and parameter count. Click to collapse or expand,
/// drop parameters on it to move them in, drag it to reorder the groups, right-click for group actions.
/// </summary>
internal sealed class ParameterGroupHeader : BlackboardItem
{
	private readonly bool _collapsible;

	internal GroupData Group { get; }

	/// <summary>
	/// The group everything starts in. It always stays first.
	/// </summary>
	private bool IsGeneral => Group.Name.Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase );

	public ParameterGroupHeader( BlackboardPanel blackboard, GroupData group, int count, bool collapsed, bool collapsible ) : base( blackboard )
	{
		Group = group;
		group.Graph = blackboard.Graph;
		_collapsible = collapsible;

		var title = BlackboardPanel.GroupTitle( group.Name );

		AddClass( "sg-group" );
		SetClass( "collapsed", collapsed );
		SetClass( "fixed", !collapsible );

		this.Icon( collapsed ? "chevron_right" : "expand_more", "arrow" );
		this.Icon( "folder", "folder" );
		Add.Label( title, "title" );
		Add.Label( count.ToString(), "count" );

		var reorder = IsGeneral ? "" : "\nDrag to reorder this group.";

		Tooltip = collapsible
			? $"{title}\nClick to collapse or expand.\nDrag parameters here to move them into this group.{reorder}\nRight-click for group actions."
			: $"{title}\nMatching groups are expanded while filtering.\nDrag parameters here to move them into this group.{reorder}\nRight-click for group actions.";
	}

	/// <summary>
	/// The General group stays put, so it isn't dragged; that leaves the drag to scroll the list.
	/// </summary>
	public override bool WantsDrag => !IsGeneral;

	protected override void OnClick( MousePanelEvent e )
	{
		if ( !_collapsible ) return;

		e.StopPropagation();
		Blackboard.ToggleGroup( IsGeneral ? "" : Group.Name );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		e.StopPropagation();
		Blackboard.OpenGroupMenu( this );
	}

	/// <summary>
	/// A parameter dropped here moves into the group. A group dropped here takes its place: below when it came from
	/// above, above when it came from below. Nothing lands on or moves the General group.
	/// </summary>
	internal override DropMode GetDrop( BlackboardItem source )
	{
		switch ( source )
		{
			case ParameterRow row:
				return BlackboardPanel.GroupTitle( BlackboardPanel.GroupName( row.Parameter ) ).Equals( Group.Name, StringComparison.OrdinalIgnoreCase )
					? DropMode.None
					: DropMode.Onto;

			case ParameterGroupHeader header when header != this && !header.IsGeneral && !IsGeneral:
				var graph = Blackboard.Graph;

				return graph.GetGroupDataIndex( header.Group ) <= graph.GetGroupDataIndex( Group )
					? DropMode.Below
					: DropMode.Above;

			default:
				return DropMode.None;
		}
	}

	protected override void ApplyDrop( BlackboardItem source, DropMode mode )
	{
		switch ( source )
		{
			case ParameterRow row:
				Blackboard.MoveToGroup( row.Parameter, Group.Name );
				break;

			case ParameterGroupHeader header:
				Blackboard.ReorderGroup( header.Group, Blackboard.Graph.GetGroupDataIndex( Group ) );
				break;
		}
	}
}

/// <summary>
/// The text field that stands in for a row's name while it's renamed. Enter or losing focus commits, Escape cancels.
/// </summary>
internal sealed class RenameEntry : Sandbox.UI.TextEntry
{
	private readonly ParameterRow _row;
	private bool _cancelled;
	private bool _selected;

	public RenameEntry( ParameterRow row, string text )
	{
		_row = row;
		AddClass( "rename" );
		Text = text;
	}

	public override void Tick()
	{
		base.Tick();

		// Selecting before the label has had a frame to lay its text out does nothing
		if ( _selected || !HasFocus ) return;

		_selected = true;
		Label.ShouldDrawSelection = true;
		Label.SetSelection( 0, TextLength );
		CaretPosition = TextLength;
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		// The base cancels by blurring, and blurring commits, so say so first
		if ( e.Button == "escape" ) _cancelled = true;

		base.OnButtonTyped( e );
	}

	protected override void OnBlur( PanelEvent e )
	{
		base.OnBlur( e );
		_row.EndRename( _cancelled ? null : Text );
	}
}

/// <summary>
/// One parameter: a strip and type label in the type's color, the name, its tags, and the default value.
/// </summary>
internal sealed class ParameterRow : BlackboardItem
{
	private readonly Sandbox.UI.Label _nameLabel;
	private RenameEntry _rename;

	internal BlackboardParameter Parameter { get; }

	/// <summary>
	/// The group data the row was built under.
	/// </summary>
	internal GroupData Group { get; }

	/// <summary>
	/// The group and name the row was built with, to tell when the parameter has moved on and the list is stale.
	/// </summary>
	internal string BuiltGroup { get; }

	/// <inheritdoc cref="BuiltGroup"/>
	internal string BuiltName { get; }

	internal bool IsRenaming => _rename is not null;

	public ParameterRow( BlackboardPanel blackboard, BlackboardParameter parameter, GroupData group ) : base( blackboard )
	{
		Parameter = parameter;
		Group = group;
		BuiltGroup = BlackboardPanel.GroupName( parameter );
		BuiltName = parameter.Name;

		AddClass( "sg-param" );

		var config = TypeConfig( parameter );

		Style.BorderLeftColor = config.Color;

		// Selection tint, shown by .selected. Toggling the row's own background left it magenta.
		Add.Panel( "highlight" ).Style.BackgroundColor = config.Color.WithAlpha( 0.2f );

		AddTypeChip( this, config );

		_nameLabel = Add.Label( parameter.Name, "name" );

		var tags = Add.Panel( "tags" );
		foreach ( var tag in Tags( parameter ) )
		{
			tags.Add.Label( tag, "tag" );
		}

		AddValue( this, parameter );

		OnTooltip = BuildTooltip;
	}

	/// <summary>
	/// Full name, type, description, a big preview for textures, then default, range, group and tags.
	/// </summary>
	private void BuildTooltip( Panel tip )
	{
		// Tooltips open in their own window, so bring the styles along
		tip.StyleSheet.AddInline( ShaderGraphPlusStyleSheets.ShaderGraphPlusScss, "shadergraphplus_style" );
		tip.AddClass( "sg-tip" );

		var head = tip.Add.Panel( "head" );
		AddTypeChip( head, TypeConfig( Parameter ) );
		head.Add.Label( Parameter.Name, "name" );

		if ( !string.IsNullOrWhiteSpace( Parameter.DisplayInfo.Description ) )
			tip.Add.Label( Parameter.DisplayInfo.Description, "description" );

		if ( TextureOf( Parameter ) is { DefaultTexture: { Length: > 0 } texturePath } )
		{
			tip.Add.Panel( "preview" ).Style.BackgroundImage = Texture.Load( texturePath );
			Detail( tip, "Texture" ).Add.Label( texturePath, "text" );
		}
		else if ( Parameter is not IBlackboardSubgraphOutputParameter )
		{
			AddValue( Detail( tip, "Default" ), Parameter );
		}

		if ( Parameter is IRangedBlackboardMaterialParameter ranged )
			Detail( tip, "Range" ).Add.Label( $"{Format( ranged.GetRangeMin() )} – {Format( ranged.GetRangeMax() )}", "text" );

		Detail( tip, "Group" ).Add.Label( Parameter.GetGroupTitle(), "text" );

		// The range has its own line above
		var tags = Tags( Parameter ).Where( x => !x.Contains( '–' ) ).ToList();
		if ( tags.Count > 0 )
		{
			var row = tip.Add.Panel( "tags" );
			foreach ( var tag in tags ) row.Add.Label( tag, "tag" );
		}

		tip.Add.Label( "Drag onto the graph to create a node, or onto another parameter to reorder. Double-click or F2 to rename. Right-click for more.", "hint" );
	}

	private static Panel Detail( Panel tip, string key )
	{
		var row = tip.Add.Panel( "detail" );
		row.Add.Label( key, "key" );
		return row;
	}

	/// <summary>
	/// Highlight the row in the type's color.
	/// </summary>
	internal void SetSelected( bool selected ) => SetClass( "selected", selected );

	/// <summary>
	/// Dragged onto the graph, the row becomes a node for its parameter; dragged within the blackboard
	/// it moves. While it's being renamed it's a text field, not a drag handle.
	/// </summary>
	public override bool WantsDrag => !IsRenaming;

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		if ( e.Button == "mouseleft" )
			Blackboard.Select( Parameter );
	}

	protected override void OnDoubleClick( MousePanelEvent e )
	{
		if ( e.Button != "mouseleft" ) return;

		e.StopPropagation();
		StartRename();
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		e.StopPropagation();
		Blackboard.Select( Parameter );
		Blackboard.OpenParameterMenu( this );
	}

	/// <summary>
	/// Swap the name for a text field until Enter, Escape, or focus loss.
	/// </summary>
	internal void StartRename()
	{
		if ( IsRenaming ) return;

		_rename = new RenameEntry( this, Parameter.Name );
		AddChild( _rename );
		SetChildIndex( _rename, GetChildIndex( _nameLabel ) + 1 );
		AddClass( "renaming" );

		_rename.Focus();
	}

	/// <summary>
	/// Put the name back. <paramref name="text"/> is what to rename the parameter to, or null to keep its name.
	/// </summary>
	internal void EndRename( string text )
	{
		if ( _rename is null ) return;

		var entry = _rename;
		_rename = null;
		entry.Delete( true );
		RemoveClass( "renaming" );

		if ( text is not null )
			Blackboard.Rename( Parameter, text );
	}

	/// <summary>
	/// Same group: the dragged row takes the target's slot, so it lands below when it came from above and above
	/// when it came from below. Another group: below when that group is further down, above when it's further up.
	/// </summary>
	internal override DropMode GetDrop( BlackboardItem source )
	{
		if ( source is not ParameterRow dragged || dragged.Parameter == Parameter )
			return DropMode.None;

		if ( dragged.Group.Name.Equals( Group.Name, StringComparison.OrdinalIgnoreCase ) )
		{
			var references = Group.ParameterReferences;

			return references.IndexOf( dragged.Parameter.Identifier ) < references.IndexOf( Parameter.Identifier )
				? DropMode.Below
				: DropMode.Above;
		}

		var graph = Blackboard.Graph;

		return graph.GetGroupDataIndex( Group ) > graph.GetGroupDataIndex( dragged.Group )
			? DropMode.Below
			: DropMode.Above;
	}

	protected override void ApplyDrop( BlackboardItem source, DropMode mode )
	{
		if ( source is ParameterRow dragged )
			Blackboard.DropParameter( dragged, this, mode );
	}

	/// <summary>
	/// The label and color of a parameter's type.
	/// </summary>
	internal static BlackboardConfig TypeConfig( IBlackboardParameter parameter )
	{
		return ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( parameter.GetType(), out var config )
			? config
			: new BlackboardConfig( parameter.DisplayInfo.Name, ShaderGraphPlusTheme.DefaultTypeColor );
	}

	internal static void AddTypeChip( Panel parent, BlackboardConfig config )
	{
		// Box that centers the label vertically
		var chip = parent.Add.Panel( "sg-type" );
		chip.Style.BackgroundColor = config.Color.WithAlpha( 0.12f );
		chip.Style.BorderColor = config.Color.WithAlpha( 0.45f );
		chip.Add.Panel( "dot" ).Style.BackgroundColor = config.Color;
		chip.Add.Label( config.Name, "text" ).Style.FontColor = config.Color;
	}

	private static void AddValue( Panel parent, IBlackboardParameter parameter )
	{
		var value = parent.Add.Panel( "value" );

		switch ( parameter )
		{
			case ColorParameter color:
				AddSwatch( value, color.Value );
				break;

			case ColorSubgraphInputParameter color:
				AddSwatch( value, color.Value );
				break;

			case BlackboardTextureMaterialParameter texture:
				AddTexture( value, texture.Value );
				break;

			case IBlackboardSubgraphInputParameter when parameter.GetValue() is TextureInput texture:
				AddTexture( value, texture );
				break;

			case ShaderFeatureBooleanParameter feature:
				value.Add.Label( feature.Preview ? "On" : "Off", "text" );
				break;

			case ShaderFeatureEnumParameter feature:
				value.Add.Label( feature.Options.ElementAtOrDefault( feature.PreviewIndex )?.Name ?? "", "text" );
				break;

			case SamplerStateParameter sampler:
				value.Add.Label( sampler.Value.Filter.ToString(), "text" );
				break;

			case IBlackboardSubgraphInputParameter when parameter.GetValue() is Sampler sampler:
				value.Add.Label( sampler.Filter.ToString(), "text" );
				break;

			default:
				if ( parameter is not IBlackboardSubgraphOutputParameter && Format( parameter.GetValue() ) is { } text )
					value.Add.Label( text, "text" );
				break;
		}
	}

	private static void AddSwatch( Panel value, Color color )
	{
		var swatch = value.Add.Panel( "swatch" );
		swatch.Style.BackgroundColor = color;
		value.Add.Label( color.Hex, "text" );
	}

	private static void AddTexture( Panel value, TextureInput texture )
	{
		if ( !string.IsNullOrWhiteSpace( texture.DefaultTexture ) )
		{
			// The asset browser's thumbnail; the path is on hover
			var thumb = value.Add.Panel( "thumb" );
			thumb.Style.BackgroundImage = Texture.Load( $"{texture.DefaultTexture}" );
			thumb.Tooltip = texture.DefaultTexture;
			return;
		}

		AddSwatch( value, texture.DefaultColor );
	}

	private static TextureInput? TextureOf( IBlackboardParameter parameter ) => parameter switch
	{
		BlackboardTextureMaterialParameter t => t.Value,
		IBlackboardSubgraphInputParameter when parameter.GetValue() is TextureInput t => t,
		_ => null,
	};

	/// <summary>
	/// Short labels for the settings that change how the parameter compiles or shows up.
	/// </summary>
	private static IEnumerable<string> Tags( IBlackboardParameter parameter )
	{
		if ( parameter is IBlackboardMaterialParameter { IsAttribute: true } )
			yield return "Attribute";

		if ( parameter is IRangedBlackboardMaterialParameter ranged && ranged.GetRangeMin() is int or float )
			yield return $"{Format( ranged.GetRangeMin() )} – {Format( ranged.GetRangeMax() )}";

		if ( parameter is IBlackboardMaterialParameter { UI: FloatParameterUI { Type: UIType.Slider } } )
			yield return "Slider";

		if ( TextureOf( parameter ) is { } tex )
		{
			if ( tex.SrgbRead ) yield return "sRGB";
			if ( tex.ColorSpace == TextureColorSpace.Linear ) yield return "Linear";
			yield return tex.ImageFormat.ToString();
			if ( tex.Processor != TextureProcessor.None ) yield return tex.Processor.ToString();
			if ( tex.IsAttribute ) yield return "Attribute";
		}

		var sampler = parameter switch
		{
			SamplerStateParameter s => s.Value,
			IBlackboardSubgraphInputParameter when parameter.GetValue() is Sampler s => s,
			_ => (Sampler?)null,
		};

		if ( sampler is { } smp )
		{
			yield return smp.AddressModeU == smp.AddressModeV && smp.AddressModeV == smp.AddressModeW
				? smp.AddressModeU.ToString()
				: $"{smp.AddressModeU}/{smp.AddressModeV}/{smp.AddressModeW}";
			if ( smp.Filter == FilterMode.Anisotropic ) yield return $"x{smp.MaxAnisotropy}";
			if ( smp.IsAttribute ) yield return "Attribute";
		}

		if ( parameter is IBlackboardSubgraphInputParameter { IsRequired: true } )
			yield return "Required";

		if ( parameter is IBlackboardSubgraphOutputParameter { Preview: not SubgraphOutputPreviewType.None } output )
			yield return $"Preview {output.Preview}";

		if ( parameter is ShaderFeatureEnumParameter feature )
			yield return $"{feature.Options.Count} options";
	}

	private static string Format( object value ) => value switch
	{
		bool b => b ? "true" : "false",
		int i => i.ToString( CultureInfo.InvariantCulture ),
		float f => Number( f ),
		Vector2 v => $"{Number( v.x )}, {Number( v.y )}",
		Vector3 v => $"{Number( v.x )}, {Number( v.y )}, {Number( v.z )}",
		Vector4 v => $"{Number( v.x )}, {Number( v.y )}, {Number( v.z )}, {Number( v.w )}",
		_ => null,
	};

	private static string Number( float value ) => value.ToString( "0.###", CultureInfo.InvariantCulture );
}
