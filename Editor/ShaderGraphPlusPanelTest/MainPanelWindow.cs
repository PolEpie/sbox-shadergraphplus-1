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

		StyleSheet.Load( ShaderGraphPlusStyles.EditorScss );
		StyleSheet.Load( ShaderGraphPlusStyles.GalleryScss );

		StyleSheet.AddInline( ShaderGraphPlusStyles.ShaderGraphPlusScss, "shadergraphplus_style" );

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
		
		toolbar.Add.Label( "Example only", "status" );
	}

	private void CreateUI()
	{
		var properties = new Panel();
		properties.AddClass( "sg-props" );

		//var graph = new TestGraphView { OnSelect = node => ShowProperties( properties, node ) };
		//ShowProperties( properties, graph.Nodes[^1] );

		ConsolePanel.Hook();

		var dock = AddChild( new DockHost() );
		//dock.Register( "graph", "Graph", graph, canClose: false, icon: "schema" );
		dock.Register( "palette", "Palette", () => new PalettePanel(), icon: "palette" );
		dock.Register( "preview", "Preview", () => new PreviewPanel(), icon: "photo" );
		dock.Register( "properties", "Properties", properties, icon: "tune" );
		dock.Register( "console", "Console", () => new ConsolePanel(), icon: "text_snippet" );

		//dock.Dock( "graph" );
		dock.Dock( "palette", position: DockPosition.Left, fraction: 0.18f );
		dock.Dock( "preview", position: DockPosition.Right, fraction: 0.26f );
		dock.Dock( "console", position: DockPosition.Bottom, fraction: 0.26f );
		dock.Dock( "properties", "preview", DockPosition.Bottom, 0.55f );
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

