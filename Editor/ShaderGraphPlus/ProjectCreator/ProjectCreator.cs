using Editor;
using MaterialDesign;
using static Editor.WidgetGalleryWindow;

namespace ShaderGraphPlus;

internal class FieldTitle : Label
{
	public FieldTitle( string title )
	   : base( title, (Widget)null )
	{
	}

}

internal class FieldSubtitle : Label
{
	public FieldSubtitle( string title ) : base( title, null )
	{
		WordWrap = true;
	}
}

public class ProjectCreator : Dialog
{
	private Button _okayButton;

	private LineEdit _titleEdit;

	private FolderEdit _folderEdit;

	private ProjectTemplate _activeTemplate;

	private ProjectTemplates _templates;

	private TemplateUserConfig _templateUserConfig;

	private bool _noTemplates = false;

	public string FolderEditPath
	{
		get => _folderEdit.Text;
		set
		{
			_folderEdit.Text = value;
		}
	}

	public Action<string> OnProjectCreated { get; set; }

	public ProjectCreator( Widget parent = null ) : base( null, true )
	{
		// Set some basic window stuff.
		Window.Size = new Vector2( 800, 500 );
		Window.MaximumSize = Window.Size;
		Window.MinimumSize = Window.Size;
		Window.Title = "Create New Shadergraph Plus Project";
		Window.SetWindowIcon( MaterialIcons.Gradient );
		Window.SetModal( true, true );
		//Window.WindowFlags = WindowFlags.Dialog | WindowFlags.Customized | WindowFlags.WindowTitle | WindowFlags.CloseButton | WindowFlags.WindowSystemMenuHint;

		Init();
		_okayButton.Enabled = true;
	}

