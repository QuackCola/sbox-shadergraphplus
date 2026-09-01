using Editor;

namespace ShaderGraphPlus.AssetBrowser;

internal static class ShaderGraphPlusCreateAsset
{
	static void CreateAssetFromTemplate( string targetPath, string templateFolder, string extension, string nameAppend = "" )
	{
		var sourceFile = $"{templateFolder}/$name.{extension}";

		if ( !System.IO.File.Exists( sourceFile ) )
			return;

		// assure extension
		targetPath = System.IO.Path.ChangeExtension( targetPath, extension );

		if ( !string.IsNullOrWhiteSpace( nameAppend ) )
		{
			var fileName = System.IO.Path.GetFileNameWithoutExtension( targetPath );
			targetPath = targetPath.Replace( fileName, $"{fileName}_{nameAppend}" );
		}

		System.IO.File.Copy( sourceFile, targetPath );
		var asset = AssetSystem.RegisterFile( targetPath );

		MainAssetBrowser.Instance?.Local.UpdateAssetList();
	}

	static void CreateShaderTemplateAsset( string targetPath )
	{
		var newTemplate = new ShaderTemplateResource();
		var serializedAsset = newTemplate.Serialize();

		System.IO.File.WriteAllText( targetPath, serializedAsset );

		var asset = AssetSystem.RegisterFile( targetPath );

		MainAssetBrowser.Instance?.Local.UpdateAssetList();
	}

	static void AddShaderGraphPlusAssetOption( FolderContextMenu e, Menu menu, string shaderType, string name, string icon = "" )
	{
		var templatesFolderRoot = ShaderGraphPlusFileSystem.Root.GetFullPath( "templates" );
		var templateFolder = shaderType switch
		{
			"Surface" => $"{templatesFolderRoot}/shadergraphplus.surface.lit",
			"Surface Unlit" => $"{templatesFolderRoot}/shadergraphplus.surface.unlit",
			"Sky" => $"{templatesFolderRoot}/shadergraphplus.sky",
			"PostProcess" => $"{templatesFolderRoot}/shadergraphplus.postprocessing",
			_ => throw new NotImplementedException(),
		};
	
		menu.AddOption( name, icon, () =>
		{
			var fd = new FileDialog( null );
			fd.Title = $"Create {ShaderGraphPlusGlobals.AssetTypeName}";
			fd.Directory = e.Target.FullName;
			fd.DefaultSuffix = $".{ShaderGraphPlusGlobals.AssetTypeExtension}";
			fd.SelectFile( $"untitled.{ShaderGraphPlusGlobals.AssetTypeExtension}" );
			fd.SetFindFile();
			fd.SetModeSave();
			fd.SetNameFilter( $"{ShaderGraphPlusGlobals.AssetTypeName} (*.{ShaderGraphPlusGlobals.AssetTypeExtension})" );

			if ( !fd.Execute() )
				return;

			CreateAssetFromTemplate( fd.SelectedFile, templateFolder, ShaderGraphPlusGlobals.AssetTypeExtension, shaderType == "Sky" ? "sky" : "" );
		} );
	}

	[Event( "folder.contextmenu", Priority = 100 )]
	internal static void OnShaderGraphPlusAssetFolderContext( FolderContextMenu e )
	{
		// Remove broken option
		var otherMenu = e.Menu.FindOrCreateMenu( "New" ).FindOrCreateMenu( "Other" );
		otherMenu.RemoveOption( ShaderGraphPlusGlobals.AssetTypeName );
		otherMenu.RemoveOption( ShaderGraphPlusGlobals.SubgraphAssetTypeName );
		otherMenu.RemoveOption( "Shader Template" );

		if ( e.Target != null )
		{
			var templatesFolder = ShaderGraphPlusFileSystem.Root.GetFullPath( "templates" );

			var menu = e.Menu.FindOrCreateMenu( "New" ).FindOrCreateMenu( "Shader" );

			AddShaderGraphPlusAssetOption( e, menu, "Surface", $"New {ShaderGraphPlusGlobals.AssetTypeName} Lit Surface Shader", "view_in_ar" );
			AddShaderGraphPlusAssetOption( e, menu, "Surface Unlit", $"New {ShaderGraphPlusGlobals.AssetTypeName} Unlit Surface Shader", "view_in_ar" );
			AddShaderGraphPlusAssetOption( e, menu, "Sky", $"New {ShaderGraphPlusGlobals.AssetTypeName} Sky Shader", "nights_stay" );
			AddShaderGraphPlusAssetOption( e, menu, "PostProcess", $"New {ShaderGraphPlusGlobals.AssetTypeName} PostProcess Shader", "desktop_windows" );

			menu.AddOption( $"New {ShaderGraphPlusGlobals.SubgraphAssetTypeName}", "account_tree", () =>
			{
				var fd = new FileDialog( null );
				fd.Title = $"Create {ShaderGraphPlusGlobals.SubgraphAssetTypeName}";
				fd.Directory = e.Target.FullName;
				fd.DefaultSuffix = $".{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension}";
				fd.SelectFile( $"untitled.{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension}" );
				fd.SetFindFile();
				fd.SetModeSave();
				fd.SetNameFilter( $"{ShaderGraphPlusGlobals.SubgraphAssetTypeName} (*.{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension})" );

				if ( !fd.Execute() )
					return;

				CreateAssetFromTemplate( fd.SelectedFile, templatesFolder, ShaderGraphPlusGlobals.SubgraphAssetTypeExtension );
			} );
			menu.AddOption( "New Shader Template", "account_tree", () =>
			{
				var fd = new FileDialog( null );

				fd.Title = $"Create Shader Template";
				fd.Directory = e.Target.FullName;
				fd.DefaultSuffix = $".shdrtpl";
				fd.SelectFile( $"untitled.shdrtpl" );
				fd.SetFindFile();
				fd.SetModeSave();
				fd.SetNameFilter( $"Shader Template (*.shdrtpl)" );

				if ( !fd.Execute() )
					return;

				CreateShaderTemplateAsset( fd.SelectedFile );
			} );
		}
	}
}
