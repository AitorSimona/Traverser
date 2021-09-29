using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

////TODO: control schemes, like actions and maps, should have stable IDs so that they can be renamed

////REVIEW: have some way of expressing 'contracts' on action maps? I.e. something like
////        "I expect a 'look' and a 'move' action in here"

////REVIEW: rename this from "InputActionAsset" to something else that emphasizes the asset aspect less
////        and instead emphasizes the map collection aspect more?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// An asset containing action maps and control schemes.
    /// </summary>
    /// <remarks>
    /// InputActionAssets can be created in code but are usually stored in JSON format on
    /// disk with the ".inputactions" extension and are imported by Unity using a custom
    /// importer.
    ///
    /// To create an InputActionAsset in code, use the <c>Singleton</c> API and populate the
    /// asset with the methods found in <see cref="InputActionSetupExtensions"/>. Alternatively,
    /// you can load an InputActionAsset directly from a string in JSON format using <see cref="FromJson"/>.
    ///
    /// <example>
    /// <code>
    /// // Create and configure an asset in code.
    /// var asset1 = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
    /// var actionMap1 = asset1.CreateActionMap("map1");
    /// action1Map.AddAction("action1", binding: "&lt;Keyboard&gt;/space");
    /// </code>
    /// </example>
    ///
    /// Each asset can contain arbitrary many action maps that can be enabled and disabled individually
    /// (see <see cref="InputActionMap.Enable"/> and <see cref="InputActionMap.Disable"/>) or in bulk
    /// (see <see cref="Enable"/> and <see cref="Disable"/>). The name of each action map must be unique.
    /// The list of action maps can be queried from <see cref="actionMaps"/>.
    ///
    /// InputActionAssets can only define <see cref="InputControlScheme"/>s. They can be added to
    /// an asset with <see cref="InputActionSetupExtensions.AddControlScheme(InputActionAsset,string)"/>
    /// and can be queried from <see cref="controlSchemes"/>.
    ///
    /// Be aware that input action assets do not separate between static (configuration) data and dynamic
    /// (instance) data. For audio, for example, <c>AudioClip</c> represents the static,
    /// shared data portion of audio playback whereas <c>AudioSource"</c> represents the
    /// dynamic, per-instance audio playback portion (referencing the clip through <c>AudioSource.clip</c>).
    ///
    /// For input, such a split is less beneficial as the same input is generally not exercised
    /// multiple times in parallel. Keeping both static and dynamic data together simplifies
    /// using the system.
    ///
    /// However, there are scenarios where you indeed want to take the same input action and
    /// exercise it multiple times in parallel. A prominent example of such a use case is
    /// local multiplayer where each player gets the same set of actions but is controlling
    /// them with a different device (or devices) each. This is easily achieved by simply
    /// using <c>UnityEngine.Object.Instantiate</c> to instantiate the input action
    /// asset multiple times. <see cref="PlayerInput"/> will automatically do so in its
    /// internals.
    ///
    /// Note also that all action maps in an asset share binding state. This means that if
    /// one map in an asset has to resolve its bindings, all maps in the asset have to.
    /// </remarks>
    public class InputActionAsset : ScriptableObject, IInputActionCollection
    {
        /// <summary>
        /// File extension (without the dot) for InputActionAssets in JSON format.
        /// </summary>
        /// <value>File extension for InputActionAsset source files.</value>
        /// <remarks>
        /// Files with this extension will automatically be imported by Unity as
        /// InputActionAssets.
        /// </remarks>
        public const string Extension = "inputactions";

        /// <summary>
        /// True if any action in the asset is currently enabled.
        /// </summary>
        /// <seealso cref="InputAction.enabled"/>
        /// <seealso cref="InputActionMap.enabled"/>
        /// <seealso cref="InputAction.Enable"/>
        /// <seealso cref="InputActionMap.Enable"/>
        /// <seealso cref="Enable"/>
        public bool enabled
        {
            get
            {
                foreach (var actionMap in actionMaps)
                    if (actionMap.enabled)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// List of action maps defined in the asset.
        /// </summary>
        /// <value>Action maps contained in the asset.</value>
        /// <seealso cref="InputActionSetupExtensions.AddActionMap(InputActionAsset,string)"/>
        /// <seealso cref="InputActionSetupExtensions.RemoveActionMap(InputActionAsset,InputActionMap)"/>
        /// <seealso cref="FindActionMap(string,bool)"/>
        public ReadOnlyArray<InputActionMap> actionMaps => new ReadOnlyArray<InputActionMap>(m_ActionMaps);

        /// <summary>
        /// List of control schemes defined in the asset.
        /// </summary>
        /// <value>Control schemes defined for the asset.</value>
        /// <seealso cref="InputActionSetupExtensions.AddControlScheme(InputActionAsset,string)"/>
        /// <seealso cref="InputActionSetupExtensions.RemoveControlScheme"/>
        public ReadOnlyArray<InputControlScheme> controlSchemes => new ReadOnlyArray<InputControlScheme>(m_ControlSchemes);

        /// <summary>
        /// Binding mask to apply to all action maps and actions in the asset.
        /// </summary>
        /// <value>Optional mask that determines which bindings in the asset to enable.</value>
        /// <remarks>
        /// Binding masks can be applied at three different levels: for an entire asset through
        /// this property, for a specific map through <see cref="InputActionMap.bindingMask"/>,
        /// and for single actions through <see cref="InputAction.bindingMask"/>. By default,
        /// none of the masks will be set (i.e. they will be <c>null</c>).
        ///
        /// When an action is enabled, all the binding masks that apply to it are taken into
        /// account. Specifically, this means that any given binding on the action will be
        /// enabled only if it matches the mask applied to the asset, the mask applied
        /// to the map that contains the action, and the mask applied to the action itself.
        /// All the masks are individually optional.
        ///
        /// Masks are matched against bindings using <see cref="InputBinding.Matches"/>.
        ///
        /// Note that if you modify the masks applicable to an action while it is
        /// enabled, the action's <see cref="InputAction.controls"/> will get updated immediately to
        /// respect the mask. To avoid repeated binding resolution, it is most efficient
        /// to apply binding masks before enabling actions.
        ///
        /// Binding masks are non-destructive. All the bindings on the action are left
        /// in place. Setting a mask will not affect the value of the <see cref="InputAction.bindings"/>
        /// and <see cref="InputActionMap.bindings"/> properties.
        /// </remarks>
        /// <seealso cref="InputBinding.MaskByGroup"/>
        /// <seealso cref="InputAction.bindingMask"/>
        /// <seealso cref="InputActionMap.bindingMask"/>
        public InputBinding? bindingMask
        {
            get => m_BindingMask;
            set
            {
                if (m_BindingMask == value)
                    return;

                m_BindingMask = value;

                ReResolveIfNecessary();
            }
        }

        /// <summary>
        /// Set of devices that bindings in the asset can bind to.
        /// </summary>
        /// <value>Optional set of devices to use by bindings in the asset.</value>
        /// <remarks>
        /// By default (with this property being <c>null</c>), bindings will bind to any of the
        /// controls available through <see cref="InputSystem.devices"/>, i.e. controls from all
        /// devices in the system will be used.
        ///
        /// By setting this property, binding resolution can instead be restricted to just specific
        /// devices. This restriction can either be applied to an entire asset using this property
        /// or to specific action maps by using <see cref="InputActionMap.devices"/>. Note that if
        /// both this property and <see cref="InputActionMap.devices"/> is set for a specific action
        /// map, the list of devices on the action map will take precedence and the list on the
        /// asset will be ignored for bindings in that action map.
        ///
        /// <example>
        /// <code>
        /// // Create an asset with a single action map and a single action with a
        /// // gamepad binding.
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        /// var actionMap = new InputActionMap();
        /// var fireAction = actionMap.AddAction("Fire", binding: "&lt;Gamepad&gt;/buttonSouth");
        /// asset.AddActionMap(actionMap);
        ///
        /// // Let's assume we have two gamepads connected. If we enable the
        /// // action map now, the 'Fire' action will bind to both.
        /// actionMap.Enable();
        ///
        /// // This will print two controls.
        /// Debug.Log(string.Join("\n", fireAction.controls));
        ///
        /// // To restrict the setup to just the first gamepad, we can assign
        /// // to the 'devices' property (in this case, we could do so on either
        /// // the action map or on the asset; we choose the latter here).
        /// asset.devices = new InputDevice[] { Gamepad.all[0] };
        ///
        /// // Now this will print only one control.
        /// Debug.Log(string.Join("\n", fireAction.controls));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputActionMap.devices"/>
        public ReadOnlyArray<InputDevice>? devices
        {
            get
            {
                if (m_DevicesCount < 0)
                    return null;
                return new ReadOnlyArray<InputDevice>(m_DevicesArray, 0, m_DevicesCount);
            }
            set
            {
                if (value == null)
                {
                    if (m_DevicesCount < 0)
                        return; // No change.

                    if (m_DevicesArray != null & m_DevicesCount > 0)
                        Array.Clear(m_DevicesArray, 0, m_DevicesCount);
                    m_DevicesCount = -1;
                }
                else
                {
                    // See if the array actually changes content. Avoids re-resolving when there
                    // is no need to.
                    if (m_DevicesCount == value.Value.Count)
                    {
                        var noChange = true;
                        for (var i = 0; i < m_DevicesCount; ++i)
                        {
                            if (!ReferenceEquals(m_DevicesArray[i], value.Value[i]))
                            {
                                noChange = false;
                                break;
                            }
                        }
                        if (noChange)
                            return;
                    }

                    if (m_DevicesCount > 0)
                        m_DevicesArray.Clear(ref m_DevicesCount);
                    m_DevicesCount = 0;
                    ArrayHelpers.AppendListWithCapacity(ref m_DevicesArray, ref m_DevicesCount, value.Value);
                }

                ReResolveIfNecessary();
            }
        }

        /// <summary>
        /// Look up an action by name or ID.
        /// </summary>
        /// <param name="actionNameOrId">Name of the action as either a "map/action" combination (e.g. "gameplay/fire") or
        /// a simple name. In the former case, the name is split at the '/' slash and the first part is used to find
        /// a map with that name and the second part is used to find an action with that name inside the map. In the
        /// latter case, all maps are searched in order and the first action that has the given name in any of the maps
        /// is returned. Note that name comparisons are case-insensitive.
        ///
        /// Alternatively, the given string can be a GUID as given by <see cref="InputAction.id"/>.</param>
        /// <returns>The action with the corresponding name or null if no matching action could be found.</returns>
        /// <remarks>
        /// This method is equivalent to <see cref="FindAction(string)"/> except that it throws
        /// <see cref="KeyNotFoundException"/> if no action with the given name or ID
        /// could be found.
        /// </remarks>
        /// <exception cref="KeyNotFoundException">No action was found matching <paramref name="actionNameOrId"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is <c>null</c> or empty.</exception>
        /// <seealso cref="FindAction(string)"/>
        public InputAction this[string actionNameOrId]
        {
            get
            {
                var action = FindAction(actionNameOrId);
                if (action == null)
                    throw new KeyNotFoundException($"Cannot find action '{actionNameOrId}' in '{this}'");
                return action;
            }
        }

        /// <summary>
        /// Return a JSON representation of the asset.
        /// </summary>
        /// <returns>A string in JSON format that represents the static/configuration data present
        /// in the asset.</returns>
        /// <remarks>
        /// This will not save dynamic execution state such as callbacks installed on
        /// <see cref="InputAction">actions</see> or enabled/disabled states of individual
        /// maps and actions.
        ///
        /// Use <see cref="LoadFromJson"/> to deserialize the JSON data back into an InputActionAsset.
        ///
        /// Be aware that the format used by this method is <em>different</em> than what you
        /// get if you call <c>JsonUtility.ToJson</c> on an InputActionAsset instance. In other
        /// words, the JSON format is not identical to the Unity serialized object representation
        /// of the asset.
        /// </remarks>
        /// <seealso cref="FromJson"/>
        public string ToJson()
        {
            var fileJson = new WriteFileJson
            {
                name = name,
                maps = InputActionMap.WriteFileJson.FromMaps(m_ActionMaps).maps,
                controlSchemes = InputControlScheme.SchemeJson.ToJson(m_ControlSchemes),
            };

            return JsonUtility.ToJson(fileJson, true);
        }

        /// <summary>
        /// Replace the contents of the asset with the data in the given JSON string.
        /// </summary>
        /// <param name="json">JSON contents of an <c>.inputactions</c> asset.</param>
        /// <remarks>
        /// <c>.inputactions</c> assets are stored in JSON format. This method allows reading
        /// the JSON source text of such an asset into an existing <c>InputActionMap</c> instance.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        /// asset.LoadFromJson(@"
        /// {
        ///     ""maps"" : [
        ///         {
        ///             ""name"" : ""gameplay"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""fire"", ""type"" : ""button"" },
        ///                 { ""name"" : ""look"", ""type"" : ""value"" },
        ///                 { ""name"" : ""move"", ""type"" : ""value"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftTrigger"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftStick"", ""action"" : ""move"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/rightStick"", ""action"" : ""look"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""dpad"", ""action"" : ""move"", ""groups"" : ""Gamepad"", ""isComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/a"", ""name"" : ""left"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/d"", ""name"" : ""right"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/w"", ""name"" : ""up"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/s"", ""name"" : ""down"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Mouse&gt;/delta"", ""action"" : ""look"", ""groups"" : ""Keyboard&amp;Mouse"" },
        ///                 { ""path"" : ""&lt;Mouse&gt;/leftButton"", ""action"" : ""fire"", ""groups"" : ""Keyboard&amp;Mouse"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""ui"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""navigate"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/dpad"", ""action"" : ""navigate"", ""groups"" : ""Gamepad"" }
        ///             ]
        ///         }
        ///     ],
        ///     ""controlSchemes"" : [
        ///         {
        ///             ""name"" : ""Gamepad"",
        ///             ""bindingGroup"" : ""Gamepad"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Gamepad&gt;"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""Keyboard&amp;Mouse"",
        ///             ""bindingGroup"" : ""Keyboard&amp;Mouse"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Keyboard&gt;"" },
        ///                 { ""devicePath"" : ""&lt;Mouse&gt;"" }
        ///             ]
        ///         }
        ///     ]
        /// }");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c> or empty.</exception>
        /// <seealso cref="FromJson"/>
        /// <seealso cref="ToJson"/>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            var parsedJson = JsonUtility.FromJson<ReadFileJson>(json);
            parsedJson.ToAsset(this);
        }

        /// <summary>
        /// Replace the contents of the asset with the data in the given JSON string.
        /// </summary>
        /// <param name="json">JSON contents of an <c>.inputactions</c> asset.</param>
        /// <returns>The InputActionAsset instance created from the given JSON string.</returns>
        /// <remarks>
        /// <c>.inputactions</c> assets are stored in JSON format. This method allows turning
        /// the JSON source text of such an asset into a new <c>InputActionMap</c> instance.
        ///
        /// Be aware that the format used by this method is <em>different</em> than what you
        /// get if you call <c>JsonUtility.ToJson</c> on an InputActionAsset instance. In other
        /// words, the JSON format is not identical to the Unity serialized object representation
        /// of the asset.
        ///
        /// <example>
        /// <code>
        /// var asset = InputActionAsset.FromJson(@"
        /// {
        ///     ""maps"" : [
        ///         {
        ///             ""name"" : ""gameplay"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""fire"", ""type"" : ""button"" },
        ///                 { ""name"" : ""look"", ""type"" : ""value"" },
        ///                 { ""name"" : ""move"", ""type"" : ""value"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftTrigger"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftStick"", ""action"" : ""move"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/rightStick"", ""action"" : ""look"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""dpad"", ""action"" : ""move"", ""groups"" : ""Gamepad"", ""isComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/a"", ""name"" : ""left"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/d"", ""name"" : ""right"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/w"", ""name"" : ""up"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/s"", ""name"" : ""down"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Mouse&gt;/delta"", ""action"" : ""look"", ""groups"" : ""Keyboard&amp;Mouse"" },
        ///                 { ""path"" : ""&lt;Mouse&gt;/leftButton"", ""action"" : ""fire"", ""groups"" : ""Keyboard&amp;Mouse"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""ui"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""navigate"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/dpad"", ""action"" : ""navigate"", ""groups"" : ""Gamepad"" }
        ///             ]
        ///         }
        ///     ],
        ///     ""controlSchemes"" : [
        ///         {
        ///             ""name"" : ""Gamepad"",
        ///             ""bindingGroup"" : ""Gamepad"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Gamepad&gt;"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""Keyboard&amp;Mouse"",
        ///             ""bindingGroup"" : ""Keyboard&amp;Mouse"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Keyboard&gt;"" },
        ///                 { ""devicePath"" : ""&lt;Mouse&gt;"" }
        ///             ]
        ///         }
        ///     ]
        /// }");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c> or empty.</exception>
        /// <seealso cref="LoadFromJson"/>
        /// <seealso cref="ToJson"/>
        public static InputActionAsset FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            var asset = CreateInstance<InputActionAsset>();
            asset.LoadFromJson(json);
            return asset;
        }

        /// <summary>
        /// Find an <see cref="InputAction"/> by its name in one of the <see cref="InputActionMap"/>s
        /// in the asset.
        /// </summary>
        /// <param name="actionNameOrId">Name of the action as either a "map/action" combination (e.g. "gameplay/fire") or
        /// a simple name. In the former case, the name is split at the '/' slash and the first part is used to find
        /// a map with that name and the second part is used to find an action with that name inside the map. In the
        /// latter case, all maps are searched in order and the first action that has the given name in any of the maps
        /// is returned. Note that name comparisons are case-insensitive.
        ///
        /// Alternatively, the given string can be a GUID as given by <see cref="InputAction.id"/>.</param>
        /// <param name="throwIfNotFound">If <c>true</c>, instead of returning <c>null</c> when the action
        /// cannot be found, throw <c>ArgumentException</c>.</param>
        /// <returns>The action with the corresponding name or <c>null</c> if no matching action could be found.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        ///
        /// var map1 = new InputActionMap("map1");
        /// var map2 = new InputActionMap("map2");
        ///
        /// asset.AddActionMap(map1);
        /// asset.AddActionMap(map2);
        ///
        /// var action1 = map1.AddAction("action1");
        /// var action2 = map1.AddAction("action2");
        /// var action3 = map2.AddAction("action3");
        ///
        /// // Search all maps in the asset for any action that has the given name.
        /// asset.FindAction("action1") // Returns action1.
        /// asset.FindAction("action2") // Returns action2
        /// asset.FindAction("action3") // Returns action3.
        ///
        /// // Search for a specific action in a specific map.
        /// asset.FindAction("map1/action1") // Returns action1.
        /// asset.FindAction("map2/action2") // Returns action2.
        /// asset.FindAction("map3/action3") // Returns action3.
        ///
        /// // Search by unique action ID.
        /// asset.FindAction(action1.id.ToString()) // Returns action1.
        /// asset.FindAction(action2.id.ToString()) // Returns action2.
        /// asset.FindAction(action3.id.ToString()) // Returns action3.
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="throwIfNotFound"/> is true and the
        /// action could not be found. -Or- If <paramref name="actionNameOrId"/> contains a slash but is missing
        /// either the action or the map name.</exception>
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            if (actionNameOrId == null)
                throw new ArgumentNullException(nameof(actionNameOrId));

            if (m_ActionMaps != null)
            {
                // Check if we have a "map/action" path.
                var indexOfSlash = actionNameOrId.IndexOf('/');
                if (indexOfSlash == -1)
                {
                    // No slash so it's just a simple action name.
                    for (var i = 0; i < m_ActionMaps.Length; ++i)
                    {
                        var action = m_ActionMaps[i].FindAction(actionNameOrId);
                        if (action != null)
                            return action;
                    }
                }
                else
                {
                    // Have a path. First search for the map, then for the action.
                    var mapName = new Substring(actionNameOrId, 0, indexOfSlash);
                    var actionName = new Substring(actionNameOrId, indexOfSlash + 1);

                    if (mapName.isEmpty || actionName.isEmpty)
                        throw new ArgumentException("Malformed action path: " + actionNameOrId, nameof(actionNameOrId));

                    for (var i = 0; i < m_ActionMaps.Length; ++i)
                    {
                        var map = m_ActionMaps[i];
                        if (Substring.Compare(map.name, mapName, StringComparison.InvariantCultureIgnoreCase) != 0)
                            continue;

                        var actions = map.m_Actions;
                        for (var n = 0; n < actions.Length; ++n)
                        {
                            var action = actions[n];
                            if (Substring.Compare(action.name, actionName,
                                StringComparison.InvariantCultureIgnoreCase) == 0)
                                return action;
                        }

                        break;
                    }
                }
            }

            if (throwIfNotFound)
                throw new ArgumentException($"No action '{actionNameOrId}' in '{this}'");

            return null;
        }

        /// <summary>
        /// Find an <see cref="InputActionMap"/> in the asset by its name or ID.
        /// </summary>
        /// <param name="nameOrId">Name or ID (see <see cref="InputActionMap.id"/>) of the action map
        /// to look for. Matching is case-insensitive.</param>
        /// <param name="throwIfNotFound">If true, instead of returning <c>null</c>, throw <c>ArgumentException</c>.</param>
        /// <returns>The <see cref="InputActionMap"/> with a name or ID matching <paramref name="nameOrId"/> or
        /// <c>null</c> if no matching map could be found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="throwIfNotFound"/> is <c>true</c>, thrown if
        /// the action map cannot be found.</exception>
        /// <seealso cref="actionMaps"/>
        /// <seealso cref="FindActionMap(System.Guid)"/>
        public InputActionMap FindActionMap(string nameOrId, bool throwIfNotFound = false)
        {
            if (nameOrId == null)
                throw new ArgumentNullException(nameof(nameOrId));

            if (m_ActionMaps == null)
                return null;

            // If the name contains a hyphen, it may be a GUID.
            if (nameOrId.Contains('-') && Guid.TryParse(nameOrId, out var id))
            {
                for (var i = 0; i < m_ActionMaps.Length; ++i)
                {
                    var map = m_ActionMaps[i];
                    if (map.idDontGenerate == id)
                        return map;
                }
            }

            // Default lookup is by name (case-insensitive).
            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                if (string.Compare(nameOrId, map.name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return map;
            }

            if (throwIfNotFound)
                throw new ArgumentException($"Cannot find action map '{nameOrId}' in '{this}'");

            return null;
        }

        /// <summary>
        /// Find an <see cref="InputActionMap"/> in the asset by its ID.
        /// </summary>
        /// <param name="id">ID (see <see cref="InputActionMap.id"/>) of the action map
        /// to look for.</param>
        /// <returns>The <see cref="InputActionMap"/> with ID matching <paramref name="id"/> or
        /// <c>null</c> if no map in the asset has the given ID.</returns>
        /// <seealso cref="actionMaps"/>
        /// <seealso cref="FindActionMap"/>
        public InputActionMap FindActionMap(Guid id)
        {
            if (m_ActionMaps == null)
                return null;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                if (map.idDontGenerate == id)
                    return map;
            }

            return null;
        }

        /// <summary>
        /// Find an action by its ID (see <see cref="InputAction.id"/>).
        /// </summary>
        /// <param name="guid">ID of the action to look for.</param>
        /// <returns>The action in the asset with the given ID or null if no action
        /// in the asset has the given ID.</returns>
        public InputAction FindAction(Guid guid)
        {
            if (m_ActionMaps == null)
                return null;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                var action = map.FindAction(guid);
                if (action != null)
                    return action;
            }

            return null;
        }

        /// <summary>
        /// Find the control scheme with the given name and return its index
        /// in <see cref="controlSchemes"/>.
        /// </summary>
        /// <param name="name">Name of the control scheme. Matching is case-insensitive.</param>
        /// <returns>The index of the given control scheme or -1 if no control scheme
        /// with the given name could be found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>
        /// or empty.</exception>
        public int FindControlSchemeIndex(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (m_ControlSchemes == null)
                return -1;

            for (var i = 0; i < m_ControlSchemes.Length; ++i)
                if (string.Compare(name, m_ControlSchemes[i].name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return i;

            return -1;
        }

        /// <summary>
        /// Find the control scheme with the given name and return it.
        /// </summary>
        /// <param name="name">Name of the control scheme. Matching is case-insensitive.</param>
        /// <returns>The control scheme with the given name or null if no scheme
        /// with the given name could be found in the asset.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>
        /// or empty.</exception>
        public InputControlScheme? FindControlScheme(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var index = FindControlSchemeIndex(name);
            if (index == -1)
                return null;

            return m_ControlSchemes[index];
        }

        /// <summary>
        /// Enable all action maps in the asset.
        /// </summary>
        /// <remarks>
        /// This method is equivalent to calling <see cref="InputActionMap.Enable"/> on
        /// all maps in <see cref="actionMaps"/>.
        /// </remarks>
        public void Enable()
        {
            foreach (var map in actionMaps)
                map.Enable();
        }

        /// <summary>
        /// Disable all action maps in the asset.
        /// </summary>
        /// <remarks>
        /// This method is equivalent to calling <see cref="InputActionMap.Disable"/> on
        /// all maps in <see cref="actionMaps"/>.
        /// </remarks>
        public void Disable()
        {
            foreach (var map in actionMaps)
                map.Disable();
        }

        /// <summary>
        /// Return <c>true</c> if the given action is part of the asset.
        /// </summary>
        /// <param name="action">An action. Can be null.</param>
        /// <returns>True if the given action is part of the asset, false otherwise.</returns>
        public bool Contains(InputAction action)
        {
            var map = action?.actionMap;
            if (map == null)
                return false;

            return map.asset == this;
        }

        /// <summary>
        /// Enumerate all actions in the asset.
        /// </summary>
        /// <returns>Enumerate over all actions in the asset.</returns>
        /// <remarks>
        /// Actions will be enumerated one action map in <see cref="actionMaps"/>
        /// after the other. The actions from each map will be yielded in turn.
        ///
        /// This method will allocate GC heap memory.
        /// </remarks>
        public IEnumerator<InputAction> GetEnumerator()
        {
            if (m_ActionMaps == null)
                yield break;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var actions = m_ActionMaps[i].actions;
                var actionCount = actions.Count;

                for (var n = 0; n < actionCount; ++n)
                    yield return actions[n];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ReResolveIfNecessary()
        {
            if (m_SharedStateForAllMaps == null)
                return;

            Debug.Assert(m_ActionMaps != null && m_ActionMaps.Length > 0);
            // State is share between all action maps in the asset. Resolving bindings for the
            // first map will resolve them for all maps.
            m_ActionMaps[0].LazyResolveBindings();
        }

        private void OnDestroy()
        {
            Disable();
            if (m_SharedStateForAllMaps != null)
            {
                m_SharedStateForAllMaps.Dispose(); // Will clean up InputActionMap state.
                m_SharedStateForAllMaps = null;
            }
        }

        ////TODO: ApplyBindingOverrides, RemoveBindingOverrides, RemoveAllBindingOverrides

        [SerializeField] internal InputActionMap[] m_ActionMaps;
        [SerializeField] internal InputControlScheme[] m_ControlSchemes;

        ////TODO: make this persistent across domain reloads
        /// <summary>
        /// Shared state for all action maps in the asset.
        /// </summary>
        [NonSerialized] internal InputActionState m_SharedStateForAllMaps;
        [NonSerialized] internal InputBinding? m_BindingMask;

        [NonSerialized] private int m_DevicesCount = -1;
        [NonSerialized] private InputDevice[] m_DevicesArray;

        [Serializable]
        internal struct WriteFileJson
        {
            public string name;
            public InputActionMap.WriteMapJson[] maps;
            public InputControlScheme.SchemeJson[] controlSchemes;
        }

        [Serializable]
        internal struct ReadFileJson
        {
            public string name;
            public InputActionMap.ReadMapJson[] maps;
            public InputControlScheme.SchemeJson[] controlSchemes;

            public void ToAsset(InputActionAsset asset)
            {
                asset.name = name;
                asset.m_ActionMaps = new InputActionMap.ReadFileJson {maps = maps}.ToMaps();
                asset.m_ControlSchemes = InputControlScheme.SchemeJson.ToSchemes(controlSchemes);

                // Link maps to their asset.
                if (asset.m_ActionMaps != null)
                    foreach (var map in asset.m_ActionMaps)
                        map.m_Asset = asset;
            }
        }
    }
}
