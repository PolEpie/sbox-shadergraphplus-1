using Editor;
using Sandbox.UI;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class PreviewPanel : Panel
{
	public PreviewPanel() : base()
	{
		AddClass( "sg-preview" );
		Add.Panel( "ball" );
	}
}
