using System;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.WildVacuum.Services
{
    public class MainMenuPayload : IStatePayload { }

    public class MainMenuState : State<MainMenuPayload>
    {
        // Reminder: Keep variables in payload for easy reset of states!

        static string[] Scenes => new []
        {
            "MainMenu"
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
            App.Heartbeat.Wait(
                () =>
                {
                    Debug.Log("Waited 0.1 seconds...");
                    var counter = 10;
                    App.Heartbeat.Wait(
                        () =>
                        {
                            Debug.Log("Waited for counter to hit zero");
                            App.Heartbeat.Wait(
                                result =>
                                {
                                    Debug.Log("Waited for time to be equal with result: " + result);
                                    App.S.RequestState<GamePayload>();
                                },
                                () => (DateTime.Now.Second % 2) == 0
                            );
                        },
                        () =>
                        {
                            counter--;
                            return counter == 0;
                        }
                    );
                },
                0.1f
            );
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