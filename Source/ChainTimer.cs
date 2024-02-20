using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using Modding;
using System.Collections;
using MonoMod.RuntimeDetour;
using HutongGames.PlayMaker.Actions;
namespace DebugMod
{
    public static class ChainTimer
    {
        //toggleable logger bools
        public static bool logTFT = false;
        public static bool logChains = false;
        public static bool logWallJumps = false;
        public static bool logRepresses = false;

        public static Timer repressTimer = new Timer();

        public static Timer wallJumpTimer = new Timer();

        public static Timer jumpChainTimer = new Timer();
        
        private static List<string> outputText = [];
        private static bool modified = false;
        private static readonly int MAX_ENTRIES = 20;

        #region Dependencies
        private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly MethodInfo canJumpmi = typeof(HeroController).GetMethod("CanJump", flags);
        private static bool CanJump() => (bool)canJumpmi.Invoke(HeroController.instance, null);

        private static readonly MethodInfo canWallJumpmi = typeof(HeroController).GetMethod("CanWallJump", flags);
        private static bool CanWallJump() => (bool)canWallJumpmi.Invoke(HeroController.instance, null);

        private static readonly MethodInfo canDashmi = typeof(HeroController).GetMethod("CanDash", flags);
        private static bool CanDash() => (bool)canDashmi.Invoke(HeroController.instance, null);

        private static readonly FieldInfo isGameplayScenemi = typeof(HeroController).GetField("isGameplayScene", flags);

        private static bool IsGameplayScene() => (bool)isGameplayScenemi.GetValue(HeroController.instance);

        private static bool CanQueueInput() => DebugMod.HC.acceptingInput && !DebugMod.GM.isPaused && IsGameplayScene();

        private static bool CanJumpChain() => CanJump() && HeroController.instance.cState.touchingWall && !CanWallJump();

        private static bool CanDashChain() => CanDash() && HeroController.instance.cState.touchingWall && HeroController.instance.cState.onGround;

        #endregion

        #region UI & MonoBehaviour
        public class ChainTimerMonobehaviour : MonoBehaviour
        {
            private bool exists = false;
            private static string GetText() => string.Join("\n", outputText.ToArray());
            public void FixedUpdate()
            {
                ChainTimer.FixedUpdate();
            }
            
            public void Update()
            {
                if (logChains || logRepresses || logWallJumps)
                {
                    if (modified || !exists)
                    {
                        ShowAlert(GetText());
                        modified = false;
                        exists = true;
                    }
                }
                else if (exists)
                {
                    exists = false;
                    DestroyImmediate(displayedAlert);
                    DestroyImmediate(canvas);

                }
            }
            static IEnumerator Kill()
            {
                yield return new WaitForSeconds(2);
                DestroyImmediate(displayedAlert);
                DestroyImmediate(canvas);
            }
            static private GameObject displayedAlert;
            static private GameObject canvas;
            public void ShowAlert(string text) 
            {
                DestroyImmediate(displayedAlert);
                DestroyImmediate(canvas); 
                canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, 100);
                var textPanel = CanvasUtil.CreateTextPanel(
                    canvas,
                    text,
                    20,
                    TextAnchor.LowerRight,
                    new CanvasUtil.RectData(
                        new Vector2(1920, 1080),
                        new Vector2(-40, 40),
                        new Vector2(1, 0),
                        new Vector2(1, 0),
                        new Vector2(1, 0)
                    )
                );
                DontDestroyOnLoad(canvas);
                var canvasGroup = textPanel.AddComponent<CanvasGroup>();
                displayedAlert = textPanel;
                canvasGroup.alpha = 1;
                displayedAlert.SetActive(true);
            }
        }
        public static void AddLine(string text)
        {
            modified = true;
            if (outputText.Count >= MAX_ENTRIES)
            {
                outputText.RemoveAt(0);
            }
            outputText.Add(text);
        }
        public static void ClearLines()
        {
            modified = true;
            outputText.Clear();
            outputText.TrimExcess();
        }

