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

			GridController gridController = new(800, 600, 40, 40);
			gridController.GenerateGrid();

			window.Closed += (obj, args) => 
			{
				window.Close();
			};

			window.MouseButtonPressed += gridController.OnMousePressed;
			window.KeyPressed += gridController.OnKeyPressed;
			window.KeyReleased += gridController.OnKeyReleased;

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
				gridController.Render(window);
				MessageQueue.Instance.Render(window);
				window.Display();

				//Thread.Sleep(1000 / 60);
			}
		}
	}
}
