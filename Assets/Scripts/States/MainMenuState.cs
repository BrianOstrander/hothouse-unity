using System;
using Lunra.Core;
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
            GenerateNewGame(
                result =>
                {
                    if (result.Status != ResultStatus.Success)
                    {
                        result.Log("Generating new game did not succeed!");
                        return;
                    }
                    
                    App.S.RequestState(
                        new GamePayload
                        {
                            Preferences = Payload.Preferences,
                            Game = result.Payload
                        }
                    );
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

        void GenerateNewGame(Action<Result<GameModel>> done)
        {
            var game = new GameModel();

            game.WorldCamera.Enabled.Value = true;
            
            var room1 = new RoomPrefabModel();

            room1.PrefabId.Value = "default_spawn";
            room1.Enabled.Value = true;
            
            var room2 = new RoomPrefabModel();

            room2.PrefabId.Value = "rectangle";
            room2.Enabled.Value = true;
            room2.Position.Value = new Vector3(0f, 3.01f, -18.74f);

            game.Rooms.Value = new[]
            {
                room1,
                room2
            };
            
            done(Result<GameModel>.Success(game));
        }
    }
}