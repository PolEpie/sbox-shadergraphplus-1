using Sandbox.UI;

namespace ShaderGraphPlusPanelTest;

public sealed class OutputPanel : Panel
{
	public OutputPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
	}
}
