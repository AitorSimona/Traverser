using System;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    [Flags]
    public enum DebugDrawFlags
    {
        Trajectory = 1 << 0,
        InputFragment = 1 << 1,
        BestFragment = 1 << 2,
        SelectedFragment = 1 << 3,
        OutputFragment = 1 << 4,
    };

    public struct DebugDrawOptions
    {
        public float timeOffset;
        public float distanceOffset;
        public int textWindowIdentifier;
        public DebugDrawFlags drawFlags;

        public Color inputTrajectoryColor;
        public Color inputOutputFragTextColor;
        public Color selectedFragTextColor;
        public Color bestFragTextColor;

        public Color inputOutputFragmentColor;
        public Color selectedFragmentColor;
        public Color bestFragmentColor;

        public float graphColorModifier;

        public static DebugDrawOptions Create()
        {
            return new DebugDrawOptions()
            {
                timeOffset = 0.0f,
                distanceOffset = 1.0f,
                textWindowIdentifier = -1,
                drawFlags = DebugDrawFlags.Trajectory | DebugDrawFlags.BestFragment | DebugDrawFlags.SelectedFragment,
                inputTrajectoryColor = new Color(0.0f, 1.0f, 0.5f, 0.75f),
                inputOutputFragTextColor = new Color(0.9f, 0.65f, 0.65f, 1.0f),
                selectedFragTextColor = new Color(250.0f / 255.0f, 233.0f / 255.0f, 41.0f / 255.0f),
                bestFragTextColor = new Color(43.0f / 255.0f, 189.0f / 255.0f, 249.0f / 255.0f),
                inputOutputFragmentColor = new Color(1.0f, 0.25f, 0.25f),
                selectedFragmentColor = new Color(250.0f / 255.0f, 233.0f / 255.0f, 41.0f / 255.0f),
                bestFragmentColor = new Color(0.35f, 0.35f, 1.0f),
                graphColorModifier = 1.2f,
            };
        }
    }
}
