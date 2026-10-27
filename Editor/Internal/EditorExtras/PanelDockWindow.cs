using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlusPanelTest;

namespace EditorExtras;

/// <summary>
/// A window that is built from docking windows.
/// </summary>
public class PanelDockWindow : Panel
{
	private readonly List<RootPanel> _floatingRoots = new();

	private Sandbox.UI.Label _maximizeIcon;
	private Sandbox.UI.Label _windowLabel;
	private Sandbox.UI.Label _titleBarLabel;

	protected string _defaultLayout;
	protected string _savedLayout;

	public DockHost DockHost { get; private set; }
	public PanelDockWindows DockWindows { get; private set; }
	public virtual Sandbox.UI.MenuBar MenuBar { get; set; }

	public string WindowTitle
	{
		get
		{
			return _windowLabel.Text;
		}
		set
		{
			_windowLabel.Text = value;
		}
	}

	/// <summary>
	/// The window this panel is shown in. Looked up rather than stored, so it survives hotload.
	/// </summary>
	protected PanelWindow Window => PanelWindow.FromPanel( this );

	public PanelDockWindow() : base()
	{
		AddClass( "editor-window" );

		StyleSheet.Load( "/styles/editor.scss" );
		StyleSheet.Load( "/styles/gallery.scss" );

		EditorEvent.Register( this );

		Init();
	}

	private void Init()
	{
		var titleBar = Add.Panel( "titlebar window-drag" );
		_titleBarLabel = titleBar.Add.Label( "s&", "brand" );

		// Menus live in the title bar, same as the editor's
		MenuBar = titleBar.AddChild( new Sandbox.UI.MenuBar() );
		MenuBar.AddClass( "window-nodrag" );

		titleBar.Add.Panel( "grow" );

		// Centred over the bar, so the menus on the left don't push it off centre
		var titleWrap = titleBar.Add.Panel( "titlewrap" );
		_windowLabel = titleWrap.Add.Label( "", "title" );

		WindowButton( titleBar, "remove", null, () => Window?.Minimize() );
		_maximizeIcon = WindowButton( titleBar, "crop_square", null, () =>
		{
			Window?.ToggleMaximized();
			OnMaximize();
		} );
		WindowButton( titleBar, "close", "close", Quit );

		Add.Panel( "accentline" );
	}

	public void SetWindowIcon( string name )
	{
		if ( _titleBarLabel is not null )
		{
			_titleBarLabel.Text = name;
		}
	}

	protected virtual void CreateUI()
	{
		DockHost = AddChild( new DockHost() );
		DockWindows = new PanelDockWindows( DockHost ) { ConfigureWindow = ConfigureFloatingWindow };
	}

	protected void ShowDock( string dockId )
	{
		var host = DockHost.Find( dockId ) is not null ? DockHost : _floatingRoots
			.Where( x => x.IsValid )
			.SelectMany( x => x.Descendants.OfType<DockHost>() )
			.FirstOrDefault( x => x.Find( dockId ) is not null );
		if ( host is null ) return;
		if ( !host.IsOpen( dockId ) ) host.Dock( dockId );
		host.Activate( dockId );
		PanelWindow.FromPanel( host )?.Focus();
	}

	private void ConfigureFloatingWindow( RootPanel root )
	{
		root.AddClass( "mock-editor-surface" );
		root.AddClass( "editor-window" );

		root.StyleSheet.Load( "/styles/gallery.scss" );
		_floatingRoots.Add( root );

		// A floated tool no longer bubbles keys through EditorWindow.
		var host = root.Children.OfType<DockHost>().Single();
		var shortcuts = root.AddChild( new FloatingShortcuts( this ) );
		host.Parent = shortcuts;
	}

	public void OnMaximize()
	{
		if ( Window is not { } window )
			return;

		_maximizeIcon.Text = window.IsMaximized ? "filter_none" : "crop_square";
	}

	/// <summary>
	/// Take a key pressed while this window, or a tool floated out of it, has focus. True when it
	/// did something.
	/// </summary>
	/// <param name="e"></param>
	/// <param name="scope">The panel the key came through</param>
	/// <returns></returns>
	protected internal virtual bool HandleShortcut( ButtonEvent e, Panel scope )
	{
		return false;
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( HandleShortcut( e, this ) )
		{
			e.StopPropagation = true;
			return;
		}

		base.OnButtonTyped( e );
	}

	protected virtual void Quit()
	{
		Close();
	}

	protected void Close()
	{
		Window?.Dispose();
	}

	public override void OnDeleted()
	{
		EditorEvent.Unregister( this );
		DockWindows?.Dispose();
		_floatingRoots.Clear();

		base.OnDeleted();
	}

	[EditorEvent.Hotload]
	private void OnHotloadInternal()
	{
		// A closed window can still be registered - one closed by the old code, before this hotload
		if ( !IsValid || IsDeleting )
			return;

		// Rebuilding adds a new title bar, toolbar and dock host, so drop the old ones and their floating windows first
		DockWindows?.Dispose();
		DeleteChildren( true );

		Init();

		OnHotload();
	}

	protected virtual void OnHotload()
	{
	}

	/// <summary>
	/// A row with a shortcut shown beside it. Rows with nothing to do are just there to look at.
	/// </summary>
	protected static Sandbox.UI.Menu Option( Sandbox.UI.Menu menu, string text, string icon = "", string shortcut = null, Action action = null )
	{
		var option = menu.AddOption( text, icon, action );
		option.Shortcut = shortcut;
		return option;
	}

	protected Sandbox.UI.Label WindowButton( Panel parent, string icon, string classname, Action onClick )
	{
		var button = parent.Clickable( "windowbutton window-nodrag", onClick );
		if ( classname is not null ) button.AddClass( classname );
		return button.Icon( icon );
	}
}

sealed class FloatingShortcuts : Panel
{
	readonly PanelDockWindow _editorWindow;

	internal FloatingShortcuts( PanelDockWindow editor )
	{
		_editorWindow = editor;
		Style.Width = Length.Percent( 100 );
		Style.Height = Length.Percent( 100 );
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( _editorWindow.IsValid && _editorWindow.HandleShortcut( e, this ) )
		{
			e.StopPropagation = true;
			return;
		}

		base.OnButtonTyped( e );
	}
}
