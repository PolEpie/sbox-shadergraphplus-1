using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using System.Globalization;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The graph's parameters as a list, one row each: type in its type color, name, default value,
/// and tags for the settings worth seeing at a glance (sRGB, attribute, range...).
/// </summary>
public sealed class BlackboardPanel : Panel
{
	private readonly bool _isSubgraph;
	private readonly Dictionary<string, IBlackboardParameterType> _parameterTypes = new();

	private TextInput _searchFilter;
	private string _filter = "";
	private readonly Panel _list;

	private ShaderGraphPlus.ShaderGraphPlus _graph;

	/// <summary>
	/// The graph whose parameters are listed. Setting it rebuilds the list.
	/// </summary>
	public ShaderGraphPlus.ShaderGraphPlus Graph
	{
		get => _graph;
		set
		{
			_graph = value;
			Rebuild();
		}
	}

	public BlackboardPanel( bool isSubgraph )
	{
		_isSubgraph = isSubgraph;

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-blackboard" );

		foreach ( var type in EditorTypeLibrary.GetTypes<BlackboardParameter>().Where( x => !x.IsAbstract ) )
		{
			var parameterType = ClassBlackboardParameterType.HookupParameterType( type );
			_parameterTypes.TryAdd( parameterType.Identifier, parameterType );
		}

		BuildToolbar();

		_list = Add.Panel( "sg-params" );

		Rebuild();
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter..", "search" ) );
		_searchFilter.OnChange = value =>
		{
			_filter = value?.Trim() ?? "";
			Rebuild();
		};

		var addMenu = new Sandbox.UI.Menu();
		addMenu.AboutToShow += BuildAddMenu;
		toolbar.AddMenu( "", "add", addMenu ).Tooltip = "Add parameter";
	}

	private void BuildAddMenu( Sandbox.UI.Menu menu )
	{
		menu.Clear();

		if ( _graph is null )
			return;

		var types = BlackboardParameter.GetRelevantParameters( _parameterTypes, _isSubgraph )
			.OfType<ClassBlackboardParameterType>()
			.OrderBy( x => x.Type.Order )
			.ToArray();

		if ( !_isSubgraph )
		{
			var parameters = menu.AddMenu( "Parameter", "edit_attributes" );
			var attributes = menu.AddMenu( "Attribute", "edit_attributes" );
			var combos = menu.AddMenu( "Combo", "alt_route" );

			foreach ( var type in types )
			{
				var target = type.Type.TargetType;
				var submenu = target.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) ? combos
					: target == typeof( SamplerStateParameter ) ? attributes
					: parameters;

				AddTypeOption( submenu, type );
			}
		}
		else
		{
			var inputs = menu.AddMenu( "Input", "input" );
			var outputs = menu.AddMenu( "Output", "output" );

			foreach ( var type in types )
			{
				AddTypeOption( type.Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) ? outputs : inputs, type );
			}
		}
	}

	private void AddTypeOption( Sandbox.UI.Menu menu, ClassBlackboardParameterType type )
	{
		var option = menu.AddOption( type.Type.Title, type.DisplayInfo.Icon, () => CreateParameter( type ) );
		option.Tooltip = type.DisplayInfo.Description;
	}

	private void CreateParameter( IBlackboardParameterType type )
	{
		var parameter = (BlackboardParameter)type.CreateParameter( _graph );

		// New parameters land in the default group, the way the Qt blackboard files them
		var groupName = parameter.GetGroupTitle();
		if ( _graph.TryFindGroupData( groupName, out var group ) )
		{
			group.ParameterReferences.Add( parameter.Identifier );
		}
		else
		{
			_graph.AddGroupData( new GroupData( [parameter.Identifier] ) { Name = groupName }, 0 );
		}

		_graph.AddParameter( parameter );

		_searchFilter.SetValue( "" );
		_filter = "";
		Rebuild();
	}

	/// <summary>
	/// Recreate the rows from the graph, applying the filter.
	/// </summary>
	public void Rebuild()
	{
		_list.DeleteChildren( true );

		var parameters = _graph?.Parameters.Where( MatchesFilter ).ToArray() ?? [];

		if ( parameters.Length == 0 )
		{
			_list.Add.Label( _graph is null ? "No graph open" : !string.IsNullOrEmpty( _filter ) ? "No matching parameters" : "No parameters yet - add one with +", "empty" );
			return;
		}

		foreach ( var parameter in parameters )
		{
			_list.AddChild( new ParameterRow( parameter ) );
		}
	}

	private bool MatchesFilter( BlackboardParameter parameter )
	{
		if ( string.IsNullOrEmpty( _filter ) )
			return true;

		return parameter.Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| ParameterRow.TypeConfig( parameter ).Name.Contains( _filter, StringComparison.OrdinalIgnoreCase );
	}
}

/// <summary>
/// One parameter: a strip and type label in the type's color, the name, its tags, and the default value.
/// </summary>
internal sealed class ParameterRow : Panel
{
	public ParameterRow( BlackboardParameter parameter )
	{
		AddClass( "sg-param" );

		var config = TypeConfig( parameter );

		Style.BorderLeftColor = config.Color;

		var type = Add.Label( config.Name, "type" );
		type.Style.FontColor = config.Color;
		type.Style.BackgroundColor = config.Color.WithAlpha( 0.12f );
		type.Style.BorderColor = config.Color.WithAlpha( 0.45f );

		Add.Label( parameter.Name, "name" );

		var tags = Add.Panel( "tags" );
		foreach ( var tag in Tags( parameter ) )
		{
			tags.Add.Label( tag, "tag" );
		}

		AddValue( parameter );

		Tooltip = $"{parameter.DisplayInfo.Name} parameter";
	}

