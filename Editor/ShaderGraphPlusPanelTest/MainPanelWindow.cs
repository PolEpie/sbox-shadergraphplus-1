using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class ShaderGraphPlusPanelTest : PanelEditorWindow
{
	private bool _dirty = false;

	private DockHost _dockHost;
	private PanelDockWindows _dockWindows;
	private string _defaultLayout;
	private string _savedLayout;
	private readonly List<RootPanel> floatingRoots = new();

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

		var view = menus.AddMenu( "View" );
		Option( view, "Graph", action: () => ShowDock( "graph" ) );
		Option( view, "Palette", action: () => ShowDock( "palette" ) );
		Option( view, "Blackboard", action: () => ShowDock( "blackboard" ) );
		Option( view, "Preview", action: () => ShowDock( "preview" ) );
		Option( view, "Properties", action: () => ShowDock( "properties" ) );
		Option( view, "Console", action: () => ShowDock( "console" ) );

		view.AddSeparator();
		
		Option( view, "Save Layout", action: () => _savedLayout = _dockWindows.State );
		Option( view, "Restore Layout", action: () => _dockWindows.RestoreState( _savedLayout ) );
		Option( view, "Reset Layout", action: () => _dockWindows.RestoreState( _defaultLayout ) );
		
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

	private void CreateUI()
	{
		var properties = new Panel();
		properties.AddClass( "sg-props" );

		ConsolePanel.Hook();

		_dockHost = AddChild( new DockHost() );
		_dockWindows = new PanelDockWindows( _dockHost ) { ConfigureWindow = ConfigureFloatingWindow };

		_dockHost.Register( "graph", "Graph", () => new GraphViewPanel(), canClose: false, icon: "account_tree" );
		_dockHost.Register( "palette", "Palette", () => new PalettePanel( false ), icon: "palette" );
		_dockHost.Register( "blackboard", "Blackboard", () => new BlackboardPanel(), icon: "format_list_bulleted" );

		_dockHost.Register( "preview", "Preview", () => new PreviewPanel(), icon: "photo" );
		_dockHost.Register( "properties", "Properties", properties, icon: "tune" );
		_dockHost.Register( "console", "Console", () => new ConsolePanel(), icon: "text_snippet" );

		_dockHost.Dock( "graph" );
		_dockHost.Dock( "palette", position: DockPosition.Left, fraction: 0.24f );
		_dockHost.Dock( "blackboard", "palette", position: DockPosition.Center, fraction: 0.18f );
		_dockHost.Dock( "preview", position: DockPosition.Right, fraction: 0.26f );
		_dockHost.Dock( "console", position: DockPosition.Bottom, fraction: 0.26f );
		_dockHost.Dock( "properties", "preview", DockPosition.Bottom, 0.55f );

		_dockHost.Activate( "palette" );

		_defaultLayout = _dockWindows.State;
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

	private void UpdateMaximizeIcon()
	{
		_maximizeIcon.Text = _window.IsMaximized ? "filter_none" : "crop_square";
	}

	public override void OnDeleted()
	{
		_dockWindows?.Dispose();
		floatingRoots.Clear();
		base.OnDeleted();
	}

	void ShowDock( string id )
	{
		var host = _dockHost.Find( id ) is not null ? _dockHost : floatingRoots
			.Where( x => x.IsValid )
			.SelectMany( x => x.Descendants.OfType<DockHost>() )
			.FirstOrDefault( x => x.Find( id ) is not null );
		if ( host is null ) return;
		if ( !host.IsOpen( id ) ) host.Dock( id );
		host.Activate( id );
		PanelWindow.FromPanel( host )?.Focus();
	}

	void ConfigureFloatingWindow( RootPanel root )
	{
		root.AddClass( "mock-editor-surface" );
		root.AddClass( "editor-window" );
		root.StyleSheet.Load( "/styles/gallery.scss" );
		floatingRoots.Add( root );


		// A floated tool no longer bubbles keys through EditorWindow.
		var host = root.Children.OfType<DockHost>().Single();
		var shortcuts = root.AddChild( new FloatingShortcuts( this ) );
		host.Parent = shortcuts;
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

	sealed class FloatingShortcuts : Panel
	{
		readonly ShaderGraphPlusPanelTest editor;

		internal FloatingShortcuts( ShaderGraphPlusPanelTest editor )
		{
			this.editor = editor;
			Style.Width = Length.Percent( 100 );
			Style.Height = Length.Percent( 100 );
		}

		public override void OnButtonEvent( ButtonEvent e )
		{
			//if ( e.Pressed && editor.IsValid && editor.HandleShortcut( e ) )
			//{
			//	e.StopPropagation = true;
			//	return;
			//}
			base.OnButtonEvent( e );
		}
	}
}

