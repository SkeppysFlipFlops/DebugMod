using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HutongGames.PlayMaker.Actions;
using UnityEngine;



namespace DebugMod
{
    /// <summary>
    /// Handles struct SaveStateData and individual SaveState operations
    /// </summary>
    internal class SaveState
    {
        //used to stop double loads/saves
        public static bool loadingSavestate = false;

        [Serializable]
        public class SaveStateData
        {
            public string saveStateIdentifier;
            public string saveScene;
            public int useRoomSpecific = 0;
            public int facingRight = -1; // -1 for unknown, so that older savestate files behave the same.
            public PlayerData savedPd;
            public object lockArea;
            public SceneData savedSd;
            public Vector3 savePos;
            public FieldInfo cameraLockArea;
            public string filePath;
            internal SaveStateData() { }
            
            internal SaveStateData(SaveStateData _data)
            {
                saveStateIdentifier = _data.saveStateIdentifier;
                saveScene = _data.saveScene;
                useRoomSpecific = _data.useRoomSpecific;
                facingRight = _data.facingRight;
                cameraLockArea = _data.cameraLockArea;
                savedPd = _data.savedPd;
                savedSd = _data.savedSd;
                savePos = _data.savePos;
                lockArea = _data.lockArea;
            }
        }

        [SerializeField]
        public SaveStateData data;

        internal SaveState()
        {
            data = new SaveStateData();
        }
        /* TODO:
            handle fury/full-health fury
            apply/deapply twister effects when charms changed 
            adjust nail dmg from one save to another
        */
        #region saving

