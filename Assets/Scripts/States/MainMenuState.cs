using System;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Models;
using UnityEngine;

namespace Lunra.WildVacuum.Services
{
    public class MainMenuPayload : IStatePayload
    {
        public PreferencesModel Preferences;
    }

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
            App.S.RequestState(
                new GamePayload
                {
                    Preferences = Payload.Preferences
                }
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