using CalcopodsViewports;
using Godot;

namespace Roguelike.Managers;

public partial class GameplayWrapper : Node2D
{
	private SubViewport _viewport;
	// private ViewportSettings _settings;

 public override void _EnterTree()
    {
		_viewport = GetNode<SubViewport>("%SubViewport");

		Node currentScene = GetTree().CurrentScene;
		if (currentScene.IsInGroup("fullres_scenes"))
			return;

		currentScene.QueueFree();

		string scenePath = currentScene.SceneFilePath;
		currentScene = ResourceLoader.Load<PackedScene>(scenePath).Instantiate();
		_viewport.AddChild(currentScene);
    }

	public void SwitchScene(PackedScene scene)
	{
		_viewport.GetChild(0).QueueFree();
		_viewport.AddChild(scene.Instantiate());
	}

	public void InitSettings(ViewportSettings settings)
	{
		// _settings = settings;
		// _viewport.Size = _settings.LowRes;
	}
}
