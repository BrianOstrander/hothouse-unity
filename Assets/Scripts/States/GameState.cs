using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;

namespace Lunra.WildVacuum.Services
{
	public class GamePayload : IStatePayload { }

	public class GameState : State<GamePayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		static string[] Scenes => new []
		{
			"Game"
		};
		
		#region Begin
		protected override void Begin()
		{
			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.Load(result => done(), Scenes))    
			);
		}
		#endregion

		#region Idle
		protected override void Idle()
		{
			
		}
		#endregion
        
		#region End
		protected override void End()
		{
			App.S.PushBlocking(
				done => App.P.UnRegisterAll(done)
			);

			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.UnLoad(result => done(), Scenes))
			);
		}
		#endregion
	}
}