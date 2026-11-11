using Sandbox.UI;
using Sandbox.UI.Construct;

namespace EditorExtras;

/// <summary>
/// A window titlebar. Has the standard assortment of features - icon, optional menubar, title and window controls.
/// </summary>
public class PanelTitleBar : Panel
{
	private readonly Sandbox.UI.Label _titlebarIcon;
	private readonly Sandbox.UI.Label _titlebarTitle;
	private readonly Sandbox.UI.Label _maximizeIcon;

	public Sandbox.UI.MenuBar MenuBar { get; private init; }

	public string Icon
	{
		get => _titlebarIcon.Text;
		set => _titlebarIcon.Text = value;
	}

	public string Title
	{
		get => _titlebarTitle.Text;
		set => _titlebarTitle.Text = value;
	}

	public string MaximizeControlIcon
	{
		get => _maximizeIcon.Text;
		set => _maximizeIcon.Text = value;
	}

	/// <summary>
	/// When the minimize button is clicked.
	/// </summary>
	public Action OnMinimize { get; set; }

	/// <summary>
	/// When the maximize button is clicked.
	/// </summary>
	public Action OnMaximize { get; set; }

	/// <summary>
	/// When the close button is clicked.
	/// </summary>
	public Action OnClose { get; set; }

	/// <param name="onBuildSpecial">
	/// Any additional items to the left of the window control buttons.
	/// </param>
	public PanelTitleBar( Action<Sandbox.UI.Panel> onBuildSpecial = null ) : base()
	{
		if ( OperatingSystem.IsMacOS() )
			throw new NotSupportedException( "TODO" );

		AddClass( "titlebar window-drag" );
		_titlebarIcon = Add.Label( "s&", "brand" );

		MenuBar = AddChild<Sandbox.UI.MenuBar>( "window-nodrag" );

		Add.Panel( "grow" );

		// Centred over the bar, so the menus on the left don't push it off centre
		var titleWrap = Add.Panel( "titlewrap" );
		_titlebarTitle = titleWrap.Add.Label( "", "title" );

		onBuildSpecial?.Invoke( this );

		WindowButton( this, "remove", null, () => OnMinimize?.Invoke() );
		_maximizeIcon = WindowButton( this, "crop_square", null, () => OnMaximize?.Invoke() );
		WindowButton( this, "close", "close", () => OnClose?.Invoke() );
	}

	protected Sandbox.UI.Label WindowButton( Panel parent, string icon, string classname, Action onClick )
	{
		var button = parent.Clickable( "windowbutton window-nodrag", onClick );
		if ( classname is not null ) button.AddClass( classname );
		return button.Icon( icon );
	}
}