	private void Init()
	{
		Layout = Layout.Column();
		Layout.Spacing = 3;

		// Templates ListView & Template setup
		{
			var row = Layout.AddRow( 8 );

			row.AddColumn();

			// Templates ListView
			Layout listViewBody = row.AddColumn( 2, false );
			listViewBody.Margin = 20f;
			listViewBody.Spacing = 8f;

			listViewBody.AddSpacingCell( 16f );

			listViewBody.Add( new FieldTitle( "Templates" ) );

			listViewBody.AddSpacingCell( 16f );

			listViewBody.AddSeparator();

			ProjectTemplates templates = listViewBody.Add( new ProjectTemplates( this ), 2 );

			_templates = templates;

			// Template list view for all the projects in the templates folder.
			ProjectTemplatesListView listView = _templates.ListView;

			listView.ItemSelected = (Action<object>)Delegate.Combine( listView.ItemSelected, delegate ( object item )
			{
				_activeTemplate = item as ProjectTemplate;
			} );

			_activeTemplate = templates.ListView.ChosenTemplate; // Set the intital selected template.

			if ( _activeTemplate != null )
			{
				SGPLogger.Info( $"Active template : {_activeTemplate.TemplatePath}" );
			}

			//listViewBody.AddSpacingCell(128f);

			row.AddColumn();

			Layout setupBody = row.AddColumn( 2, false );
			setupBody.Margin = 20f;
			setupBody.Spacing = 8f;

			setupBody.AddSpacingCell( 16f );

			setupBody.Add( new FieldTitle( "Shader Graph Plus Project Setup" ) );

			setupBody.AddSpacingCell( 16f );

			setupBody.AddSeparator();

			setupBody.Add( new FieldTitle( "Name" ) );
			{
				_titleEdit = setupBody.Add( new LineEdit( "", null )
				{
					PlaceholderText = "Garry's Project"
				} );
				_titleEdit.Text = DefaultProjectName();
				_titleEdit.ToolTip = "Name of your Shader Graph Plus project.";
				_titleEdit.TextEdited += delegate
				{
					Validate();
				};
			}

			setupBody.AddSpacingCell( 16 );

			// Folder Edit.
			setupBody.Add( new FieldTitle( "Location" ) );
			{
				_folderEdit = setupBody.Add( new FolderEdit( null ) );
				_folderEdit.PlaceholderText = "";
				_folderEdit.ToolTip = "Absolute path to where the Shader Graph Plus project will be saved to.";
				_folderEdit.TextEdited += delegate
				{
					Validate();
				};
				FolderEdit folderEdit = _folderEdit;
				folderEdit.FolderSelected = (Action<string>)Delegate.Combine( folderEdit.FolderSelected, (Action<string>)delegate
				{
					Validate();
				} );
			}

			setupBody.AddSpacingCell( 16 );

			// Additional per-template config. 

			setupBody.Add( new FieldTitle( "Config" ) );
			{

				_templateUserConfig = new TemplateUserConfig();

				var canvas = new Widget( null );
				canvas.Layout = Layout.Row();
				canvas.Layout.Spacing = 32;

				var so = _templateUserConfig.GetSerialized();
				var cs = new ControlSheet();
				//canvas.MinimumWidth = 350;

				cs.AddProperty( _templateUserConfig, x => x.Description );

				setupBody.Add( cs );
			}


			// Create button & any errors.
			{
				_okayButton = new Button.Primary( "Create", "add_box", null );
				_okayButton.Clicked = CreateProject;

				var footer = Layout.AddRow( 2, false );
				footer.Margin = 16;
				footer.Spacing = 8;
				footer.AddStretchCell();

				// Handle situations where there is no templates found.
				if ( _templates.ListView.Items.Count() != 0 )
				{
					_activeTemplate = this._templates.ListView.SelectedItems.First() as ProjectTemplate;
				}
				else
				{
					_noTemplates = true;
					var error = footer.AddColumn( 2, false );
					error.Spacing = 8f;
					error.AddStretchCell( 0 );
					var errorlabel = new Label( "No Templates found!" )
					{
						Color = Color.Red
					};
					error.Add( errorlabel );
				}

				footer.Add( _okayButton );
			}

			setupBody.AddSpacingCell( 16f );
		}

		Validate();
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.WindowBackground.Lighten( 0.4f ) );
		Paint.DrawRect( LocalRect );
	}

	private static string DefaultProjectName()
	{
		string name = "My Shadergraph Plus Project";
		int i = 1;
		//while (Path.Exists(Path.Combine(EditorPreferences.AddonLocation, ConvertToIdent(name))))
		//{
		name = $"My Project {i++}";
		//}
		return name;
	}

	private void Validate()
	{
		bool enabled = true;

		if ( string.IsNullOrWhiteSpace( _folderEdit.Text ) )
		{
			enabled = false;
		}

		if ( string.IsNullOrWhiteSpace( _titleEdit.Text ) )
		{
			enabled = false;
		}

		_okayButton.Enabled = enabled;
	}

	private void ConfigureTemplate( ShaderGraphPlus shaderGraphPlusTemplate )
	{
		shaderGraphPlusTemplate.Description = _templateUserConfig.Description;
	}

	private ShaderGraphPlus ReadTemplate( string templatePath )
	{
		var shaderGraphPlusTemplate = new ShaderGraphPlus();
		shaderGraphPlusTemplate.Deserialize( System.IO.File.ReadAllText( ShaderGraphPlusFileSystem.Root.GetFullPath( $"{templatePath}/$name.{ShaderGraphPlusGlobals.AssetTypeExtension}" ) ) );

		// configure the template.
		ConfigureTemplate( shaderGraphPlusTemplate );

		shaderGraphPlusTemplate.SetMeta( "ProjectTemplate", null );

		return shaderGraphPlusTemplate;
	}

	private void CreateProject()
	{
		if ( _noTemplates )
		{
			return;
		}

		var shaderGraphProjectPath = $"{_folderEdit.Text}/";
		Directory.CreateDirectory( shaderGraphProjectPath );

		var outputPath = Path.Combine( shaderGraphProjectPath, $"{_titleEdit.Text}.{ShaderGraphPlusGlobals.AssetTypeExtension}" ).Replace( '\\', '/' );
		File.WriteAllText( outputPath, ReadTemplate( $"{_templates.ListView.ChosenTemplate.TemplatePath}" ).Serialize() );

		// Register the generated project with the assetsystem.
		AssetSystem.RegisterFile( outputPath );

		Utilities.EdtiorSound.Success();
		Close();

		OnProjectCreated?.Invoke( outputPath );
	}

	//[EditorEvent.Hotload]
	//public void OnHotload()
	//{
	//	Init();
	//}
}
