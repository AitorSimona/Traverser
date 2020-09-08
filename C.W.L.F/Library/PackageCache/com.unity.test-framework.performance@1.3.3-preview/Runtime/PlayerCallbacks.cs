using System;
using UnityEngine;
using Unity.PerformanceTesting.Runtime;
using NUnit.Framework;

namespace Unity.PerformanceTesting
{
    public class PlayerCallbacks
    {
        internal static bool saved;

        internal static void LogMetadata()
        {
            if(saved) return;
            var run = ReadPerformanceTestRun();
            run.PlayerSystemInfo = GetSystemInfo();
            run.QualitySettings = GetQualitySettings();
            run.ScreenSettings = GetScreenSettings();
            run.TestSuite = Application.isPlaying ? "Playmode" : "Editmode";
            run.BuildSettings.Platform = Application.platform.ToString();

            TestContext.Out?.Write("##performancetestruninfo:" + JsonUtility.ToJson(run));
            saved = true;            
        }
        
        
        private static PerformanceTestRun ReadPerformanceTestRun()
        {
            try
            {            
                var runResource = Resources.Load<TextAsset>(Utils.TestRunInfo.Replace(".json", ""));
                var json = Application.isEditor ? PlayerPrefs.GetString(Utils.PlayerPrefKeyRunJSON) : runResource.text;
                return JsonUtility.FromJson<PerformanceTestRun>(json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return null;
        }
        
        private static PlayerSystemInfo GetSystemInfo()
        {
            return new PlayerSystemInfo
            {
                OperatingSystem = SystemInfo.operatingSystem,
                DeviceModel = SystemInfo.deviceModel,
                DeviceName = SystemInfo.deviceName,
                ProcessorType = SystemInfo.processorType,
                ProcessorCount = SystemInfo.processorCount,
                GraphicsDeviceName = SystemInfo.graphicsDeviceName,
                SystemMemorySize = SystemInfo.systemMemorySize,
#if ENABLE_XR
                XrModel = UnityEngine.XR.XRDevice.model,
                XrDevice = UnityEngine.XR.XRSettings.loadedDeviceName
#endif
            };
        }

        private static QualitySettings GetQualitySettings()
        {
            return new QualitySettings()
            {
                Vsync = UnityEngine.QualitySettings.vSyncCount,
                AntiAliasing = UnityEngine.QualitySettings.antiAliasing,
                ColorSpace = UnityEngine.QualitySettings.activeColorSpace.ToString(),
                AnisotropicFiltering = UnityEngine.QualitySettings.anisotropicFiltering.ToString(),
                BlendWeights = UnityEngine.QualitySettings.skinWeights.ToString()
            };
        }

        private static ScreenSettings GetScreenSettings()
        {
            return new ScreenSettings
            {
                ScreenRefreshRate = Screen.currentResolution.refreshRate,
                ScreenWidth = Screen.currentResolution.width,
                ScreenHeight = Screen.currentResolution.height,
                Fullscreen = Screen.fullScreen
            };
        }
    }
}