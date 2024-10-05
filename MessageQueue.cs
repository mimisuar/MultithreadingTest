using SFML.Graphics;

namespace MoreMultithreading
{
	public struct Message
	{
		public string text;
		public Color color;
	}

	public class MessageRenderer
	{
		private Text text;
		private float lifetime = -1;
		private bool free = true;

		public string DisplayedString
		{
			get => text.DisplayedString;
			set => text.DisplayedString = value;
		}

		public Color FillColor
		{
			get => text.FillColor;
			set => text.FillColor = value;
		}

		public uint CharacterSize => text.CharacterSize;

		public Text DisplayText => text;

		public bool IsFree => free;
		public MessageRenderer(Font font)
		{
			text = new();
			text.Font = font;
			text.CharacterSize = 24;
			text.Style = Text.Styles.Regular;
			text.FillColor = Color.White;
			text.OutlineColor = Color.Black;
			text.OutlineThickness = 2;
		}

		public void Update(float dt)
		{
			if (lifetime > 0)
			{
				lifetime -= dt;
				if (lifetime < 0)
				{
					lifetime = 0;
					free = true;
					DisplayedString = "";
				}
			}
		}

		public void SetMessage(Message message)
		{
			lifetime = 1;
			FillColor = message.color;
			DisplayedString = message.text;
			free = false;
		}
	}

	public class MessageQueue
	{
		public static object locker = new();
		private static MessageQueue? instance = null;
		public static MessageQueue Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new();
				}

				return instance!;
			}
		}

		private MessageRenderer[] messageRendererPool;
		private Queue<Message> messages;
		private List<MessageRenderer> displayedMessages;
		private Font font;
		public void Initialize()
		{ 
			font = new Font("fonts/Roboto-Regular.ttf");
			messages = new Queue<Message>();
			messageRendererPool = new MessageRenderer[5];
			displayedMessages = new();

			// only 5 messages can be on screen at a time
			for (int i = 0; i < messageRendererPool.Length; i++)
			{
				MessageRenderer r = new MessageRenderer(font);
				messageRendererPool[i] = r;
			}
		}

		public void Update(float dt)
		{
			int firstFree = -1;
			for (int i = 0; i < messageRendererPool.Length; i++)
			{
				messageRendererPool[i].Update(dt);

				if (messageRendererPool[i].IsFree)
				{
					if (firstFree == -1)
					{
						firstFree = i;
					}

					if (displayedMessages.Contains(messageRendererPool[i]))
					{
						displayedMessages.Remove(messageRendererPool[i]);
					}
				}
			}

			if (firstFree == -1) { return; }
			if (messages.Count == 0) { return; }

			Message m = messages.Dequeue();
			messageRendererPool[firstFree].SetMessage(m);
			displayedMessages.Add(messageRendererPool[firstFree]);
		}

		public void Render(RenderTarget target)
		{
			int yPosition = 0;
			for (int i = 0; i < displayedMessages.Count; i++)
			{
				if (displayedMessages[i].IsFree)
				{
					continue;
				}

				displayedMessages[i].DisplayText.Position = new SFML.System.Vector2f(0, yPosition);
				yPosition += (int)displayedMessages[i].CharacterSize;
				target.Draw(displayedMessages[i].DisplayText);
			}
		}

		public void WriteMessage(string message, Color color)
		{
			lock (MessageQueue.locker)
			{
				messages.Enqueue(new Message() { color = color, text = message });
			}
		}

		public void WriteMessage(string message)
		{
			WriteMessage(message, Color.White);
		}
	}
}
