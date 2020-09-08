using System;

using UnityEngine;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
#endif

using PreUpdate = UnityEngine.PlayerLoop.PreUpdate;
using PostLateUpdate = UnityEngine.PlayerLoop.PostLateUpdate;
using EarlyUpdate = UnityEngine.PlayerLoop.EarlyUpdate;

namespace Unity.SnapshotDebugger
{
    public class Debugger : System.IDisposable
    {
        public enum State
        {
            Inactive,
            Record,
            Rewind,
        }

        public float deltaTime
        {
            get; private set;
        }

        public float time
        {
            get; private set;
        }

        public float rewindTime
        {
            get; set;
        }

        public bool isActive => !IsState(State.Inactive);

        public bool rewind
        {
            get => IsState(State.Rewind);
            set
            {
                if (isActive)
                {
                    state = value ? State.Rewind : State.Record;
                }
            }
        }

        public float capacityInSeconds
        {
            get
            {
                return _preferences.capacityInSeconds;
            }

            set
            {
                _preferences.capacityInSeconds = value;

                _preferences.Save();

                _storage.capacityInSeconds = value;
            }
        }

        public int memorySize
        {
            get { return _storage.memorySize; }
        }

        public float startTimeInSeconds
        {
            get { return _storage.startTimeInSeconds; }
        }

        public float endTimeInSeconds
        {
            get { return _storage.endTimeInSeconds; }
        }

        public static Debugger instance
        {
            get
            {
                if (_instance == null)
                {
                    Initialize();
                }

                return _instance;
            }
        }

        private static Debugger _instance;

        internal static Registry registry
        {
            get { return instance._registry; }
        }

        public static FrameDebugger frameDebugger
        {
            get { return instance._frameDebugger; }
        }

        public Identifier<Aggregate> this[GameObject gameObject]
        {
            get
            {
                var aggregate = registry[gameObject];

                if (aggregate != null)
                {
                    return aggregate.identifier;
                }

                return Identifier<Aggregate>.Undefined;
            }
        }

        public GameObject this[Identifier<Aggregate> identifier]
        {
            get { return registry[identifier].gameObject; }
        }

        public bool IsState(State state)
        {
            return this.state == state;
        }

        public State state
        {
            get => _state;

            set
            {
                if (_state != value)
                {
                    State prevState = _state;
                    _state = value;

                    switch (_state)
                    {
                        case State.Inactive:
                        {
                            _frameDebugger.Clear();
                            _frameDebugger.DisableRecording();

                            time = 0.0f;
                            rewindTime = 0.0f;
                            deltaTime = 0.0f;

                            _storage.Discard();
                        }
                        break;
                        case State.Record:
                        {
                            _storage.DiscardAfterTimeStamp(time);

                            _frameDebugger.EnableRecording(time);

                            _wasRewinding = prevState == State.Rewind;
                        }
                        break;
                        case State.Rewind:
                        {
                            rewindTime = _storage.endTimeInSeconds;

                            _frameDebugger.DisableRecording();

                            OnPreUpdate();
                        }
                        break;
                    }
                }

                _state = value;
            }
        }

        [Serializable]
        struct Preferences
        {
            public float capacityInSeconds;


            public static Preferences Load()
            {
#if UNITY_EDITOR
                var json = EditorPrefs.GetString(preferencesKey);

                if (!string.IsNullOrEmpty(json))
                {
                    return JsonUtility.FromJson<Preferences>(json);
                }
#endif

                return new Preferences
                {
                    capacityInSeconds = 10.0f
                };
            }

            public void Save()
            {
#if UNITY_EDITOR
                var json = JsonUtility.ToJson(this);

                EditorPrefs.SetString(preferencesKey, json);
#endif
            }

            const string preferencesKey = "Unity.SnapshotDebugger.Preferences";
        }

        Debugger()
        {
            deltaTime = Time.deltaTime;

            _preferences = Preferences.Load();

            _storage = MemoryStorage.Create(capacityInSeconds);

            _frameDebugger = new FrameDebugger();

            _state = State.Inactive;

            _wasRewinding = false;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#else
            RegisterUpdateFunctions();
#endif
        }

        public void Dispose()
        {
            (_storage as IDisposable).Dispose();
        }

        public static void Initialize()
        {
            Assert.IsTrue(_instance == null);

            ReadExtensions.InitializeExtensionMethods();
            WriteExtensions.InitializeExtensionMethods();

            _instance = new Debugger();
        }

#if UNITY_EDITOR
        void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredPlayMode)
            {
                RegisterUpdateFunctions();
            }
            else if (playModeState == PlayModeStateChange.ExitingPlayMode)
            {
                state = State.Inactive;

                UnregisterUpdateFunctions();
            }
        }

#endif

        void OnEarlyUpdate()
        {
            if (!rewind && !_wasRewinding)
            {
                deltaTime = Time.deltaTime;
            }

            registry.OnEarlyUpdate(rewind);
        }

        void OnPreUpdate()
        {
            if (IsState(State.Record))
            {
                time = _storage.endTimeInSeconds;

                _storage.Record(
                    registry.RecordSnapshot(
                        time, deltaTime));

                rewindTime = time;

                if (!_wasRewinding)
                {
                    _frameDebugger.Update(time, startTimeInSeconds);
                }

                _wasRewinding = false;
            }
            else if (IsState(State.Rewind))
            {
                var snapshot = _storage.Retrieve(rewindTime);

                if (snapshot != null)
                {
                    time = snapshot.startTimeInSeconds;
                    deltaTime = snapshot.durationInSeconds;

                    registry.RestoreSnapshot(snapshot);
                }
            }
        }

        void OnPostLateUpdate()
        {
            if (IsState(State.Record))
            {
                var snapshot = _storage.Retrieve(time);

                Assert.IsTrue(snapshot != null);

                snapshot.PostProcess();
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnLoadMethod()
        {
            Initialize();
        }

#endif

        void RegisterUpdateFunctions()
        {
            UpdateSystem.Listen<EarlyUpdate>(OnEarlyUpdate);
            UpdateSystem.Listen<PreUpdate>(OnPreUpdate);
            UpdateSystem.Listen<PostLateUpdate>(OnPostLateUpdate);
        }

        void UnregisterUpdateFunctions()
        {
            UpdateSystem.Ignore<PostLateUpdate>(OnPostLateUpdate);
            UpdateSystem.Ignore<PreUpdate>(OnPreUpdate);
            UpdateSystem.Ignore<EarlyUpdate>(OnEarlyUpdate);
        }

        Registry _registry = new Registry();

        Preferences _preferences;

        Storage _storage;

        FrameDebugger _frameDebugger;

        State _state;

        bool _wasRewinding;
    }
}
