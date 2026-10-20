namespace ShaderGraphPlus;

public static class ShaderGraphPlusGlobals
{
	internal static class GraphCompiler
	{
		internal const int NoNodePreviewID = 0;
	}

	internal static class Preview
	{
		internal static Material DefaultMaterial => Material.Load( "materials/core/shader_editor.vmat" );
	}

	public static class PreviewShaderAttributes
	{
		/// <summary>
		/// Shader time, as the preview shaders read it. The compiler's Time node reads this attribute
		/// instead of g_flTime when compiling for the preview.
		/// </summary>
		internal const string PreviewTime = "g_flPreviewTime";

		internal const string StageID = "g_iStageId";
	}

	internal static class BlackboardGlobals
	{
		internal const string EmptyGroupName = "General";
	}

	internal static class ControlWidgetCustomEditors
	{
		internal const string UIGroupEditor = "sgp.UiGroupEditor";
		internal const string ShaderFeatureEnumPreviewIndexEditor = "sgp.ShaderFeatureEnumPreviewIndexEditor";
		internal const string NamedRerouteReferenceEditor = "sgp.NamedRerouteReferenceEditor";
		internal const string PortTypeChoiceEditor = "sgp.PortTypeChoiceEditor";
		internal const string ShaderTypeDropdownEditor = "sgp.ShaderTypeDropdownEditor";
	}

	internal static class EditorEvents
	{
		internal const string ShaderGraphPlusEditorCreated = "sgp.EditorCreatedEvent";
		internal const string SubgraphUpdate = "sgp.UpdateSubgraphEvent";
		internal const string ShaderTemplateUpdate = "sgp.ShaderTemplateUpdateEvent";
	}

	internal static class EditorCookieNames
	{
		internal const string ShaderGraphPlusBlackboardCollapsedGroupsCookie = "sgp.Blackboard.ParameterGroups.Collapsed";
		internal const string ShaderGraphPlusGridWires = "sgp.GraphView.Gridwires";
	}

	internal const string CleanName = "ShaderGraphPlus";

	internal const string AssetTypeName = "Shader Graph Plus";
	internal const string AssetTypeExtension = "sgrph";

	internal const string ShaderGraphEditorStateCookieName = "ShaderGraphPlus";

	internal const string SubgraphAssetTypeName = "Shader Graph Plus Function";
	internal const string SubgraphAssetTypeExtension = "sgpfunc";

	internal const string ShaderTemplateCleanName = "ShaderTemplate";
	internal const string ShaderTemplateAssetTypeName = "Shader Template";
	internal const string ShaderTemplateAssetTypeExtension = "shdrtpl";

	internal const string ShaderTemplateEditorStateCookieName = "ShaderTemplateEditor";
	internal const string ShaderTemplateEditorToolbarName = "ShaderTemplateEditorToolbar";
}
