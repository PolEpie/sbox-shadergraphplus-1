using Editor;
using Sandbox.UI;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// A window that is built from docking windows.
/// </summary>
public class PanelDockWindow : Panel
{
	[SkipHotload]
	protected static PanelWindow _window;

	protected Sandbox.UI.Label _maximizeIcon;

	protected string _defaultLayout;
	protected string _savedLayout;

	private readonly List<RootPanel> _floatingRoots = new();

	public Sandbox.UI.Label WindowTitle { get; set; }

	public DockHost DockHost { get; private set; }
	public PanelDockWindows DockWindows { get; private set; }

	public PanelDockWindow() : base()
	{
		AddClass( "editor-window" );

		StyleSheet.Load( ShaderGraphPlusStyleSheets.EditorScss );
		StyleSheet.Load( ShaderGraphPlusStyleSheets.GalleryScss );
	}

	protected virtual void CreateUI()
	{
		DockHost = AddChild( new DockHost() );
		DockWindows = new PanelDockWindows( DockHost ) { ConfigureWindow = ConfigureFloatingWindow };
	}

	protected void ShowDock( string dockId )
	{
		var host = DockHost.Find( dockId ) is not null ? DockHost : _floatingRoots
			.Where( x => x.IsValid )
			.SelectMany( x => x.Descendants.OfType<DockHost>() )
			.FirstOrDefault( x => x.Find( dockId ) is not null );
		if ( host is null ) return;
		if ( !host.IsOpen( dockId ) ) host.Dock( dockId );
		host.Activate( dockId );
		PanelWindow.FromPanel( host )?.Focus();
	}

	private void ConfigureFloatingWindow( RootPanel root )
	{
		root.AddClass( "mock-editor-surface" );
		root.AddClass( "editor-window" );
		root.StyleSheet.Load( ShaderGraphPlusStyleSheets.GalleryScss );
		_floatingRoots.Add( root );


		// A floated tool no longer bubbles keys through EditorWindow.
		var host = root.Children.OfType<DockHost>().Single();
		var shortcuts = root.AddChild( new FloatingShortcuts( this ) );
		host.Parent = shortcuts;
	}

	public void UpdateMaximizeIcon()
	{
		_maximizeIcon.Text = _window.IsMaximized ? "filter_none" : "crop_square";
	}

	public override void OnDeleted()
	{
		DockWindows?.Dispose();
		_floatingRoots.Clear();
		base.OnDeleted();
	}

	/// <summary>
	/// A row with a shortcut shown beside it. Rows with nothing to do are just there to look at.
	/// </summary>
	protected static Sandbox.UI.Menu Option( Sandbox.UI.Menu menu, string text, string icon = "", string shortcut = null, Action action = null )
	{
		var option = menu.AddOption( text, icon, action );
		option.Shortcut = shortcut;
		return option;
	}

	protected Sandbox.UI.Label WindowButton( Panel parent, string icon, string classname, Action onClick )
	{
		var button = parent.Clickable( "windowbutton window-nodrag", onClick );
		if ( classname is not null ) button.AddClass( classname );
		return button.Icon( icon );
	}
}

sealed class FloatingShortcuts : Panel
{
	readonly Panel _editorWindow;

	internal FloatingShortcuts( Panel editor )
	{
		_editorWindow = editor;
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
