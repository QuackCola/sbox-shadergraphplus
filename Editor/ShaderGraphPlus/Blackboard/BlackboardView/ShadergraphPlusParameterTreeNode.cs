using Editor;
using System.Text;

using static Editor.BaseItemWidget;

namespace ShaderGraphPlus;

public class ShaderGraphPlusParameterTreeNode : BlackboardTreeNode<BlackboardParameter, ShaderGraphPlusBlackboardTree>
{
	public override string Name
	{
		get => Value.Name;
		set => Value.Name = value;
	}

	public override bool IsGrouped => Value is IGroupableBlackboardParameter groupable && groupable.IsGrouped;

	public override int ValueHash => Value.GetHashCode();

	public ShaderGraphPlusParameterTreeNode( BlackboardParameter parameter ) : base( parameter )
	{
	}

	public override string GetTooltip()
	{
		var sb = new StringBuilder();

		sb.AppendLine( $"<h3>{Name}</h3>" );

		var usrDesc = "";

		if ( Value is IBlackboardSubgraphParameter subgraphParameter )
		{
			usrDesc = subgraphParameter.Description;
		}

		if ( !string.IsNullOrWhiteSpace( usrDesc ) )
		{
			sb.AppendLine( $"<br />" );
			sb.AppendLine( $"<i>{usrDesc}</i>" );
		}

		return sb.ToString();
	}

