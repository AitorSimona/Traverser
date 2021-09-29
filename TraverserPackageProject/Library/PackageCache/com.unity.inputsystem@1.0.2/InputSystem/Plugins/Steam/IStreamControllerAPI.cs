#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT

namespace UnityEngine.InputSystem.Steam
{
    /// <summary>
    /// This is a wrapper around the Steamworks SDK controller API.
    /// </summary>
    /// <seealso href="https://partner.steamgames.com/doc/api/ISteamController"/>
    public interface ISteamControllerAPI
    {
        void RunFrame();

        int GetConnectedControllers(SteamHandle<SteamController>[] outHandles);

        SteamHandle<InputActionMap> GetActionSetHandle(string actionSetName);

        SteamHandle<InputAction> GetDigitalActionHandle(string actionName);

        SteamHandle<InputAction> GetAnalogActionHandle(string actionName);

        void ActivateActionSet(SteamHandle<SteamController> controllerHandle, SteamHandle<InputActionMap> actionSetHandle);

        SteamHandle<InputActionMap> GetCurrentActionSet(SteamHandle<SteamController> controllerHandle);

        void ActivateActionSetLayer(SteamHandle<SteamController> controllerHandle,
            SteamHandle<InputActionMap> actionSetLayerHandle);

        void DeactivateActionSetLayer(SteamHandle<SteamController> controllerHandle,
            SteamHandle<InputActionMap> actionSetLayerHandle);

        void DeactivateAllActionSetLayers(SteamHandle<SteamController> controllerHandle);

        int GetActiveActionSetLayers(SteamHandle<SteamController> controllerHandle,
            out SteamHandle<InputActionMap> handlesOut);

        SteamAnalogActionData GetAnalogActionData(SteamHandle<SteamController> controllerHandle,
            SteamHandle<InputAction> analogActionHandle);

        SteamDigitalActionData GetDigitalActionData(SteamHandle<SteamController> controllerHandle,
            SteamHandle<InputAction> digitalActionHandle);
    }

    public struct SteamDigitalActionData
    {
        public bool active { get; set; }
        public bool pressed { get; set; }
    }

    public struct SteamAnalogActionData
    {
        public bool active { get; set; }
        public Vector2 position { get; set; }
    }
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
