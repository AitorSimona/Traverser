
# Rigging workflow

The following sections present the steps and the required components for defining an Animation Rig. This [**video**](https://youtu.be/DzW_jQGO1dc) demonstrates how to use these package component to create a custom Animation Rig.

## Defining an Animation Rig

A Rig is a hierarchy of GameObjects that use a minimum of these four components: 

* Animator Component
* Rig Builder Component
* Rig Component
* Constraints

A typical setup is to have a hierarchy of GameObjects with an Animator component added to the root GameObject.

To make this hierarchy available to the Animation Rig, the **Rig Builder** component is required. Once the **Rig Builder** component is added to the root, a _Rig GameObject [ex: Rig Setup]_ can be created as a child of this root. To enable animation rigging, the _Rig GameObject_ must contain the **Rig** Component. Then, to connect the Rig to the Animator, the _Rig GameObject [ex: Rig Setup]_ needs to be assigned to the _Rig Layer_ field of the *Rig Builder* component. This will associate the Animation Rig with the Animator.

![Rig Setup](images/rig_setup/rig_setup.gif)

The following illustration represents a schematic overview of the interdependencies between the Animator and the Animation Rig components.

![Rig Setup Overview](images/rig_setup/rig_setup_overview.png)

Once the _Rig GameObject (ex: Rig Setup)_ holding the **Rig** component is connected to the **Rig Builder** component,  rig elements (effector GameObjects) that hold rig Constraint components can be added under the _Rig GameObject (ex: Rig Setup)_ hierarchy. Different rig elements can be organized and hierarchically structured to create any kind of Rig, in order to accommodate any rigging requirement.  

Source GameObjects for Constraints, such as Target Effectors or Hint Effectors, can be placed under their associated _Rig GameObject (ex: Rig Setup)_. In the following illustration, the **Left Leg** is acting as the Rig element that contains a **Two Bone IK Constraint** component which is at the same time the parent of both **LeftFootEffector** and **LeftLegHint** source objects.  

![Source Object Example](images/rig_setup/source_object_example.png) 


## Rig Builder Component

The Rig Builder component lives alongside the Animator component and creates a Playable Graph that is appended to the existing Animator state machine.
The Rig Builder component needs to be added to the GameObject that has the Animator component.  Rig Builder needs to affect the same hierarchy as the Animator.

![Rig Builder Setup](images/rig_builder/rig_builder_setup.gif)

Using **Rig Layers**, the Rig Builder component allows for stacking of multiple Rigs, that can be enabled/disabled at any time.

![Rig Builder Layers](images/rig_builder/rig_builder_layers.gif)


## Rig Component

The Rig component is the main entry point to all rig constraints for a given Rig. This component is assigned to a Rig Builder component under the Rig Layer field.
There should only be one Rig component per control rig hierarchy. For multiple rigs, multiple control rig hierarchies can be assigned to different Rig Layers,
and enabled/disabled independently. The main purpose of the Rig component is to collect all Constraint components defined under its local hierarchy and
generate an ordered list (evaluation order) of _IAnimationJobs_, which will then be applied after the Animator evaluation. The order in which the jobs are
evaluated is defined by component order and the way the rig hierarchy is constructed, since constraints are gathered using _GetComponentsInChildren_,
which follows depth-first traversal as shown below:

![Rig Constraint Evaluation Order](images/rig/eval_order.png)

In other words, grouping constraints under a GameObject allows the user to the manage the evaluation order of these constraints
by modifying the hierarchy.

Control rig hierarchies should hold all the necessary rig elements such as effectors, constraints, and other objects/elements required by the constraint definitions.
The root of a control rig hierarchy should be at the same level as the skeleton root, both under the Game Object holding the Animator. In other words,
it should not be in the skeleton hierarchy, but rather live beside it.

![Rig Setup](images/rig/rig_setup.gif)

![Rig Weight](images/rig/rig_weight.gif)


Rig components, like all Constraint components, have a Weight property that can be used, animated, and scripted to enable/disable
or ease-in/ease-out an entire control rig hierarchy.


## Bone Renderer Component

The Bone Renderer component allows the user to define a transforms hierarchy to be drawn as bones for visualization and selection during the rigging process.
These bones are not visible in the Game view. This, for example, allows the user to define his character deform skeleton for rigging purposes.

![Bone Renderer Setup](images/bone_renderer/bone_renderer_setup.gif)
![Bone Renderer Component](images/bone_renderer/bone_renderer_component.gif)

The look of the bones can be customized. The Bone Size, Shape and Color can be modified.
Tripods of local axes can also be displayed and their size adjusted to accommodate user preference.
The user can choose from one of the default looks; Pyramid, Line or Box.

![Bone Look Pyramid](images/bone_renderer/bone_looks.png)

## Rig Effectors

Similarly to bones, Rig Effectors allow the user to add visual gizmos to transforms for visualization and selection.  These can be added to any transform
in the same hierarchy as the **Rig Builder** or **Rig** component.  Effectors are not visible in the Game view.  A special Scene View overlay has been added
to manage and customize effectors in the Rig hierarchy.

![Rig Effector Overlay](images/rig_effector/rig_effector_setup.gif)

The look of the effectors can also be customized.  The Effector Size, Shape, Color, Offset Position and Offset Rotation can be modified.
The shape can be any **Mesh** asset available in the project.  Multiple effectors can be created, deleted and edited at once.

![Rig Effector Shapes](images/rig_effector/rig_effector_shapes.png)


## Rig Transform

When a specific GameObject part of your rig hierarchy is important for manipulation but not referenced by any rig constraints, you'll want to add the **RigTransform** component which is found under _Animation Rigging/Setup_.
As shown in the video below, in order to manipulate both the left and right foot IK targets (_lfik_ and _rfik_) of the _2BoneIK_ sample using
their parent transform (_ik_ ), you will need to add this component to get the expected behavior.

![Rig Transform](images/rig_transform/rig_transform_manipulation.gif)

