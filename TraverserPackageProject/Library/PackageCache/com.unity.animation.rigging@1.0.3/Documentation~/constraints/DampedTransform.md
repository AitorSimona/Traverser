# Damped Transform

![Example](../images/constraint_damped_transform/damped_transform.gif)

The damped transform constraint allows damping the position and rotation transform values from the source GameObject to the constrained GameObject.
The Maintain Aim option forces the constrained object to always aim at the source object.

![Component](../images/constraint_damped_transform/damped_transform_component.png)

|Properties|Description|
|---|---|
|Weight|The weight of the constraint. If set to 0, the constraint has no influence on the Constrained Object while when set to 1, it applies full influence given the specified settings.|
|Constrained Object|The GameObject affected by the Source GameObjects.|
|Source|The GameObject that affects the constrained GameObject.|
|Damp Position|Damp position weight. If set to 0, no damping is applied object follows source while if set to 0, full damping is applied.|
|Maintain Aim|When enabled, the original orientation of Constrained GameObject to the Source Object is maintained.|
