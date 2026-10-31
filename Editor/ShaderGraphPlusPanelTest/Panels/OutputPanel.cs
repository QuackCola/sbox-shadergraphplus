using Sandbox.UI;
using Sandbox.UI.Construct;
using EditorExtras;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

[StyleSheet.Inline( "sgp_outputpanel", Styles )]
public sealed partial class OutputPanel : Panel
{
	public List<GraphCompiler.GraphIssue> Issues
	{
		get;
		set
		{
			field = value;

			_listView.SetItems( value.Cast<object>() );
		}
	}

	private ListViewPanel _listView;

	public OutputPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
	
		_listView = new ListViewPanel();
		_listView.OnCreateRow += OnCreateListRow;
		AddChild( _listView );

		// Test
		AddTestIssues( 12 );

		_listView.Rebuild();
	}

	private Panel OnCreateListRow( object item )
	{
		return new IssuePanel( (GraphCompiler.GraphIssue)item );
	}

	// Test
	private void AddTestIssues( int count )
	{
		var testItems = new List<GraphCompiler.GraphIssue>();

		for ( int i = 0; i < count; i++ )
		{
			testItems.Add( new GraphCompiler.GraphIssue()
			{
				IsWarning = (i % 2) == 0,
				Message = $"TestIssue {i}"
			} );
		}

		Issues = testItems;
	}

	public void ClearAll()
	{
		ClearErrors();
		ClearWarnings();
	}

	public void ClearErrors()
	{
		foreach ( var error in _listView.Items.Where( x => x is GraphCompiler.GraphIssue issue && issue.IsWarning == false ) )
		{
			_listView.RemoveItem( error );
		}
	}

	public void ClearWarnings()
	{
		foreach ( var warning in _listView.Items.Where( x => x is GraphCompiler.GraphIssue issue && issue.IsWarning == true ) )
		{
			_listView.RemoveItem( warning );
		}
	}

	public void Rebuild()
	{
		_listView.Rebuild();
	}
}

sealed class IssuePanel : Panel
{
	public GraphCompiler.GraphIssue Issue { get; init; }

	public IssuePanel( GraphCompiler.GraphIssue issue ) : base()
	{
		Issue = issue;

		AddClass( "treerow" );

		Style.Height = 48;
		Add.Label( issue.Message, "name" );
	}
}
