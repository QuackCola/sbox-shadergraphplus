using Sandbox.UI;

namespace EditorExtras;

public class ListViewPanel : Panel
{
	private readonly List<object> _items = new List<object>();

	public IEnumerable<object> Items
	{
		get
		{
			lock ( _items )
			{
				return _items.ToList();
			}
		}
	}

	public Func<object,Panel> OnCreateRow { get; set; }

	public ListViewPanel()
	{
		AddClass( "scroll" );
	}

	public void Rebuild()
	{
		DeleteChildren( true );

		if ( OnCreateRow == null )
			return;

		foreach ( var item in Items )
		{
			AddChild( OnCreateRow.Invoke( item ) );
		}
	}

	public void SetItems( IEnumerable<object> items )
	{
		_items.Clear();
		if ( items != null )
		{
			_items.AddRange( items );
		}

		Rebuild();
	}

	public void RemoveItem( object item )
	{
		if ( _items.Remove( item ) )
		{
		}
	}
}
