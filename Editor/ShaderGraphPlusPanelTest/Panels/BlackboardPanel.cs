using Sandbox.UI;

namespace ShaderGraphPlusPanelTest;

public sealed class BlackboardPanel : Panel
{
	private TextInput _searchFilter;

	public BlackboardPanel()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );

		BuildToolbar();
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter..", "search" ) );
		_searchFilter.OnChange = ( value ) =>
		{
			
		};

		var addButton = toolbar.AddChild( new Button( "", "add" ) );
	}
}
