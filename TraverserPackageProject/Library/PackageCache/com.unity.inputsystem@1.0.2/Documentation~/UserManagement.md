# User Management

The Input System supports multi-user management through the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class. This comprises both user account management features on platforms that have these capabilities built into them (such as Xbox and PS4), as well as features to manage Device allocations to one or more local users.

>__Note__: The user management API is quite low-level in nature. The stock functionality of [`PlayerInputManager`](Components.md#playerinputmanager-component) (see [Components](./Components.md)) provides an easier way to set up user management. The API described here is useful when you want more control over user management.

In the Input System, each [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) represents a human interacting with the application. For example, you can have multiple users playing a game together on a single computer or device (local multiplayer), where each user has one or more [paired Input Devices](#device-pairing). A user might be associated with a platform [user account](#user-account-management), if the platform and Devices support it.

The [`PlayerInputManager`](Components.md#playerinputmanager-component) class uses [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) internally to handle users.

## Device pairing

You can use the [`InputUser.PerformPairingWithDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_PerformPairingWithDevice_UnityEngine_InputSystem_InputDevice_UnityEngine_InputSystem_Users_InputUser_UnityEngine_InputSystem_Users_InputUserPairingOptions_) method to create a new [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance and pair it with an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html). You can also optionally pass in an existing [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance to pair it with the Device, if you don't want to create a new user instance.

To query the Devices paired to a specific [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html), use [`InputUser.pairedDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_pairedDevices). To remove the pairing, use [`InputUser.UnpairDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevice_UnityEngine_InputSystem_InputDevice_) or [`InputUser.UnpairDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevices).

### Initial engagement

After you create a user, you can use [`InputUser.AssociateActionsWithUser`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_AssociateActionsWithUser_UnityEngine_InputSystem_IInputActionCollection_) to associate [Input Actions](Actions.md) to it, and use [`InputUser.ActivateControlScheme`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_ActivateControlScheme_System_String_) to associate and activate a [Control Scheme](ActionBindings.md#control-schemes). You can use [`InputControlScheme.FindControlSchemeForDevice`](../api/UnityEngine.InputSystem.InputControlScheme.html#UnityEngine_InputSystem_InputControlScheme_FindControlSchemeForDevice__1_UnityEngine_InputSystem_InputDevice___0_) to pick a control scheme that matches the selected Actions and Device:

```
var scheme = InputControlScheme.FindControlSchemeForDevice(user.pairedDevices[0], user.actions.controlsSchemes);
if (scheme != null)
    user.ActivateControlScheme(scheme);
```

When you activate a Control Scheme, the Input System automatically switches the active Binding mask for the user's Actions to that Control Scheme.

### Loss of Device

If paired Input Devices disconnect during the session, the system notifies the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class. It still keeps track of the Device, and automatically re-pairs the Device if it becomes available again.

To get notifications about these changes, subscribe to the [`InputUser.onChange`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_onChange) event.

## User account management

The Input System can associate a user with a platform-specific user account, if both the platform and the Devices support this. Consoles commonly support this functionality. Platforms that support user account association are Xbox One, PlayStation 4, Nintendo Switch, and UWP.

Use the [`platformUserAccountHandle`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_platformUserAccountHandle) property to query the associated user account for an [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html). This property gets determined when the user is first [paired to a Device](#device-pairing), and the Device has any platform user information the Input System can query.

The account associated with an InputUser might change if the player uses the platform's facilities to switch to a different account ([`InputUser.onChange`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_onChange) receives an `InputUserChange.AccountChanged` notification).

Note that for WSA/UWP apps, the *User Account Information* capability must be enabled for the app in order for user information to come through on input devices.

## Debugging

Check the debugger documentation to learn [how to debug active users](Debugging.md#debugging-users-and-playerinput).
