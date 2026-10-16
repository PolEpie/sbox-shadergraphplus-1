
namespace ShaderGraphPlusPanelTest;

public static class ShaderGraphPlusStyleSheets
{

	/// <summary>
	/// Found in : sbox\addons\editor\assets\styles
	/// </summary>
	public const string EditorScss = "/styles/editor.scss";

	/// <summary>
	/// Found in : sbox\addons\editor\assets\styles
	/// </summary>
	public const string GalleryScss = "/styles/gallery.scss";

	public const string ShaderGraphPlusScss = @"
		ShaderGraphPlusPanel > .main-toolbar { flex-shrink: 0; height: 40px; padding: 0px 8px; border-bottom: 1px solid #26282e; }
		ShaderGraphPlusPanel .status { color: #6e737c; font-size: 11px; align-self: center; }
		
		.console > .body { flex-direction: row; flex-grow: 1; min-height: 0; }
		.console > .body > .list { min-width: 0; }
		.console > .body > .detail-splitter { width: 5px; flex-shrink: 0; cursor: ew-resize; background-color: #1a1c21; }
		.console > .body > .detail-splitter:hover { background-color: #4b8bf5; }
		.console > .body > .detail-splitter.dragging { background-color: #4b8bf5; }
		.console > .body > .detail { flex-shrink: 0; height: 100%; padding: 0px; overflow: hidden; border-top-width: 0px; }
		.console .detail-header { flex-direction: row; align-items: center; flex-shrink: 0; height: 26px; padding: 0px 4px 0px 10px; border-bottom: 1px solid #26282e; }
		.console .detail-title { flex-grow: 1; color: #8a8f98; font-size: 11px; white-space: nowrap; overflow: hidden; }
		.console .detail-body { flex-direction: column; flex-grow: 1; min-height: 0; overflow: scroll; padding: 7px 10px; }
		
		.sg-list { flex-direction: column; flex-grow: 1; padding: 4px 0px; }
		.sg-list .category { padding: 8px 10px 4px 10px; color: #8a8f98; font-size: 11px; font-weight: 600; text-transform: uppercase; }
		.sg-list .item { height: 26px; align-items: center; gap: 8px; padding: 0px 14px; }
		.sg-list .item > .icon { font-size: 15px; color: #8a8f98; }
		
		.sg-preview { flex-grow: 1; align-items: center; justify-content: center; background-color: #121317; }
		.sg-preview > .ball { width: 160px; height: 160px; border-radius: 80px; background-color: #d9773a; }
		
		.sg-props { flex-direction: column; flex-grow: 1; }
		.sg-props > .heading { flex-direction: column; padding: 10px 12px; border-bottom: 1px solid #26282e; }
		.sg-props .title { font-size: 13px; font-weight: 600; }
		.sg-props .subtitle { color: #8a8f98; font-size: 11px; }
		.sg-props .pin { height: 26px; align-items: center; gap: 8px; padding: 0px 12px; }
	";
}
