using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace DebugMod
{
    public static class RoomSpecific
    {
        //This class is intended to recreate some scenarios, with more accuracy than that of the savestate class. 
        //used to ensure no double loads.
        #region Rooms
        
        private static readonly float MAX_TIMESCALE_LAGGY = 6;
        private static readonly float MAX_TIMESCALE = 20;
        private static bool loading = false;
        private static void startCoro(IEnumerator routine)
        {
            if (loading) DebugMod.GM.StopCoroutine(coroRef);
            coroRef = DebugMod.GM.StartCoroutine(routine);
            loading = true;
        }
        private static void stopCoro() {
            GameManager.instance.hero_ctrl.RegainControl();
            if (loading) DebugMod.GM.StopCoroutine(coroRef);
            loading = false;
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

            Vector3 roomStartPos = new Vector3(8.156f, 58.5f);
            Vector3 activateLeftPos = new Vector3(23f, 58.5f);
            Vector3 activateRightPos = new Vector3(44f, 58.5f);
            Vector3 trappedPos = new Vector3(263.1f, 52.406f);

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
                Time.timeScale = afterTimeScale;

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
            loading = false;
            //auto break webs to normalize
        }
        private static Coroutine coroRef;
        private static void EnterSpiderTownTrap(int index) //Deepnest_Spider_Town
        {
            startCoro(SpiderTownHelper(index));
        }
        private static IEnumerator WaitForTime(float seconds, float scale)
        {
            float t = seconds + Time.time;
            Time.timeScale = scale;
            while (t > Time.time)
            {
                yield return new WaitUntil(() => t > Time.time | (Time.timeScale != scale));
                Time.timeScale = scale;
            }
            Time.timeScale = 1;
            loading = false;
        }
        private static void BreakTHKChains(int index)
        {
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
            startCoro(WaitForTime(13.5f, scale));
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
            DebugMod.HC.transform.position = new Vector3(263.1f, 52.406f);
        }
        private static void FastSoulMaster(int index)
        {
            string goName = "Mage Lord"; //soul master gameobject
            string fsmName = "Mage Lord";//soul master fsm
            if (index == 1)
            {
                //start phase 1
                DebugMod.HC.transform.position = new Vector3(19.5810f, 29.41113f); //make sure youre at the right spot ig?
                PlayMakerFSM fsm = FindFsmGlobally(goName, fsmName);
                fsm.SetState("Init");
            }
            else if(index == 2)
            {
                //start phase 2
                DebugMod.HC.transform.position = new Vector3(19.5810f, 29.41113f); //make sure youre at the right spot ig?
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
            stopCoro();
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
