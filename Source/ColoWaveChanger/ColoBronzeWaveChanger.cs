using System.Collections;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DebugMod.JankColoStuff
{
    public class ColoBronzeWaveChanger
    {
        //Wave Preset isn't currently implemented in any useful way but could be
        private static int WavePreset = 0;
        private static int MaxPresets = 4;

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
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SetColoBronzeWavePreset;
                WavePreset = PresetNumber;
                Console.AddLine("Colo 1 Presets Off, reload room");
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

                    //Aspids
                    case 1:
                        WaveStateName = "Arena 2";
                        break;

                    //Baldurs
                    case 2:
                        WaveStateName = "Wave 22";
                        break;

                    //Gruzzers
                    case 3:
                        WaveStateName = "Gruz Arena";
                        break;

                    //Zote
                    case 4:
                        WaveStateName = "Wave 29 Zote";
                        break;

                    default:
                        WaveStateName = "Wave 30";
                        Console.AddLine("Error, preset" + WavePreset + "not found, default");
                        break;
                }

                //if you're already in colos, we need you to reload to avoid messing terrain up and duping waves
                WavePreset = PresetNumber;
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SetColoBronzeWavePreset;
                Console.AddLine("Colo Preset " + WavePreset + " Started, please load Colo 1");
            }
        }


        private static string WaveStateName = "Wave 1";

        private static string ColoBronzeWaveScene = "Room_Colosseum_Bronze";
        private static void SetColoBronzeWavePreset(Scene sceneFrom, Scene sceneTo) => SetColoBronzeWavePreset(sceneTo.name);
        private static void SetColoBronzeWavePreset(string NextSceneName)
        {
            if (NextSceneName == ColoBronzeWaveScene)
            {
                GameManager.instance.StartCoroutine(ColoBronzeWavePresetCoro(NextSceneName));
            }
        }

        private static IEnumerator ColoBronzeWavePresetCoro(string activeScene)
        {
            yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == activeScene);
            // Find Colo 3, the wave managment fsm, change idle to send the event to a specific wave corresponding to the preset
            GameObject waveController = GameObject.Find("Colosseum Manager");
            PlayMakerFSM fsm = waveController.LocateMyFSM("Battle Control");
            FsmState idle = fsm.FsmStates.First(t => t.Name == "Idle");
            idle.Transitions.First(tr => tr.EventName == "WAVES START").ToState = WaveStateName;
            yield break;

        }

        public static void DebugWaves()
        {
            GameObject waveController = GameObject.Find("Colosseum Manager");
            PlayMakerFSM fsm = waveController.LocateMyFSM("Battle Control");
            fsm.SendEvent("WAVE END");
        }
    }
}