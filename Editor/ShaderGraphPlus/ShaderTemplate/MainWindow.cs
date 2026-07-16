using Editor;

namespace ShaderGraphPlus;

[EditorForAssetType( "shdrtpl" )]
[EditorApp( "Shader Template Editor", "gradient", "edit shader templates" )]
public class ShaderTemplateEditorWindow : DockWindow, IAssetEditor
{
	private ShaderTemplateResource _template;
	private Asset _asset;
	private bool _dirty = false;

	private Properties _properties;

	public bool CanOpenMultipleAssets => false;

	public ShaderTemplateEditorWindow() : base()
	{
		Size = new Vector2( 640, 480 );

		CreateToolBar();

		CreateUI();
		Show();
		StateCookie = "ShaderTemplateEditor";
		CreateNew();
	}

	void IAssetEditor.SelectMember( string memberName )
	{
		throw new NotImplementedException();
	}

	public void AssetOpen( Asset asset )
	{
		if ( asset == null || string.IsNullOrWhiteSpace( asset.AbsolutePath ) )
			return;

		Open( asset.AbsolutePath );
	}

	private void CreateUI()
	{
		BuildMenuBar();

		_properties = new Properties( this );
		_properties.Target = _template;
		_properties.PropertyUpdated += OnPropertyUpdated;
		
		DockManager.SetCentralWidget( _properties );
	}

	protected override void BuildDefaultLayout()
	{
		DockManager.OpenDock( "Properties", DockArea.Center );
		DockManager.RaiseDock( "Properties" );
	}

	private void PromptSave( Action action )
	{
		if ( !_dirty )
		{
			action?.Invoke();
			return;
		}

		var confirm = new PopupWindow(
			"Save Current Shader Template", "The open template has unsaved changes. Would you like to save now?", "Cancel",
			new Dictionary<string, Action>()
			{
				{ "No", () => action?.Invoke() },
				{ "Yes", () => { if ( SaveInternal( false ) ) action?.Invoke(); } }
			}
		);

		confirm.Show();
	}

	[Shortcut( "editor.new", "CTRL+N" )]
	public void New()
	{
		PromptSave( CreateNew );
	}

	private void CreateNew()
	{
		_asset = null;
		_template = new();
		_dirty = false;

		_properties.Target = _template;

		WindowTitle = "untitled";
	}

	private void BuildMenuBar()
	{
		var file = MenuBar.AddMenu( "File" );
		file.AddOption( "New", "common/new.png", New, "editor.new" ).StatusTip = "New Template";
		file.AddOption( "Open", "common/open.png", Open, "editor.open" ).StatusTip = "Open Template";
		file.AddOption( "Save", "common/save.png", Save, "editor.save" ).StatusTip = "Save Template";
		file.AddOption( "Save As...", "common/save.png", SaveAs, "editor.save-as" ).StatusTip = "Save Template As...";


		var view = MenuBar.AddMenu( "View" );
		view.AboutToShow += () => OnViewMenu( view );
	}

	private void OnViewMenu( Menu view )
	{
		view.Clear();
		view.AddOption( "Restore To Default", "settings_backup_restore", ResetLayout );
		view.AddSeparator();

		foreach ( var dock in DockManager.DockTypes )
		{
			var o = view.AddOption( dock.Title, dock.Icon );
			o.Checkable = true;
			o.Checked = DockManager.IsDockOpen( dock.Title );
			o.Toggled += ( b ) => DockManager.SetDockState( dock.Title, b );
		}
	}

	private void CreateToolBar()
	{
		var toolBar = new ToolBar( this, "ShaderTemplateEditorToolbar" );
		AddToolBar( toolBar, ToolbarPosition.Top );

		toolBar.AddOption( "New", "common/new.png", New ).StatusTip = "New Template";
		toolBar.AddOption( "Open", "common/open.png", Open ).StatusTip = "Open Template";
		toolBar.AddOption( "Save", "common/save.png", () => Save() ).StatusTip = "Save Template";
	}

	[Shortcut( "editor.quit", "CTRL+Q" )]
	void Quit()
	{
		Close();
	}

	private void Open()
	{
		var fd = new FileDialog( null )
		{
			Title = $"Open Shader Template",
			DefaultSuffix = $".shdrtpl"
		};

		fd.SetNameFilter( $"Shader Template ( *.shdrtpl)" );

		if ( !fd.Execute() )
			return;

		PromptSave( () => Open( fd.SelectedFile ) );
	}

	public void Open( string path, bool addToPath = true )
	{
		var asset = AssetSystem.FindByPath( path );

		if ( asset == null )
			return;

		if ( asset == _asset )
		{
			Focus();
			return;
		}

		var template = JsonSerializer.Deserialize<ShaderTemplateResource>( System.IO.File.ReadAllText( path ), ShaderGraphPlus.SerializerOptions() );
	
		_asset = asset;
		_template = template;
		_dirty = false;
		_properties.Target = _template;

		WindowTitle = _asset?.Name;
	}

	[Shortcut( "editor.save-as", "CTRL+SHIFT+S" )]
	public void SaveAs()
	{
		SaveInternal( true );
	}

	[Shortcut( "editor.save", "CTRL+S" )]
	public void Save()
	{
		SaveInternal( false );
	}

	private string GetSavePath()
	{
		var fd = new FileDialog( null )
		{
			Title = $"Save Shader Template",
			DefaultSuffix = $".shdrtpl"
		};

		fd.SelectFile( $"untitled.shdrtpl" );
		fd.SetFindFile();
		fd.SetModeSave();
		fd.SetNameFilter( $"Shader Template (*.shdrtpl)" );
		if ( !fd.Execute() )
			return null;

		return fd.SelectedFile;
	}

	private bool SaveInternal( bool saveAs )
	{
		var savePath = _asset == null || saveAs ? GetSavePath() : _asset.AbsolutePath;
		if ( string.IsNullOrWhiteSpace( savePath ) )
			return false;

		// Write serialized file to asset file
		System.IO.File.WriteAllText( savePath, JsonSerializer.Serialize<ShaderTemplateResource>( _template, ShaderGraphPlus.SerializerOptions() ) );

		if ( saveAs )
		{
			// If we're saving as, we want to register the new asset
			_asset = null;
		}

		// Register asset if we haven't already
		_asset ??= AssetSystem.RegisterFile( savePath );

		if ( _asset == null )
		{
			SGPLogger.Warning( $"Unable to register asset {savePath}" );

			return false;
		}

		MainAssetBrowser.Instance?.Local.UpdateAssetList();

		_dirty = false;
		WindowTitle = _asset?.Name;

		return true;
	}

	public void SetDirty()
	{
		Update();

		_dirty = true;
		WindowTitle = $"{_asset?.Name ?? "untitled"}*";
	}

	private void OnPropertyUpdated( SerializedProperty serializedProperty )
	{
		SetDirty();
	}

	protected override bool OnClose()
	{
		if ( !_dirty )
		{
			return true;
		}

		var confirm = new PopupWindow(
			"Save Current Shader Template", "The open template has unsaved changes. Would you like to save now?", "Cancel",
			new Dictionary<string, Action>()
			{
				{ "No", () => { _dirty = false; Close(); } },
				{ "Yes", () => { if ( SaveInternal( false ) ) Close(); } }
			}
		);

		confirm.Show();

		return false;
	}
}
