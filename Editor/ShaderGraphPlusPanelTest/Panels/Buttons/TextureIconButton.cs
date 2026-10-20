using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ShaderGraphPlusPanelTest;

public sealed class TextureIconButton : Button
{
	public TextureIconButton() : base()
	{
	}

	public TextureIconButton( string text, Texture texture, Action action = null ) : base( text, action )
	{
		IconTexture = texture;
	}

	public TextureIconButton( string text, Texture texture, string className, Action action = null ) : base( text, null, className, action )
	{
		IconTexture = texture;
	}
}
