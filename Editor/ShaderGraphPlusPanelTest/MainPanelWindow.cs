using Editor;
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

	private GraphViewPanel _graphView;

	[Menu( "Editor", "Shader Graph Plus/NewEditorTest", "schema" )]
	public static void OpenNewEditorTest()
	{
		_window?.Dispose();
		_window = new PanelWindow( "Shader Graph Plus", new Vector2( 1700, 1050 ), null, true ) { MinSize = new Vector2( 800, 480 ) };
		_window.Root.AddChild( new ShaderGraphPlusPanelTest() );
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

		var view = menus.AddMenu( "View" );
		Option( view, "Graph", action: () => ShowDock( "graph" ) );
		Option( view, "Palette", action: () => ShowDock( "palette" ) );
		Option( view, "Blackboard", action: () => ShowDock( "blackboard" ) );
		Option( view, "Preview", action: () => ShowDock( "preview" ) );
		Option( view, "Properties", action: () => ShowDock( "properties" ) );
		Option( view, "Console", action: () => ShowDock( "console" ) );

		view.AddSeparator();

		Option( view, "Save Layout", action: () => _savedLayout = DockWindows.State );
		Option( view, "Restore Layout", action: () => DockWindows.RestoreState( _savedLayout ) );
		Option( view, "Reset Layout", action: () => DockWindows.RestoreState( _defaultLayout ) );

		view.AddSeparator();

		Option( view, "Full Screen", "F11", () => { _window.ToggleMaximized(); UpdateMaximizeIcon(); } );

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

	protected override void CreateUI()
	{
		base.CreateUI();

		var properties = new Panel();
		properties.AddClass( "sg-props" );

		ConsolePanel.Hook();

		_graphView = new GraphViewPanel();

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
		DockHost.Register( "palette", "Palette", () => new PalettePanel( false ), icon: "palette" );
		DockHost.Register( "blackboard", "Blackboard", () => new BlackboardPanel(), icon: "format_list_bulleted" );

		DockHost.Register( "preview", "Preview", () => new PreviewPanel(), icon: "photo" );
		DockHost.Register( "properties", "Properties", properties, icon: "tune" );
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
		SGPLogger.Info( $"New Graph" );

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

	private void UpdateTitle()
	{
		WindowTitle.Text = $"Shader Graph Plus - untitled{(_dirty ? "*" : "")}";
	}
}
