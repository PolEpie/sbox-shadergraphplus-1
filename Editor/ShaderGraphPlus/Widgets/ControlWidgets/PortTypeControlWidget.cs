using Editor;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

[CustomEditor( typeof( string ), NamedEditor = ControlWidgetCustomEditors.PortTypeChoiceEditor )]
sealed class PortTypeControlWidget : DropdownControlWidget<string>
{
	public PortTypeControlWidget( SerializedProperty property ) : base( property )
	{
		var currentValue = SerializedProperty.GetValue<object>()?.ToString();
		SerializedProperty.SetValue<string>( currentValue ?? "float" );
	}

	/// <summary>
	/// The port types to choose from, shared with <see cref="PortTypeControl"/>.
	/// </summary>
	internal static readonly string[] PortTypes =
	[
		"bool",
		"int",
		"float",
		"Vector2",
		"Vector3",
		"Vector4",
		"float2x2",
		"float3x3",
		"float4x4",
		"Texture2D",
		"TextureCube",
		"SamplerState",
	];

	protected override IEnumerable<object> GetDropdownValues()
	{
		return PortTypes;
	}
}

