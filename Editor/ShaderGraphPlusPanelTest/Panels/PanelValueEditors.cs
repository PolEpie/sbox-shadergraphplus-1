using NodeEditorPlusTest;
using ShaderGraphPlus;
using EngineGraph = EngineNodeGraph;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// Panel version of <see cref="BasePlug.CreateEditor"/> and <see cref="PlugInfo.CreateEditor"/>.
/// </summary>
public static class PanelValueEditors
{
	/// <summary>
	/// The inline editor for <paramref name="plug"/>. A plug with no editor of its own gets a
	/// <see cref="DefaultEditorUI"/>.
	/// </summary>
	public static EngineGraph.NodeValueEditor Create( EngineGraph.PlugPanel plug )
	{
		if ( plug.Inner is not BasePlug bp )
			return null;

		return Create( plug, bp.Info, bp.Type ) ?? new DefaultEditorUI( plug );
	}

	static EngineGraph.NodeValueEditor Create( EngineGraph.PlugPanel plug, PlugInfo info, Type type )
	{
		var property = info.Property;
		var editor = property?.GetCustomAttribute<BaseNodePlus.NodeValueEditorAttribute>();

		if ( editor is null || plug.Node.Node is not BaseNodePlus node )
			return null;

		var so = node.GetSerialized();
		var title = info.DisplayInfo.Name;
		var valueProperty = so.GetProperty( editor.ValueName );

		if ( type == typeof( int ) )
		{
			var slider = new IntValueEditorUI( plug ) { Title = title };
			slider.ValueProperty = valueProperty;

			var range = property.GetCustomAttribute<BaseNodePlus.RangeAttribute>();
			if ( range != null )
			{
				slider.MinProperty = so.GetProperty( range.Min );
				slider.MaxProperty = so.GetProperty( range.Max );
			}
			else if ( property.GetCustomAttribute<MinMaxAttribute>() is MinMaxAttribute minMax )
			{
				slider.Min = (int)minMax.MinValue;
				slider.Max = (int)minMax.MaxValue;
			}

			return slider;
		}

		if ( type == typeof( float ) )
		{
			var range = property.GetCustomAttribute<BaseNodePlus.RangeAttribute>();
			if ( range != null )
			{
				return new RangedFloatValueEditor( plug, valueProperty )
				{
					Title = title,
					MinProperty = so.GetProperty( range.Min ),
					MaxProperty = so.GetProperty( range.Max ),
					StepProperty = so.GetProperty( range.Step )
				};
			}

			var slider = new EngineGraph.FloatValueEditor( plug, valueProperty ) { Title = title };

			if ( property.GetCustomAttribute<MinMaxAttribute>() is MinMaxAttribute minMax )
			{
				slider.Min = minMax.MinValue;
				slider.Max = minMax.MaxValue;
			}

			return slider;
		}

		if ( type == typeof( Color ) )
		{
			return new EngineGraph.ColorValueEditor( plug, valueProperty ) { Title = title };
		}

		if ( type == typeof( Gradient ) )
		{
			return new GradientValueEditorUI( plug ) { Title = title, ValueProperty = valueProperty };
		}

		return null;
	}

	/// <summary>
	/// The engine float editor with <see cref="EngineGraph.FloatValueEditor.Min"/>, <see cref="EngineGraph.FloatValueEditor.Max"/>
	/// and <see cref="EngineGraph.FloatValueEditor.Step"/> read every frame from the node's properties, as
	/// <see cref="BaseNodePlus.RangeAttribute"/> names them.
	/// </summary>
	sealed class RangedFloatValueEditor : EngineGraph.FloatValueEditor
	{
		public SerializedProperty MinProperty { get; init; }
		public SerializedProperty MaxProperty { get; init; }
		public SerializedProperty StepProperty { get; init; }

		public RangedFloatValueEditor( EngineGraph.PlugPanel plug, SerializedProperty property ) : base( plug, property )
		{
		}

		public override void Tick()
		{
			if ( MinProperty is not null ) Min = MinProperty.GetValue<float>();
			if ( MaxProperty is not null ) Max = MaxProperty.GetValue<float>();
			if ( StepProperty is not null ) Step = StepProperty.GetValue<float>();

			base.Tick();
		}
	}
}
