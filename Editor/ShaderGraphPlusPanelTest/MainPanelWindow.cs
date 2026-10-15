using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class ShaderGraphPlusPanelTest : PanelEditorWindow
{
	public bool IsSubgraph => false;
	public string FileType => ShaderGraphPlusGlobals.AssetTypeName;
	public string FileExtension => ShaderGraphPlusGlobals.AssetTypeExtension;

	[Menu( "Editor", "Shader Graph Plus/NewEditorTest", "schema" )]
	public static void OpenNewEditorTest()
	{
		_window?.Dispose();
		_window = new PanelWindow( "Shader Graph Plus", new Vector2( 1700, 1050 ) ) { MinSize = new Vector2( 800, 480 ) };
		_window.Root.AddChild( new ShaderGraphPlusPanelTest() );
	}

	public ShaderGraphPlusPanelTest()
	{
		AddClass( "editor-window" );

		StyleSheet.Load( ShaderGraphPlusStyleSheets.EditorScss );
		StyleSheet.Load( ShaderGraphPlusStyleSheets.GalleryScss );

		StyleSheet.AddInline( ShaderGraphPlusStyleSheets.ShaderGraphPlusScss, "shadergraphplus_style" );

		Style.FlexDirection = FlexDirection.Column;

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
		SGPLogger.Info( $"Save Graph" );

		return true;
	}

	private void Compile()
	{
		SGPLogger.Info( $"Compile Graph" );
	}
}

