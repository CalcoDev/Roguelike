using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using Godot;
using Godot.NativeInterop;

namespace Roguelike;

public partial class MapGenerator : Node
{
	public static class TileTypes
	{
		public const int Empty = 0;
		public const int Water = 1;
		public const int Grass = 2;
		public const int Sand = 3;
		public const int Stone = 4;
		public const int Marsh = 5;

		public const int MAX = 6;

		public static readonly float[] Weights = new float[MAX] {
			0f, 0f, 1f, 1.5f, 2f, 1f
		};

		public static readonly bool[] IsLiquid = new bool[MAX] {
			false, true, false, false, false, false
		};
	}
	
	[ExportGroup("Map Settings")]
	[Export] private int _mapWidth;
	[Export] private int _mapHeight;
	[Export] private float _minMapLandPercentage;

	[ExportGroup("Noise Settings")]
	[Export] private int _seed;
	[Export] private float _lacunarity;
	[Export] private int _octaves;
	[Export] private float _scale;
	
	[ExportGroup("Falloff Settings")]
	[Export] private bool _applyFalloff;
	[Export] private float _edgeFalloff;
	
	[ExportGroup("Heightmap Settings")]
	[Export] private float _sandHeight;
	[Export] private float _grassHeight;

	[ExportGroup("Cellular Automata Settings")]
	[Export] private bool _applyCellularAutomata;
	[Export] private int _minLiveNeighbourCount;

	[ExportGroup("References")]
	[Export] private TileMap _tilemap;

	// Stuff
	private FastNoiseLite _noise;

	private int[,] _map;
	private float _mapLandPercentage;	

	private float[,] _noiseMap;

	// .......
	public Vector2 SpawnPos { get; private set; }

    public override void _Process(double delta)
    {
		if (Input.IsActionJustPressed("gen_map"))
		{
			Generate();
			_seed = (int)GD.Randi();
		}
    }

    public void Generate()
	{
        _noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            FractalLacunarity = _lacunarity,
            FractalOctaves = _octaves,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            Seed = _seed
        };

		ComputeNoiseMap();

		if (_applyFalloff)
			ApplyFalloffMap();

		ComputeMap();
	
		if (_mapLandPercentage < _minMapLandPercentage)
		{
			// TODO(calco): Probably apply some smart stuff, for now regenerate with diff seed
			_seed = (int) GD.Randi();
			Generate();
			return;
		}
		
		if (_applyCellularAutomata)
			ApplyCellularAutomata();

		// Split in regions and determine if and where to place
		// marsh and stone

		// Determine spawn point

		// Place chests
		// Place props in each area

		// Place mobs

		int layer = 0;
		int tilesetSource = 0;
		for (int y = 0; y < _mapHeight; ++y)
		{
			for (int x = 0; x < _mapWidth; ++x)
			{
				Vector2I pos = new(x, y);
				Vector2I atlasCoords = GetTilePos(_map[x, y]); 
				_tilemap.SetCell(layer, pos, tilesetSource, atlasCoords);
			}
		}
	}
	
	private void ComputeNoiseMap()
	{
		_noiseMap = new float[_mapWidth, _mapHeight];
		for (int y = 0; y < _mapHeight; ++y)
		{
			for (int x = 0; x < _mapWidth; ++x)
			{
				float sampleX = x / _scale;
				float sampleY = y / _scale;
				
				float noise = _noise.GetNoise2D(sampleX, sampleY);
				_noiseMap[x, y] = (noise + 1f) / 2f;
			}
		}
	}

	private void ApplyFalloffMap()
	{
		for (int y = 0; y < _mapHeight; ++y)
		{
			for (int x = 0; x < _mapWidth; ++x)
			{
				float xDist = Mathf.Min(x, _mapWidth - x - 1);
				float yDist = Mathf.Min(y, _mapHeight - y - 1);

				float minDist = Mathf.Min(xDist, yDist);

				float edgeDist = 2f * minDist / (Mathf.Max(_mapWidth, _mapHeight) * 0.5f);
				float falloff = Mathf.Pow(1f - Mathf.Clamp(edgeDist, 0f, 1f), _edgeFalloff);

				_noiseMap[x, y] = Mathf.Clamp(_noiseMap[x,y] - falloff, 0f, 1f);
			}
		}
	}

	private void ComputeMap()
	{
		_mapLandPercentage = 0;
		float squarePercentage = 1f / (_mapWidth * _mapHeight);

		_map = new int[_mapWidth, _mapHeight];
		for (int y = 0; y < _mapHeight; ++y)
		{
			for (int x = 0; x < _mapWidth; ++x)
			{
				int tileType = TileTypes.Water;
				float v = _noiseMap[x, y];
				if (v < _sandHeight)
					tileType = TileTypes.Water;
				else if (v < _grassHeight)
					tileType = TileTypes.Sand;
				else
					tileType = TileTypes.Grass;

				_map[x, y] = tileType;
				if (!TileTypes.IsLiquid[tileType])
					_mapLandPercentage += squarePercentage;
			}
		}
	}

	private void ApplyCellularAutomata()
	{
		int[,] buffer = new int[_mapWidth, _mapHeight];
		for (int y = 0; y < _mapHeight; ++y)
		{
			for (int x = 0; x < _mapWidth; ++x)
			{
				int neighbourCount = 0;
				int[] cellCount = new int[TileTypes.MAX-1];
				for (int yoff = -1; yoff < 3; ++yoff)
				{
					for (int xoff = -1; xoff < 3; ++xoff)
					{
						if (xoff == yoff && xoff == 0)
							continue;
						
						if (x+xoff<0||x+xoff>=_mapWidth||y+yoff<0||y+yoff>=_mapHeight)
							continue;
						
						cellCount[_map[x + xoff, y + yoff]] += 1;
						if (_map[x+xoff, y+yoff] != TileTypes.Water)
							neighbourCount += 1;
					}
				}

				if (neighbourCount > _minLiveNeighbourCount)
				{
					int type;
					if (cellCount[TileTypes.Water] > 0)
					{
						type = cellCount
							.Select((cnt, idx) => (idx, cnt*TileTypes.Weights[idx]))
							.Aggregate((a, b) => a.Item2 > b.Item2 ? a : b).idx;
					}
					else
					{
						// type = _map[x, y];
						type = cellCount.Select((cnt, idx) => (cnt, idx)).Max().idx;
					}
					
					buffer[x, y] = type;
				}
			}
		}
		_map = buffer;
	}

	private static Vector2I GetTilePos(int tileType)
	{
		return tileType switch
		{
			TileTypes.Water => new Vector2I(0, 0),
			TileTypes.Grass => new Vector2I(1, 0),
			TileTypes.Sand => new Vector2I(1, 1),
			TileTypes.Stone => new Vector2I(0, 1),
			TileTypes.Marsh => new Vector2I(0, 2),
			_ => Vector2I.Zero,
		};
	}
}
