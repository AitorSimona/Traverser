using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System;

namespace Unity.Kinematica
{
    public static partial class DebugDraw
    {
        public static int CreateMovableText(float2 position)
        {
            return TextDrawerBehavior.instance.CreateMovableText(position);
        }

        public static void DestroyMovableText(int identifier)
        {
            TextDrawerBehavior.instance.DestroyMovableText(identifier);
        }

        public static void ClearMovableText(int identifier)
        {
            TextDrawerBehavior.instance.ClearMovableText(identifier);
        }

        public static void SetMovableTextTitle(int identifier, string title)
        {
            TextDrawerBehavior.instance.SetMovableTextTitle(identifier, title);
        }

        public static void AddMovableTextLine(int identifier, string line, Color color)
        {
            TextDrawerBehavior.instance.AddMovableTextLine(identifier, line, color);
        }

        public static Rect GetMovableTextRect(int identifier)
        {
            return TextDrawerBehavior.instance.GetMovableTextRect(identifier);
        }
    }
}
