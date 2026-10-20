using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ShaderGraphPlusPanelTest;

public sealed class TextureButton : Button
{
	public TextureButton() : base()
	{
	}

	public TextureButton( string text, Texture texture, Action action = null ) : base( text, action )
	{
		IconTexture = texture;
	}

	public TextureButton( string text, Texture texture, string className, Action action = null ) : base( text, null, className, action )
	{
		IconTexture = texture;
	}
}
