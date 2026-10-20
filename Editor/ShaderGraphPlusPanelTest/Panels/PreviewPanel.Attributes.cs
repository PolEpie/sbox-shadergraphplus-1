using Editor;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using Label = Sandbox.UI.Label;

#pragma warning disable CS0618 // ScenePanel.Camera is obsolete, but it's the path that lets us add the gizmo world and drive the camera ourselves

namespace ShaderGraphPlusPanelTest;

public sealed partial class PreviewPanel
{
	private Dictionary<string, bool> _boolAttributes = new();
	private Dictionary<string, int> _intAttributes = new();
	private Dictionary<string, float> _floatAttributes = new();
	private Dictionary<string, Vector2> _float2Attributes = new();
	private Dictionary<string, Vector3> _float3Attributes = new();
	private Dictionary<string, Color> _float4Attributes = new();
	private Dictionary<string, Texture> _textureAttributes = new();
	private Dictionary<string, SamplerState> _samplerStateAttributes = new();
	private Dictionary<string, int> _dynamicComboIntAttributes = new();
	private Dictionary<string, int> _shaderFeatures = new();
	private Dictionary<string, Float2x2> _float2x2Attributes = new();
	private Dictionary<string, Float3x3> _float3x3Attributes = new();
	private Dictionary<string, Float4x4> _float4x4Attributes = new();

	public void SetAttribute( string id, in bool value )
	{
		_boolAttributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetAttribute( string id, in int value )
	{
		_intAttributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetAttribute( string id, in float value )
	{
		_floatAttributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetAttribute( string id, in Vector2 value )
	{
		_float2Attributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetAttribute( string id, in Vector3 value )
	{
		_float3Attributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetAttribute( string id, in Vector4 value )
	{
		SetAttribute( id, in value );
	}

	public void SetAttribute( string id, in Color value )
	{
		_float4Attributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetAttribute( string id, in SamplerState value )
	{
		_samplerStateAttributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetAttribute( string id, in Texture value )
	{
		_textureAttributes.Add( id, value );

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( id, value );
		}
		else
		{
			_modelRenderer.Attributes.Set( id, value );
		}
	}

	public void SetFeature( string id, int value )
	{
		_shaderFeatures.Add( id, value );
		throw new NotImplementedException( "TODO" );
	}

	public void SetDynamicCombo( string id, int value )
	{
		if ( !_dynamicComboIntAttributes.ContainsKey( id ) )
		{
			_dynamicComboIntAttributes.Add( id, value );

			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( id, value );
			}
			else
			{
				_modelRenderer.Attributes.Set( id, value );
			}
		}
	}

	public void SetStage( int value )
	{
		_stageId = value;

		if ( Material.Flags.IsSky )
		{
			_previewScene.RenderAttributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.StageID, EnableNodePreview ? _stageId : ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
		}
		else if ( _modelRenderer.IsValid() )
		{
			_modelRenderer.Attributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.StageID, EnableNodePreview ? _stageId : ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
		}
	}

	private void SetAllAttributes()
	{

		foreach ( var v in _boolAttributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		foreach ( var v in _intAttributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		foreach ( var v in _floatAttributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		foreach ( var v in _float2Attributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		foreach ( var v in _float3Attributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		foreach ( var v in _float4Attributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		foreach ( var v in _samplerStateAttributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		foreach ( var v in _textureAttributes )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( v.Key, v.Value );
			}
			else
			{
				_modelRenderer.Attributes.Set( v.Key, v.Value );
			}
		}

		if ( EnableNodePreview )
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.StageID, _stageId );
			}
			else
			{
				_modelRenderer.Attributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.StageID, _stageId );
			}
		}
		else
		{
			if ( Material.Flags.IsSky )
			{
				_previewScene.RenderAttributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.StageID, ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
			}
			else
			{
				_modelRenderer.Attributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.StageID, ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
			}
		}
	}

	public void ClearAllAttributes()
	{
		_samplerStateAttributes.Clear();
		_textureAttributes.Clear();
		_float2x2Attributes.Clear();
		_float3x3Attributes.Clear();
		_float4x4Attributes.Clear();
		_float4Attributes.Clear();
		_float3Attributes.Clear();
		_float2Attributes.Clear();
		_intAttributes.Clear();
		_floatAttributes.Clear();
		_boolAttributes.Clear();
		_dynamicComboIntAttributes.Clear();
		_shaderFeatures.Clear();

		if ( _modelRenderer is not null && _modelRenderer.IsValid() )
		{
			_modelRenderer.Attributes.Clear();
		}

		if ( _previewScene is not null && _previewScene.IsValid() )
		{
			_previewScene.RenderAttributes.Clear();
		}
	}

	/// <summary>
	/// Shader preview time runs on its own clock so it can stop and change speed. It's set on the
	/// model's renderer, which is where the Qt preview sets it. The engine's own g_flTime can't be
	/// steered from here: it is picked per frame from real time, and the view-level override
	/// (<see cref="Sandbox.Rendering.ViewSetup.Time"/>) isn't reachable through a ScenePanel.
	/// </summary>
	private void UpdateTime()
	{
		if ( !Paused )
			_previewTime += RealTime.Delta * TimeScale;

		_modelRenderer.Attributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.PreviewTime, _previewTime );
	}
}
