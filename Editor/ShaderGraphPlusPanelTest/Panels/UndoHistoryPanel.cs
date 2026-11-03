using Sandbox.UI;
using Sandbox.UI.Construct;
using EditorExtras;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class UndoHistoryPanel : EditorDockPanel
{
	public IEnumerable<string> History { set => _listView.SetItems( value ); }

	public int UndoLevel
	{
		get;
		set
		{
			_undoOption.Disabled = !_undoStack.CanUndo;
			_redoOption.Disabled = !_undoStack.CanRedo;
			_clearOption.Disabled = !(_undoStack.CanUndo || _undoStack.CanRedo);
			_undoOption.Tooltip = _undoStack.UndoName;
			_redoOption.Tooltip = _undoStack.RedoName;
			//_undoOption.StatusTip = _undoStack.UndoName;
			//_redoOption.StatusTip = _undoStack.RedoName;

			if ( field == value )
				return;

			field = value;

			//Update();
		}
	}

	private UndoStack _undoStack;

	private Sandbox.UI.Button _undoOption;
	private Sandbox.UI.Button _redoOption;
	private Sandbox.UI.Button _clearOption;

	private ListViewPanel _listView;

	public Action OnUndo { get; set; }
	public Action OnRedo { get; set; }
	public Action<int> OnHistorySelected { get; set; }

	public UndoHistoryPanel( UndoStack undoStack ) : base()
	{
		_undoStack = undoStack;

		CreateToolBar();

		_listView = new ListViewPanel();
		_listView.StyleSheet.Load( "/styles/undopanel.scss" );
		_listView.OnCreateRow += OnCreateListRow;
		AddChild( _listView );

		History = _undoStack.Names;
		UndoLevel = _undoStack.UndoLevel;
	}

	private void CreateToolBar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "main-toolbar" );

		_undoOption = toolbar.AddButton( "", "undo", () => OnUndo?.Invoke() );
		_redoOption = toolbar.AddButton( "", "redo", () => OnRedo?.Invoke() );

		toolbar.AddSeparator();

		_clearOption = toolbar.AddButton( "", "playlist_remove", Clear );
		_clearOption.Tooltip = "Clear History";
		//_clearOption.StatusTip = "Clear History";
	}

	private Panel OnCreateListRow( object item )
	{
		return new UndoItemPanel( (string)item );
	}

	private void Clear()
	{
		_undoStack.Clear();

		History = _undoStack.Names;
		UndoLevel = 0;
	}
}

sealed class UndoItemPanel : Panel
{
	public UndoItemPanel( string undoName ) : base()
	{
		AddClass( "undoitem" );
		Add.Label( undoName, "name" );
	}
}
