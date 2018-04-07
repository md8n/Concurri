using System;
using RulEng.Helpers;

namespace RulEng.States
{
    /// <summary>
    /// The base class for Rule, Operation and Request
    /// </summary>
    public abstract class BaseExecutableEntity
    {
        /// <summary>
        /// The last time this Entity was executed
        /// </summary>
        public DateTime LastExecuted { get; set; }

        public BaseExecutableEntity()
        {
            LastExecuted = DefaultHelpers.DefDate();
        }
    }
}
