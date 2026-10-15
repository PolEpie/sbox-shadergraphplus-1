using Editor;
using Sandbox.UI;

namespace ShaderGraphPlusPanelTest;

public abstract class PanelEditorWindow : Panel
{
	protected static PanelWindow _window;

	protected Sandbox.UI.Label _maximizeIcon;
	
	public  Sandbox.UI.Label WindowTitle { get; set; }

	public PanelEditorWindow()
	{
	}
}
