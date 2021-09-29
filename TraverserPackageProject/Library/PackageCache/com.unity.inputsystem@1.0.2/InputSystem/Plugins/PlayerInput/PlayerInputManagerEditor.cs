#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.InputSystem.Users;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="PlayerInputManager"/>.
    /// </summary>
    [CustomEditor(typeof(PlayerInputManager))]
    internal class PlayerInputManagerEditor : UnityEditor.Editor
    {
        public void OnEnable()
        {
            InputUser.onChange += OnUserChange;
        }

        public void OnDestroy()
        {
            InputUser.onChange -= OnUserChange;
        }

        private void OnUserChange(InputUser user, InputUserChange change, InputDevice device)
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            ////TODO: cache properties

            EditorGUI.BeginChangeCheck();

            DoNotificationSectionUI();
            EditorGUILayout.Space();
            DoJoinSectionUI();
            EditorGUILayout.Space();
            DoSplitScreenSectionUI();

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (EditorApplication.isPlaying)
                DoDebugUI();
        }

        private void DoNotificationSectionUI()
        {
            var notificationBehaviorProperty = serializedObject.FindProperty("m_NotificationBehavior");
            EditorGUILayout.PropertyField(notificationBehaviorProperty);
            switch ((PlayerNotifications)notificationBehaviorProperty.intValue)
            {
                case PlayerNotifications.SendMessages:
                    if (m_SendMessagesHelpText == null)
                        m_SendMessagesHelpText = EditorGUIUtility.TrTextContent(
                            $"Will SendMessage() to GameObject: " + string.Join(",", PlayerInputManager.messages));
                    EditorGUILayout.HelpBox(m_SendMessagesHelpText);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    if (m_BroadcastMessagesHelpText == null)
                        m_BroadcastMessagesHelpText = EditorGUIUtility.TrTextContent(
                            $"Will BroadcastMessage() to GameObject: " + string.Join(",", PlayerInputManager.messages));
                    EditorGUILayout.HelpBox(m_BroadcastMessagesHelpText);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_EventsExpanded = EditorGUILayout.Foldout(m_EventsExpanded, m_EventsLabel, toggleOnLabelClick: true);
                    if (m_EventsExpanded)
                    {
                        var playerJoinedEventProperty = serializedObject.FindProperty("m_PlayerJoinedEvent");
                        var playerLeftEventProperty = serializedObject.FindProperty("m_PlayerLeftEvent");

                        EditorGUILayout.PropertyField(playerJoinedEventProperty);
                        EditorGUILayout.PropertyField(playerLeftEventProperty);
                    }
                    break;
            }
        }

        private void DoJoinSectionUI()
        {
            EditorGUILayout.LabelField(m_JoiningGroupLabel, EditorStyles.boldLabel);

            // Join behavior
            var joinBehaviorProperty = serializedObject.FindProperty("m_JoinBehavior");
            EditorGUILayout.PropertyField(joinBehaviorProperty);
            if ((PlayerJoinBehavior)joinBehaviorProperty.intValue != PlayerJoinBehavior.JoinPlayersManually)
            {
                ++EditorGUI.indentLevel;

                // Join action.
                if ((PlayerJoinBehavior)joinBehaviorProperty.intValue ==
                    PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered)
                {
                    var joinActionProperty = serializedObject.FindProperty("m_JoinAction");
                    EditorGUILayout.PropertyField(joinActionProperty);
                }

                // Player prefab.
                var playerPrefabProperty = serializedObject.FindProperty("m_PlayerPrefab");
                EditorGUILayout.PropertyField(playerPrefabProperty);

                --EditorGUI.indentLevel;
            }

            // Enabled-by-default.
            var allowJoiningProperty = serializedObject.FindProperty("m_AllowJoining");
            if (m_AllowingJoiningLabel == null)
                m_AllowingJoiningLabel = new GUIContent("Joining Enabled By Default", allowJoiningProperty.tooltip);
            EditorGUILayout.PropertyField(allowJoiningProperty, m_AllowingJoiningLabel);

            // Max player count.
            var maxPlayerCountProperty = serializedObject.FindProperty("m_MaxPlayerCount");
            if (maxPlayerCountProperty.intValue > 0)
                m_MaxPlayerCountEnabled = true;
            m_MaxPlayerCountEnabled = EditorGUILayout.Toggle(m_EnableMaxPlayerCountLabel, m_MaxPlayerCountEnabled);
            if (m_MaxPlayerCountEnabled)
            {
                ++EditorGUI.indentLevel;
                if (maxPlayerCountProperty.intValue < 0)
                    maxPlayerCountProperty.intValue = 1;
                EditorGUILayout.PropertyField(maxPlayerCountProperty);
                --EditorGUI.indentLevel;
            }
            else
                maxPlayerCountProperty.intValue = -1;
        }

        private void DoSplitScreenSectionUI()
        {
            EditorGUILayout.LabelField(m_SplitScreenGroupLabel, EditorStyles.boldLabel);

            // Split-screen toggle.
            var splitScreenProperty = serializedObject.FindProperty("m_SplitScreen");
            if (m_SplitScreenLabel == null)
                m_SplitScreenLabel = new GUIContent("Enable Split-Screen", splitScreenProperty.tooltip);
            EditorGUILayout.PropertyField(splitScreenProperty, m_SplitScreenLabel);
            if (!splitScreenProperty.boolValue)
                return;

            ++EditorGUI.indentLevel;

            // Maintain-aspect-ratio toggle.
            var maintainAspectRatioProperty = serializedObject.FindProperty("m_MaintainAspectRatioInSplitScreen");
            if (m_MaintainAspectRatioLabel == null)
                m_MaintainAspectRatioLabel =
                    new GUIContent("Maintain Aspect Ratio", maintainAspectRatioProperty.tooltip);
            EditorGUILayout.PropertyField(maintainAspectRatioProperty, m_MaintainAspectRatioLabel);

            // Fixed-number toggle.
            var fixedNumberProperty = serializedObject.FindProperty("m_FixedNumberOfSplitScreens");
            if (fixedNumberProperty.intValue > 0)
                m_FixedNumberOfSplitScreensEnabled = true;
            m_FixedNumberOfSplitScreensEnabled = EditorGUILayout.Toggle(m_EnableFixedNumberOfSplitScreensLabel,
                m_FixedNumberOfSplitScreensEnabled);
            if (m_FixedNumberOfSplitScreensEnabled)
            {
                ++EditorGUI.indentLevel;
                if (fixedNumberProperty.intValue < 0)
                    fixedNumberProperty.intValue = 4;
                if (m_FixedNumberOfSplitScreensLabel == null)
                    m_FixedNumberOfSplitScreensLabel = EditorGUIUtility.TrTextContent("Number of Screens",
                        fixedNumberProperty.tooltip);
                EditorGUILayout.PropertyField(fixedNumberProperty, m_FixedNumberOfSplitScreensLabel);
                --EditorGUI.indentLevel;
            }
            else
            {
                fixedNumberProperty.intValue = -1;
            }

            // Split-screen area.
            var splitScreenAreaProperty = serializedObject.FindProperty("m_SplitScreenRect");
            if (m_SplitScreenAreaLabel == null)
                m_SplitScreenAreaLabel = new GUIContent("Screen Rectangle", splitScreenAreaProperty.tooltip);
            EditorGUILayout.PropertyField(splitScreenAreaProperty, m_SplitScreenAreaLabel);

            --EditorGUI.indentLevel;
        }

        private void DoDebugUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_DebugLabel, EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);

            var players = PlayerInput.all;
            if (players.Count == 0)
            {
                EditorGUILayout.LabelField("No Players");
            }
            else
            {
                foreach (var player in players)
                {
                    var str = player.gameObject.name;
                    if (player.splitScreenIndex != -1)
                        str += $" (Screen #{player.splitScreenIndex})";
                    EditorGUILayout.LabelField("Player #" + player.playerIndex, str);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        [SerializeField] private bool m_EventsExpanded;
        [SerializeField] private bool m_MaxPlayerCountEnabled;
        [SerializeField] private bool m_FixedNumberOfSplitScreensEnabled;

        [NonSerialized] private readonly GUIContent m_JoiningGroupLabel = EditorGUIUtility.TrTextContent("Joining");
        [NonSerialized] private readonly GUIContent m_SplitScreenGroupLabel = EditorGUIUtility.TrTextContent("Split-Screen");
        [NonSerialized] private readonly GUIContent m_EventsLabel = EditorGUIUtility.TrTextContent("Events");
        [NonSerialized] private readonly GUIContent m_DebugLabel = EditorGUIUtility.TrTextContent("Debug");
        [NonSerialized] private GUIContent m_SendMessagesHelpText;
        [NonSerialized] private GUIContent m_BroadcastMessagesHelpText;
        [NonSerialized] private GUIContent m_AllowingJoiningLabel;
        [NonSerialized] private GUIContent m_SplitScreenLabel;
        [NonSerialized] private GUIContent m_MaintainAspectRatioLabel;
        [NonSerialized] private GUIContent m_SplitScreenAreaLabel;
        [NonSerialized] private GUIContent m_FixedNumberOfSplitScreensLabel;
        [NonSerialized] private readonly GUIContent m_EnableMaxPlayerCountLabel =
            EditorGUIUtility.TrTextContent("Limit Number of Players", "TODO");
        [NonSerialized] private readonly GUIContent m_EnableFixedNumberOfSplitScreensLabel =
            EditorGUIUtility.TrTextContent("Set Fixed Number", "TODO");
    }
}
#endif // UNITY_EDITOR