	/// <summary>
	/// The label and color the Qt blackboard uses for this parameter's type.
	/// </summary>
	internal static BlackboardConfig TypeConfig( BlackboardParameter parameter )
	{
		return ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( parameter.GetType(), out var config )
			? config
			: new BlackboardConfig( parameter.DisplayInfo.Name, ShaderGraphPlusTheme.DefaultTypeColor );
	}

	private void AddValue( BlackboardParameter parameter )
	{
		var value = Add.Panel( "value" );

		switch ( parameter )
		{
			case ColorParameter color:
				AddSwatch( value, color.Value );
				break;

			case ColorSubgraphInputParameter color:
				AddSwatch( value, color.Value );
				break;

			case BlackboardTextureMaterialParameter texture:
				AddTexture( value, texture.Value );
				break;

			case IBlackboardSubgraphInputParameter when parameter.GetValue() is TextureInput texture:
				AddTexture( value, texture );
				break;

			case ShaderFeatureBooleanParameter feature:
				value.Add.Label( feature.Preview ? "On" : "Off", "text" );
				break;

			case ShaderFeatureEnumParameter feature:
				value.Add.Label( feature.Options.ElementAtOrDefault( feature.PreviewIndex )?.Name ?? "", "text" );
				break;

			case SamplerStateParameter sampler:
				value.Add.Label( sampler.Value.Filter.ToString(), "text" );
				break;

			case IBlackboardSubgraphInputParameter when parameter.GetValue() is Sampler sampler:
				value.Add.Label( sampler.Filter.ToString(), "text" );
				break;

			default:
				if ( parameter is not IBlackboardSubgraphOutputParameter && Format( parameter.GetValue() ) is { } text )
					value.Add.Label( text, "text" );
				break;
		}
	}

	private static void AddSwatch( Panel value, Color color )
	{
		var swatch = value.Add.Panel( "swatch" );
		swatch.Style.BackgroundColor = color;
		value.Add.Label( color.Hex, "text" );
	}

	private static void AddTexture( Panel value, TextureInput texture )
	{
		if ( !string.IsNullOrWhiteSpace( texture.DefaultTexture ) )
		{
			value.Add.Icon( "image", "icon" );
			value.Add.Label( System.IO.Path.GetFileName( texture.DefaultTexture ), "text" );
			return;
		}

		AddSwatch( value, texture.DefaultColor );
	}

	/// <summary>
	/// Short labels for the settings that change how the parameter compiles or shows up.
	/// </summary>
	private static IEnumerable<string> Tags( BlackboardParameter parameter )
	{
		if ( parameter is IBlackboardMaterialParameter { IsAttribute: true } )
			yield return "Attribute";

		if ( parameter is IRangedBlackboardMaterialParameter ranged && ranged.GetRangeMin() is int or float )
			yield return $"{Format( ranged.GetRangeMin() )} – {Format( ranged.GetRangeMax() )}";

		if ( parameter is IBlackboardMaterialParameter { UI: FloatParameterUI { Type: UIType.Slider } } )
			yield return "Slider";

		var texture = parameter switch
		{
			BlackboardTextureMaterialParameter t => t.Value,
			IBlackboardSubgraphInputParameter when parameter.GetValue() is TextureInput t => t,
			_ => (TextureInput?)null,
		};

		if ( texture is { } tex )
		{
			if ( tex.SrgbRead ) yield return "sRGB";
			if ( tex.ColorSpace == TextureColorSpace.Linear ) yield return "Linear";
			yield return tex.ImageFormat.ToString();
			yield return $"_{tex.ExtensionString.ToLowerInvariant()}";
			if ( tex.Processor != TextureProcessor.None ) yield return tex.Processor.ToString();
			if ( tex.IsAttribute ) yield return "Attribute";
		}

		var sampler = parameter switch
		{
			SamplerStateParameter s => s.Value,
			IBlackboardSubgraphInputParameter when parameter.GetValue() is Sampler s => s,
			_ => (Sampler?)null,
		};

		if ( sampler is { } smp )
		{
			yield return smp.AddressModeU == smp.AddressModeV && smp.AddressModeV == smp.AddressModeW
				? smp.AddressModeU.ToString()
				: $"{smp.AddressModeU}/{smp.AddressModeV}/{smp.AddressModeW}";
			if ( smp.Filter == FilterMode.Anisotropic ) yield return $"x{smp.MaxAnisotropy}";
			if ( smp.IsAttribute ) yield return "Attribute";
		}

		if ( parameter is IBlackboardSubgraphInputParameter { IsRequired: true } )
			yield return "Required";

		if ( parameter is IBlackboardSubgraphOutputParameter { Preview: not SubgraphOutputPreviewType.None } output )
			yield return $"Preview {output.Preview}";

		if ( parameter is ShaderFeatureEnumParameter feature )
			yield return $"{feature.Options.Count} options";
	}

	private static string Format( object value ) => value switch
	{
		bool b => b ? "true" : "false",
		int i => i.ToString( CultureInfo.InvariantCulture ),
		float f => Number( f ),
		Vector2 v => $"{Number( v.x )}, {Number( v.y )}",
		Vector3 v => $"{Number( v.x )}, {Number( v.y )}, {Number( v.z )}",
		Vector4 v => $"{Number( v.x )}, {Number( v.y )}, {Number( v.z )}, {Number( v.w )}",
		_ => null,
	};

	private static string Number( float value ) => value.ToString( "0.###", CultureInfo.InvariantCulture );
}
