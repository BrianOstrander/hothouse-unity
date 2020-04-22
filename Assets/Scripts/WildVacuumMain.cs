using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Services;

namespace Lunra.WildVacuum
{
	public class WildVacuumMain : Main
	{
		protected override IState[] InstantiateStates()  => new IState[]
		{
			new InitializeState(),
			new MainMenuState(), 
			new GameState()
		};
		
		protected override void OnStartupDone()
		{
			App.S.RequestState<InitializePayload>();
		}
	}
}