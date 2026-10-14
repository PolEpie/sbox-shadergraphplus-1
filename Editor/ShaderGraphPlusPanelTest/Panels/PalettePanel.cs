using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ShaderGraphPlusPanelTest;

public sealed class PalettePanel : Panel
{
	public PalettePanel() : base()
	{
		AddClass( "sg-list" );

		void Category( string name, params (string Icon, string Title)[] items )
		{
			Add.Label( name, "category" );
			foreach ( var (icon, title) in items )
			{
				var item = Add.Panel( "item" );
				item.Add.Icon( icon, "icon" );
				item.Add.Label( title );
			}
		}

		Category( "Parameters", ("looks_one", "Float"), ("palette", "Color") );
		Category( "Math", ("close", "Multiply"), ("add", "Add"), ("tune", "Lerp") );
		Category( "Textures", ("image", "Texture Sample"), ("grid_on", "Texture Coord") );
	}
}
