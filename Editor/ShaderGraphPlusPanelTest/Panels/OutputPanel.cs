using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

[StyleSheet.Inline( "sgp_outputpanel", Styles )]
public sealed partial class OutputPanel : Panel
{
	private BaseVirtualPanel _issueList;
	
	public List<GraphCompiler.GraphIssue> Issues
	{
		get;
		set
		{ 
			field = value;

			_issueList.Items = value.Cast<object>();
			_issueList.ScrollOffset = Vector2.Zero;
		}
	}

	public OutputPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );

		_issueList = new VirtualList
		{
			ItemHeight = 48,
			OnCreateCell = OnCreateCell
		};

		AddChild( _issueList );
		
		// Test
		AddTestIssues( 12 );
	}

	private void OnCreateCell( Panel cell, object value )
	{
		var issue = (GraphCompiler.GraphIssue)value;
		var issuePanel = new IssuePanel( issue );
		
		cell.AddChild( issuePanel );

		// Test
		if ( !issue.IsWarning )
		{
			SGPLogger.Info( $"Error \"{issue.Message}\"" );
		}
		else
		{
			SGPLogger.Info( $"Warning \"{issue.Message}\"" );
		}
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
		//foreach ( var error in _issueList.Children.Where( x => x is IssuePanel issuePanel && issuePanel.Issue.IsWarning == false ) )
		//{
		//	_issueList.RemoveItem( error );
		//}
	}

	public void ClearWarnings()
	{
		//foreach ( var warning in _issueList.Children.Where( x => x is IssuePanel issuePanel && issuePanel.Issue.IsWarning == true ) )
		//{
		//	_issueList.RemoveItem( warning );
		//}
	}
}

sealed class IssuePanel : Panel
{
	public GraphCompiler.GraphIssue Issue { get; init; }

	public IssuePanel( GraphCompiler.GraphIssue issue ) : base()
	{
		Issue = issue;
	}
}
