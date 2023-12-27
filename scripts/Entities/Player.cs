using Godot;
using Roguelike.Managers;

namespace Roguelike.Entites;

public partial class Player : CharacterBody2D
{

	[ExportGroup("Movement Settings")]
	[Export] public const float Speed = 5.0f;

    public override void _Process(double delta)
    {
		float zoomIn = Input.GetActionStrength("zoom_in");
		float zoomOut = Input.GetActionStrength("zoom_out");

		if (Input.IsActionJustPressed("zoom_in"))
			Game.Instance.Camera.Zoom += Vector2.One * 0.15f;
		if (Input.IsActionJustPressed("zoom_out"))
			Game.Instance.Camera.Zoom -= Vector2.One * 0.15f;
    }

    public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
			"move_down"
		);

		MoveAndCollide(direction.Normalized() * Speed);
	}
}
