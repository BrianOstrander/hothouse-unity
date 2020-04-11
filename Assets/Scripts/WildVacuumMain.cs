using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Services;

namespace Lunra.WildVacuum
{
    public class WildVacuumMain : Main
    {
        protected override IState[] States => new []
        {
            new InitializeState()
        };
        
        protected override void OnStartupDone()
        {
            App.S.RequestState<InitializePayload>();
        }
    }
}