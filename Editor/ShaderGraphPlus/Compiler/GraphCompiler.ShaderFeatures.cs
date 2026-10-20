using System.Text;
using ShaderGraphPlus.Internal;

namespace ShaderGraphPlus;

public struct FeatureRule : IValid
{
	/// <summary>
	/// Features bound to this rule.
	/// </summary>
	[InlineEditor( Label = false )]
	public List<string> Features { get; set; }

	/// <summary>
	/// Text hint when hovering over features
	/// </summary>
	public string HoverHint { get; set; }

	[Hide, JsonIgnore]
	public bool IsValid
	{
		get
		{
			if ( Features.Any() )
			{
				foreach ( var feature in Features )
				{
					if ( string.IsNullOrWhiteSpace( feature ) )
					{
						return false;
					}
					else
					{
						continue;
					}
				}

				return true;
			}
			else
			{
				return false;
			}
		}
	}

	public FeatureRule()
	{
		Features = new();
		HoverHint = string.Empty;
	}
}

public sealed partial class GraphCompiler
{
	private partial class CompileResult
	{
		public List<string> ShaderFeatureResultStrings { get; set; } = new();
	}

	/// <summary>
	/// Registerd ShaderFeatures
	/// </summary>
	public Dictionary<string, ShaderFeatureBase> ShaderFeatures = new();

	struct SwitchBlockResultHolder
	{
		public string GeneratedLocals { get; private set; }
		public NodeResult Result { get; private set; }

		public SwitchBlockResultHolder( string locals, NodeResult nodeResult )
		{
			GeneratedLocals = locals;
			Result = nodeResult;
		}
	}

	/// <summary>
	/// Tell the preview which option of <paramref name="feature"/> to show. The preview declares features as
	/// specialization constants, so picking one is setting the material's feature, with no recompile.
	/// </summary>
	private void SetFeaturePreview( ShaderFeatureBase feature, int preview )
	{
		var featureName = feature.GetFeatureString();
		OnAttribute?.Invoke( featureName, new ShaderFeatureWrapper( featureName, preview ) );
	}

	/// <summary>
	/// Opens a feature switch branch. The preview's features are specialization constants, which are language
	/// constants, so it branches with <c>if</c>; the exported shader's static combos use the preprocessor.
	/// </summary>
	private string SwitchIf( string condition ) => IsPreview ? $"if ( {condition} )" : $"#if ( {condition} )";

	private string SwitchElseIf( string condition ) => IsPreview ? $"else if ( {condition} )" : $"#elif ( {condition} )";

	public NodeResult ResultFeatureSwitch( IEnumerable<NodeInput> inputs, ShaderFeatureBase shaderFeature, int previewInt )
	{
		var sb = new StringBuilder();
		var blockResults = new List<SwitchBlockResultHolder>();

		foreach ( var input in inputs )
		{
			if ( !input.IsValid )
			{
				blockResults.Add( new SwitchBlockResultHolder( $"float l_0 = 1.0f;", new NodeResult( ResultType.Float, "l_0" ) ) );
			}
			else
			{
				blockResults.Add( GenerateSwitchBlockCode( input, input.Identifier, shaderFeature ) );
			}
		}

		if ( !blockResults.Any() )
			return default;

		var featureNameClean = CleanName( shaderFeature.Name );
		var resultType = blockResults.Select( x => x.Result.ResultType ).Where( x => x != ResultType.Invalid && !((int)x > 6) ).Max();
		var id = 0;

		while ( ShaderResult.ShaderFeatureResultStrings.Contains( $"{featureNameClean}_result{id}" ) )
		{
			id++;
		}

		var resultAssignmentLocal = $"{featureNameClean}_result{id}";
		var resultDataType = resultType switch
		{
			ResultType.Bool => "bool",
			ResultType.Int => "int",
			ResultType.Float => "float",
			ResultType.Vector2 => "float2",
			ResultType.Vector3 => "float3",
			ResultType.Vector4 => "float4",
			ResultType.Float2x2 => "float2x2",
			ResultType.Float3x3 => "float3x3",
			ResultType.Float4x4 => "float4x4",
			ResultType.Gradient => "Gradient",
			ResultType.Sampler => "SamplerState",
			ResultType.Texture2D => "Texture2D",
			ResultType.TextureCube => "TextureCube",
			_ => throw new Exception( $"Unsupported ResultType \"{resultType}\"" ),
		};
		var resultTypeComponents = blockResults.Select( x => x.Result.Components ).Max();

		foreach ( var (index, result) in blockResults.Index() )
		{
			if ( !result.Result.IsValid )
			{
				continue;
			}

			var lastResult = new NodeResult( resultType, result.Result.Cast( resultTypeComponents ) );

			var combo = $"S_{featureNameClean.ToUpper()}";

			if ( index == 0 )
			{
				sb.AppendLine( InitializeVariable( resultType, resultAssignmentLocal ) ); //$"{resultDataType} {resultLocal};" );

				var value = shaderFeature is ShaderFeatureBoolean ? "SWITCH_TRUE" : $"{index}";
				sb.AppendLine( SwitchIf( $"{combo} == {value}" ) );

				BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
			}

			if ( shaderFeature is ShaderFeatureBoolean )
			{
				if ( index == blockResults.Count - 1 )
				{
					sb.AppendLine( IsPreview ? "else" : "#else" );
					BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
					if ( IsNotPreview ) sb.AppendLine( $"#endif" );
				}
			}
			else
			{
				if ( index != 0 && index != blockResults.Count - 1 )
				{
					sb.AppendLine( SwitchElseIf( $"{combo} == {index}" ) );
					BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
				}
				else if ( index == blockResults.Count - 1 )
				{
					sb.AppendLine( SwitchElseIf( $"{combo} == {index}" ) );
					BuildSwitchBlock( sb, result.GeneratedLocals, resultAssignmentLocal, lastResult );
					if ( IsNotPreview ) sb.AppendLine( $"#endif" );
				}
			}
		}

		//SGPLog.Info( $"Generated Switch S_{featureNameClean.ToUpper()}: \n {sb.ToString()}" );

		if ( IsPreview )
		{
			SetFeaturePreview( shaderFeature, previewInt );
		}

		var finalResult = new NodeResult( resultType, resultAssignmentLocal );
		finalResult.AddMetadataEntry( nameof( MetadataType.ComboSwitchBody ), sb.ToString() );

		if ( !ShaderResult.ShaderFeatureResultStrings.Contains( resultAssignmentLocal ) )
		{
			ShaderResult.ShaderFeatureResultStrings.Add( resultAssignmentLocal );
		}

		return finalResult;
	}

