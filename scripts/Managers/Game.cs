
using CalcopodsViewports;
using Godot;
using Roguelike.Components;

namespace Roguelike.Managers;

public partial class Game : Node 
{
    public static Game Instance { get; private set; }
    public Camera Camera { get; set; }

    [ExportGroup("Resources")]
    [Export] private PackedScene _gameplayWrapperScene;

    // [ExportGroup("Viewports")]
    // [Export] private ViewportSettings _viewportSettings;

    // Things
    private GameplayWrapper _gameplayWrapper;


    // Lifecycle
    public override void _EnterTree()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            GD.PrintErr("ERROR: Game manager already exists!");
            QueueFree();
        }
        
        // _gameplayWrapper = _gameplayWrapperScene.Instantiate<GameplayWrapper>();
        // _gameplayWrapper.InitSettings(_viewportSettings);
        // AddChild(_gameplayWrapper);
    }

    public override void _Ready()
	{
	}

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("quit"))
            GetTree().Quit();
    }

    // Public Interface
    // public ViewportSettings ViewportSettings => _viewportSettings;
    
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