        public void SaveTempState()
        {
            DebugMod.GM.SaveLevelState();
            data.saveScene = GameManager.instance.GetSceneNameString();
            data.saveStateIdentifier = "(tmp)_" + data.saveScene + "-" + DateTime.Now.ToString("H:mm_d-MMM");
            data.savedPd = JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(PlayerData.instance));
            data.savedSd = JsonUtility.FromJson<SceneData>(JsonUtility.ToJson(SceneData.instance));
            data.savePos = HeroController.instance.gameObject.transform.position;
            data.cameraLockArea = (data.cameraLockArea ?? typeof(CameraController).GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic));
            data.lockArea = data.cameraLockArea.GetValue(GameManager.instance.cameraCtrl);
            data.useRoomSpecific = 0;
            data.facingRight = (HeroController.instance.cState.facingRight) ? 1:0;
        }

        public void NewSaveStateToFile(int paramSlot)
        {
            SaveTempState();
            SaveStateToFile(paramSlot);
        }

        public void SaveStateToFile(int paramSlot)
        {
            try
            {
                if (data.saveStateIdentifier.StartsWith("(tmp)_"))
                {
                    data.saveStateIdentifier = data.saveStateIdentifier.Substring(6);
                }
                else if (String.IsNullOrEmpty(data.saveStateIdentifier))
                {
                    throw new Exception("No temp save state set");
                }
                
                File.WriteAllText (
                    string.Concat(new object[] {
                        SaveStateManager.path,
                        "savestate",
                        paramSlot,
                        ".json"
                    }),
                    JsonUtility.ToJson( data, 
                        prettyPrint: true 
                    )
                );

                
                /*
                DebugMod.instance.Log(string.Concat(new object[] {
                    "SaveStateToFile (this): \n - ", data.saveStateIdentifier,
                    "\n - ", data.saveScene,
                    "\n - ", (JsonUtility.ToJson(data.savedPd)),
                    "\n - ", (JsonUtility.ToJson(data.savedSd)),
                    "\n - ", data.savePos.ToString(),
                    "\n - ", data.cameraLockArea ?? typeof(CameraController).GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic),
                    "\n - ", data.lockArea.ToString(), " ========= ", data.cameraLockArea.GetValue(GameManager.instance.cameraCtrl)
                }));
                DebugMod.instance.Log("SaveStateToFile (data): " + data);
                */
            }
            catch (Exception ex)
            {
                DebugMod.instance.LogDebug(ex.Message);
                throw ex;
            }
        }
        #endregion

        #region loading

        public void LoadTempState()
        {
            //Don't load states if not alive/in transition (breaks savestates)
            if (!PlayerDeathWatcher.playerDead && !HeroController.instance.cState.transitioning) {
                HeroController.instance.StartCoroutine(LoadStateCoro());
            }
            else
            {
                Console.AddLine("Don't load states while dead or in a transition! if you are not, this is a bug.");
            }
        }

        public void NewLoadStateFromFile()
        {
            LoadStateFromFile(SaveStateManager.currentStateSlot);
            LoadTempState();
        }

        public void LoadStateFromFile(int paramSlot)
        {
            try
            {
                data.filePath = string.Concat(
                new object[]
                {
                    SaveStateManager.path,
                    "savestate",
                    paramSlot,
                    ".json"
                });
                DebugMod.instance.Log("prep filepath: " + data.filePath);

                if (File.Exists(data.filePath))
                {
                    //DebugMod.instance.Log("checked filepath: " + data.filePath);
                    SaveStateData tmpData = JsonUtility.FromJson<SaveStateData>(File.ReadAllText(data.filePath));
                    try
                    {
                        data.saveStateIdentifier = tmpData.saveStateIdentifier;
                        data.cameraLockArea = tmpData.cameraLockArea;
                        data.savedPd = tmpData.savedPd;
                        data.savedSd = tmpData.savedSd;
                        data.savePos = tmpData.savePos;
                        data.saveScene = tmpData.saveScene;
                        data.lockArea = tmpData.lockArea;
                        data.useRoomSpecific = tmpData.useRoomSpecific;
                        data.facingRight = tmpData.facingRight;
                        DebugMod.instance.LogFine("Load SaveState ready: " + data.saveStateIdentifier);
                    }
                    catch (Exception ex)
                    {
                        DebugMod.instance.Log(string.Format(ex.Source, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                DebugMod.instance.LogDebug(ex.Message);
                throw ex;
            }
        }

        private IEnumerator LoadStateCoro()
        {
            //prevent double loads/saves, black screen, etc
            loadingSavestate = true;

            //timer for loading savestates, used in diagnostic purposes
            System.Diagnostics.Stopwatch loadingStateTimer = new System.Diagnostics.Stopwatch();
            loadingStateTimer.Start();

            /*
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (data.saveScene == scene)
            {
                yield return UnityEngine.SceneManagement.SceneManager.UnloadScene(scene);
            }
            */
            //Console.AddLine("LoadStateCoro line1: " + data.savedPd.hazardRespawnLocation.ToString());
            int oldMPReserveMax = PlayerData.instance.MPReserveMax;
            int oldMP = PlayerData.instance.MPCharge;

            data.cameraLockArea = (data.cameraLockArea ?? typeof(CameraController).GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic));
            string scene = "Room_Mender_House";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Room_Mender_House")
            {
                scene = "Room_Sly_Storeroom";
            }
            GameManager.instance.ChangeToScene(scene, "", 0f);// i hate that i have to do this.
            while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != scene)
            {
                yield return null;
            }
            GameManager.instance.sceneData = (SceneData.instance = JsonUtility.FromJson<SceneData>(JsonUtility.ToJson(data.savedSd)));
            GameManager.instance.ResetSemiPersistentItems();

            yield return null;
            HeroController.instance.gameObject.transform.position = data.savePos;
            PlayerData.instance = (GameManager.instance.playerData = (HeroController.instance.playerData = JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(data.savedPd))));
            GameManager.instance.ChangeToScene(data.saveScene, "", 0.4f);
            try
            {
                data.cameraLockArea.SetValue(GameManager.instance.cameraCtrl, data.lockArea);
                GameManager.instance.cameraCtrl.LockToArea(data.lockArea as CameraLockArea);
                BindableFunctions.cameraGameplayScene.SetValue(GameManager.instance.cameraCtrl, true);
            }
            catch (Exception message)
            {
                Debug.LogError(message);
            }
            yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == data.saveScene);
            HeroController.instance.playerData = PlayerData.instance;
            HeroController.instance.geoCounter.playerData = PlayerData.instance;
            HeroController.instance.geoCounter.TakeGeo(0);

            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-HeroLanded");
            HeroAnimationController component = HeroController.instance.GetComponent<HeroAnimationController>();
            typeof(HeroAnimationController).GetField("pd", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, PlayerData.instance);
           
            HeroController.instance.TakeHealth(1);
            HeroController.instance.AddHealth(1);
            GameCameras.instance.hudCanvas.gameObject.SetActive(true);
            HeroController.instance.TakeHealth(1);
            HeroController.instance.AddHealth(1);
            
            GameManager.instance.inputHandler.RefreshPlayerData();

            if (data.facingRight == 1)
            {
                HeroController.instance.FaceRight();
            }
            else if (data.facingRight == 0)
            {
                HeroController.instance.FaceLeft();
            }

            //This is all the shit that fixes lp debug
            HeroController.instance.CharmUpdate();

            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");    //update twister             
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");       //update nail

            //step 2,manually trigger vessels lol, this is the only way besides turning off the mesh, but that will break stuff when you collect them
            if (PlayerData.instance.MPReserveMax < 33) GameObject.Find("Vessel 1").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 1").LocateMyFSM("vessel_orb").SetState("Up Check");
            if (PlayerData.instance.MPReserveMax < 66) GameObject.Find("Vessel 2").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 2").LocateMyFSM("vessel_orb").SetState("Up Check");
            if (PlayerData.instance.MPReserveMax < 99) GameObject.Find("Vessel 3").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 3").LocateMyFSM("vessel_orb").SetState("Up Check");
            if (PlayerData.instance.MPReserveMax < 132) GameObject.Find("Vessel 4").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 4").LocateMyFSM("vessel_orb").SetState("Up Check");
            //step 3, take and add some soul
            HeroController.instance.TakeMP(1);
            HeroController.instance.AddMPChargeSpa(1);
            //step 4, run animations later to actually add the soul on the main vessel
            PlayMakerFSM.BroadcastEvent("MP DRAIN");
            PlayMakerFSM.BroadcastEvent("MP LOSE");
            PlayMakerFSM.BroadcastEvent("MP RESERVE DOWN");

            //write geo
            HeroController.instance.geoCounter.geoTextMesh.text = data.savedPd.geo.ToString();

            //this kills enemies that were dead on the state, they respawn from previous code
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(data.savedSd), SceneData.instance);


            if (DebugMod.settings.EnemiesPanelVisible) EnemiesPanel.RefreshEnemyList();
            //UnityEngine.Object.Destroy(GameCameras.instance.gameObject);
            //yield return null;
            //DebugMod.GM.SetupSceneRefs();
            // need to redraw UI somehow

            //end the timer
            loadingStateTimer.Stop();
            loadingSavestate = false;
            TimeSpan loadingStateTime = loadingStateTimer.Elapsed;
            //formattings not working on this version idk
            string loadingtime = loadingStateTime.ToString();
            Console.AddLine("Loaded savestate in " + loadingtime);

            PlayerData.instance.hasXunFlower = false;
            PlayerData.instance.health = data.savedPd.health;
            HeroController.instance.TakeHealth(1);
            HeroController.instance.AddHealth(1);
            PlayerData.instance.hasXunFlower = data.savedPd.hasXunFlower;

            Time.timeScale = 1f;

            if (data.useRoomSpecific != 0) RoomSpecific.DoRoomSpecific(data.saveScene, data.useRoomSpecific);
            else if (DebugMod.settings.SaveStateGlitchFixes) SaveStateGlitchFixes(); //it breaks so many things for roomspecifics

            //Benchwarp fixes courtesy of homothety, needed since savestates are now performed while paused
            // Revert pause menu timescale

            GameManager.instance.FadeSceneIn();

            // We have to set the game non-paused because TogglePauseMenu sucks and UIClosePauseMenu doesn't do it for us.
            GameManager.instance.isPaused = false;
            //This allows the next pause to stop the game correctly, idk what the variable for 1221 api is
            //Time.TimeController.GenericTimeScale = 1f;x

            yield break;



        }

        //these are toggleable, as they will prevent glitches from persisting
        private void SaveStateGlitchFixes()
        {
            var rb2d = HeroController.instance.GetComponent<Rigidbody2D>();
            GameObject knight = GameObject.Find("Knight");
            PlayMakerFSM wakeFSM = knight.LocateMyFSM("Dream Return");
            PlayMakerFSM spellFSM = knight.LocateMyFSM("Spell Control");

            //White screen fixes
            wakeFSM.SetState("Idle");

            //float
            HeroController.instance.AffectedByGravity(true);
            rb2d.gravityScale = 0.79f;
            spellFSM.SetState("Inactive");

            //invuln
            HeroController.instance.gameObject.LocateMyFSM("Roar Lock").FsmVariables.FindFsmBool("No Roar").Value = false;
            HeroController.instance.cState.invulnerable = false;

            //no clip
            rb2d.isKinematic = false;

            //TODO: Fix this so it works
            //bench storage 
            GameManager.instance.SetPlayerDataBool(nameof(PlayerData.atBench), false);

            //if (HeroController.SilentInstance != null) (i cant find the equivalent for 1221 of silent instance)
            //{
                if (HeroController.instance.cState.onConveyor || HeroController.instance.cState.onConveyorV || HeroController.instance.cState.inConveyorZone)
                {
                    HeroController.instance.GetComponent<ConveyorMovementHero>()?.StopConveyorMove();
                    HeroController.instance.cState.inConveyorZone = false;
                    HeroController.instance.cState.onConveyor = false;
                    HeroController.instance.cState.onConveyorV = false;
                }

                HeroController.instance.cState.nearBench = false;
            //}
        }
        #endregion

        #region helper functionality

        public bool IsSet()
        {
            bool isSet = !String.IsNullOrEmpty(data.saveStateIdentifier);
            return isSet;
        }

        public string GetSaveStateID()
        {
            return data.saveStateIdentifier;
        }

        public string[] GetSaveStateInfo()
        {
            return new string[]
            {
                data.saveStateIdentifier,
                data.saveScene
            };

        }
        public SaveStateData DeepCopy()
        {
            return new SaveStateData(this.data);
        }
        
        #endregion
    }
}
