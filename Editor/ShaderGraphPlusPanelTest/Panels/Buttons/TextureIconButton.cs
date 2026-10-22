using Sandbox.UI;
using Sandbox.UI.Construct;
using static System.Net.Mime.MediaTypeNames;

namespace ShaderGraphPlusPanelTest;

public class TextureIconButton : Button
{
	public TextureIconButton() : base()
	{
	}

	public TextureIconButton( string text, Action action = null ) : base( text, action )
	{
		
	} 
	
	public TextureIconButton( string text, string className, Action action ) : base( text, null, className, action )
	{
	
	}

	public TextureIconButton( string text, Texture texture, Action action = null ) : this( text, action )
	{
		IconTexture = texture;
	}

	public TextureIconButton( string text, Texture texture, string className, Action action = null ) : this( text, className, action )
	{
		IconTexture = texture;
	}
}

public sealed class ToggleTextureIconButton : TextureIconButton
{
	public Texture ToggledTexture { get; set; }
	public Texture UnToggledTexture { get; set; }

	public bool Toggle
	{
		get;
		set
		{
			field = value;

			if ( ToggledTexture == null && UnToggledTexture == null )
				return;

			IconTexture = value ? ToggledTexture : UnToggledTexture;
		}
	}

	public ToggleTextureIconButton() : base()
	{
		Toggle = true;
	}

	public ToggleTextureIconButton( string text, Action action = null ) : base( text, action )
	{
		AddEventListener( "onclick", () => { Toggle = !Toggle; } );
	}

	public ToggleTextureIconButton( string text, string className, Action action ) : base( text, null, className, action )
	{
		AddEventListener( "onclick", () => { Toggle = !Toggle; } );
	}

	public ToggleTextureIconButton( string text, Texture toggledTexture, Texture unToggledTexture, Action action = null ) : this( text, action )
	{
		ToggledTexture = toggledTexture;
		UnToggledTexture = unToggledTexture;

		Toggle = true;
		IconTexture = toggledTexture;
	}

	public ToggleTextureIconButton( string text, Texture toggledTexture, Texture unToggledTexture, string className, Action action = null ) : this( text, className, action )
	{
		ToggledTexture = toggledTexture;
		UnToggledTexture = unToggledTexture;

		Toggle = true;
		IconTexture = toggledTexture;
	}
}
