using System.Collections;
using System.Linq;
using DebugMod;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SpeedRunQoL.Functionality

{
    public class ColoGoldWaveChanger
    {
        //Wave Preset isn't currently implemented in any useful way but could be
        private static int WavePreset = 0;
        private static int MaxPresets = 6;

        public static void SetWavePreset(int PresetNumber)
        {
            //probably not the most efficient way to declare min/max values for presets but i'm new dont @ me
            if (PresetNumber > MaxPresets)
            {
                Console.AddLine("Not a valid preset!");
                return;
            }

            else if (PresetNumber < 0)

            {
                Console.AddLine("Not a valid preset!");
                return;
            }

            else if (PresetNumber == 0)
            {
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SetColoGoldWavePreset;
                WavePreset = PresetNumber;
                Console.AddLine("Colo 3 Presets Off, reload room");
            }

            else
            {
                //Presets Stored Here
                switch (PresetNumber)
                {
                    //case 0 should never be called but its a redundancy in case binds are used in loads or something
                    case 0:
                        WaveStateName = "Wave 1";
                        Console.AddLine("Error, no preset selected");
                        break;

                    //Frogs 1
                    case 1:
                        WaveStateName = "Wave 9";
                        break;

                    //Sanctum
                    case 2:
                        WaveStateName = "Wave 22";
                        break;

                    //Mawlurks
                    case 3:
                        WaveStateName = "Wave 30";
                        break;

                    //Floorless
                    case 4:
                        WaveStateName = "GC Pause 1";
                        break;

                    //Final Waves
                    case 5:
                        WaveStateName = "Reset 6";
                        break;

                    //GodTamer
                    case 6:
                        WaveStateName = "Lancer Pause";
                        break;

                    default:
                        WaveStateName = "Wave 30";
                        Console.AddLine("Error, preset" + WavePreset + "not found, default");
                        break;
                }

                //if you're already in colos, we need you to reload to avoid messing terrain up and duping waves
                WavePreset = PresetNumber;
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SetColoGoldWavePreset;
                Console.AddLine("Colo Preset " + WavePreset + " Started, please load Colo 3");
            }
        }


        private static string WaveStateName = "Wave 1";

        private static string ColoGoldWaveScene = "Room_Colosseum_Gold";
        private static void SetColoGoldWavePreset(Scene sceneFrom, Scene sceneTo) => SetColoGoldWavePreset(sceneTo.name);
        private static void SetColoGoldWavePreset(string NextSceneName)
        {
            if (NextSceneName == ColoGoldWaveScene)
            {
                GameManager.instance.StartCoroutine(ColoGoldWavePresetCoro(NextSceneName));
            }
        }

        private static IEnumerator ColoGoldWavePresetCoro(string activeScene)
        {
            Console.AddLine("e");
            yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == activeScene);



            // Find Colo 3, the wave managment fsm, change idle to send the event to a specific wave corresponding to the preset
            Console.AddLine("debugging, fsm search start");
            GameObject waveController = GameObject.Find("Colosseum Manager");
            PlayMakerFSM fsm = waveController.LocateMyFSM("Battle Control");
            FsmState idle = fsm.FsmStates.First(t => t.Name == "Idle");
            idle.Transitions.First(tr => tr.EventName == "WAVES START").ToState = WaveStateName;
            yield break;

        }

        public static void debugwaves()
        {
            GameObject waveController = GameObject.Find("Colosseum Manager");
            PlayMakerFSM fsm = waveController.LocateMyFSM("Battle Control");
            fsm.SendEvent("WAVE END");
        }
    }
}