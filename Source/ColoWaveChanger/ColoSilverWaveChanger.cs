using System.Collections;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DebugMod.JankColoStuff
{
    public class ColoSilverWaveChanger
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
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SetColoSilverWavePreset;
                WavePreset = PresetNumber;
                Console.AddLine("Colo 2 Presets Off, reload room");
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

                    //Hoppers
                    case 1:
                        WaveStateName = "Hopper Arena";
                        break;

                    //Mimic Grub
                    case 2:
                        WaveStateName = "Wave 20";
                        break;

                    //Obbles
                    case 3:
                        WaveStateName = "Reset 4";
                        break;

                    //Oblobbles
                    case 4:
                        WaveStateName = "Arena Final Obble";
                        break;

                    default:
                        WaveStateName = "Wave 30";
                        Console.AddLine("Error, preset" + WavePreset + "not found, default");
                        break;
                }

                //if you're already in colos, we need you to reload to avoid messing terrain up and duping waves
                WavePreset = PresetNumber;
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SetColoSilverWavePreset;
                Console.AddLine("Colo Preset " + WavePreset + " Started, please load Colo 2");
            }
        }


        private static string WaveStateName = "Wave 1";

        private static string ColoSilverWaveScene = "Room_Colosseum_Silver";
        private static void SetColoSilverWavePreset(Scene sceneFrom, Scene sceneTo) => SetColoSilverWavePreset(sceneTo.name);
        private static void SetColoSilverWavePreset(string NextSceneName)
        {
            if (NextSceneName == ColoSilverWaveScene)
            {
                GameManager.instance.StartCoroutine(ColoSilverWavePresetCoro(NextSceneName));
            }
        }

        private static IEnumerator ColoSilverWavePresetCoro(string activeScene)
        {
            yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == activeScene);

            // Find Colo 3, the wave managment fsm, change idle to send the event to a specific wave corresponding to the preset
            Console.AddLine("debugging, fsm search start");
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