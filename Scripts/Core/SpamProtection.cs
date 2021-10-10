using System;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public class SpamProtection
    {
        public float debounceInSeconds = 0.5f;
        public DateTime lastExecution { get; private set; }
        
        public SpamProtection(float debounce)
        {
            debounceInSeconds = debounce;
        }

        /// <summary>
        /// Executes function if enough time has passed
        /// since the last execution.
        /// </summary>
        /// <param name="fn">Function to be executed</param>
        /// <param name="returnValue">Return value of the function, if it was executed</param>
        /// <typeparam name="T">Return type of function.</typeparam>
        /// <returns>True if successfully executed, false if debounced.</returns>
        public bool SafeExecute<T>(Func<T> fn, out T returnValue)
        {
            DateTime now = DateTime.Now;
            bool canExecute = (now - lastExecution).TotalSeconds >= debounceInSeconds;
            if (canExecute)
            {
                lastExecution = now;
                returnValue = fn();
            }
            else returnValue = default(T);
            
            return canExecute;
        }
        
        /// <summary>
        /// Executes function if enough time has passed
        /// since the last execution.
        /// </summary>
        /// <param name="fn">Function to be executed</param>
        /// <returns>True if successfully executed, false if debounced.</returns>
        public bool SafeExecute(Action fn)
        {
            DateTime now = DateTime.Now;
            bool canExecute = (now - lastExecution).TotalSeconds >= debounceInSeconds;
            if (canExecute)
            {
                lastExecution = now; 
                fn();
            }
            
            return canExecute;
        }
    }
}