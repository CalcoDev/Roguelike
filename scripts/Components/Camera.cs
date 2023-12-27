using Godot;
using Roguelike.Managers;

namespace Roguelike.Components;

public partial class Camera : Camera2D
{
    public override void _EnterTree()
    {
        Game.Instance.Camera = this;
    }
}