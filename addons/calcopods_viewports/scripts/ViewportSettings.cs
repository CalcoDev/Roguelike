using Godot;

namespace CalcopodsViewports;

[GlobalClass]
public partial class ViewportSettings : Resource
{
    [Export] public Vector2I LowRes;
    [Export] public Vector2I HighRes;

    public Vector2I LowToHighScale => LowRes / HighRes;
    public Vector2I HighToLowScale => HighRes / LowRes;

    public Vector2 ConvertLowToHigh(Vector2 lowCoords)
    {
        return lowCoords * LowToHighScale;
    }

    public Vector2 ConvertHighToLow(Vector2 highCoords)
    {
        return highCoords * HighToLowScale;
    }
}