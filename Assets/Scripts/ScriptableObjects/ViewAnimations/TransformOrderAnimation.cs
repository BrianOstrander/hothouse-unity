namespace LunraGames.SubLight
{
	public class TransformOrderAnimation : ViewAnimation
	{
		public enum Orders
		{
			Unknown = 0,
			First = 10,
			Last = 20
		}

		public Orders Order;
		public bool OrderOnIdle;

		public override void OnPrepare(IView view)
		{
			ProcessOrder(view);
		}

		public override void OnIdle(IView view, float delta)
		{
			if (OrderOnIdle) ProcessOrder(view);
		}

		void ProcessOrder(IView view)
		{
			switch (Order)
			{
				case Orders.First:
					view.Root.SetAsFirstSibling();
					break;
				case Orders.Last:
					view.Root.SetAsLastSibling();
					break;
			}
		}
	}
}