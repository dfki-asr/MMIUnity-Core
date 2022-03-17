﻿// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.

using UnityEngine;

namespace MMIUnity.Development
{
    /// <summary>
    /// Class displays the current frames per second
    /// </summary>
    public class FPSView : MonoBehaviour
    {
        private float deltaTime = 0.0f;

        void Update()
        {
            //Increment the delta time
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }


        void OnGUI()
        {
            int w = Screen.width, h = Screen.height;

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            GUI.Label(rect, text, style);
        }
    }

}
