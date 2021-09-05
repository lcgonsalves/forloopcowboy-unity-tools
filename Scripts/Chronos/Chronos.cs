using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Chronos
{

    public class Chronos : MonoBehaviour
    {

        [SerializeField, Tooltip("Duration and transition between regular time and fractional time.")]
        public Transition easeIn;

        [SerializeField, Tooltip("Duration and transition between regular time and fractional time.")]
        public Transition easeOut;

        // cached coroutine references

        private Coroutine easeInCoroutine = null;
        private Coroutine easeOutCoroutine = null;

        // Gradually warps time to the end of the transition.
        public void Warp(float warpFactor, bool interrupt = false)
        {
            if (interrupt) { StopCoroutine(easeInCoroutine); StopCoroutine(easeOutCoroutine); easeInCoroutine = null; easeOutCoroutine = null; }
            if (easeInCoroutine == null)
            {
                // in case coroutine was not cleared, clear it
                if (easeInCoroutine != null) StopCoroutine(easeInCoroutine);

                easeInCoroutine = easeIn.PlayOnce(
                    this,
                    state => {
                        float scaledFactor = Mathf.Lerp(1f, warpFactor, state.Snapshot());

                        Time.timeScale += scaledFactor;
                        Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);

                        Time.fixedDeltaTime = 0.02f * Time.timeScale;
                    }, 
                    _ => { easeInCoroutine = null; }
                );
            }
        }

        // Gradually returns time scale to normal to the end of the transition.
        public void Normalize(bool interrupt = true)
        {
            if (easeOutCoroutine == null)
            {
                // stop warping
                if (interrupt && easeInCoroutine != null) { StopCoroutine(easeInCoroutine); easeInCoroutine = null; }

                easeOutCoroutine = easeOut.PlayOnce(
                    this,
                    state => Time.timeScale = Mathf.Lerp(Time.timeScale, 1F, state.Snapshot()), 
                    _ => { easeOutCoroutine = null; }
                );
            }

        }

    }


}