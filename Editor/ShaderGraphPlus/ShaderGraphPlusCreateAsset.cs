using Editor;

namespace ShaderGraphPlus.AssetBrowser;

internal static class ShaderGraphPlusCreateAsset
{
	static void CreateSubgraphAsset( string targetPath )
	{
		var template_path = ShaderGraphPlusFileSystem.Root.GetFullPath( "templates" );
		var sourceFile = $"{template_path}/$name.{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension}";

		if ( !System.IO.File.Exists( sourceFile ) )
			return;

		// assure extension
		targetPath = System.IO.Path.ChangeExtension( targetPath, ShaderGraphPlusGlobals.SubgraphAssetTypeExtension );

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
			var menu = e.Menu.FindOrCreateMenu( "New" ).FindOrCreateMenu( "Shader" );
			menu.AddOption( $"New {ShaderGraphPlusGlobals.AssetTypeName}", "account_tree", () =>
			{
				var ProjectCreator = new ProjectCreator();
				ProjectCreator.DeleteOnClose = true;
				ProjectCreator.FolderEditPath = e.Target.FullName;
				ProjectCreator.Show();
			} );
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

				CreateSubgraphAsset( fd.SelectedFile );
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
