using Editor;

namespace ShaderGraphPlusPanelTest;

internal static class ShaderGraphPlusPanelTestEditorMenus
{
	[Menu( "Editor", "Shader Graph Plus/Panel Test" )]
	public static void OpenPanelTest()
	{
		Log.Info( "OpenPanelTest() called!" );

		//MainPanelWindow.OpenTestPanelWindow();
	}
}
