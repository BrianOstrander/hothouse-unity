using Lunra.Hothouse.Services;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;

namespace Lunra.Hothouse
{
	public class HothouseMain : Main
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