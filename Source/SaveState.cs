using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HutongGames.PlayMaker.Actions;
using MonoMod.Utils;
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
                // no 
                //DebugMod.instance.Log("prep filepath: " + data.filePath);

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

            //prevents silly things from happening
            Time.timeScale = 0;

            //timer for loading savestates, used in diagnostic purposes
            System.Diagnostics.Stopwatch loadingStateTimer = new System.Diagnostics.Stopwatch();
            loadingStateTimer.Start();

            int oldMPReserveMax = PlayerData.instance.MPReserveMax;
            int oldMP = PlayerData.instance.MPCharge;

            //if (DebugMod.CurrentHazardCoro != null) HeroController.instance.StopCoroutine(DebugMod.CurrentHazardCoro);
            //if (DebugMod.CurrentInvulnCoro != null) HeroController.instance.StopCoroutine(DebugMod.CurrentInvulnCoro);
            //DebugMod.CurrentHazardCoro = null;
            //DebugMod.CurrentInvulnCoro = null;

            //fixes knockback storage
            typeof(HeroController).GetMethod("CancelDamageRecoil", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(HeroController.instance, []);

            //ends hazard respawn animation
            var invPulse = HeroController.instance.GetComponent<InvulnerablePulse>();
            invPulse.stopInvulnerablePulse();

            //remove dialogues if exists
            PlayMakerFSM.BroadcastEvent("BOX DOWN DREAM");
            PlayMakerFSM.BroadcastEvent("CONVO CANCEL");

            data.cameraLockArea = (data.cameraLockArea ?? typeof(CameraController).GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic));
            string dummyScene = "Room_Mender_House";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Room_Mender_House") dummyScene = "Room_Sly_Storeroom";

            GameManager.instance.ChangeToScene(dummyScene, "", 0f);//trust

            yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != dummyScene);

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(data.savedSd), SceneData.instance);
            GameManager.instance.ResetSemiPersistentItems();

            yield return null;
            HeroController.instance.gameObject.transform.position = data.savePos;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(data.savedPd), PlayerData.instance);
            GameManager.instance.ChangeToScene(data.saveScene, "", 0f);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(data.savedSd), SceneData.instance);
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
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-HeroLanded");
            HeroAnimationController component = HeroController.instance.GetComponent<HeroAnimationController>();
            typeof(HeroAnimationController).GetField("pd", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, PlayerData.instance);

            if (data.facingRight == 1) HeroController.instance.FaceRight();
            else if (data.facingRight == 0) HeroController.instance.FaceLeft();
            HeroController.instance.SetHazardRespawn(data.savePos, HeroController.instance.cState.facingRight);
            //This is all the shit that fixes lp debug
            GameManager.instance.cameraCtrl.FadeSceneIn();

            HeroController.instance.CharmUpdate();

            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");    //update twister             
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");       //update nail

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


            Time.timeScale = 1f;
            
            typeof(HeroController).GetMethod("FinishedEnteringScene",BindingFlags.Instance | BindingFlags.NonPublic).Invoke(HeroController.instance, [true]);
            HUDFixes();
            if (DebugMod.settings.SaveStateGlitchFixes) SaveStateGlitchFixes();
            RoomSpecific.StopCoros();
            if (data.useRoomSpecific != 0) RoomSpecific.DoRoomSpecific(data.saveScene, data.useRoomSpecific);


            // We have to set the game non-paused because TogglePauseMenu sucks and UIClosePauseMenu doesn't do it for us.
            GameManager.instance.isPaused = false;
            //This allows the next pause to stop the game correctly, idk what the variable for 1221 api is
            //Time.TimeController.GenericTimeScale = 1f;x
           
            yield break;



        }
        private void HUDFixes()
        {

            GameCameras.instance.hudCanvas.gameObject.SetActive(true);

            HeroController.instance.geoCounter.geoTextMesh.text = data.savedPd.geo.ToString();

            bool isInfiniteHp = DebugMod.infiniteHP;
            DebugMod.infiniteHP = false;
            PlayerData.instance.hasXunFlower = false;
            PlayerData.instance.health = data.savedPd.health;
            HeroController.instance.TakeHealth(1);
            HeroController.instance.AddHealth(1);
            PlayerData.instance.hasXunFlower = data.savedPd.hasXunFlower;
            DebugMod.infiniteHP = isInfiniteHp;

            int healthBlue = data.savedPd.healthBlue;
            for (int i = 0; i < healthBlue; i++)
            {
                PlayMakerFSM.BroadcastEvent("ADD BLUE HEALTH");
            }

            //should fix hp
            //the "Idle" mesh never gets disabled when Charm Indicator runs
            //running the animation quickly would work but this does too and i understand it
            int maxHP = GameManager.instance.playerData.maxHealth;
            for (int i = maxHP; i > 0; i--)
            {
                GameObject health = GameObject.Find("Health " + i.ToString());
                PlayMakerFSM fsm = health.LocateMyFSM("health_display");
                Transform idle = health.transform.Find("Idle");
                //This is the "HP Full" mesh, which covers the empty mesh
                idle.gameObject.GetComponent<MeshRenderer>().enabled = false;
                //This is the "HP Empty" mesh
                health.GetComponent<MeshRenderer>().enabled = true;
                //This turns back on the "HP Full" mesh if we should
                fsm.SetState("Check if Full");
            }

            if (PlayerData.instance.MPReserveMax < 33) GameObject.Find("Vessel 1").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 1").LocateMyFSM("vessel_orb").SetState("Up Check");
            if (PlayerData.instance.MPReserveMax < 66) GameObject.Find("Vessel 2").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 2").LocateMyFSM("vessel_orb").SetState("Up Check");
            if (PlayerData.instance.MPReserveMax < 99) GameObject.Find("Vessel 3").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 3").LocateMyFSM("vessel_orb").SetState("Up Check");
            if (PlayerData.instance.MPReserveMax < 132) GameObject.Find("Vessel 4").LocateMyFSM("vessel_orb").SetState("Init");
            else GameObject.Find("Vessel 4").LocateMyFSM("vessel_orb").SetState("Up Check");

            HeroController.instance.TakeMP(1);
            HeroController.instance.AddMPChargeSpa(1);

            PlayMakerFSM.BroadcastEvent("MP DRAIN");
            PlayMakerFSM.BroadcastEvent("MP LOSE");
            PlayMakerFSM.BroadcastEvent("MP RESERVE DOWN");

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
            //errors out idk why
            //HeroController.instance.gameObject.LocateMyFSM("Roar Lock").FsmVariables.FindFsmBool("No Roar").Value = false;
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
            //}*/
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
            return
            [
                data.saveStateIdentifier,
                data.saveScene
            ];

        }
        public SaveStateData DeepCopy()
        {
            return new SaveStateData(this.data);
        }
        
        #endregion
    }
}
