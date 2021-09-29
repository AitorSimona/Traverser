
# Animation Rigging Menu

The Animation Rigging menu contains useful utilities to help with the rigging workflow.

## Align

Use Align to align the Transform, Position, or Rotation of a GameObject to another GameObject. This is particularly useful when aligning effectors to bones, or props to an effector.
To use Align, select the GameObject you want to align and then select the GameObject with the transform that you want to align to. In the Animation Rigging menu, select the appropriate align option.

|Tool|Description|
|---|---|
|Align Transform|Updates the Position and Rotation of the GameObject.|
|Align Rotation|Updates the Rotation of the GameObject.|
|Align Position|Updates the Position of the GameObject.|

## Restore Bind Pose

Use the Restore Bind Pose option to restore the Skinned Mesh Renderer bind pose that was originally imported. This is useful for restoring the original character pose of a skinned mesh. This option will only restore poses for bones that are used by skinning. The restore bind pose option may not work as expected for setups where the mesh is only skinned to twist bones.

## Rig Setup

Use the Rig Setup option to set up the required components for Animation Rigging on the selected object.

To use Rig Setup, select a GameObject hierarchy with an Animator component on which you want to create the constraints. Then, select the Rig Setup option.
This creates a RigBuilder component on the selected GameObject and a child GameObject named "Rig 1" with a Rig component that will be added to the RigBuilder layers.

Note: Rig Setup is also found in the Animator component options.

## Bone Renderer Setup

Use the Bone Renderer Setup option to create a BoneRenderer component with bones extracted from children SkinnedMeshRenderer components in the selected GameObject hierarchy.
To use Bone Renderer Setup, select the root GameObject and select Bone Renderer Setup.

Note: Bone Renderer Setup is also found in the Animator component options.