	private SwitchBlockResultHolder GenerateSwitchBlockCode( NodeInput input, string blockName, ShaderFeatureBase shaderFeature )
	{
		var outerResult = ShaderResult;
		var outerInputStack = InputStack;

		if ( IsVs )
		{
			VertexResult = new();
		}
		else
		{
			PixelResult = new();
		}

		// Preview constants are named from the attribute count; carry it on, or every branch's first
		// constant would be g_ps_0 and all branches would read the same value
		ShaderResult.Replace( new(), new( outerResult.Attributes ), new() );

		InputStack = new();

		var result = Result( input );
		var codeBlock = GenerateLocals( true );

		foreach ( var samplerState in ShaderResult.SamplerStates )
		{
			outerResult.SamplerStates[samplerState.Key] = samplerState.Value;
		}

		foreach ( var textureInput in ShaderResult.TextureInputs )
		{
			outerResult.TextureInputs[textureInput.Key] = textureInput.Value;
		}

		foreach ( var gradient in ShaderResult.Gradients )
		{
			outerResult.Gradients[gradient.Key] = gradient.Value;
		}

		foreach ( var parameter in ShaderResult.Parameters )
		{
			outerResult.Parameters[parameter.Key] = parameter.Value;
		}

		foreach ( var attribute in ShaderResult.Attributes )
		{
			outerResult.Attributes[attribute.Key] = attribute.Value;
		}

		foreach ( var function in ShaderResult.Functions )
		{
			outerResult.Functions.Add( function );
		}

		if ( IsVs )
		{
			VertexResult = outerResult;
		}
		else
		{
			PixelResult = outerResult;
		}
		InputStack = outerInputStack;

		//SGPLogger.Info( $"GeneratedBlock {blockName} : \n {{ {IndentString( codeBlock, 1)} \n }}" );

		return new( codeBlock, result );
	}

	private static StringBuilder BuildSwitchBlock( StringBuilder sb, string generatedLocals, string resultAssignmentLocal, NodeResult lastResult )
	{
		sb.AppendLine( $"{{" );
		sb.AppendLine( $"{IndentString( generatedLocals, 1 )}" );
		sb.AppendLine( $"{IndentString( $"{resultAssignmentLocal} = {lastResult}", 1 )};" );
		sb.AppendLine( $"}}" );

		return sb;
	}

	private static string BuildFeatureOptionsBody( List<ShaderFeatureEnumOption> options )
	{
		var options_body = "";
		int count = 0;

		foreach ( var option in options )
		{
			if ( count == 0 ) // first option starts at 0 :)
			{
				options_body += $"0=\"{option.Name}\", ";
				count++;
			}
			else if ( count != (options.Count - 1) )  // These options dont get the privilege of being the first >:)
			{
				options_body += $"{count}=\"{option.Name}\", ";
				count++;
			}
			else // Last option in the list oh well...:(
			{
				options_body += $"{count}=\"{option.Name}\"";
			}
		}

		return options_body;
	}
}
