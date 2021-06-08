# Traverser
### *By Aitor Simona*

Traverser is a player traversal toolkit featuring as of now the most basic traversal abilities which are Locomotion, Parkour and Climbing. It is self-contained in a set of scripts and has no dependencies apart from the basic Unity functionality. Use the given abilities or expand the system through its shared ability workflow.

## HOW TO USE 

In github, click on Code and copy the git URL. Go to Unity's Package manager, and add the package through this git URL. Unity's package manager will automatically
download the files and add them to your project.

Unfortunately Unity's current default input manager does not allow to create entries directly from code, so you have to do this yourself.

Create input entries in Edit->ProjectSettings->InputManager.

Xbox-like gamepads supported

- Horizontal -> Default

- Vertical -> Default

- A Button -> 
	- Alt Positive Button: joystick button 0
	- Gravity / Dead / Sensitivity: 0 / 0 / 0
	- Snap / Invert: Unchecked
	- Type: Key or Mouse Button
	- Axis: X axis
	- Joy Num: Get Motion from all Joysticks

- B Button -> 
	- Alt Positive Button: joystick button 1
	- Gravity / Dead / Sensitivity: 0 / 0 / 0
	- Snap / Invert: Unchecked
	- Type: Key or Mouse Button
	- Axis: X axis
	- Joy Num: Get Motion from all Joysticks

- Right Joystick Y -> 
	- Gravity / Dead / Sensitivity: 0 / 0.19 / 1
	- Snap / Invert: Unchecked
	- Type: Joystick Axis
	- Axis: 5th axis (Joysticks)
	- Joy Num: Get Motion from all Joysticks

- Right Joystick X -> 
	- Gravity / Dead / Sensitivity: 0 / 0.19 / 1
	- Snap / Invert: Unchecked
	- Type: Joystick Axis
	- Axis: 4th axis (Joysticks)
	- Joy Num: Get Motion from all Joysticks

- Left Joystick Button -> 
	- Alt Positive Button: joystick button 8
	- Gravity / Dead / Sensitivity: 0 / 0 / 0
	- Snap / Invert: Unchecked
	- Type: Joystick Axis
	- Axis: 3rd axis (Joysticks and Scrollwheel)
	- Joy Num: Get Motion from all Joysticks

## CONTROLS

- Connect a gamepad (xbox)
- move with left joystick, aim with right joystick
- Run keeping left joystick pressed while moving
- Use B to climb to wall, dismount
- Use A to pull up from a ledge, drop down to ledge
- Use A to vault and get on top of platforms
- Use B to drop from platforms

## DEPENDENCIES

- Basic Unity functionality, core engine
- Unity's animation system, Mecanim's humanoid. 

## TOOLS USED

- Microsoft Visual Studio 2019
- Unity 2020.2.2f1
- Kinematica's 0.8 demo assets
- Mixamo character and several animations
- Autodesk Maya 2019
