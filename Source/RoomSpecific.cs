using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DebugMod
{
    public static class RoomSpecific
    {
        //This class is intended to recreate some scenarios, with more accuracy than that of the savestate class. 
        #region Rooms
        
        private static readonly float MAX_TIMESCALE_LAGGY = 6;
        private static readonly float MAX_TIMESCALE = 20;
                private static List<Coroutine> coroRefs =[];
        private static void StartCoro(IEnumerator routine)
        {
            coroRefs.Add(DebugMod.GM.StartCoroutine(routine));
        }
        public static void StopCoros() { 
            coroRefs.ForEach(DebugMod.GM.StopCoroutine);
            coroRefs = [];
        }
        private static IEnumerator SpiderTownHelper(int index)
        {
            float beforeFirstSpider = 1.39f;//all from roomsob
            float activateLeftTime = 2.02f;
            float activateRightTime = 26.64f;
            float lastSlashTime = 27.73f;

            float delay1 = beforeFirstSpider;
            float delay2 = activateLeftTime - beforeFirstSpider;
            float delay3 = activateRightTime - activateLeftTime;
            float delay4 = lastSlashTime - activateRightTime;

            float afterTimeScale = index; //janky but idc

            Vector2 roomStartPos = new(8.156f, 58.5f);
            Vector2 activateLeftPos = new(23f, 58.5f);
            Vector2 activateRightPos = new(44f, 58.5f);
            Vector2 trappedPos = new(263.1f, 52.406f);

            string goName = "RestBench Spider";
            string websFsmName = "Fade";
            string benchFsmName = "Bench Control Spider";

            DebugMod.HC.transform.position = roomStartPos;

            PlayMakerFSM websFSM = FindFsmGlobally(goName, websFsmName);
            PlayMakerFSM benchFSM = FindFsmGlobally(goName, benchFsmName);

            if (afterTimeScale > MAX_TIMESCALE_LAGGY) afterTimeScale = MAX_TIMESCALE_LAGGY;
            if (index >= 1)
            {
                GameManager.instance.hero_ctrl.RelinquishControl();
                WaitForTime(lastSlashTime, afterTimeScale);

                GameManager.instance.isPaused = false;
                
                DebugMod.HC.transform.position = roomStartPos;
                yield return new WaitForSeconds(delay1);
                DebugMod.HC.transform.position = activateLeftPos;
                yield return new WaitForSeconds(delay2);
                DebugMod.HC.transform.position = activateRightPos;
                yield return new WaitForSeconds(delay3);
                DebugMod.HC.transform.position = trappedPos;
            }
            else DebugMod.HC.transform.position = trappedPos;

            benchFSM.SetState("Start Rest");
            benchFSM.SendEvent("WAIT");
            benchFSM.SendEvent("FINISHED");
            benchFSM.SendEvent("STRUGGLE");
            websFSM.SendEvent("FIRST STRUGGLE");
            websFSM.SendEvent("FINISHED");
            websFSM.SendEvent("FINISHED");
            websFSM.SendEvent("FINISHED");
            websFSM.SendEvent("LAND");
            websFSM.SendEvent("FINISHED");
            websFSM.SendEvent("FINISHED"); //now can wiggle n stuff
            websFSM.FsmVariables.GetFsmInt("Struggles").Value = 4;
            if (index >= 1)
            {
                yield return new WaitForSeconds(delay4);
                websFSM.SetState("Struggle");
                Time.timeScale = 1f;
            }
            //auto break webs to normalize
        }
        private static void EnterSpiderTownTrap(int index) //Deepnest_Spider_Town
        {
            StartCoro(SpiderTownHelper(index));
        }
        private static void BreakTHKChains(int index) //Room_Final_Boss
        {
            StartCoro(BreakTHKChainscoro(index));
        }
        private static IEnumerator BreakTHKChainscoro(int index)
        {
            float time = 14.2f;
            float scale = index;
            if (index < 5) { scale = 5; }
            if (index > MAX_TIMESCALE) scale = MAX_TIMESCALE;
            string fsmName = "Control";
            string goName1 = "hollow_knight_chain_base";
            string goName2 = "hollow_knight_chain_base 2";
            string goName3 = "hollow_knight_chain_base 3";
            string goName4 = "hollow_knight_chain_base 4";
            PlayMakerFSM fsm1 = FindFsmGlobally(goName1, fsmName);
            PlayMakerFSM fsm2 = FindFsmGlobally(goName2, fsmName);
            PlayMakerFSM fsm3 = FindFsmGlobally(goName3, fsmName);
            PlayMakerFSM fsm4 = FindFsmGlobally(goName4, fsmName);
            fsm1.SetState("Break");
            fsm2.SetState("Break");
            fsm3.SetState("Break");
            fsm4.SetState("Break");
            bool right = HeroController.instance.cState.facingRight;
            Vector2 current = DebugMod.HC.transform.position;
            DebugMod.HC.transform.position = new Vector2(27.4200f, 6.410425f);
            WaitForTime(1, scale);
            yield return new WaitForSeconds(1);
            DebugMod.HC.transform.position = current;
            if (right)
            {
                HeroController.instance.FaceRight();
            }
            else
            {
                HeroController.instance.FaceLeft();
            }
            WaitForTime(time-1, scale);
            yield return new WaitForSeconds(time-1);
            Time.timeScale = 1f;
        } //Room_Final_Boss
        private static void ObtainDreamNail(int index)
        {
            string goName = "Witch Control";
            string fsmName = "Control";
            PlayMakerFSM fsm = FindFsmGlobally(goName, fsmName);
            fsm.SetState("Pause");
            fsm.SendEvent("FINISHED");
            fsm.SendEvent("DREAM WAKE");
            fsm.SendEvent("FINISHED");
            fsm.SendEvent("FINISHED");
            fsm.SendEvent("ZONE 1");
            fsm.SendEvent("ZONE 2");
            fsm.SendEvent("ZONE 3");
            fsm.SendEvent("FINISHED");
            DebugMod.HC.transform.position = new Vector2(263.1f, 52.406f);
        }
        private static void WaitForTime(float seconds, float scale)
        {
            StartCoro(WaitForTimeCoro(seconds,scale));
        }
        private static IEnumerator WaitForTimeCoro(float seconds, float scale)
        {
            float t = seconds + Time.time;
            Time.timeScale = scale;
            while (t > Time.time)
            {
                yield return new WaitUntil(() => t > Time.time | (Time.timeScale != scale));
                Time.timeScale = scale;
            }
            Time.timeScale = 1;
        }
        private static void FastSoulMaster(int index)
        {
            string goName = "Mage Lord"; //soul master gameobject
            string fsmName = "Mage Lord";//soul master fsm
            if (index == 1)
            {
                //start phase 1
                DebugMod.HC.transform.position = new Vector2(19.5810f, 29.41113f); //make sure youre at the right spot ig?
                PlayMakerFSM fsm = FindFsmGlobally(goName, fsmName);
                fsm.SetState("Init");
            }
            else if(index == 2)
            {
                //start phase 2
                DebugMod.HC.transform.position = new Vector2(19.5810f, 29.41113f); //make sure youre at the right spot ig?
                string quakegoname= "Quake Fake Parent";
                string quakefsmname = "Appear";
                PlayMakerFSM fsm = FindFsmGlobally(goName, fsmName);
                PlayMakerFSM quakeFakeFSM = FindFsmGlobally(quakegoname, quakefsmname);
                fsm.SetState("Init"); //to close gate and avoid save shenanigans
                GameObject.Destroy(GameObject.Find("Mage Lord"));
                quakeFakeFSM.SendEvent("QUAKE FAKE APPEAR");
            }
        }
        private static void FastDreamerCutscene(int index)
        {
            if (index == 1)
            {
                string goName ="Mask Break Cutscene";
                string fsmName = "Control";
                PlayMakerFSM CutsceneFSM = FindFsmGlobally(goName, fsmName);
                CutsceneFSM.SetState("Fade");
            }
        }

        #endregion

        public static void DoRoomSpecific(string scene, int index)
        {
            //GameManager.instance.hero_ctrl.RegainControl();
            if (index == 0) return;
            switch (scene)
            {
                case "Deepnest_Spider_Town":
                    EnterSpiderTownTrap(index);
                    break;
                case "Room_Final_Boss_Core":
                    BreakTHKChains(index);
                    break;
                case "Dream_NailCollection":
                    ObtainDreamNail(index);
                    break;
                case "Ruins1_24":
                    FastSoulMaster(index);
                    break;
                case "Cutscene_Boss_Door":
                    FastDreamerCutscene(index); 
                    break;
                default:
                    Console.AddLine("No Room Specific Function Found In: " + scene);
                    break;
            }
        }
        private static PlayMakerFSM FindFsmGlobally(string gameObjectName, string fsmName)
        {
            return GameObject.Find(gameObjectName).LocateMyFSM(fsmName);
        }
    }
}
