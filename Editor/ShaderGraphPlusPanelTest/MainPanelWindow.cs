using Editor;
using NodeEditorPlusTest;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class ShaderGraphPlusPanelTest : PanelDockWindow
{
	private bool _dirty = false;

	public bool IsSubgraph => false;
	public string FileType => ShaderGraphPlusGlobals.AssetTypeName;
	public string FileExtension => ShaderGraphPlusGlobals.AssetTypeExtension;

	private ShaderGraphPlusViewPanel _graphView;
	private PropertiesPanel _properties;
	private PalettePanel _palette;
	private BlackboardPanel _blackboard;
	private PreviewPanel _preview;

	private ShaderGraphPlus.ShaderGraphPlus _graph;

	[Menu( "Editor", "Shader Graph Plus/NewEditorTest", "schema" )]
	public static void OpenNewEditorTest()
	{
		// Look the open editor up rather than keeping a static: statics don't survive hotload, the window does.
		foreach ( var existing in PanelWindow.All.Where( x => x.Root?.Children.OfType<ShaderGraphPlusPanelTest>().Any() == true ).ToArray() )
		{
			existing.Dispose();
		}

#pragma warning disable CA2000 // Dispose objects before losing scope
		// The window owns itself until closed; Quit or the next open disposes it
		var window = new PanelWindow( "Shader Graph Plus", new Vector2( 1700, 1050 ), null, true ) { MinSize = new Vector2( 800, 480 ) };
#pragma warning restore CA2000 // Dispose objects before losing scope
		window.Root.AddChild( new ShaderGraphPlusPanelTest() );
	}

	public ShaderGraphPlusPanelTest() : base()
	{
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

		Option( file, "New", "note_add", "Ctrl+N", New );
		Option( file, "Open", "file_open", "Ctrl+O", Open );
		Option( file, "Save", "save", "Ctrl+S", Save );
		Option( file, "Save As...", "save_as", "Ctrl+Shift+S", SaveAs );

		file.AddSeparator();

		Option( file, "Quit", "logout", "Alt+F4", Quit );

		var edit = menus.AddMenu( "Edit" );

		Option( edit, "Undo", "undo", "Ctrl+Z", Undo );
		Option( edit, "Redo", "redo", "Ctrl+Y", Redo );

		edit.AddSeparator();

		Option( edit, "Cut", "content_cut", "Ctrl+X", CutSelection );
		Option( edit, "Copy", "content_copy", "Ctrl+C", CopySelection );
		Option( edit, "Paste", "content_paste", "Ctrl+V", PasteSelection );
		Option( edit, "Select All", "select_all", "Ctrl+A", SelectAll );

		var view = menus.AddMenu( "View" );

		Option( view, "Graph", "account_tree", action: () => ShowDock( "graph" ) );
		Option( view, "Palette", "palette", action: () => ShowDock( "palette" ) );
		Option( view, "Blackboard", "format_list_bulleted", action: () => ShowDock( "blackboard" ) );
		Option( view, "Preview", "photo", action: () => ShowDock( "preview" ) );
		Option( view, "Properties", "tune", action: () => ShowDock( "properties" ) );
		Option( view, "Console", "text_snippet", action: () => ShowDock( "console" ) );

		view.AddSeparator();

		Option( view, "Save Layout", "playlist_add", action: () => _savedLayout = DockWindows.State );
		Option( view, "Restore Layout", "playlist_add_check", action: () => DockWindows.RestoreState( _savedLayout ) );
		Option( view, "Reset Layout", "restart_alt", action: () => DockWindows.RestoreState( _defaultLayout ) );

		view.AddSeparator();

		Option( view, "Full Screen", "F11", "", () => { Window?.ToggleMaximized(); UpdateMaximizeIcon(); } );

		bar.Add.Panel( "grow" );

		// Centred over the bar, so the menus on the left don't push it off centre
		var titleWrap = bar.Add.Panel( "titlewrap" );
		WindowTitle = titleWrap.Add.Label( "", "title" );

		WindowButton( bar, "remove", null, () => Window?.Minimize() );
		_maximizeIcon = WindowButton( bar, "crop_square", null, () =>
		{
			Window?.ToggleMaximized();
			UpdateMaximizeIcon();
		} );
		WindowButton( bar, "close", "close", Quit );

		Add.Panel( "accentline" );

		UpdateTitle();
	}

	protected override void CreateUI()
	{
		base.CreateUI();

		ConsolePanel.Hook();

		_graphView = new ShaderGraphPlusViewPanel();
		_properties = new PropertiesPanel();
		_palette = new PalettePanel( IsSubgraph );
		_blackboard = new BlackboardPanel( IsSubgraph ) { Graph = _graph };
		_preview = new PreviewPanel();

		var nodeTypes = EditorTypeLibrary.GetTypes<ShaderNodePlus>()
			.Where( x => !x.IsAbstract && !x.HasAttribute<HideAttribute>() ).OrderBy( x => x.Name );

		foreach ( var type in nodeTypes )
		{
			_graphView.AddNodeType( type );
		}

		var subgraphs = AssetSystem.All.Where( x => x.Path.EndsWith( $".{ShaderGraphPlusGlobals.SubgraphAssetTypeExtension}", StringComparison.OrdinalIgnoreCase ) );
		foreach ( var subgraph in subgraphs )
		{
			// Skip any _c compiled subgraph files.
			if ( subgraph.CanRecompile )
			{
				_graphView.AddNodeType( subgraph.Path );
			}
		}

		DockHost.Register( "graph", "Graph", _graphView, canClose: false, icon: "account_tree" );
		DockHost.Register( "palette", "Palette", _palette, icon: "palette" );
		DockHost.Register( "blackboard", "Blackboard", _blackboard, icon: "format_list_bulleted" );

		DockHost.Register( "preview", "Preview", _preview, icon: "photo" );
		DockHost.Register( "properties", "Properties", _properties, icon: "tune" );
		DockHost.Register( "console", "Console", () => new ConsolePanel(), icon: "text_snippet" );

		DockHost.Dock( "graph" );
		DockHost.Dock( "palette", position: DockPosition.Left, fraction: 0.24f );
		DockHost.Dock( "blackboard", "palette", position: DockPosition.Center, fraction: 0.18f );
		DockHost.Dock( "preview", position: DockPosition.Right, fraction: 0.26f );
		DockHost.Dock( "console", position: DockPosition.Bottom, fraction: 0.26f );
		DockHost.Dock( "properties", "preview", DockPosition.Bottom, 0.55f );

		DockHost.Activate( "palette" );

		_defaultLayout = DockWindows.State;
		_savedLayout = _defaultLayout;
	}

	private void PromptSave( Action action )
	{
		if ( !_dirty )
		{
			action?.Invoke();
			return;
		}

		var confirm = new PopupWindow(
			"Save Current Graph", "The open graph has unsaved changes. Would you like to save now?", "Cancel",
			new Dictionary<string, Action>()
			{
				{ "No", () => action?.Invoke() },
				{ "Yes", () => { if ( SaveInternal( false ) ) action?.Invoke(); } }
			}
		);

		confirm.Show();
	}

	public void New()
	{
		PromptSave( CreateNew );
	}

	public void CreateNew()
	{
		SGPLogger.Info( $"TODO : New Graph" );

		_graph = new ShaderGraphPlus.ShaderGraphPlus { IsSubgraph = IsSubgraph };
		_blackboard.Graph = _graph;

		_graphView.RebuildFromGraph();

		if ( !IsSubgraph )
		{
			var result = _graphView.CreateNewNode( _graphView.FindNodeType( typeof( Result ) ), 0 );
		}
	}

	public void Open()
	{
		var fd = new FileDialog( null )
		{
			Title = $"Open {FileType}",
			DefaultSuffix = $".{FileExtension}"
		};

		fd.SetNameFilter( $"{FileType} ( *.{FileExtension})" );

		if ( !fd.Execute() )
			return;

		PromptSave( () => Open( fd.SelectedFile ) );
	}

	public void Open( string path, bool addToPath = true )
	{
		SGPLogger.Info( $"TODO : Open Graph" );

		_preview.LoadSettings( new() );
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
		SGPLogger.Info( $"TODO : Save Graph{(saveAs ? " As" : "")}" );

		return true;
	}

	void Quit()
	{
		SGPLogger.Info( $"TODO : Quit ShaderGraphPlus Editor" );

		Window?.Dispose();
	}

	private void Undo()
	{
		SGPLogger.Info( $"TODO : Undo" );
	}

	private void Redo()
	{
		SGPLogger.Info( $"TODO : Redo" );
	}

	private void CutSelection()
	{
		SGPLogger.Info( $"TODO : Cut Selection" );
	}

	private void CopySelection()
	{
		SGPLogger.Info( $"TODO : Copy Selection" );
	}

	private void PasteSelection()
	{
		SGPLogger.Info( $"TODO : Paste Selection" );
	}

	private void SelectAll()
	{
		SGPLogger.Info( $"TODO : Select All" );
	}

	private void Compile()
	{
		SGPLogger.Info( $"TODO : Compile Graph" );
	}

	private string GeneratePreviewCode()
	{
		return "";
	}

	private void SetDirty( bool evaluate = true )
	{
		_dirty = true;

		UpdateTitle();

		if ( evaluate )
			GeneratePreviewCode();
	}

	// For Development only
	[EditorEvent.Hotload]
	private void OnHotload()
	{
		BuildMenuBar();
		CreateToolBar();
		CreateUI();

		Compile();

		//_preview.LoadSettings( _graph.PreviewSettings );
		_preview.LoadSettings( new() );

		Log.Info( "ShaderGraphPlus hotloaded!" );
	}

	private void UpdateTitle()
	{
		WindowTitle.Text = $"Shader Graph Plus - untitled{(_dirty ? "*" : "")}";
	}
}
