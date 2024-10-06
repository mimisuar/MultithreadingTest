using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreMultithreading
{
	public class GridController
	{
		private bool holdingShift = false;
		public static object locker = new();
		private Grid grid;
		private PathFinder pathFinder;

		public GridController(int windowWidth, int windowHeight, int tileWidth, int tileHeight)
		{
			grid = new Grid(windowWidth, windowHeight, tileWidth, tileHeight);
			pathFinder = new PathFinder();
		}
		public void Render(RenderTarget target)
		{
			RectangleShape shape = new(new Vector2f(grid.TileWidth, grid.TileHeight));

			for (int i = 0; i < grid.Width * grid.Height; i++)
			{
				switch (grid.GetObjectAt(i).type)
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

				shape.Position = new Vector2f(grid.GetObjectAt(i).x * grid.TileWidth, grid.GetObjectAt(i).y * grid.TileHeight);
				target.Draw(shape);
			}

			CircleShape pathShape = new(8);
			Color c = Color.Yellow;
			c.A = (byte)(0.5f * 255);
			pathShape.FillColor = c;

			int offset = 12;

			lock (GridController.locker)
			{
				foreach (GridObject pathObject in pathFinder.Path)
				{
					pathShape.Position = new Vector2f(pathObject.x * grid.TileWidth + offset, pathObject.y * grid.TileHeight + offset);
					target.Draw(pathShape);
				}
			}
		}

		public void GenerateGrid()
		{
			int id = 0;
			int x = 0;
			int y = 0;

			lock (GridController.locker)
			{

				Random rando = new();
				for (int i = 0; i < grid.Width * grid.Height; i++)
				{
					var gridObject = new GridObject();
					gridObject.x = x;
					gridObject.y = y;
					gridObject.id = id++;
					gridObject.type = GridObjectType.Empty;

					if (rando.NextDouble() < 0.2)
					{
						if (rando.NextDouble() < 0.1)
						{
							gridObject.type = GridObjectType.SpeedBoost;
						}
						else if (rando.NextDouble() < 0.1)
						{
							gridObject.type = GridObjectType.Mud;
						}
						else
						{

							gridObject.type = GridObjectType.Wall;
						}
					}

					grid.SetObjectAt(i, gridObject);

					x++;
					if (x >= grid.Width)
					{
						x = 0;
						y += 1;
						if (y >= grid.Height)
						{
							break;
						}
					}
				}

				int j = rando.Next(grid.Width * grid.Height);
				var goalObject = grid.GetObjectAt(j);
				grid.SetGoal(goalObject.x, goalObject.y);

				j = rando.Next(grid.Width * grid.Height);
				var startObject = grid.GetObjectAt(j);
				grid.SetStart(startObject.x, startObject.y);
			}

			MessageQueue.Instance.WriteMessage("Generating grid");
			pathFinder.FindPath(grid);
		}

		public void OnMousePressed(object? obj, MouseButtonEventArgs args)
		{
			int gridX = args.X / grid.TileWidth;
			int gridY = args.Y / grid.TileHeight;

			if (args.Button == Mouse.Button.Left)
			{
				if (holdingShift)
				{
					grid.MoveGoal(gridX, gridY);
					pathFinder.FindPath(grid);
				}
				else
				{
					grid.MoveStart(gridX, gridY);
					pathFinder.FindPath(grid);
				}
			}
			else if (args.Button == Mouse.Button.Right)
			{
				grid.ToggleTile(gridX, gridY);
				pathFinder.FindPath(grid);
			}
		}

		public void OnKeyPressed(object? obj, KeyEventArgs args)
		{
			if (args.Code == Keyboard.Key.R)
			{
				GenerateGrid();
			}

			if (args.Code == Keyboard.Key.LShift)
			{
				holdingShift = true;
			}
		}

		public void OnKeyReleased(object? obj, KeyEventArgs args)
		{
			if (args.Code == Keyboard.Key.LShift)
			{
				holdingShift = false;
			}
		}
	}
}
