// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer, Andreas Kaiser

using UnityEngine;

namespace MMIAdapterUnity
{
    /// <summary>
    /// Implementation of a logger which outputs the text on the unity console
    /// </summary>
    public class UnityLogger : MMICSharp.Logger
    {

        /// <summary>
        /// Flag which specifies whether a server build is utilized
        /// </summary>
        private bool isServerBuild = false;

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="isServerBuild"></param>
        public UnityLogger(bool isServerBuild = false)
        {
            this.isServerBuild = isServerBuild;
        }

		protected override void CreateDebugLog(string text)
        {
            if (isServerBuild)
                base.CreateDebugLog(text);
            else
                Debug.Log(text);
            
        }

        protected override void CreateErrorLog(string text)
        {
            if (isServerBuild)
                base.CreateErrorLog(text);
            else
                Debug.LogError(text);
        }

        protected override void CreateInfoLog(string text)
        {
            if (isServerBuild)
                base.CreateInfoLog(text);
            else
                Debug.Log(text);
        }

		protected override void CreateWarningLog(string text)
		{
			if (isServerBuild)
				base.CreateWarningLog(text);
			else
				Debug.LogWarning(text);
		}
	}
}
