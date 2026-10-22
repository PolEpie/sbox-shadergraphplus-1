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
		return toolbar.AddChild( new TextureIconButton( text, texture, className, action ) );
	}

	public static ToggleTextureIconButton AddButton( this Toolbar toolbar, string text, Texture toggledTexture, Texture unToggledTexture, Action action )
	{
		return toolbar.AddChild( new ToggleTextureIconButton( text, toggledTexture, unToggledTexture, action ) );
	}

	public static ToggleTextureIconButton AddButton( this Toolbar toolbar, string text, Texture toggledTexture, Texture unToggledTexture, string className, Action action )
	{
		return toolbar.AddChild( new ToggleTextureIconButton( text, toggledTexture, unToggledTexture, className, action ) );
	}
}
