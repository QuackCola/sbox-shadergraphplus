using Editor;
using Sandbox.Helpers;

namespace ShaderGraphPlus;

[AttributeUsage( AttributeTargets.Property )]
internal sealed class TabPageAttribute : Attribute
{
	public string Name { get; set; }

	public TabPageAttribute( string name )
	{
		Name = name;
	}
}

[EditorForAssetType( ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension )]
[EditorApp( "Shader Template Editor", "gradient", "edit shader templates" )]
public sealed class ShaderTemplateEditorWindow : DockWindow, IAssetEditor
{
	private ShaderTemplateResource _template;
	private Asset _asset;
	private bool _dirty = false;

	private Widget _primaryDockCanvas;
	private TabWidget _tabWidget;

	private TextEditAreaWidget _textEditArea;

	private UndoSystem _undoSystem;

	private Option _undoMenuOption;
	private Option _redoMenuOption;
	private Option _undoOption;
	private Option _redoOption;

	private ShaderDomain _lastShaderDomain;

	public bool CanOpenMultipleAssets => false;

	public ShaderTemplateEditorWindow() : base()
	{
		Size = new Vector2( 640, 480 );

		InitUndo();

		CreateToolBar();
		CreateUI();

		UpdateUndoRedoOptions();

		Show();

		StateCookie = ShaderGraphPlusGlobals.ShaderTemplateEditorStateCookieName;

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

	private void InitUndo()
	{
		_undoSystem = new UndoSystem();
		_undoSystem.Initialize();
		_undoSystem.OnUndo += _ => UpdateUndoRedoOptions();
		_undoSystem.OnRedo += _ => UpdateUndoRedoOptions();
	}

	private void CreateUI()
	{
		BuildMenuBar();

		_primaryDockCanvas = new Widget( this );
		_primaryDockCanvas.Layout = Layout.Column();

		DockManager.SetCentralWidget( _primaryDockCanvas );
	}

	private void BuildMenuBar()
	{
		var file = MenuBar.AddMenu( "File" );
		file.AddOption( "New", "common/new.png", New, "editor.new" ).StatusTip = "New Template";
		file.AddOption( "Open", "common/open.png", Open, "editor.open" ).StatusTip = "Open Template";
		file.AddOption( "Save", "common/save.png", Save, "editor.save" ).StatusTip = "Save Template";
		file.AddOption( "Save As...", "common/save.png", SaveAs, "editor.save-as" ).StatusTip = "Save Template As...";

		file.AddSeparator();

		file.AddOption( "Reset Template Code To Default", "common/reset.png", () => ResetTemplateCodeToDefault() ).StatusTip = "Reset Template Code To Default";

		file.AddSeparator();

		file.AddOption( "Quit", null, Quit, "editor.quit" ).StatusTip = "Quit";

		var edit = MenuBar.AddMenu( "Edit" );
		_undoMenuOption = edit.AddOption( "Undo", "undo", () => Undo(), "editor.undo" );
		_redoMenuOption = edit.AddOption( "Redo", "redo", () => Redo(), "editor.redo" );

		var view = MenuBar.AddMenu( "View" );
		view.AboutToShow += () => OnViewMenu( view );
	}

	private void CreateToolBar()
	{
		var toolBar = new ToolBar( this, ShaderGraphPlusGlobals.ShaderTemplateEditorToolbarName );
		AddToolBar( toolBar, ToolbarPosition.Top );

		toolBar.AddOption( "New", "common/new.png", New ).StatusTip = "New Template";
		toolBar.AddOption( "Open", "common/open.png", Open ).StatusTip = "Open Template";
		toolBar.AddOption( "Save", "common/save.png", () => Save() ).StatusTip = "Save Template";

		toolBar.AddSeparator();

		_undoOption = toolBar.AddOption( new Option( "Undo", "undo", () => Undo() ) );
		_redoOption = toolBar.AddOption( new Option( "Redo", "redo", () => Redo() ) );

		toolBar.AddSeparator();
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

	protected override void BuildDefaultLayout()
	{

	}

	private void Rebuild()
	{
		_primaryDockCanvas.Layout.Clear( true );

		var so = _template.GetSerialized();
		var oldestSerialized = _template.Serialize();

		so.OnPropertyChanged += ( prop ) => OnPropertyUpdated( prop, oldestSerialized );

		_tabWidget = new TabWidget( this );

		_tabWidget.AddPage( "General", "settings", CreateTab( so, "General" ) );

		if ( so.TryGetProperty( nameof( ShaderTemplateResource.Code ), out var codeProp ) )
		{
			_tabWidget.AddPage( "Code", "code", CreateCodeTab( codeProp ) );
		}
		else
		{
			throw new Exception( "Failed to get code SerilaizedProperty" );
		}

		_primaryDockCanvas.Layout.Add( _tabWidget );
	}

	private Widget CreateTab( SerializedObject serialized, string tabName )
	{
		var container = new Widget( null );
		container.Layout = Layout.Column();
		container.VerticalSizeMode = SizeMode.CanGrow;

		var sheet = new ControlSheet();

		sheet.AddObject( serialized, ( x ) =>
		{
			return x.TryGetAttribute<TabPageAttribute>( out var attrib ) && attrib.Name == tabName;
		} );

		container.Layout.Add( sheet );
		container.Layout.AddStretchCell();

		return container;
	}

	private Widget CreateCodeTab( SerializedProperty property )
	{
		var container = new Widget( null );
		container.Layout = Layout.Column();
		container.VerticalSizeMode = SizeMode.CanGrow;

		_textEditArea = new TextEditAreaWidget( container );
		_textEditArea.Value = property.GetValue<string>();
		_textEditArea.ValueChanged = ( x ) =>
		{
			_template.Code = x;
			SetDirty();
		};

		container.Layout.Add( _textEditArea );

		return container;
	}

	private void ResetTemplateCodeToDefault()
	{
		if ( _template == null || _textEditArea == null )
			return;

		ExecuteUndoableAction( "Reset Template Code To Default", () =>
		{
			switch ( _template.ShaderDomain )
			{
				case ShaderDomain.Surface:
					_template.Code = ShaderTemplateSurface.Code;
					break;
				case ShaderDomain.Sky:
					_template.Code = ShaderTemplateSky.Code;
					break;
				case ShaderDomain.PostProcess:
					_template.Code = ShaderTemplatePostProcess.Code;
					break;
			}

			_textEditArea.Value = _template.Code;
		} );

		SetDirty();
	}

	private void PromptSave( Action action )
	{
		if ( !_dirty )
		{
			action?.Invoke();
			return;
		}

		var confirm = new PopupWindow(
			$"Save Current {ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName}", "The open template has unsaved changes. Would you like to save now?", "Cancel",
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
		_lastShaderDomain = ShaderDomain.Surface;

		WindowTitle = "untitled";

		_undoSystem.Initialize();

		Rebuild();
	}

	[Shortcut( "editor.quit", "CTRL+Q" )]
	private void Quit()
	{
		Close();
	}

	[Shortcut( "editor.undo", "CTRL+Z", ShortcutType.Window )]
	private bool Undo()
	{
		return _undoSystem.Undo();
	}

	[Shortcut( "editor.redo", "CTRL+Y", ShortcutType.Window )]
	private bool Redo()
	{
		return _undoSystem.Redo();
	}

	private void Open()
	{
		var fd = new FileDialog( null )
		{
			Title = $"Open {ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName}",
			DefaultSuffix = $".{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension}"
		};

		fd.SetNameFilter( $"{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName} (*.{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension})" );

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

		var template = new ShaderTemplateResource();
		template.Deserialize( System.IO.File.ReadAllText( path ), Path.GetFileName( path ) );

		_asset = asset;
		_template = template;
		_dirty = false;
		_lastShaderDomain = template.ShaderDomain;

		WindowTitle = _asset?.Name;

		_undoSystem.Initialize();
		Rebuild();
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
			Title = $"Save {ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName}",
			DefaultSuffix = $".{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension}"
		};

		fd.SelectFile( $"untitled.{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension}" );
		fd.SetFindFile();
		fd.SetModeSave();
		fd.SetNameFilter( $"S{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName} (*.{ShaderGraphPlusGlobals.ShaderTemplateAssetTypeExtension})" );
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
		System.IO.File.WriteAllText( savePath, _template.Serialize() );

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

		Update();

		_dirty = false;
		WindowTitle = _asset?.Name;

		EditorEvent.Run( ShaderGraphPlusGlobals.EditorEvents.ShaderTemplateUpdate, _asset?.RelativePath );

		return true;
	}

	public void SetDirty()
	{
		Update();
		UpdateUndoRedoOptions();

		_dirty = true;
		WindowTitle = $"{_asset?.Name ?? "untitled"}*";
	}

	private void UpdateUndoRedoOptions()
	{
		var undoOptionText = $"Undo {(_undoSystem.Back.Count > 0 ? $"\"{_undoSystem.Back.Peek().Name}\"" : "")}";
		var redoOptionText = $"Redo {(_undoSystem.Forward.Count > 0 ? $"\"{_undoSystem.Forward.Peek().Name}\"" : "")}";

		_undoMenuOption.Enabled = _undoSystem.Back.Count > 0;
		_redoMenuOption.Enabled = _undoSystem.Forward.Count > 0;
		_undoMenuOption.Text = undoOptionText;
		_redoMenuOption.Text = redoOptionText;

		_undoOption.Enabled = _undoSystem.Back.Count > 0;
		_redoOption.Enabled = _undoSystem.Forward.Count > 0;
		_undoOption.ToolTip = undoOptionText;
		_redoOption.ToolTip = redoOptionText;
		_undoOption.StatusTip = undoOptionText;
		_redoOption.StatusTip = redoOptionText;
	}

	public void ExecuteUndoableAction( string undoName, Action action )
	{
		var preState = _template.Serialize();

		action.Invoke();

		var postState = _template.Serialize();

		_undoSystem.Insert( undoName,
			() =>
			{
				_template.Deserialize( preState );
				_textEditArea.Value = _template.Code;
				SetDirty();
			},
			() =>
			{
				_template.Deserialize( postState );
				_textEditArea.Value = _template.Code;
				SetDirty();
			} );
	}

	private void OnPropertyUpdated( SerializedProperty serializedProperty, string oldestSerialized )
	{
		if ( serializedProperty is null ) return;

		var undoName = $"Modify {serializedProperty.Name}";

		if ( serializedProperty.Name == nameof( ShaderTemplateResource.ShaderDomain ) )
		{
			var currentShaderDomain = serializedProperty.GetValue<ShaderDomain>();

			if ( _lastShaderDomain != currentShaderDomain )
			{
				_lastShaderDomain = currentShaderDomain;

				var popup = new PopupWindow(
					"Shader Domian Changed", "Would you like to reset the template code to the selected shader domain default?", "No",
					new Dictionary<string, Action>()
					{
						{ "Yes", () => ResetTemplateCodeToDefault() }
					}
				);

				popup.Show();
			}
		}

		var serializedTemplate = _template.Serialize();

		if ( _undoSystem.Back.Count > 0 )
		{
			var lastUndo = _undoSystem.Back.Peek();
			if ( lastUndo?.Name == undoName )
			{
				lastUndo = _undoSystem.Back.Pop();
				_undoSystem.Insert( undoName, lastUndo.Undo, () =>
				{
					_template.Deserialize( serializedTemplate );
					_textEditArea.Value = _template.Code;
					SetDirty();
				} );
			}
			else
			{
				_undoSystem.Insert( undoName, lastUndo.Redo, () =>
				{
					_template.Deserialize( serializedTemplate );
					_textEditArea.Value = _template.Code;
					SetDirty();
				} );
			}
		}
		else
		{
			_undoSystem.Insert( undoName, () =>
			{
				_template.Deserialize( oldestSerialized );
				_textEditArea.Value = _template.Code;
				SetDirty();
			}, () =>
			{
				_template.Deserialize( serializedTemplate );
				_textEditArea.Value = _template.Code;
				SetDirty();
			} );
		}

		SetDirty();
	}

	protected override bool OnClose()
	{
		if ( !_dirty )
		{
			return true;
		}

		var confirm = new PopupWindow(
			$"Save Current {ShaderGraphPlusGlobals.ShaderTemplateAssetTypeName}", "The open template has unsaved changes. Would you like to save now?", "Cancel",
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
