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
	private readonly Dictionary<GridObjectType, float> tileCosts = new()
	{
		{GridObjectType.Empty, 1f },
		{GridObjectType.Wall, 1f }, 
		{GridObjectType.Goal, 1f },
		{GridObjectType.Start, 1f },
		{GridObjectType.Mud, 2f }, // takes longer to get out of mud
		{GridObjectType.SpeedBoost, 0.5f }
	};

	private int width;
	private int height;
	private int tileWidth;
	private int tileHeight;
	private GridObject[] grid;

	private object lockObject = new();
	private int startX;
	private int startY;
	private int goalX;
	private int goalY;
	private List<GridObject> path = new();

	private Task? pathFindingTask = null;

	public int TileWidth => tileWidth;
	public int TileHeight => tileHeight;
	public Grid(int windowWidth, int windowHeight, int tileWidth, int tileHeight)
	{
		width = windowWidth / tileWidth;
		height = windowHeight / tileHeight;
		this.tileWidth = tileWidth;
		this.tileHeight = tileHeight;
		grid = new GridObject[width * height];

		GenerateGrid();
	}

	public void Render(RenderTarget target)
	{
		RectangleShape shape = new(new Vector2f(tileWidth, tileHeight));


		for (int i = 0; i < width * height; i++)
		{
			switch (grid[i].type)
			{
				case GridObjectType.Wall:
					shape.FillColor = Color.White;
					break;

				case GridObjectType.Start:
					shape.FillColor = Color.Green;
					break;

				case GridObjectType.Goal:
					shape.FillColor = Color.Red;
					break;

				case GridObjectType.Mud:
					shape.FillColor = new Color(41, 19, 3);
					break;

				case GridObjectType.SpeedBoost:
					shape.FillColor = Color.Cyan;
					break;

				default:
					continue;
			}

			shape.Position = new Vector2f(grid[i].x * tileWidth, grid[i].y * tileHeight);
			target.Draw(shape);
		}

		CircleShape pathShape = new(8);
		Color c = Color.Yellow;
		c.A = (byte)(0.5f * 255);
		pathShape.FillColor = c;

		int offset = 12;

		lock (lockObject)
		{
			foreach (GridObject pathObject in path)
			{
				pathShape.Position = new Vector2f(pathObject.x * tileWidth + offset, pathObject.y * tileHeight + offset);
				target.Draw(pathShape);
			}
		}
	}

	public void GenerateGrid()
	{
		int id = 0;
		int x = 0;
		int y = 0;

		lock (lockObject)
		{

			Random rando = new();
			for (int i = 0; i < width * height; i++)
			{
				grid[i] = new GridObject();
				grid[i].x = x;
				grid[i].y = y;
				grid[i].id = id++;
				grid[i].type = GridObjectType.Empty;

				if (rando.NextDouble() < 0.2)
				{
					if (rando.NextDouble() < 0.1)
					{
						grid[i].type = GridObjectType.SpeedBoost;
					}
					else if (rando.NextDouble() < 0.1)
					{
						grid[i].type = GridObjectType.Mud;
					}
					else
					{

						grid[i].type = GridObjectType.Wall;
					}
				}

				x++;
				if (x >= width)
				{
					x = 0;
					y += 1;
					if (y >= height)
					{
						break;
					}
				}
			}

			bool startPlaced = false;
			bool goalPlaced = false;

			while (!goalPlaced)
			{
				int i = rando.Next(width * height);
				grid[i].type = GridObjectType.Goal;
				goalX = grid[i].x;
				goalY = grid[i].y;
				goalPlaced = true;
			}

			while (!startPlaced)
			{
				int i = rando.Next(width * height);
				grid[i].type = GridObjectType.Start;
				startX = grid[i].x;
				startY = grid[i].y;
				startPlaced = true;
			}
		}

		MessageQueue.Instance.WriteMessage("Generating grid");

		FindPath();
	}

	public void MoveGoal(int x, int y)
	{
		if (GetObjectType(x, y) == GridObjectType.Empty)
		{
			lock (lockObject)
			{
				SetObjectType(goalX, goalY, GridObjectType.Empty);
				SetObjectType(x, y, GridObjectType.Goal);
				goalX = x;
				goalY = y;
			}

			FindPath();
		}
	}

	public void MoveStart(int x, int y)
	{
		if (GetObjectType(x, y) == GridObjectType.Empty)
		{
			lock (lockObject)
			{
				SetObjectType(startX, startY, GridObjectType.Empty);
				SetObjectType(x, y, GridObjectType.Start);
				startX = x;
				startY = y;
			}

			FindPath();
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

		lock (lockObject)
		{
			SetObjectType(x, y, nextType);
		}
		FindPath();
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

		lock (lockObject)
		{
			grid[y * width + x].type = type;
		}
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

	private void FindPath()
	{
		if (pathFindingTask != null && !pathFindingTask.IsCompleted)
		{
			pathFindingTask.Wait();
		}
		pathFindingTask = Task.Run(FindPathWorker);
		
	}

	private void FindPathWorker()
	{
		PriorityQueue<GridObject, float> queue = new();
		Dictionary<GridObject, float> cost = new();
		Dictionary<GridObject, GridObject> prev = new();
		List<GridObject> seen = new();
		float finalCost = -1;

		lock (lockObject)
		{
			path.Clear();
			GridObject start = GetObject(startX, startY);
			GridObject goal = GetObject(goalX, goalY);

			prev[start] = null;
			cost[start] = 0;
			queue.Enqueue(start, 0);

			while (queue.Count > 0)
			{
				GridObject current = queue.Dequeue();
				if (current.type == GridObjectType.Goal)
				{
					break;
				}
				seen.Add(current);

				foreach (GridObject neighbor in GetNeighbors(current.x, current.y))
				{
					if (seen.Contains(neighbor)) { continue; }
					if (neighbor.type == GridObjectType.Wall) { continue; }

					float newCost = cost[current] + tileCosts[current.type];
					if (!cost.ContainsKey(neighbor) || newCost < cost[neighbor])
					{
						cost[neighbor] = newCost;
						prev[neighbor] = current;
						queue.Enqueue(neighbor, newCost);
					}
				}
			}

			
			GridObject pathObject = goal;
			while (pathObject != start)
			{
				if (pathObject != goal)
				{
					path.Add(pathObject);
				}

				if (!prev.ContainsKey(pathObject))
				{
					MessageQueue.Instance.WriteMessage("Failed to find path.", Color.Red);
					path.Clear();
					return;
				}
				pathObject = prev[pathObject];
			}

			finalCost = cost[goal];
		}

		MessageQueue.Instance.WriteMessage("Found path with cost " + finalCost.ToString() + ".", Color.Green);
	}
}
