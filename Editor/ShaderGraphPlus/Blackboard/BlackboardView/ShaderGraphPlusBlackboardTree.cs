using Editor;

namespace ShaderGraphPlus;

public sealed class ShaderGraphPlusBlackboardTree : BlackboardTree
{

	public new ShaderGraphPlus Graph
	{
		get => (ShaderGraphPlus)base.Graph;
		set => base.Graph = value;
	}

	/// <summary>
	/// Called when a parameter is selected.
	/// </summary>
	public Action<BlackboardParameter> OnParameterSelected { get; set; }

	public ShaderGraphPlusBlackboardTree( ShaderGraphPlusBlackboardView parent ) : base( parent )
	{
		ItemSelected = OnItemClicked;
	}

	private void OnItemClicked( object value )
	{
		if ( value is not BlackboardParameter parameter )
			return;

		OnParameterSelected?.Invoke( parameter );
	}

	protected override int ContentHash()
	{
		var hash = new HashCode();

		//foreach ( var item in TreeNodes )
		//{
		//	hash.Add( item );
		//}

		foreach ( var item in Graph.Parameters )
		{
			hash.Add( item );
		}

		foreach ( var item in Graph.CategoryData )
		{

			hash.Add( item );
		}

		return hash.ToHashCode();
	}

	protected override DropAction OnBodyDragDrop( ItemDragEvent ev )
	{
		var source = ev.Data.Object;

		if ( source is BlackboardGroupTreeNode ) return DropAction.Ignore;

		if ( source is ShaderGraphPlusParameterTreeNode parameterTreeNode )
		{
			if ( ev.IsDrop && parameterTreeNode.IsGrouped )
			{
				var groupable = parameterTreeNode.Value as IGroupableBlackboardParameter;

				if ( Graph.TryFindCategoryData( groupable.GroupReference, out var referencedGroup ) )
				{
					using var undoScope = UndoScope( "Remove Parameter From Group" );

					groupable.GroupReference = Guid.Empty;

					referencedGroup.ParameterReferences.Remove( groupable.Identifier );

					Graph.ReOrderParameter( groupable as BlackboardParameter, Graph.Parameters.Count() );

					SelectItem( groupable as BlackboardParameter );
				}
			}

			return DropAction.Move;
		}

		return DropAction.Ignore;
	}

	protected override void OnAddItemsToTree( List<TreeNode> nodes, string search )
	{
		if ( !string.IsNullOrWhiteSpace( search ) )
		{
			foreach ( var parameter in Graph.Parameters )
			{
				if ( !parameter.DisplayInfo.Name.Contains( search, StringComparison.OrdinalIgnoreCase ) && !parameter.Name.Contains( search, StringComparison.OrdinalIgnoreCase ) )
					continue;

				if ( parameter is BlackboardParameter blackboardParameter )
				{
					nodes.Add( new ShaderGraphPlusParameterSearchTreeNode( blackboardParameter ) );
				}
			}

			foreach ( var category in Graph.CategoryData.OrderBy( x => x.Priority ) )
			{
				if ( !category.Name.Contains( search, StringComparison.OrdinalIgnoreCase ) )
					continue;

				nodes.Add( new BlackboardGroupSearchTreeNode( category ) );
			}
		}
		else
		{
			foreach ( var parameter in Graph.Parameters )
			{
				if ( parameter is IGroupableBlackboardParameter groupableParameter && groupableParameter.IsGrouped )
					continue;

				if ( parameter is BlackboardParameter blackboardParameter )
				{
					nodes.Add( new ShaderGraphPlusParameterTreeNode( blackboardParameter ) );
				}
			}

			foreach ( var category in Graph.CategoryData.OrderBy( x => x.Priority ) )
			{
				var priority = category.Priority;

				if ( category.Priority > nodes.Count() - 1 )
				{
					nodes.Add( new BlackboardGroupTreeNode( category ) );
				}
				else
				{
					nodes.Insert( priority, new BlackboardGroupTreeNode( category ) );
				}
			}
		}
	}

	protected override void OnSelectionRestore( IEnumerable<object> selection )
	{
		var items = new List<object>();
		items.AddRange( Graph.Parameters );
		items.AddRange( Graph.CategoryData );

		foreach ( var item in items )
		{
			if ( item is IBlackboardParameter parameter )
			{
				if ( selection.OfType<BlackboardParameter>().FirstOrDefault( x => x.IsValid() && x.Identifier == parameter.Identifier ).IsValid() )
				{
					Selection.Add( parameter );
				}
			}
			else if ( item is CategoryData categoryData )
			{
				if ( selection.OfType<CategoryData>().Any( x => x.Identifier == categoryData.Identifier ) )
				{
					Selection.Add( categoryData );
				}
			}
		}
	}
}
