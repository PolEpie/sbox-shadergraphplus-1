using Sandbox.UI;

namespace ShaderGraphPlusPanelTest;

public static class ToolbarExtensions
{
	public static TextureIconButton AddButton( this Toolbar toolbar, string text, Texture texture, Action action )
	{
		return toolbar.AddChild( new TextureIconButton( text, texture, action ) );
	}

	public static TextureIconButton AddButton( this Toolbar toolbar, string text, Texture texture, string className, Action action )
	{
		TextureIconButton button = toolbar.AddChild( new TextureIconButton( text, texture, className, action ) );
		return toolbar.AddChild( new TextureIconButton( text, texture, className, action ) );
	}
}
