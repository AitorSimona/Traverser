namespace Unity.Kinematica.Editor
{
    [GraphNode(typeof(NavigationPath))]
    internal class NavigationNode : GraphNode
    {
        public override void OnSelected(ref MotionSynthesizer synthesizer)
        {
            using (NavigationPath navPath = GetDebugObject<NavigationPath>())
            {
                navPath.DrawPath();
            }
        }
    }
}
