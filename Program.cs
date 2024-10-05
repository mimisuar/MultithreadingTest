using SFML.Graphics;
using SFML.Window;
using SFML.System;

namespace MoreMultithreading
{
	internal class Program
	{
		static void Main(string[] args)
		{
			RenderWindow window = new RenderWindow(new VideoMode(800, 600), "Multithreaded");
			MessageQueue.Instance.Initialize();

			Grid grid = new(800, 600, 40, 40);

			bool holdingShift = false;

			window.Closed += (obj, args) => 
			{
				window.Close();
			};

			window.MouseButtonPressed += (obj, args) =>
			{
				int gridX = args.X / grid.TileWidth;
				int gridY = args.Y / grid.TileHeight;

				if (args.Button == Mouse.Button.Left)
				{
					if (holdingShift)
					{
						grid.MoveGoal(gridX, gridY);
					}
					else
					{
						grid.MoveStart(gridX, gridY);
					}
				}
				else if (args.Button == Mouse.Button.Right)
				{
					grid.ToggleTile(gridX, gridY);
				}

				
			};

			window.KeyPressed += (obj, args) =>
			{
				if (args.Code == Keyboard.Key.R)
				{
					grid.GenerateGrid();
				}

				if (args.Code == Keyboard.Key.LShift)
				{
					holdingShift = true;
				}
			};

			window.KeyReleased += (obj, args) =>
			{
				if (args.Code == Keyboard.Key.LShift)
				{
					holdingShift = false;
				}
			};

			Clock clock = new();
			float lastTime = clock.ElapsedTime.AsSeconds();

			while (window.IsOpen)
			{
				float currentTime = clock.ElapsedTime.AsSeconds();
				float dt = currentTime - lastTime;
				lastTime = currentTime;
				MessageQueue.Instance.Update(dt);

				window.DispatchEvents();

				window.Clear();
				grid.Render(window);
				MessageQueue.Instance.Render(window);
				window.Display();

				//Thread.Sleep(1000 / 60);
			}
		}
	}
}
