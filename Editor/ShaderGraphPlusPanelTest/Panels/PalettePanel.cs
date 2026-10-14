using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class PalettePanel : Panel
{
	private bool IsSubgraph { get; }

	private TextInput _searchFilter;
	private Panel _list;

	public PalettePanel() : base()
	{
		var panel = new Panel();
		panel.AddClass( "editor-dock-pane" );
		
		BuildToolbar( panel );
		
		AddChild( panel );

		_list = panel.AddChild( new Panel() );
		_list.AddClass( "sg-list" );

		UpdateList();
	}

	void BuildToolbar( Panel panel )
	{
		var toolbar = panel.AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter Nodes..", "search" ) );
		_searchFilter.OnChange = ( value ) =>
		{
			UpdateList();
		};
	}

	public IEnumerable<IGrouping<string, DisplayInfo>> GetItems()
	{
		var types = EditorTypeLibrary.GetTypes<ShaderNodePlus>()
			.Where( x =>
			{
				if ( x.IsAbstract ) return false;
				if ( x.HasAttribute<HideAttribute>() ) return false;
				if ( x.HasAttribute<InternalNodeAttribute>() ) return false;
				if ( IsSubgraph && x.TargetType == typeof( Result ) )
				{
					return false;
				}
				if ( !IsSubgraph && x.HasAttribute<SubgraphOnlyAttribute>() ) return false;
				if ( x.TargetType == typeof( SubgraphNode ) )
				{
					return false;
				}
				return true;
			} )
			.Select( x => DisplayInfo.ForType( x.TargetType ) );

		if ( !string.IsNullOrEmpty( _searchFilter.Value ) )
		{
			return types.Where( e => e.Name.Contains( _searchFilter.Value, StringComparison.OrdinalIgnoreCase ) )
				.GroupBy( x => x.Group?.ToLower() ).OrderBy( p => p.Key == null )
				.ThenBy( p => p.Key );
		}

		return types.GroupBy( x => x.Group?.ToLower() )
			.OrderBy( p => p.Key == null )
			.ThenBy( p => p.Key );
	}

	public void UpdateList()
	{
		if ( _list == null )
			return;

		_list.DeleteChildren( true );

		foreach ( var category in GetItems() )
		{
			_list.Add.Label( category.Key, "category" );

			//foreach ( var displayInfo in category.OrderBy( x => x.Name ) )
			//{
			//	var item = _list.Add.Panel( "item" );
			//	item.Add.Icon( displayInfo.Icon, "icon" );
			//	item.Add.Label( displayInfo.Name );
			//}
		}
	}
}
