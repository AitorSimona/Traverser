using UnityEditor;
using UnityEngine;

public class TraverserRootMotion : EditorWindow
{
    [Header("Animation Data Assets")]
    public int t;
    private AnimationClip clip;
    private string boneName;

    [MenuItem("Traverser/Traverser Editor %g")]
    public static void OpenWindow()
    {
        GetWindow<TraverserRootMotion> ("Traverser Editor");
    }

    void OnEnable()
    {
        // cache any data you need here.
        // if you want to persist values used in the inspector, you can use eg. EditorPrefs
    }

    void OnGUI()
    {
        //Draw things here. Same as custom inspectors, EditorGUILayout and GUILayout has most of the things you need
        GUILayout.Label("Animation Data Assets", EditorStyles.largeLabel);


        if (GUILayout.Button("Build Root motion curves"))
        {
            // do the interesting thing.
        }

        boneName = EditorGUILayout.TextField("Bone Name", boneName);
        clip = EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false) as AnimationClip;

        EditorGUILayout.LabelField("Curves:");
        if (clip != null)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (boneName != null && binding.propertyName.Contains(boneName))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    //clip.SetCurve("", typeof(Transform), "testCurve", curve);
                    //AnimationUtility.SetEditorCurve(clip, EditorCurveBinding., curve);
                    EditorGUILayout.LabelField(binding.path + "/" + binding.propertyName + ", Keys: " + curve.keys.Length);
                }
            }
        }

    }
}