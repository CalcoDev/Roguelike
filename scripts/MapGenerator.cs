using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Schema;
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

	[ExportGroup("Random Walker Settings")]
	[Export] private int _walkerCount;
	[Export] private int _walkerSteps;
	[Export] private float _walkerDirChanceChance;

	[ExportGroup("Noise Settings")]
	[Export] private int _seed;
	[Export] private float _lacunarity;
	[Export] private int _octaves;
	[Export] private float _scale;
	
	[ExportGroup("Falloff Settings")]
	[Export] private bool _applyFalloff;
	[Export] private float _edgeFalloff;
	[Export] private bool _circularFalloff;
	[Export] private float _circularFalloffRadius;
	
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
		GenerateBasePerlin();
		// GenerateBaseRandomWalk();
		// GenerateBaseDLA();

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

	private void GenerateBasePerlin()
	{
        _noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalLacunarity = _lacunarity,
            FractalOctaves = _octaves,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            Seed = _seed
        };
		
		ComputeNoiseMap();
		if (_applyFalloff)
			ApplyFalloffMap();
		ComputeMap();
		// TODO(calco): Probably apply some smart stuff, for now regenerate with diff seed
		// if (_mapLandPercentage < _minMapLandPercentage)
		// {
		// 	_seed = (int) GD.Randi();
		// 	Generate();
		// 	return;
		// }

		if (_applyCellularAutomata)
			ApplyCellularAutomata();
	}

	private void GenerateBaseRandomWalk()
	{
		_map = new int[_mapWidth, _mapHeight];
		for (int y = 0; y < _mapHeight; ++y)
		{
			for (int x = 0; x < _mapWidth; ++x)
				_map[x, y] = TileTypes.Water;
		}

		// TODO(calco): Some land percentage thing.
		// Start
		List<Vector2I> positions = new();

		int walkerX = _mapWidth / 2;
		int walkerY = _mapHeight / 2;
		for (int _w = 0; _w < _walkerCount; ++_w)
		{
			int x = walkerX;
			int y = walkerY;
			int remainingSteps = _walkerSteps;
			Vector2I dir = GetRandomDir();

			while (remainingSteps > 0 && IsInBounds(x, y))
			{
				positions.Add(new Vector2I(x, y));
				_map[x, y] = TileTypes.Grass;

				float changeDir = GD.Randf();
				if (changeDir < _walkerDirChanceChance)
					dir = GetRandomDir();
				
				x += dir.X;
				y += dir.Y;
				remainingSteps -= 1;
			}

			Vector2I pos = positions[(int)(GD.Randi() % positions.Count)];
			walkerX = pos.X;
			walkerY = pos.Y;
		}

		if (_applyCellularAutomata)
			ApplyCellularAutomata();
	}

	private void GenerateBaseDLA()
	{
		_map = new int[_mapWidth, _mapHeight];
		int y;
		int x;
		for (y = 0; y < _mapHeight; ++y)
		{
			for (x = 0; x < _mapWidth; ++x)
				_map[x, y] = TileTypes.Water;
		}

		// Init cluster
		int cX = _mapWidth / 2;
		int cY = _mapHeight / 2;

		// _map[cX, cY] = TileTypes.Grass;
		// _map[cX - 1, cY] = TileTypes.Grass;
		// _map[cX + 1, cY] = TileTypes.Grass;
		// _map[cX, cY + 1] = TileTypes.Grass;
		// _map[cX, cY - 1] = TileTypes.Grass;

		_mapLandPercentage = 0;
		GenerateBaseRandomWalk();

		float squarePercentage = 1f / (_mapWidth * _mapHeight);
		// _mapLandPercentage = _mapLandPercentage;

		Vector2I vec = GetRandomMapPos();
		x = vec.X;
		y = vec.Y;
		vec = GetRandomDir();
		while (_mapLandPercentage < _minMapLandPercentage)
		{
			x += vec.X;
			y += vec.Y;

			float chance = GD.Randf();
			if (chance < _walkerDirChanceChance)
				vec = GetRandomDir();

			if (!IsInBounds(x, y))
			{
				vec = GetRandomMapPos();
				x = vec.X;
				y = vec.Y;
				vec = GetRandomDir();
				continue;
			}

			if (_map[x, y] != TileTypes.Water)
			{
				_map[x - vec.X, y - vec.Y] = TileTypes.Grass;
				vec = GetRandomMapPos();
				x = vec.X;
				y = vec.Y;
				vec = GetRandomDir();
				_mapLandPercentage += squarePercentage;
			}
		}

		if (_applyCellularAutomata)
			ApplyCellularAutomata();
	}

	private bool IsInBounds(int x, int y)
	{
		return x >= 0 && x < _mapWidth && y >= 0 && y < _mapHeight;
	}
	
	private void ComputeNoiseMap()
	{
		_noiseMap = new float[_mapWidth, _mapHeight];

		int cx = _mapWidth / 2;
		int cy = _mapHeight / 2;
		float offset = 0;
		float value = 0;
		while (value <= _grassHeight)
		{
			value = _noise.GetNoise2D(cx + offset, cy + offset);
			offset += _scale;
		}

		for (int y = 0; y < _mapHeight; ++y)
		{
			for (int x = 0; x < _mapWidth; ++x)
			{
				float sampleX = (x + cx) / _scale + offset;
				float sampleY = (y + cy) / _scale + offset;
				
				float noise = _noise.GetNoise2D(sampleX, sampleY);
				_noiseMap[x, y] = (noise + 1f) / 2f;
			}
		}
	}

	private void ApplyFalloffMap()
	{
		if (_circularFalloff)
		{
			string msg = "";

			int cx = _mapWidth / 2;
			int cy = _mapHeight / 2;

			float radiusDist = Mathf.Sqrt(2 * _circularFalloffRadius * _circularFalloffRadius);
			float maxDist = Mathf.Sqrt(cx * cx + cy * cy) - radiusDist;
			radiusDist /= Mathf.Sqrt(cx * cx + cy * cy);
			GD.Print(radiusDist);
			for (int y = 0; y < _mapHeight; ++y)
			{
				for (int x = 0; x < _mapWidth; ++x)
				{
					float dx = Mathf.Abs(cx - x - 1);
					float dy = Mathf.Abs(cy - y - 1);
				
					float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
					dist = Mathf.Clamp(dist - radiusDist, 0f, 1f);
					float falloff = Mathf.Pow(Mathf.Clamp(dist, 0f, 1f), _edgeFalloff);
					
					_noiseMap[x, y] = Mathf.Clamp(_noiseMap[x,y] - falloff, 0f, 1f);

					msg += $"{falloff:F2} ";
				}

				msg += "\n";
			}

			GD.Print(msg);
		}
		else
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
                float v = _noiseMap[x, y];
                int tileType;
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

	private static Vector2I GetRandomDir()
	{
		return (GD.Randi() % 4) switch {
			0 => Vector2I.Up,
			1 => Vector2I.Down,
			2 => Vector2I.Right,
			3 => Vector2I.Left,
			_ => Vector2I.Up
		};
	}

	private Vector2I GetRandomMapPos()
	{
		return new Vector2I(
			(int)(GD.Randi() % _mapWidth),
			(int)(GD.Randi() % _mapHeight)
		);
	}
}
