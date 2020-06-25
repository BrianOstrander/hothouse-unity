using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;

namespace Lunra.Hothouse.Presenters
{ 
	public class PrefabPresenter<M, V> : PooledPresenter<M, V>
		where M : IPrefabModel
		where V : class, IPrefabView
	{
		protected RoomModel Room { get; private set; }
		
		public PrefabPresenter(
			GameModel game,
			M model
		) : base(
			game,
			model,
			App.V.Get<V>(v => v.PrefabId == model.PrefabId.Value)
		) { }

		protected override void Bind()
		{
			if (AutoShowCloseOnRoomReveal)
			{
				Room = Game.Rooms.FirstActive(Model.RoomTransform.Id.Value);

				Room.IsRevealed.Changed += OnRoomIsRevealed;
			}

			Model.PrefabTags.Value = View.PrefabTags;
			Model.Tag.Value = View.Tag;

			base.Bind();
		}

		protected override void UnBind()
		{
			if (AutoShowCloseOnRoomReveal)
			{
				Room.IsRevealed.Changed -= OnRoomIsRevealed;
			}

			base.UnBind();
		}

		protected virtual bool AutoShowCloseOnRoomReveal => true;
		
		#region View Events
		protected override void OnViewPrepare()
		{
			View.RoomId = Model.RoomTransform.Id.Value;
			View.ModelId = Model.Id.Value;
		}
		#endregion
		
		#region RoomModel Events
		void OnRoomIsRevealed(bool isRevealed)
		{
			if (isRevealed)
			{
				if (CanShow()) Show();
			}
			else Close();
		}
		#endregion
	}
}