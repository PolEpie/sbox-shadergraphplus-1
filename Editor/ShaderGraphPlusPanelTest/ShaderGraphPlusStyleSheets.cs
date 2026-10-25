
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

		.sg-tip { flex-direction: column; gap: 6px; max-width: 320px; }
		.sg-tip > .head { flex-direction: row; align-items: center; gap: 8px; }
		.sg-tip > .head > .name { font-size: 13px; font-weight: 600; }
		.sg-tip > .description { color: #8a8f98; font-size: 11px; }
		.sg-tip > .preview { width: 96px; height: 96px; border-radius: 4px; background-size: cover; background-position: center; background-color: #ffffff0d; }
		.sg-tip > .detail { flex-direction: row; align-items: center; gap: 8px; font-size: 11px; }
		.sg-tip > .detail > .key { flex-shrink: 0; width: 52px; color: #8a8f98; }
		.sg-tip > .detail > .text, .sg-tip .value > .text { color: #c4c8cf; }
		.sg-tip .value { flex-direction: row; align-items: center; gap: 5px; }
		.sg-tip .value > .swatch { flex-shrink: 0; width: 12px; height: 12px; border-radius: 2px; border: 1px solid #00000080; }
		.sg-tip > .tags { flex-direction: row; flex-wrap: wrap; gap: 4px; }
		.sg-tip > .tags > .tag { padding: 0px 5px; border-radius: 3px; background-color: #31353e; color: #9aa0aa; font-size: 10px; }
		.sg-tip > .hint { color: #6e737c; font-size: 10px; }

		.sg-list { flex-direction: column; flex-grow: 1; padding: 4px 0px; }
		.sg-list .category { padding: 8px 10px 4px 10px; color: #8a8f98; font-size: 11px; font-weight: 600; text-transform: uppercase; }
		.sg-list .item { height: 26px; align-items: center; gap: 8px; padding: 0px 14px; }
		.sg-list .item > .icon { font-size: 15px; color: #8a8f98; }
	";
}
