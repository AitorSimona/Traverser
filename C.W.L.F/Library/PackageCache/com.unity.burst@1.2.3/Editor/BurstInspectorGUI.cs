using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Burst.LowLevel;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Burst.Editor
{
    internal enum DisassemblyKind
    {
        Asm = 0,
        IL = 1,
        UnoptimizedIR = 2,
        OptimizedIR = 3,
        IRPassAnalysis = 4
    }

    internal class BurstInspectorGUI : EditorWindow
    {
        private const string FontSizeIndexPref = "BurstInspectorFontSizeIndex";

        private static readonly string[] DisassemblyKindNames =
        {
            "Assembly",
            ".NET IL",
            "LLVM IR (Unoptimized)",
            "LLVM IR (Optimized)",
            "LLVM IR Optimisation Diagnostics"
        };

        private static readonly string[] DisasmOptions =
        {
            "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.Asm),
            "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IL),
            "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IR),
            "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IROptimized),
            "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IRPassAnalysis)
        };

        private static readonly SplitterState TreeViewSplitterState = new SplitterState(new float[] { 30, 70 }, new int[] { 128, 128 }, null);


        private static readonly string[] TargetCpuNames = Enum.GetNames(typeof(TargetCpu));

        private static readonly int[] FontSizes =
        {
            8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20
        };

        private static string[] _fontSizesText;

        [NonSerialized]
        private readonly BurstDisassembler _burstDisassembler;

        [SerializeField] private TargetCpu _targetCpu = TargetCpu.Auto;

        [SerializeField] private DisassemblyKind _disasmKind = DisassemblyKind.Asm;

        [NonSerialized]
        private GUIStyle _fixedFontStyle;

        [NonSerialized]
        private int _fontSizeIndex = -1;

        [NonSerialized]
        private int _previousTargetIndex = -1;

        [SerializeField] private bool _safetyChecks;
        [SerializeField] private bool _enhancedDisassembly = true;
        private Vector2 _scrollPos;
        private SearchField _searchField;

        [SerializeField] private string _selectedItem;

        [NonSerialized]
        private List<BurstCompileTarget> _targets;

        [NonSerialized]
        private LongTextArea _textArea;

        [NonSerialized]
        private Font _font;

        [NonSerialized]
        private BurstMethodTreeView _treeView;

        private int FontSize => FontSizes[_fontSizeIndex];


        public BurstInspectorGUI()
        {
            _burstDisassembler = new BurstDisassembler();
        }

        public void OnEnable()
        {
            if (_treeView == null) _treeView = new BurstMethodTreeView(new TreeViewState());
            _safetyChecks = BurstCompiler.Options.EnableBurstSafetyChecks;
        }

        private void CleanupFont()
        {
            if (_font != null)
            {
                DestroyImmediate(_font, true);
                _font = null;
            }
        }

        public void OnDisable()
        {
            CleanupFont();
        }

        private void FlowToNewLine(ref float remainingWidth, float resetWidth, GUIStyle style, GUIContent content)
        {
            float spaceRemainingBeforeNewLine = EditorStyles.toggle.CalcSize(new GUIContent("WWWW")).x;

            remainingWidth -= style.CalcSize(content).x;
            if (remainingWidth <= spaceRemainingBeforeNewLine)
            {
                remainingWidth = resetWidth;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }

        private void RenderButtonBars(float width, BurstCompileTarget target, out bool doCopy, out int fontIndex)
        {
            float remainingWidth = width;

            var contentDisasm = new GUIContent("Enhanced Disassembly");
            var contentSafety = new GUIContent("Safety Checks");
            var contentCodeGenOptions = new GUIContent("Auto");
            var contentLabelFontSize = new GUIContent("Font Size");
            var contentFontSize = new GUIContent("99");
            var contentCopyToClip = new GUIContent("Copy to Clipboard");

            GUILayout.BeginHorizontal();

            _enhancedDisassembly = GUILayout.Toggle(_enhancedDisassembly, contentDisasm, EditorStyles.toggle);
            FlowToNewLine(ref remainingWidth, width, EditorStyles.toggle, contentDisasm);

            _safetyChecks = GUILayout.Toggle(_safetyChecks, contentSafety, EditorStyles.toggle);
            FlowToNewLine(ref remainingWidth, width, EditorStyles.toggle, contentSafety);

            EditorGUI.BeginDisabledGroup(!target.HasRequiredBurstCompileAttributes);

            _targetCpu = (TargetCpu)EditorGUILayout.Popup((int)_targetCpu, TargetCpuNames, EditorStyles.popup);
            FlowToNewLine(ref remainingWidth, width, EditorStyles.popup, contentCodeGenOptions);

            GUILayout.Label("Font Size", EditorStyles.label);
            fontIndex = EditorGUILayout.Popup(_fontSizeIndex, _fontSizesText, EditorStyles.popup);
            FlowToNewLine(ref remainingWidth, width, EditorStyles.label,contentLabelFontSize);
            FlowToNewLine(ref remainingWidth, width, EditorStyles.popup,contentFontSize);

            doCopy = GUILayout.Button(contentCopyToClip, EditorStyles.miniButton);
            FlowToNewLine(ref remainingWidth, width, EditorStyles.miniButton,contentCopyToClip);
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            _disasmKind = (DisassemblyKind) GUILayout.Toolbar((int) _disasmKind, DisassemblyKindNames, GUILayout.Width(width));
        }

        public void OnGUI()
        {
            // Make sure that editor options are synchronized
            BurstEditorOptions.EnsureSynchronized();

            if (_targets == null)
            {
                _targets = BurstReflection.FindExecuteMethods(AssembliesType.Editor);
                foreach (var target in _targets)
                {
                    // Enable burst compilation by default (override globals for the inspector)
                    // This is not working as expected. This changes indirectly the global options while it shouldn't
                    // Unable to explain how it can happen
                    // so for now, if global enable burst compilation is disabled, then inspector is too
                    //target.Options.EnableBurstCompilation = true;
                }

                // Order targets per name
                _targets.Sort((left, right) => string.Compare(left.GetDisplayName(), right.GetDisplayName(), StringComparison.Ordinal));

                _treeView.Targets = _targets;
                _treeView.Reload();

                if (_selectedItem != null) _treeView.TrySelectByDisplayName(_selectedItem);
            }

            if (_fontSizesText == null)
            {
                _fontSizesText = new string[FontSizes.Length];
                for (var i = 0; i < FontSizes.Length; ++i) _fontSizesText[i] = FontSizes[i].ToString();
            }

            if (_fontSizeIndex == -1)
            {
                _fontSizeIndex = EditorPrefs.GetInt(FontSizeIndexPref, 5);
                _fontSizeIndex = Math.Max(0, _fontSizeIndex);
                _fontSizeIndex = Math.Min(_fontSizeIndex, FontSizes.Length - 1);
            }

            if (_fixedFontStyle == null)
            {
                _fixedFontStyle = new GUIStyle(GUI.skin.label);
                string fontName;
                if (Application.platform == RuntimePlatform.WindowsEditor)
                  fontName = "Consolas";
                else
                  fontName = "Courier";

                CleanupFont();

                _font = Font.CreateDynamicFontFromOSFont(fontName, FontSize);
                _fixedFontStyle.font = _font;
                _fixedFontStyle.fontSize = FontSize;
            }

            if (_searchField == null) _searchField = new SearchField();

            if (_textArea == null) _textArea = new LongTextArea();

            GUILayout.BeginHorizontal();

            // SplitterGUILayout.BeginHorizontalSplit is internal in Unity but we don't have much choice
            SplitterGUILayout.BeginHorizontalSplit(TreeViewSplitterState);

            GUILayout.BeginVertical(GUILayout.Width(position.width / 3));

            GUILayout.Label("Compile Targets", EditorStyles.boldLabel);

            var newFilter = _searchField.OnGUI(_treeView.Filter);

            if (newFilter != _treeView.Filter)
            {
                _treeView.Filter = newFilter;
                _treeView.Reload();
            }

            _treeView.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)));

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            var selection = _treeView.GetSelection();
            if (selection.Count == 1)
            {
                var targetIndex = selection[0];
                var target = _targets[targetIndex - 1];
                var targetOptions = target.Options;

                // Stash selected item name to handle domain reloads more gracefully
                _selectedItem = target.GetDisplayName();
                
                // Refresh if any options are changed
                bool doCopy;
                int fontSize;
                // -14 to add a little bit of space for the vertical scrollbar to display correctly
                RenderButtonBars((position.width*2)/3 - 14, target, out doCopy, out fontSize);

                // We are currently formatting only Asm output
                var isTextFormatted = _enhancedDisassembly && _disasmKind == DisassemblyKind.Asm;

                // Depending if we are formatted or not, we don't render the same text
                var textToRender = isTextFormatted ? target.FormattedDisassembly : target.RawDisassembly;

                // Only refresh if we are switching to a new selection that hasn't been disassembled yet
                // Or we are changing disassembly settings (safety checks / enhanced disassembly)
                var targetRefresh = textToRender == null
                                    || target.DisassemblyKind != _disasmKind
                                    || targetOptions.EnableBurstSafetyChecks != _safetyChecks
                                    || target.TargetCpu != _targetCpu
                                    || target.IsDarkMode != EditorGUIUtility.isProSkin;

                bool targetChanged = _previousTargetIndex != targetIndex;

                _previousTargetIndex = targetIndex;
                
                if (targetRefresh)
                {
                    // TODO: refactor this code with a proper AppendOption to avoid these "\n"
                    var options = new StringBuilder();

                    target.TargetCpu = _targetCpu;
                    target.DisassemblyKind = _disasmKind;
                    targetOptions.EnableBurstSafetyChecks = _safetyChecks;
                    target.IsDarkMode = EditorGUIUtility.isProSkin;
                    targetOptions.EnableBurstCompileSynchronously = true;

                    string defaultOptions;
                    if (targetOptions.TryGetOptions(target.IsStaticMethod ? (MemberInfo)target.Method : target.JobType, true, out defaultOptions))
                    {
                        options.Append(defaultOptions);

                        options.AppendFormat("\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionTarget, TargetCpuNames[(int)_targetCpu]));

                        options.AppendFormat("\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDebug));

                        var baseOptions = options.ToString().Trim('\n', ' ');

                        target.RawDisassembly = GetDisassembly(target.Method, baseOptions + DisasmOptions[(int)_disasmKind]);

                        if (isTextFormatted)
                        {
                            // Store the formatted version
                            target.FormattedDisassembly = _burstDisassembler.Process(target.RawDisassembly, IsIntel(_targetCpu) ? BurstDisassembler.AsmKind.Intel : BurstDisassembler.AsmKind.ARM, target.IsDarkMode);
                            textToRender = target.FormattedDisassembly;
                        }
                        else
                        {
                            target.FormattedDisassembly = null;
                            textToRender = target.RawDisassembly;
                        }
                    }
                }

                if (textToRender != null)
                {
                    _textArea.Text = textToRender;
                    if (targetChanged) _scrollPos = Vector2.zero;
                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, true, true);
                    _textArea.Render(_fixedFontStyle);
                    GUILayout.EndScrollView();
                }

                if (doCopy)
                {
                    // When copying to the clipboard, we copy the non-formatted version
                    EditorGUIUtility.systemCopyBuffer = target.RawDisassembly ?? string.Empty;
                }

                if (fontSize != _fontSizeIndex)
                {
                    _fontSizeIndex = fontSize;
                    EditorPrefs.SetInt(FontSizeIndexPref, fontSize);
                    _fixedFontStyle = null;
                }
            }

            GUILayout.EndVertical();

            SplitterGUILayout.EndHorizontalSplit();

            GUILayout.EndHorizontal();
        }

        private static string GetDisassembly(MethodInfo method, string options)
        {
            try
            {
                var result = BurstCompilerService.GetDisassembly(method, options);
                if (result.IndexOf('\t') >= 0)
                {
                    result = result.Replace("\t", "        ");
                }

                // Workaround to remove timings
                if (result.Contains("Burst timings"))
                {
                    var index = result.IndexOf("While compiling", StringComparison.Ordinal);
                    if (index > 0)
                    {
                        result = result.Substring(index);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                return "Failed to compile:\n" + e.Message;
            }
        }

        private static bool IsIntel(TargetCpu cpu)
        {
            switch (cpu)
            {
                case TargetCpu.Auto:
                case TargetCpu.X86_SSE2:
                case TargetCpu.X86_SSE4:
                case TargetCpu.X64_SSE2:
                case TargetCpu.X64_SSE4:
                case TargetCpu.AVX:
                case TargetCpu.AVX2:
                case TargetCpu.AVX512:
                    return true;
            }
            return false;
        }
    }

    internal class BurstMethodTreeView : TreeView
    {
        public BurstMethodTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
        }

        public BurstMethodTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            showBorder = true;
        }

        public List<BurstCompileTarget> Targets { get; set; }
        public string Filter { get; set; }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            var allItems = new List<TreeViewItem>();

            if (Targets != null)
            {
                allItems.Capacity = Targets.Count;
                var id = 1;
                var filter = Filter;
                foreach (var t in Targets)
                {
                    var displayName = t.GetDisplayName();
                    if (string.IsNullOrEmpty(filter) || displayName.IndexOf(filter, 0, displayName.Length, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        allItems.Add(new TreeViewItem {id = id, depth = 0, displayName = displayName});

                    ++id;
                }
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        internal void TrySelectByDisplayName(string name)
        {
            var id = 1;
            foreach (var t in Targets)
                if (t.GetDisplayName() == name)
                {
                    SetSelection(new[] {id});
                    FrameItem(id);
                    break;
                }
                else
                {
                    ++id;
                }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var target = Targets[args.item.id - 1];
            var wasEnabled = GUI.enabled;
            GUI.enabled = target.HasRequiredBurstCompileAttributes;
            base.RowGUI(args);
            GUI.enabled = wasEnabled;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
    }
}