        public class Timer()
        {
            private bool running = false;
            public float time = 0;
            public int frame = 1;
            public int graphicsFrames = 0;
            public List<float> timesList = [];
            public List<int> framesList = [];
            public bool stoppedNow = false;
            public void FixedTick()
            {
                if (running) frame++;
            }
            public void Tick(bool cond, float time)
            {
                stoppedNow = false;
                if (cond)
                {
                    if (!running) ResetAndStart();
                    this.time += time;
                    graphicsFrames++;
                }
                else if (!cond && running)
                {
                    stoppedNow = true;
                }
            }
            public void Stop()
            {
                running = false;
            }
            public void Print(string name, bool showAvg = false)
            {
                string message = name;
                message += " | " + time.ToString("F4") + "s, " + frame.ToString() + "f";//+ graphicsFrames.ToString + "phys frames";
                if (showAvg)
                {
                    message += "Average |  " + AverageTime().ToString() + "s, " + AverageFrames().ToString();
                }
                AddLine(message);
            }
            public void SaveTime()
            {
                //timesList.Add(time);
                //framesList.Add(frame);
            }
            private void ResetAndStart()
            {
                time = 0;
                graphicsFrames = 0;
                frame = 1;
                running = true;
            }
            public void ClearRecord()
            {
                graphicsFrames = 0;
                time = 0;
                frame = 0;
                running = false;
                stoppedNow = false;
                timesList.Clear();
                timesList.TrimExcess();
                framesList.Clear();
                framesList.TrimExcess();
            }
            private double AverageFrames() => framesList.Average();
            private double AverageTime() => timesList.Average();
        }
        #endregion

        #region Hook QueueInput
        public static void Setup()
        {
            CanvasUtil.CreateFonts();
            Hook hookinst = new Hook(
                typeof(HeroController).GetMethod("LookForQueueInput", BindingFlags.NonPublic | BindingFlags.Instance),
                QueuedInputHook);
            GameManager.instance.gameObject.AddComponent<ChainTimerMonobehaviour>();
        }

        private static void QueuedInputHook(Action<HeroController> orig, HeroController self)
        {
            Update();
            orig(self);
        }
        #endregion

        #region Call Loggers
        private static void FixedUpdate()
        {
            if (logChains) jumpChainTimer.FixedTick();
            if (logRepresses) repressTimer.FixedTick();
            if (logWallJumps) wallJumpTimer.FixedTick();
        }
        private static void Update()
        {
            if (logTFT) LogTFT();
            if (!CanQueueInput()) return;
            if (logChains) GraphicsChains();
            if (logRepresses) LogRepresses();
            if (logWallJumps) LogWallJumps();
        }
        #endregion

        #region Log Represses
        private static void LogRepresses()
        {
            string text = "Repress";
            bool timeCond = !DebugMod.IH.inputActions.jump.IsPressed;
            repressTimer.Tick(timeCond, Time.deltaTime);
            if (repressTimer.stoppedNow)
            {
                repressTimer.Stop();
                if (repressTimer.time < .15f)
                {
                    repressTimer.SaveTime();
                    repressTimer.Print(text);
                }
            }
        }
        #endregion

        #region Log Walljumps
        private static void LogWallJumps()
        {
            string text = "Walljump Jump Input";
            bool didInput = DebugMod.IH.inputActions.jump.IsPressed;
            bool timeCond = CanWallJump();
            wallJumpTimer.Tick(timeCond, Time.deltaTime);
            if (wallJumpTimer.stoppedNow)
            {
                wallJumpTimer.Stop();
                if (didInput && wallJumpTimer.time<.5f)
                {
                    wallJumpTimer.SaveTime();
                    wallJumpTimer.Print(text);
                }
                /*else
                {
                    wallJumpTimer.Print(text + " Failed");
                }*/
            }
        }
        #endregion

        #region Log Chains
        private static void GraphicsChains()
        {
            #region Jump Chain Graphics
            //Jump Chain Graphics
            string text = "Jump Chain ";
            bool didInput = DebugMod.IH.inputActions.jump.IsPressed;
            bool timeCond = CanJumpChain();
            jumpChainTimer.Tick(timeCond, Time.deltaTime);
            if (jumpChainTimer.stoppedNow)
            {
                jumpChainTimer.Stop();
                if (didInput)
                {
                    jumpChainTimer.Print(text + " Succeded, Frame Pressed:");
                }
                else
                {
                    jumpChainTimer.SaveTime();
                    jumpChainTimer.Print(text + " Failed, Frame Window:");
                }
            }
            #endregion
            #region Dash Chain Graphics
            /*Dash Chain Graphics
            if (CanDashChain())
            {
                if (!lastFrameDashChainableGraphics)
                {   //Start Timer
                    lastFrameDashChainableGraphics = true;
                    timeCanDashGraphics = Time.deltaTime;
                }
            }
            else
            {
                if (lastFrameDashChainableGraphics)
                {
                    //End Timer if running
                    lastFrameDashChainableGraphics = false;
                    //windowDurationDashGraphics = Time.deltaTime - timeCanDashGraphics;
                }
            }
            */
            #endregion
        }
        #endregion
        
        #region Log TFT
        public static List<float> tftTimesGraphics = [];
        public static void LogTFT()
        {
            tftTimesGraphics.Add(Time.deltaTime - Time.fixedDeltaTime);
        }
        #endregion
    }
}