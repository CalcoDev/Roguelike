#if TOOLS
using Godot;

namespace CalcopodsViewports;

[Tool]
public partial class CalcopodsViewports : EditorPlugin
{
	// [ExportGroup("References")]
	// [Export] private ResourcePreloader _preloader;

	private Control _viewportEditor;

	public override void _EnterTree()
	{
		// Viewport Editor Bottom Panel
		PackedScene viewportEditorScene = LoadResource<PackedScene>("scenes/ViewportEditor.tscn");
		_viewportEditor = viewportEditorScene.Instantiate<Control>();
		AddControlToBottomPanel(_viewportEditor, "Viewport Editor");

		// ViewportSettings resource
		Script viewportSettingsScript = LoadResource<Script>("scripts/ViewportSettings.cs");
		Texture2D viewportSettingsIcon = LoadResource<Texture2D>("assets/icons/viewport_settings.png");
		AddCustomType("ViewportSettings", "Resource", viewportSettingsScript, viewportSettingsIcon);
	}

	public override void _ExitTree()
	{
		// viewport Editor Bottom Panel
		RemoveControlFromBottomPanel(_viewportEditor);

		// ViewportSettings resource
		RemoveCustomType("ViewportSettings");
	}

	public static T LoadResource<T>(string relPath) where T : class
	{
		T res = ResourceLoader.Load<T>($"res://addons/calcopods_viewports/{relPath}");
		return res;
	}
}
#endif
