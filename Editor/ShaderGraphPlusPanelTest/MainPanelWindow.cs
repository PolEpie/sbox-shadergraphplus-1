using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class ShaderGraphPlusPanelTest : PanelEditorWindow
{
	private bool _dirty = false;

	public bool IsSubgraph => false;
	public string FileType => ShaderGraphPlusGlobals.AssetTypeName;
	public string FileExtension => ShaderGraphPlusGlobals.AssetTypeExtension;

	[Menu( "Editor", "Shader Graph Plus/NewEditorTest", "schema" )]
	public static void OpenNewEditorTest()
	{
		_window?.Dispose();
		_window = new PanelWindow( "Shader Graph Plus", new Vector2( 1700, 1050 ), null, true ) { MinSize = new Vector2( 800, 480 ) };
		_window.Root.AddChild( new ShaderGraphPlusPanelTest() );
	}

	public ShaderGraphPlusPanelTest()
	{
		AddClass( "editor-window" );

		StyleSheet.Load( ShaderGraphPlusStyleSheets.EditorScss );
		StyleSheet.Load( ShaderGraphPlusStyleSheets.GalleryScss );

		StyleSheet.AddInline( ShaderGraphPlusStyleSheets.ShaderGraphPlusScss, "shadergraphplus_style" );

		Style.FlexDirection = FlexDirection.Column;

		BuildMenuBar();
		CreateToolBar();
		CreateUI();

		CreateNew();

		EditorEvent.Run( ShaderGraphPlusGlobals.EditorEvents.ShaderGraphPlusEditorCreated );
	}

	private void CreateToolBar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "main-toolbar" );

		toolbar.AddButton( "New", "create_new_folder", New );
		toolbar.AddButton( "Open", "folder_open", Open );
		toolbar.AddButton( "Save", "save", Save );

		toolbar.AddSeparator();

		toolbar.AddButton( "Compile", "build", Compile );

		toolbar.AddSpacer();
	}

	private void BuildMenuBar()
	{
		var bar = Add.Panel( "titlebar window-drag" );

		bar.Add.Label( "s&", "brand" );

		// Menus live in the title bar, same as the editor's
		var menus = bar.AddChild( new Sandbox.UI.MenuBar() );
		menus.AddClass( "window-nodrag" );

		var file = menus.AddMenu( "File" );

		Option( file, "New", "Ctrl+N", New );
		Option( file, "Open", "Ctrl+O", Open );
		Option( file, "Save", "Ctrl+S", Save );
		Option( file, "Save As...", "Ctrl+Shift+S", SaveAs );

		file.AddSeparator();
		
		Option( file, "Quit", "Alt+F4", Quit );

		var edit = menus.AddMenu( "Edit" );

		Option( edit, "Undo", "Ctrl+Z", Undo );
		Option( edit, "Redo", "Ctrl+Y", Redo );

		edit.AddSeparator();

		Option( edit, "Cut", "Ctrl+X", CutSelection );
		Option( edit, "Copy", "Ctrl+C", CopySelection );
		Option( edit, "Paste", "Ctrl+V", PasteSelection );
		Option( edit, "Select All", "Ctrl+A", SelectAll );

		bar.Add.Panel( "grow" );

		// Centred over the bar, so the menus on the left don't push it off centre
		var titleWrap = bar.Add.Panel( "titlewrap" );
		WindowTitle = titleWrap.Add.Label( "", "title" );

		WindowButton( bar, "remove", null, _window.Minimize );
		_maximizeIcon = WindowButton( bar, "crop_square", null, () =>
		{
			_window.ToggleMaximized();
			UpdateMaximizeIcon();
		} );
		WindowButton( bar, "close", "close", Quit );

		Add.Panel( "accentline" );

		UpdateTitle();
	}

	private void CreateUI()
	{
		var properties = new Panel();
		properties.AddClass( "sg-props" );

		ConsolePanel.Hook();

		var dockHost = AddChild( new DockHost() );

		dockHost.Register( "graph", "Graph", () => new GraphViewPanel(), canClose: false, icon: "account_tree" );
		dockHost.Register( "palette", "Palette", () => new PalettePanel( false ), icon: "palette" );
		dockHost.Register( "preview", "Preview", () => new PreviewPanel(), icon: "photo" );
		dockHost.Register( "properties", "Properties", properties, icon: "tune" );
		dockHost.Register( "console", "Console", () => new ConsolePanel(), icon: "text_snippet" );

		dockHost.Dock( "graph" );
		dockHost.Dock( "palette", position: DockPosition.Left, fraction: 0.18f );
		dockHost.Dock( "preview", position: DockPosition.Right, fraction: 0.26f );
		dockHost.Dock( "console", position: DockPosition.Bottom, fraction: 0.26f );
		dockHost.Dock( "properties", "preview", DockPosition.Bottom, 0.55f );
	}

	public void New()
	{
		CreateNew();
	}

	public void CreateNew()
	{
		SGPLogger.Info( $"New Graph" );
	}

	public void Open()
	{
		Open( "" );
	}

	public void Open( string path, bool addToPath = true )
	{
		SGPLogger.Info( $"Open Graph" );
	}

	public void SaveAs()
	{
		SaveInternal( true );
	}

	public void Save()
	{
		SaveInternal( false );
	}

	private bool SaveInternal( bool saveAs )
	{
		SGPLogger.Info( $"Save Graph{(saveAs ? " As" : "")}" );

		return true;
	}

	void Quit()
	{
		SGPLogger.Info( $"Quit ShaderGraphPlus Editor" );

		_window.Dispose();
	}

	private void Undo()
	{
		SGPLogger.Info( $"Undo" );
	}

	private void Redo()
	{
		SGPLogger.Info( $"Redo" );
	}

	private void CutSelection()
	{
		SGPLogger.Info( $"Cut Selection" );
	}

	private void CopySelection()
	{
		SGPLogger.Info( $"Copy Selection" );
	}

	private void PasteSelection()
	{
		SGPLogger.Info( $"Paste Selection" );
	}

	private void SelectAll()
	{
		SGPLogger.Info( $"Select All" );
	}

	private void Compile()
	{
		SGPLogger.Info( $"Compile Graph" );
	}

	private void UpdateTitle()
	{
		WindowTitle.Text = $"Shader Graph Plus - untitled{(_dirty ? "*" : "")}";
	}

	private void UpdateMaximizeIcon()
	{
		_maximizeIcon.Text = _window.IsMaximized ? "filter_none" : "crop_square";
	}

	/// <summary>
	/// A row with a shortcut shown beside it. Rows with nothing to do are just there to look at.
	/// </summary>
	static Sandbox.UI.Menu Option( Sandbox.UI.Menu menu, string text, string shortcut = null, Action action = null )
	{
		var option = menu.AddOption( text, action );
		option.Shortcut = shortcut;
		return option;
	}

	Sandbox.UI.Label WindowButton( Panel parent, string icon, string classname, Action onClick )
	{
		var button = parent.Clickable( "windowbutton window-nodrag", onClick );
		if ( classname is not null ) button.AddClass( classname );
		return button.Icon( icon );
	}
}

