
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

		.sg-graph { background-color: #1b1d22; }

		.sg-node { position: absolute; flex-direction: column; min-width: 160px; min-height: 48px; background-color: #25282f; border: 1px solid #3a3e47; border-radius: 6px; }
		.sg-node > .header { flex-direction: row; align-items: center; gap: 6px; height: 28px; padding: 0px 10px; background-color: #323743; border-radius: 6px 6px 0px 0px; cursor: move; }
		.sg-node > .header > * { pointer-events: none; }
		.sg-node > .header > .icon { font-size: 15px; color: #8a8f98; }
		.sg-node > .header > .title { font-size: 12px; font-weight: 600; white-space: nowrap; }
		.sg-node.preview { opacity: 0.45; border-color: #4b8bf5; }

		.console > .body { flex-direction: row; flex-grow: 1; min-height: 0; }
		.console > .body > .list { min-width: 0; }
		.console > .body > .detail-splitter { width: 5px; flex-shrink: 0; cursor: ew-resize; background-color: #1a1c21; }
		.console > .body > .detail-splitter:hover { background-color: #4b8bf5; }
		.console > .body > .detail-splitter.dragging { background-color: #4b8bf5; }
		.console > .body > .detail { flex-shrink: 0; height: 100%; padding: 0px; overflow: hidden; border-top-width: 0px; }
		.console .detail-header { flex-direction: row; align-items: center; flex-shrink: 0; height: 26px; padding: 0px 4px 0px 10px; border-bottom: 1px solid #26282e; }
		.console .detail-title { flex-grow: 1; color: #8a8f98; font-size: 11px; white-space: nowrap; overflow: hidden; }
		.console .detail-body { flex-direction: column; flex-grow: 1; min-height: 0; overflow: scroll; padding: 7px 10px; }

		.palette-tree { flex-grow: 1; flex-shrink: 1; min-height: 0; }

		.sg-blackboard { flex-direction: column; }
		.sg-params { flex-direction: column; flex-grow: 1; flex-shrink: 1; min-height: 0; overflow: scroll; padding: 4px; gap: 2px; }
		.sg-params > .empty { padding: 12px; color: #6e737c; font-size: 11px; }
		.sg-param { flex-direction: row; align-items: center; flex-shrink: 0; gap: 8px; min-height: 28px; padding: 0px 8px; border-left-width: 3px; border-radius: 4px; background-color: #23262c; }
		.sg-param:hover { background-color: #2a2e36; }
		.sg-param > .type { flex-shrink: 0; padding: 1px 6px; border-width: 1px; border-radius: 3px; font-size: 11px; font-weight: 600; white-space: nowrap; }
		.sg-param > .name { flex-grow: 1; flex-shrink: 1; min-width: 0; font-size: 12px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
		.sg-param > .tags { flex-direction: row; flex-shrink: 0; gap: 4px; }
		.sg-param > .tags > .tag { padding: 0px 5px; border-radius: 3px; background-color: #31353e; color: #9aa0aa; font-size: 10px; white-space: nowrap; }
		.sg-param > .value { flex-direction: row; align-items: center; flex-shrink: 1; min-width: 0; gap: 5px; }
		.sg-param > .value > .text { color: #c4c8cf; font-size: 11px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
		.sg-param > .value > .swatch { flex-shrink: 0; width: 12px; height: 12px; border-radius: 2px; border: 1px solid #00000080; }
		.sg-param > .value > .icon { font-size: 13px; color: #8a8f98; }

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
