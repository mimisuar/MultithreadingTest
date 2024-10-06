using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MoreMultithreading;
using SFML.Graphics;
using SFML.System;

public enum GridObjectType
{
	Empty,
	Wall,
	Start,
	Goal,
	SpeedBoost,
	Mud
}
public class GridObject
{
	public int id;
	public int x;
	public int y;
	public GridObjectType type;
}

public class Grid
{
	private int width;
	private int height;
	private int tileWidth;
	private int tileHeight;
	private GridObject[] grid;

	private int startX;
	private int startY;
	private int goalX;
	private int goalY;

	public int StartX => startX;
	public int StartY => startY;
	public int GoalX => goalX;
	public int GoalY => goalY;
	

	public int TileWidth => tileWidth;
	public int TileHeight => tileHeight;
	public int Width => width;
	public int Height => height;
	public Grid(int windowWidth, int windowHeight, int tileWidth, int tileHeight)
	{
		width = windowWidth / tileWidth;
		height = windowHeight / tileHeight;
		this.tileWidth = tileWidth;
		this.tileHeight = tileHeight;
		grid = new GridObject[width * height];
	}

	public void SetStart(int x, int y)
	{
		lock (GridController.locker)
		{
			SetObjectType(x, y, GridObjectType.Start);
			startX = x;
			startY = y;
		}
	}

	public void SetGoal(int x, int y)
	{
		lock (GridController.locker)
		{
			SetObjectType(x, y, GridObjectType.Goal);
			goalX = x;
			goalY = y;
		}
	}

	public void MoveGoal(int x, int y)
	{
		if (GetObjectType(x, y) == GridObjectType.Empty)
		{
			lock (GridController.locker)
			{
				SetObjectType(goalX, goalY, GridObjectType.Empty);
				SetObjectType(x, y, GridObjectType.Goal);
				goalX = x;
				goalY = y;
			}
		}
	}

	public void MoveStart(int x, int y)
	{
		if (GetObjectType(x, y) == GridObjectType.Empty)
		{
			lock (GridController.locker)
			{
				SetObjectType(startX, startY, GridObjectType.Empty);
				SetObjectType(x, y, GridObjectType.Start);
				startX = x;
				startY = y;
			}
		}
	}

	public void ToggleTile(int x, int y)
	{
		GridObjectType type = GetObjectType(x, y);
		if (type == GridObjectType.Start || type == GridObjectType.Goal) { return; }

		GridObjectType nextType;
		switch (type)
		{
			case GridObjectType.Empty:
				nextType = GridObjectType.Wall;
				break;

			case GridObjectType.Wall:
				nextType = GridObjectType.Mud;
				break;

			case GridObjectType.Mud:
				nextType = GridObjectType.SpeedBoost;
				break;

			default:
				nextType = GridObjectType.Empty;
				break;
		}

		lock (GridController.locker)
		{
			SetObjectType(x, y, nextType);
		}
	}

	public GridObjectType GetObjectType(int x, int y)
	{
		if (x < 0 || x >= width)
		{
			throw new ArgumentOutOfRangeException();
		}

		if (y < 0 || y >= height)
		{
			throw new ArgumentOutOfRangeException(); 
		}

		return grid[y * width + x].type;
	}

	public void SetObjectType(int x, int y, GridObjectType type)
	{
		if (x < 0 || x >= width)
		{
			throw new ArgumentOutOfRangeException();
		}

		if (y < 0 || y >= height)
		{
			throw new ArgumentOutOfRangeException();
		}

		lock (GridController.locker)
		{
			grid[y * width + x].type = type;
		}
	}

	public GridObject GetObjectAt(int idx)
	{
		return grid[idx];
	}

	public void SetObjectAt(int idx, GridObject obj)
	{
		grid[idx] = obj;
	}

	public GridObject GetObject(int x, int y)
	{
		if (x < 0 || x >= width)
		{
			throw new ArgumentOutOfRangeException();
		}

		if (y < 0 || y >= height)
		{
			throw new ArgumentOutOfRangeException();
		}

		return grid[y * width + x];
	}

	

	public List<GridObject> GetNeighbors(int x, int y)
	{
		List<GridObject> neighbors = new();

		if (x > 0)
		{
			neighbors.Add(GetObject(x - 1, y));
		}

		if (x < width - 1)
		{
			neighbors.Add(GetObject(x + 1, y));
		}

		if (y > 0)
		{
			neighbors.Add(GetObject(x, y - 1));
		}

		if (y < height - 1)
		{
			neighbors.Add(GetObject(x, y + 1));
		}

		return neighbors;
	}
}