	protected override void OnPaintItemDropping( VirtualWidget item, Rect dropRect )
	{
		var dragEvent = TreeView.CurrentItemDragEvent;
		var sourceNode = dragEvent.Data.Object as BlackboardTreeNode;
		var targetNode = this;

		Paint.ClearPen();
		Paint.SetBrush( Theme.Blue );

		if ( IsGrouped )
		{
			dropRect = dropRect.Shrink( 24, 0, 0, 0 );
		}

		if ( dragEvent.DropEdge.HasFlag( ItemEdge.Top ) )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( dropRect.TopLeft, dropRect.TopRight );
		}
		else if ( dragEvent.DropEdge.HasFlag( ItemEdge.Bottom ) || dragEvent.DropEdge.HasFlag( ItemEdge.None ) )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( dropRect.BottomLeft, dropRect.BottomRight );
		}
	}

	protected static void PaintTypeLabel( Rect rect, string typeName, Color typeColor )
	{
		Color tint = "#48494c";

		var c = tint.ToHsv();
		var bg = c;

		if ( Paint.HasMouseOver )
		{
			bg = c with { Value = (c.Value + 0.1f) };
		}
		else
		{
			bg = c = Theme.SurfaceLightBackground;
		}

		if ( bg.Alpha > 0 )
		{
			float radius = 3;
			Paint.Antialiasing = true;

			Paint.ClearPen();
			Paint.SetBrush( bg with { Value = (bg.Value + 0.04f), Saturation = (c.Saturation * 0.8f) } );
			Paint.DrawRect( rect, radius );

			Paint.SetBrushLinear( rect.TopLeft, rect.BottomRight, bg, bg with { Value = (bg.Value - 0.03f) } );
			Paint.DrawRect( rect.Shrink( 1, 1, 1, 1 ), radius );
			Paint.SetPen( typeColor, 1 );
			Paint.DrawRect( rect.Shrink( 1, 1, 1, 1 ), radius );

			var r2 = rect.Grow( 1.25f );

			Paint.DrawRect( r2.Grow( 12, 0, 0, 0 ), radius );
		}
		else
		{
			c = Color.White.WithAlpha( 0.5f );
		}

		Paint.SetDefaultFont();
		Paint.SetPen( c with { Value = 0.99f, Saturation = c.Saturation * 0.20f } );
		Paint.DrawText( rect, typeName );

		var iconRect = rect;
		iconRect.Left -= 10;

		Paint.Pen = typeColor;
		Paint.DrawIcon( iconRect, "circle", 12, TextFlag.LeftCenter );
	}

	protected override void PaintSpecial( Rect item )
	{
		var targetValue = Value;

		float opacity = 0.9f;

		var r = item;
		r.Left += 4;

		Color pen = Theme.TextControl;
		var typeColor = Color.White;
		if ( ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( targetValue.GetType(), out var blackboardConfig ) )
		{
			typeColor = blackboardConfig.Color;
		}

		var typeName = DisplayInfo.ForType( targetValue.GetType() ).Name;
		var typeRect = Paint.MeasureText( r, typeName, TextFlag.LeftCenter | TextFlag.SingleLine ).Grow( 4, 0, 4, 0 );

		Paint.SetDefaultFont();

		PaintTypeLabel( typeRect, typeName, typeColor );
		r.Left += typeRect.Width;

		r.Left += 4;

		Paint.Pen = pen.WithAlphaMultiplied( opacity );

		r.Left += Paint.DrawText( r, Name, TextFlag.LeftCenter | TextFlag.SingleLine ).Width + 4;

		if ( ConCommands.BlackboardParameterIndexDebug )
		{
			if ( Value is IGroupableBlackboardParameter groupable )
			{
				r.Left += 6;

				if ( groupable.IsGrouped )
				{
					TreeView.Graph.TryFindCategoryData( groupable.GroupReference, out var category );

					Paint.DrawText( r, $"// Group Index {category.ParameterReferences.IndexOf( groupable.Identifier )}", TextFlag.LeftCenter | TextFlag.SingleLine );
				}
				else
				{
					Paint.DrawText( r, $"// Root Index {TreeView.Graph.GetParameterIndex( Value )}", TextFlag.LeftCenter | TextFlag.SingleLine );
				}
			}
		}
	}

	protected override void InsertNodeAtEdge( BlackboardTreeNode source, ItemEdge edge )
	{
		var graph = TreeView.Graph;

		void AddToGroup( IGroupableBlackboardParameter source, IGroupableBlackboardParameter targetReference, bool before = false )
		{
			graph.TryFindCategoryData( targetReference.GroupReference, out var categoryData );

			source.GroupReference = categoryData.Identifier;

			var targetIndex = categoryData.ParameterReferences.IndexOf( targetReference.Identifier );

			if ( !before ) targetIndex++;

			categoryData.ParameterReferences.Insert( targetIndex, source.Identifier );
		}

		void ReOrder( BlackboardTreeNode source, bool before = false )
		{
			var targetValue = Value;

			if ( source is not BlackboardGroupTreeNode && IsGrouped )
			{
				var sourceParameter = source.Value as BlackboardParameter;
				var sourceGroupable = sourceParameter as IGroupableBlackboardParameter;
				var targetGroupable = Value as IGroupableBlackboardParameter;

				if ( source.IsGrouped && sourceGroupable.GroupReference != targetGroupable.GroupReference )
				{
					using var undoScope = TreeView?.UndoScope( "Move Parameter To Group" );

					RemoveFromGroup( sourceGroupable );

					AddToGroup( sourceGroupable, targetGroupable, before );
				}
				else if ( source.IsGrouped && sourceGroupable.GroupReference == targetGroupable.GroupReference )
				{
					graph.TryFindCategoryData( targetGroupable.GroupReference, out var categoryData );

					var sourceIndex = categoryData.ParameterReferences.IndexOf( sourceParameter.Identifier );
					var targetIndex = categoryData.ParameterReferences.IndexOf( targetValue.Identifier );

					if ( !before ) targetIndex++;

					if ( sourceIndex != targetIndex )
					{
						using var undoScope = TreeView?.UndoScope( "Change Parameter Order In Group" );

						categoryData.ReOrderParameter( sourceParameter, targetIndex );
						graph.UpdateCategoryData( categoryData );
					}
				}
				else if ( !source.IsGrouped )
				{
					using var undoScope = TreeView?.UndoScope( "Move Parameter To Group" );

					AddToGroup( sourceGroupable, targetGroupable, before );
				}
			}
			else if ( source is not BlackboardGroupTreeNode )
			{
				var sourceParameter = source.Value as BlackboardParameter;

				var sourceTreeNodeIndex = graph.GetParameterIndex( sourceParameter );
				var targetTreeNodeIndex = graph.GetParameterIndex( targetValue );

				if ( !before )
				{
					if ( sourceTreeNodeIndex > targetTreeNodeIndex )
					{
						targetTreeNodeIndex++;
					}

					if ( targetTreeNodeIndex > graph.Parameters.Count() - 1 )
					{
						targetTreeNodeIndex = graph.Parameters.Count() - 1;
					}
				}

				if ( sourceTreeNodeIndex != targetTreeNodeIndex )
				{
					if ( source.IsGrouped )
					{
						using var undoScope = TreeView?.UndoScope( "Remove Parameter From Group" );

						RemoveFromGroup( source.Value as IGroupableBlackboardParameter );
					}
					else
					{
						using var undoScope = TreeView?.UndoScope( "Change Parameter Order" );
					}

					if ( source.Value is BlackboardParameter )
					{
						//Log.Info( $"Moving Parameter from index '{sourceTreeNodeIndex}' to index '{targetTreeNodeIndex}'" );
					}
					else if ( source.Value is CategoryData categoryData )
					{
						categoryData.Priority = targetTreeNodeIndex;
						//Log.Info( $"Moving Group from index '{sourceTreeNodeIndex}' to index '{targetTreeNodeIndex}'" );
					}

					graph.RemoveParameter( sourceParameter );
					graph.AddParameter( sourceParameter, targetTreeNodeIndex );
				}
			}
			else if ( source is BlackboardGroupTreeNode )
			{
				var sourceCategory = source.Value as CategoryData;

				var sourceTreeNodeIndex = sourceCategory.Priority;
				var targetTreeNodeIndex = TreeView.TreeNodes.IndexOf( this );

				if ( !before )
				{
					if ( sourceTreeNodeIndex > targetTreeNodeIndex )
					{
						targetTreeNodeIndex++;
					}

					if ( targetTreeNodeIndex > TreeView.TreeNodes.Count() - 1 )
					{
						targetTreeNodeIndex = TreeView.TreeNodes.Count() - 1;
					}
				}

				if ( sourceTreeNodeIndex != targetTreeNodeIndex )
				{
					using var undoScope = TreeView?.UndoScope( "Change Group Order" );

					TreeView?.Graph?.UpdateCategoryPriority( sourceCategory, targetTreeNodeIndex );
				}
			}
		}

		if ( edge.HasFlag( ItemEdge.Top ) )
		{
			ReOrder( source, true );
		}
		else if ( edge.HasFlag( ItemEdge.Bottom ) || edge.HasFlag( ItemEdge.None ) )
		{
			ReOrder( source, false );
		}
	}

	protected override void OnPopulateContextMenuSpecialOptions( ContextMenu contextMenu )
	{
		if ( Value is IGroupableBlackboardParameter groupableParameter )
		{
			if ( groupableParameter.IsGrouped )
			{
				contextMenu.AddOption( "Remove From Group", "remove", () => { Menu_RemoveFromGroup( groupableParameter ); } );
			}
			else if ( !TreeView.Graph.IsSubgraph )
			{
				contextMenu.AddOption( "Create Group", "file_copy", () => { Menu_CreateGroup( groupableParameter ); } );
			}
		}
	}

	public override void OnRename( VirtualWidget item, string text, List<TreeNode> selection = null )
	{
		using var undoScope = TreeView?.UndoScope( "Rename Parameter" );

		var parameters = selection.Select( x => x.Value ).OfType<BlackboardParameter>().ToArray();

		foreach ( var parameter in parameters )
		{
			var id = 0;
			var newName = text;

			if ( TreeView.Graph.HasParameterWithName( text ) )
			{
				while ( TreeView.Graph.HasParameterWithName( $"{text}{id}" ) )
				{
					id++;
				}

				newName = $"{text}{id}";
			}

			parameter.Name = newName;
		}
	}

	private int RemoveFromGroup( IGroupableBlackboardParameter sourceParameter )
	{
		var graph = TreeView.Graph;

		if ( graph.TryFindCategoryData( sourceParameter.GroupReference, out var category ) )
		{
			sourceParameter.GroupReference = Guid.Empty;
			category.ParameterReferences.Remove( sourceParameter.Identifier );

			return category.Priority;
		}

		return -1;
	}

	private void Menu_CreateGroup( IGroupableBlackboardParameter groupableParameter )
	{
		using var undoScope = TreeView?.UndoScope( "Create Group From Parameter" );

		var parameter = groupableParameter as BlackboardParameter;

		var newGroup = new CategoryData();
		newGroup.Priority = TreeView.TreeNodes.IndexOf( this );
		newGroup.Graph = TreeView?.Graph;
		newGroup.NewName();
		newGroup.ParameterReferences = new()
		{
			groupableParameter.Identifier,
		};

		groupableParameter.GroupReference = newGroup.Identifier;

		TreeView?.Graph.AddCategoryData( newGroup );
	}

	private void Menu_RemoveFromGroup( IGroupableBlackboardParameter groupableParameter )
	{
		using var undoScope = TreeView?.UndoScope( "Remove Parameter From Group" );

		var groupIndex = RemoveFromGroup( groupableParameter );

		TreeView?.Graph.RemoveParameter( groupableParameter.Identifier );
		TreeView?.Graph.AddParameter( groupableParameter as BlackboardParameter, groupIndex == -1 ? TreeView.Graph.Parameters.Count() : groupIndex );
	}
}

class ShaderGraphPlusParameterSearchTreeNode : ShaderGraphPlusParameterTreeNode
{
	public override bool HasChildren => false;

	public ShaderGraphPlusParameterSearchTreeNode( BlackboardParameter parameter ) : base( parameter )
	{
	}
}
