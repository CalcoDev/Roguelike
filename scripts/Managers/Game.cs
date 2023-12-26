
using CalcopodsViewports;
using Godot;

namespace Roguelike.Managers;

public partial class Game : Node 
{
    [ExportGroup("Resources")]
    [Export] private PackedScene _gameplayWrapperScene;

    [ExportGroup("Viewports")]
    [Export] private ViewportSettings _viewportSettings;

    // Things
    private GameplayWrapper _gameplayWrapper;

    // Lifecycle
    public override void _EnterTree()
    {
        _gameplayWrapper = _gameplayWrapperScene.Instantiate<GameplayWrapper>();
        AddChild(_gameplayWrapper);
    }

    public override void _Ready()
	{
	}

    // Public Interface
    public ViewportSettings ViewportSettings => _viewportSettings;
    
    // Public Methods
    public void SwitchScene(PackedScene scene)
    {
        _gameplayWrapper.SwitchScene(scene);
    }

    public void SwitchSceneFile(string filepath)
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(filepath);
        SwitchScene(scene);
    }
}
