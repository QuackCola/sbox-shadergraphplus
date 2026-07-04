using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

file struct ParameterEntry : IValid
{
	public BlackboardParameter Parameter { get; internal set; }

	public CategoryData CategoryData { get; internal set; }

	public int OrderInRoot { get; internal set; }

	public bool Grouped { get; internal set; }

	public bool IsValid => Parameter != null;

	public ParameterEntry( BlackboardParameter parameter, int orderInRoot, bool grouped )
	{
		Parameter = parameter;
		OrderInRoot = orderInRoot;
		Grouped = grouped;
	}

	public ParameterEntry( CategoryData categoryData, int orderInRoot, bool grouped )
	{
		Parameter = null;

		CategoryData = categoryData;
		OrderInRoot = orderInRoot;
		Grouped = grouped;
	}
}

public partial class ShaderGraphPlus
{
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 10 )]
	internal static void Upgrader_v10( JsonObject obj )
	{
		var categories = new List<CategoryData>();
		var updatedParameters = new List<ParameterEntry>();
		var registeredGroupNames = new List<string>();

		static int GetGroupOrder( JsonNode node, string uiKeyName )
		{
			if ( node[uiKeyName] is not JsonNode uiNode )
				return -1;

			if ( uiNode["Priority"] is JsonValue priorityValue )
			{
				return priorityValue.GetValue<int>();
			}

			return -1;
		}

		static UIGroup GetUIGroup( JsonNode node, string uiKeyName )
		{
			if ( node[uiKeyName] is not JsonNode uiNode )
				return default;

			if ( uiNode["PrimaryGroup"] is JsonNode primaryGroupNode )
			{
				return JsonSerializer.Deserialize<UIGroup>( primaryGroupNode, SerializerOptions() );
			}

			return default;
		}

		void AddToGroup( BlackboardParameter parameter, int parameterPriorityInGroup, UIGroup primaryGroup )
		{
			if ( !string.IsNullOrWhiteSpace( primaryGroup.Name ) )
			{
				if ( !registeredGroupNames.Contains( primaryGroup.Name ) )
				{
					var newCategoryData = new CategoryData
					{
						Name = $"{primaryGroup.Name} Group",
						Priority = primaryGroup.Priority
					};
					newCategoryData.ParameterReferences.Add( parameter.Identifier );

					if ( parameter is IGroupableBlackboardParameter groupableParameter )
					{
						groupableParameter.GroupReference = newCategoryData.Identifier;
					}

					if ( !categories.Contains( newCategoryData ) )
					{
						categories.Add( newCategoryData );
					}

					registeredGroupNames.Add( primaryGroup.Name );
				}
				else
				{
					var existingEntry = categories.FirstOrDefault( x => x.Name == $"{primaryGroup.Name} Group" );

					if ( existingEntry != null )
					{
						if ( !existingEntry.ParameterReferences.Contains( parameter.Identifier ) )
						{
							existingEntry.ParameterReferences.Add( parameter.Identifier );
						}

						categories[categories.IndexOf( existingEntry )] = existingEntry;

						if ( parameter is IGroupableBlackboardParameter groupableParameter )
						{
							groupableParameter.GroupReference = existingEntry.Identifier;
						}
					}
				}

				if ( !updatedParameters.Any( x => x.Parameter.Name == parameter.Name ) )
				{
					updatedParameters.Add( new ParameterEntry( parameter, 0, true ) );
				}
			}
			else
			{
				if ( !updatedParameters.Any( x => x.Parameter.Name == parameter.Name ) )
				{
					// Shove all non-grouped parameters after the grouped parameters.
					updatedParameters.Add( new ParameterEntry( parameter, updatedParameters.Count(), false ) );
				}
			}
		}

		if ( obj[JsonKeys.ParameterArray] is not JsonArray oldParameterArray )
			throw new Exception( $"Cannot find jsonArray \'{JsonKeys.ParameterArray}\'" );

		if ( obj[JsonKeys.NodeArray] is not JsonArray oldNodeArray )
			throw new Exception( $"Cannot find jsonArray \'{JsonKeys.NodeArray}\'" );

		var isSubgraph = CheckIfSubgraph( obj );

		//
		// Upgrade Parameters
		//
		var newParameterArray = new JsonArray();

		foreach ( var jsonNode in oldParameterArray )
		{
			if ( jsonNode[JsonKeys.Class] is not JsonValue classValue )
				continue;

			var typeName = classValue.GetValue<string>();
			var typeDesc = EditorTypeLibrary.GetType<BlackboardParameter>( typeName );
			var type = new ClassBlackboardParameterType( typeDesc );

			var newParameterObj = jsonNode.DeepClone().AsObject();

			BlackboardParameter parameter = null;

			if ( typeDesc != null )
			{
				parameter = EditorTypeLibrary.Create<BlackboardParameter>( typeName );
				DeserializeObject( parameter, JsonSerializer.Deserialize<JsonElement>( jsonNode ), SerializerOptions() );

				if ( !typeDesc.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphParameter ) ) && typeDesc.TargetType.IsAssignableTo( typeof( IGroupableBlackboardParameter ) ) )
				{
					var parameterPriorityInGroup = 0;
					var primaryGroup = new UIGroup();

					if ( typeDesc.TargetType.IsAssignableTo( typeof( IBlackboardMaterialParameter ) ) )
					{
						parameterPriorityInGroup = GetGroupOrder( jsonNode, "UI" );
						primaryGroup = GetUIGroup( jsonNode, "UI" );
					}
					else if ( typeDesc.TargetType.IsAssignableTo( typeof( BlackboardTextureMaterialParameter ) ) )
					{
						parameterPriorityInGroup = GetGroupOrder( jsonNode, "Value" );
						primaryGroup = GetUIGroup( jsonNode, "Value" );
					}

					if ( parameterPriorityInGroup == -1 )
					{
						parameterPriorityInGroup = 0;
					}

					AddToGroup( parameter, parameterPriorityInGroup, primaryGroup );
				}
				else
				{
					var portOrder = 0;
					if ( jsonNode["PortOrder"] is JsonValue portOrderValue )
					{
						portOrder = portOrderValue.GetValue<int>();
					}

					updatedParameters.Add( new ParameterEntry( parameter, portOrder, false ) );
				}
			}
		}

		foreach ( var parameter in updatedParameters.OrderBy( x => x.OrderInRoot ) )
		{
			var parameterType = parameter.Parameter.GetType();
			var parameterObject = new JsonObject { { JsonKeys.Class, parameterType.Name } };

			SerializeObject( parameter.Parameter, parameterObject, SerializerOptions() );
			newParameterArray.Add( parameterObject );
		}

		obj.Remove( JsonKeys.ParameterArray );
		obj.Add( JsonKeys.ParameterArray, newParameterArray );

		var newCategoryDataArray = new JsonArray();

		// Sort the categories
		var sortedCategories = categories.OrderBy( x => x.Priority ).ToList();

		for ( int i = 0; i < sortedCategories.Count; i++ )
		{
			sortedCategories[i].Priority = i;
		}

		foreach ( var category in sortedCategories )
		{
			var type = category.GetType();
			var categoryDataObject = new JsonObject { { JsonKeys.Class, type.Name } };

			SerializeObject( category, categoryDataObject, SerializerOptions() );

			newCategoryDataArray.Add( categoryDataObject );
		}

		obj.Add( JsonKeys.CategoryDataArray, newCategoryDataArray );
	}
}
