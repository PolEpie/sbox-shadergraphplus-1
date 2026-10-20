using Sandbox.UI;

namespace ShaderGraphPlusPanelTest;

public static class ToolbarExtensions
{
	public static TextureButton AddButton( this Toolbar toolbar, string text, Texture texture, Action action )
	{
		return toolbar.AddChild( new TextureButton( text, texture, action ) );
	}

	public static TextureButton AddButton( this Toolbar toolbar, string text, Texture texture, string className, Action action )
	{
		TextureButton button = toolbar.AddChild( new TextureButton( text, texture, className, action ) );
		return toolbar.AddChild( new TextureButton( text, texture, className, action ) );
	}
}
