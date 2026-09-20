using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	/// <summary>
	/// Changes : <br/>
	/// - Reference Groups by name on parameters instead of guid's.<br/>
	/// - Ungrouped parameters go into the "General" group.
	/// </summary>
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 12 )]
	internal static void Upgrader_v12( JsonObject obj )
	{
		if ( obj[JsonKeys.ParameterArray] is not JsonArray oldParameterArray )
			throw new Exception( $"Cannot find jsonArray \"{JsonKeys.ParameterArray}\"" );

		if ( obj[JsonKeys.NodeArray] is not JsonArray oldNodeArray )
			throw new Exception( $"Cannot find jsonArray \"{JsonKeys.NodeArray}\"" );

		if ( obj[JsonKeys.CategoryDataArray] is not JsonArray oldCategoryDataArray )
			throw new Exception( $"Cannot find jsonArray \"{JsonKeys.CategoryDataArray}\"" );

		var isSubgraph = CheckIfSubgraph( obj );

		var gatheredCategories = new Dictionary<Guid, string>();

		foreach ( var jsonNode in oldCategoryDataArray )
		{
			if ( jsonNode["Identifier"] is not JsonValue identifierValue )
				continue;

			if ( jsonNode["Name"] is not JsonValue nameValue )
				continue;

			if ( !gatheredCategories.ContainsKey( identifierValue.GetValue<Guid>() ) )
			{
				gatheredCategories.Add( identifierValue.GetValue<Guid>(), nameValue.GetValue<string>() );
			}
		}

		//
		// Upgrade Parameters
		//

		var newParameterArray = new JsonArray();
		var categoriesToAdd = new Dictionary<string, List<Guid>>();

		foreach ( var jsonNode in oldParameterArray )
		{
			if ( jsonNode[JsonKeys.Class] is not JsonValue classValue )
				continue;
		
			var typeName = classValue.GetValue<string>();
			var typeDesc = EditorTypeLibrary.GetType<BlackboardParameter>( typeName );
			var type = new ClassBlackboardParameterType( typeDesc );
		
			var newParameterObj = jsonNode.DeepClone().AsObject();
		
			if ( typeDesc != null )
			{
				if ( !isSubgraph && JsonUtils.GetPropertyValue( newParameterObj, "GroupReference", SerializerOptions(), Guid.Empty, out var groupReference ) && groupReference != Guid.Empty)
				{
					if ( gatheredCategories.TryGetValue( groupReference, out var groupName ) )
					{
						//newParameterObj.Remove( "GroupReference" );
						newParameterObj.Remove( "Group" );
						newParameterObj.Add( "Group", groupName );
					}

					newParameterArray.Add( newParameterObj );
				}
				else if ( !isSubgraph )
				{
					newParameterObj.Remove( "Group" );
					newParameterObj.Add( "Group", "" );

					JsonUtils.GetPropertyValue( newParameterObj, "Identifier", SerializerOptions(), Guid.Empty, out var parameterReference );

					if ( !categoriesToAdd.ContainsKey( "General" ) )
					{
						categoriesToAdd.TryAdd( "General", [parameterReference] );
					}
					else
					{
						categoriesToAdd["General"].Add( parameterReference );
					}

					//newParameterObj.Remove( "GroupReference" );
					newParameterArray.Add( newParameterObj );
				}
				else
				{
					//newParameterObj.Remove( "GroupReference" );
					newParameterArray.Add( newParameterObj );
				}
			}
		}

		//
		// Upgrade Categories
		//

		var newCategoryDataArray = new JsonArray();

		foreach ( var category in categoriesToAdd )
		{
			var newCategoryObj = new JsonObject { { JsonKeys.Class, "CategoryData" } };
			var newCategory = new CategoryData() { Name = category.Key, ParameterReferences = category.Value };

			SerializeObject( newCategory, newCategoryObj, SerializerOptions() );

			newCategoryDataArray.Add( newCategoryObj );
		}

		foreach ( var jsonNode in oldCategoryDataArray )
		{
			var newCategoryDataObj = jsonNode.DeepClone().AsObject();

			newCategoryDataArray.Add( newCategoryDataObj );
		}

		obj.Remove( JsonKeys.ParameterArray );
		obj.Add( JsonKeys.ParameterArray, newParameterArray );

		obj.Remove( JsonKeys.CategoryDataArray );
		obj.Add( JsonKeys.CategoryDataArray, newCategoryDataArray );
	}
}
