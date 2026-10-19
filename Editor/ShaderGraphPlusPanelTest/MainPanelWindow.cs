using Editor;
using NodeEditorPlusTest;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class ShaderGraphPlusPanelTest : PanelDockWindow
{
	private bool _dirty = false;
	private bool _autoCompile = true;
	private string _generatedCode;

	private bool _isCompiling = false;
	private bool _isPendingCompile = false;
	private RealTimeSince _timeSinceCompile;

	private Sandbox.UI.Menu _recentFilesMenu;
	private readonly List<string> _recentFiles = new();

	private ShaderGraphPlusViewPanel _graphView;
	private PropertiesPanel _properties;
	private PalettePanel _palette;
	private BlackboardPanel _blackboard;
	private PreviewPanel _preview;

	private ShaderGraphPlus.ShaderGraphPlus _graph;
	private Asset _asset;

	private readonly Dictionary<string, bool> _boolAttributes = new();
	private readonly Dictionary<string, int> _intAttributes = new();
	private readonly Dictionary<string, float> _floatAttributes = new();
	private readonly Dictionary<string, Vector2> _float2Attributes = new();
	private readonly Dictionary<string, Vector3> _float3Attributes = new();
	private readonly Dictionary<string, Color> _float4Attributes = new();
	private readonly Dictionary<string, SamplerState> _samplerStateAttributes = new();
	private readonly Dictionary<string, Texture> _textureAttributes = new();
	private readonly Dictionary<string, int> _dynamicComboIntAttributes = new();
	private readonly Dictionary<string, int> _shaderFeatures = new();

	public bool IsSubgraph => false;
	public string FileType => ShaderGraphPlusGlobals.AssetTypeName;
	public string FileExtension => ShaderGraphPlusGlobals.AssetTypeExtension;

	[Menu( "Editor", "Shader Graph Plus/NewEditorTest", "schema" )]
	public static void OpenNewEditorTest()
	{
		// Find the open editor instead of a static - statics don't survive hotload
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

		_graph = new ShaderGraphPlus.ShaderGraphPlus()
		{
			Title = "untitled",
			IsSubgraph = IsSubgraph
		};

		_recentFiles = Editor.FileSystem.Temporary.ReadJsonOrDefault( "shadergraphplus_recentfiles.json", _recentFiles )
			.Where( x => System.IO.File.Exists( x ) ).ToList();

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

		_recentFilesMenu = file.AddMenu( "Recent Files" );

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

		RefreshRecentFiles();

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

		_graphView = new ShaderGraphPlusViewPanel { Graph = _graph };
		_graphView.SelectionChanged += () =>
		{
			// Nothing, or several, selected shows the graph, like the Qt editor
			var selected = _graphView.SelectedNodes.Take( 2 ).ToArray();
			_properties.Target = selected.Length == 1 ? (object)selected[0].Node : _graph;
		};
		_palette = new PalettePanel( IsSubgraph );

		_blackboard = new BlackboardPanel( IsSubgraph );
		_blackboard.Graph = _graph;
		_blackboard.OnDirty += ( evaluate ) => SetDirty( evaluate );
		_graphView.Blackboard = _blackboard;
		//_blackboard.OnSelected = parameter => _properties.Target = (object)parameter ?? _graph;

		_properties = new PropertiesPanel();
		_properties.Target = _graph;
		_properties.PropertyUpdated += OnPropertyUpdated;
		_preview = new PreviewPanel();

		var nodeTypes = EditorTypeLibrary.GetTypes<ShaderNodePlus>()
			.Where( x => !x.IsAbstract && !x.HasAttribute<HideAttribute>() ).OrderBy( x => x.Name );

		var blackboardParameterTypes = EditorTypeLibrary.GetTypes<BlackboardParameter>()
			.Where( x => !x.IsAbstract ).OrderBy( x => x.Name );

		foreach ( var type in nodeTypes )
		{
			_graphView.AddNodeType( type );
		}

		foreach ( var type in blackboardParameterTypes )
		{
			_graphView.AddParameterType( type );
			_blackboard.AddParameterType( type );
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

	private void OnPropertyUpdated( SerializedProperty serializedProperty )
	{
		// TODO
		/*
		_preview.PostProcessing = _graph.Domain == ShaderDomain.PostProcess;

		if ( _properties.Target is BaseNodePlus node )
		{
			_graphView.UpdateNode( node );
		}

		// Reload shader template
		if ( _properties.Target is ShaderGraphPlus && serializedProperty.IsPropertyName( nameof( ShaderGraphPlus.ShaderType ) ) )
		{
			LoadShaderTypeInfo();
		}
		*/

		var shouldEvaluate = _properties.Target is not CommentNode;
		SetDirty( shouldEvaluate );
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

		_asset = null;
		_graph = new()
		{
			Title = "untitled"
		};
		_dirty = false;

		// Must be done before setting _graphView.Graph and _blackboardView.Graph
		// or else some nodes, namely the 'Result' node may break valid inputs upon it getting initialized via GraphView.RebuildFromGraph().
		//LoadShaderTypeInfo(); // TODO

		_graphView.Graph = _graph;
		_blackboard.Graph = _graph;

		UpdateTitle();

		// TODO
		//_preview.Model = null;
		//_preview.Tint = Color.White;
		//_undoStack.Clear();
		//_undoHistory.History = _undoStack.Names;
		_generatedCode = "";
		//_generatedCodeTextView.Value = "";
		//Selection.Set( _graph );

		// TODO
		//_output.ClearErrors();
		//_output.ClearWarnings();

		if ( !IsSubgraph )
		{
			var result = _graphView.CreateNewNode( _graphView.FindNodeType( typeof( Result ) ), 0 );
		}

		ClearAttributes();
		RestoreShader();
	}

	private void ClearAttributes()
	{
		_boolAttributes.Clear();
		_intAttributes.Clear();
		_floatAttributes.Clear();
		_float2Attributes.Clear();
		_float3Attributes.Clear();
		_float4Attributes.Clear();
		_textureAttributes.Clear();
		_samplerStateAttributes.Clear();
		_dynamicComboIntAttributes.Clear();
		_shaderFeatures.Clear();

		// TODO
		//_preview?.ClearAttributes();
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

		var asset = AssetSystem.FindByPath( path );

		if ( asset == null )
			return;

		if ( asset == _asset )
		{
			Focus();
			return;
		}

		var graph = JsonSerializer.Deserialize<ShaderGraphPlus.ShaderGraphPlus>( System.IO.File.ReadAllText( path ), ShaderGraphPlus.ShaderGraphPlus.SerializerOptions() );
		graph.Path = asset.RelativePath;
		graph.IsSubgraph = IsSubgraph;

		if ( !graph.IsSubgraph )
		{
			graph.Title = asset.Name;
		}

		//_preview.Model = string.IsNullOrWhiteSpace( graph.Model ) ? null : Model.Load( graph.Model );
		_preview.LoadSettings( new() );

		_asset = asset;
		_graph = graph;
		_dirty = false;

		// Must be done before setting _graphView.Graph and _blackboardView.Graph
		// or else some nodes, namely the 'Result' node may break valid inputs upon it getting initialized via GraphView.RebuildFromGraph().
		//LoadShaderTypeInfo(); // TOOD

		_graphView.Graph = _graph;
		_blackboard.Graph = _graph;

		UpdateTitle();

		// TODO
		//_undoStack.Clear();
		//_undoHistory.History = _undoStack.Names;
		_generatedCode = "";
		//_generatedCodeTextView.Value = "";
		//Selection.Set( _graph );
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
		_graphView.CutSelection();
	}

	private void CopySelection()
	{
		_graphView.CopySelection();
	}

	private void PasteSelection()
	{
		_graphView.Paste( EditorUtility.Clipboard.Paste() );
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
		SGPLogger.Info( $"TODO : GeneratePreviewCode" );

		return "";
	}

	private void SetDirty( bool evaluate = true )
	{
		_dirty = true;
		_blackboard.OnGraphDirty();

		UpdateTitle();

		if ( evaluate )
			GeneratePreviewCode();
	}

	private void RestoreShader()
	{
		if ( !_preview.IsValid() )
			return;

		// TODO
		//_preview.Material = Material.Load( "materials/core/shader_editor.vmat" );
	}

	private void UpdateTitle()
	{
		WindowTitle.Text = $"Shader Graph Plus - {_asset?.Path ?? "untitled"}{(_dirty ? "*" : "")}";
	}

	void RefreshRecentFiles()
	{
		_recentFilesMenu.Enabled = _recentFiles.Count > 0;

		_recentFilesMenu.Clear();

		_recentFilesMenu.AddOption( "Clear recent files", null, ClearRecentFiles );
		//.StatusTip = "Clear recent files";

		_recentFilesMenu.AddSeparator();

		const int maxFilesToDisplay = 10;
		int fileCount = 0;

		for ( int i = _recentFiles.Count - 1; i >= 0; i-- )
		{
			if ( fileCount >= maxFilesToDisplay )
				break;

			var filePath = _recentFiles[i];

			_recentFilesMenu.AddOption( $"{++fileCount} - {filePath}", null, () => PromptSave( () => Open( filePath ) ) );
			//.StatusTip = $"Open {filePath}";
		}
	}

	private void ClearRecentFiles()
	{
		if ( _recentFiles.Count == 0 )
			return;

		_recentFiles.Clear();

		RefreshRecentFiles();

		SaveRecentFiles();
	}

	private void AddToRecentFiles( string filePath )
	{
		filePath = filePath.ToLower();

		// If file is already recent, remove it so it'll become the most recent
		if ( _recentFiles.Contains( filePath ) )
		{
			_recentFiles.RemoveAll( x => x == filePath );
		}

		_recentFiles.Add( filePath );

		RefreshRecentFiles();
		SaveRecentFiles();
	}

	private void SaveRecentFiles()
	{
		Editor.FileSystem.Temporary.WriteJson( "shadergraphplus_recentfiles.json", _recentFiles );
	}


	/// <summary>
	/// Re-add the stylesheet so a hotload picks up its new text.
	/// </summary>
	public override void OnHotloaded()
	{
		StyleSheet.AddInline( ShaderGraphPlusStyleSheets.ShaderGraphPlusScss, "shadergraphplus_style" );
		base.OnHotloaded();
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
}
