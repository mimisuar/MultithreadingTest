using SFML.Graphics;

namespace MoreMultithreading
{
	internal class PathFinder
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

		private List<GridObject> path = new();
		private Task? pathFindingTask = null;

		public List<GridObject> Path => path;

		public void FindPath(Grid grid)
		{
			if (pathFindingTask != null && !pathFindingTask.IsCompleted)
			{
				pathFindingTask.Wait();
			}
			pathFindingTask = Task.Run(()=>FindPathWorker(grid));

		}
		private void FindPathWorker(Grid grid)
		{
			PriorityQueue<GridObject, float> queue = new();
			Dictionary<GridObject, float> cost = new();
			Dictionary<GridObject, GridObject> prev = new();
			List<GridObject> seen = new();
			float finalCost = -1;

			lock (GridController.locker)
			{
				path.Clear();
				GridObject start = grid.GetObject(grid.StartX, grid.StartY);
				GridObject goal = grid.GetObject(grid.GoalX, grid.GoalY);

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

					foreach (GridObject neighbor in grid.GetNeighbors(current.x, current.y))
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
}
