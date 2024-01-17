using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugMod
{
    public class ChainTimer : MonoBehaviour
    {
        //toggleable logger bools
        public bool logTFTGraphics = false;
        public bool logTFTPhysics = false;
        public bool LogChainsGraphics = false;
        public bool LogChainsPhysics = false;
        #region Dependencies
        static readonly MethodInfo CanJumpPriv = typeof(HeroController).GetMethod("CanJump", BindingFlags.NonPublic | BindingFlags.Instance);
        //public static readonly MethodInfo CanDashPriv = typeof(HeroController).GetMethod("CanDash");
        private bool CanJump()
        {
            bool canjump = (bool)CanJumpPriv.Invoke(HeroController.instance, null);
            return canjump && HeroController.instance.CanInput();
        }
        //private bool CanDash()
        //{
        //    return (bool)CanDashPriv.Invoke(HeroController.instance, null);
        //}
        private bool CanJumpChain() { return CanJump() && HeroController.instance.cState.touchingWall; }

        //private bool CanDashChain() { return CanDash() && HeroController.instance.cState.touchingWall && !HeroController.instance.cState.wallSliding; }
        #endregion

        #region Call Loggers
        public void Update()
        {
            if (DebugMod.GM.isPaused) return;
            if (LogChainsGraphics) GraphicsChains();
            if (logTFTGraphics) GraphicsTFT();
        }
        public void FixedUpdate()
        {
            if (DebugMod.GM.isPaused) return;
            if (LogChainsPhysics) PhysicsChains();
            if (logTFTPhysics) PhysicsTFT();
        }
        #endregion

        #region Log Chains
        bool lastFrameChainableGraphics = false;
        public List<float> timesJumpChainGraphics = [];
        public List<float> framesJumpChainGraphics = [];
        int JumpFramesElapsedGraphics;
        float JumpTimeElapsedGraphics;
        private void GraphicsChains()
        {
            #region Jump Chain Graphics
            //Jump Chain Graphics
            if (CanJumpChain())
            {
                if (!lastFrameChainableGraphics)
                {   //Start Timer
                    lastFrameChainableGraphics = true;
                    JumpTimeElapsedGraphics = 0;
                    JumpFramesElapsedGraphics = 0;
                }
                JumpTimeElapsedGraphics += Time.deltaTime;
                JumpFramesElapsedGraphics += 1;
            }
            else
            {
                if (lastFrameChainableGraphics)
                {
                    //End Timer if running
                    lastFrameChainableGraphics = false;
                    timesJumpChainGraphics.Add(JumpTimeElapsedGraphics);
                    framesJumpChainGraphics.Add(JumpFramesElapsedGraphics);
                    Console.AddLine("Graphics: " + JumpTimeElapsedGraphics.ToString() + "s, " + JumpFramesElapsedGraphics.ToString() + "f");
                    Console.AddLine("Graphics Average: " + timesJumpChainGraphics.Average().ToString() + "s, " + framesJumpChainGraphics.Average().ToString() + "f");
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
        //private float timeUnclingPhysics = 0;
        bool lastFrameChainablePhysics = false; 
        //bool lastFrameDashChainablePhysics = false;
        public List<float> timesJumpChainPhysics = [];
        public List<float> framesJumpChainPhysics = [];
        int JumpFramesElapsedPhysics;
        float JumpTimeElapsedPhysics;
        private void PhysicsChains() 
        {
            #region Jump Chain Physics
            //Jump Chain Physics
            if (CanJumpChain())
            {
                if (!lastFrameChainablePhysics)
                {   //Start Timer
                    lastFrameChainablePhysics = true;
                    JumpTimeElapsedPhysics = 0;
                    JumpFramesElapsedPhysics = 0;
                }
                JumpTimeElapsedPhysics += Time.fixedDeltaTime;
                JumpFramesElapsedPhysics += 1;
            }
            else
            {
                if (lastFrameChainablePhysics)
                {
                    //End Timer if running
                    lastFrameChainablePhysics = false;
                    timesJumpChainPhysics.Add(JumpTimeElapsedPhysics);
                    framesJumpChainPhysics.Add(JumpFramesElapsedPhysics);
                    Console.AddLine("Physics: " + JumpTimeElapsedPhysics.ToString() + "s, frames: " +JumpFramesElapsedPhysics.ToString());
                    Console.AddLine("Physics Average: " + timesJumpChainPhysics.Average().ToString() + "s, " + framesJumpChainPhysics.Average().ToString());
                }
            }
            #endregion
            #region Dash Chain Physics
            /*Dash Chain Physics
            if (CanDashChain())
            {
                if (!lastFrameDashChainablePhysics)
                {   //Start Timer
                    lastFrameDashChainablePhysics = true;
                    timeCanDashPhysics = Time.deltaTime;
                }
            }
            else
            {
                if (lastFrameDashChainablePhysics)
                {
                    //End Timer if running
                    lastFrameDashChainablePhysics = false;
                    //windowDurationDashPhysics = Time.deltaTime - timeCanDashPhysics;
                }
            }
            */
            #endregion
        }
        #endregion

        #region Log TFT
        public List<float> tftTimesGraphics = [];
        public void GraphicsTFT()
        {
            tftTimesGraphics.Add(Time.deltaTime - Time.fixedDeltaTime);
        }
        public List<float> tftTimesPhysics = [];
        public void PhysicsTFT()
        {
            tftTimesPhysics.Add(Time.deltaTime - Time.fixedDeltaTime);
        }
        #endregion
    }
}