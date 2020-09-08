# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/ )
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html ).

## [0.8.0-preview] - 2020-07-31

### Added
- Experimental motion matching cost timeline added to the snapshot recorder

### Changed
- __Changes in 0.8.0-preview include breaking API changes.__ _Users of the Kinematica Demo project will also need to download the latest version if they wish to use Kinematica 0.8.0-preview_
- Kinematica requires Unity 2019.4
- Completely removed Kinematica task graph. Now, using Kinematica requires to call query functions, ideally inside jobs for performance, instead of instanciating graph in code.
Execution graph window is still present (Window -> Analysis -> Kinematica Execution Window) and only shows debug graph related to motion matching queries.
- Kinematica debug draw info is now mainly displayed in game & scene views, and can manipulated from the snapshot debugger. Execution graph nodes can still be selected to display specific information
on screen, otherwise the first motion matching query happening during last frame is displayed on screen.
- Snapshot recorder is now autoscrolled when recording
- Execution graph and snapshot debugger UI layout and colors improved
- Snapshot recorder can now be activated and deactivated in playmode by clicking on "Start/Stop recording" buttons.
It's now possible to resume play from any point in the debugger timeline.
- Kinematica doesn't requires a task graph anymore, query API is directly usable from code. Execution graph still exists for debugging
- Added snapping to marker and tag manipulations in the timeline. Manipulating markers and tags will snap to other annotations in the timeline and to the start and end of the AnimationClip
- Manipulating tags and markers in the timeline with previewing enabled will update the in-scene preview
- Annotation inspectors will display start and duration times in seconds or frames depending on display setting of the timeline

### Fixed
- When switching asset while previewing, target rig wasn't updated resulting in garbage pose. Target rig is now updated.
- Fixed command and ctrl modifiers when using macos

## [0.7.0-preview] - 2020-06-25

### Fixed
- Fixed possible NaN in ScalarTransition
- Fixed borders of metric visualization so it is now possible to differentiate between adjacent metrics
- The Asset Builder will now properly update when changes are made in the inspector window

### Added
- Added fragment comparison debug info (including pose & trajectory cost) in task graph for match fragment task
- Added SynthesizerHolder to regroup all memory allocators related to the synthesizer and add required methods to forward to the synthesizer
- The Kinematica Job write the root motion into the AnimationStream in order to be able to use the normal deltaPosition and deltaRotation in the OnAnimatorMove()
- Added procedural notations to allow for example converting AnimationEvents to Marker or Tag Annotations at build time or as a right-click option in the Asset Builder.
- Added limited debug draw lines in the TaskGraph for HDRP
- Added version control checks prior to build to ensure that the binary and debug file can be written to
- Context menu options to add new Markers on the marker track
- Body joint index automatically computed from avatar import options

### Changed
- Renamed task creation functions for clarity and gather them into a single place inside TaskReference struct
- Kinematica Job made public and independent from the Kinematica component (deltatime propery handle)
- The TaskGraph and Asset Builder uses the ISynthesizerProvider interface and no longer refer to the Kinematica component
- When initilizing the MotionSynthesizer we use the world transform instead of local tranform to support nested animator component in the hierarchy and because the root motion is applied in world in the Kinematica component
- Removing unused parameter from TaggedAnimationClip.Create
- Animation clips are loaded only when necessary and a progress bar is displayed
- Kinematica Asset build process is asynchronous, cancellable and done in burst jobs
- Improved selection of overlaping Marker elements to include markers that are very close to each other in the timeline

## [0.6.0-preview] - 2020-05-29

### Fixed
- Fixed inaccurate animation clip length computation when clip was over 5 minutes long
- Fixed memory corruption potentially happening if a memory identifier is invalid
- Fixed NavigationTask desired trajectory not cleared when goal was reached
- Fixed crash in Task Graph
- Fixed bug when deleting selected marker and then trying to manipulate another marker on the same animation clip

### Added
- Added function to MotionSynthesizer to check a memory identifier is valid and prevent errors in client code
- Added option to create TrajectoryPrediction with provided current velocity
- Added tag deletion button to the inspector window
- Added a ping in the project window when double clicking on an animation clip in the animation library
- Added message in the inspector window that tags and markers cannot be edited when multi-selecting
- Added an introductory guide to the Hello World sample instructing users how to get started with Kinematica
- Added preview playback controls to the asset builder

### Changed
- Renamed SteerRootDeltaTransfrom into SteerRootMotion and expose intermediate function in API
- Exposed MotionSynthesizer.CurrentVelocity in API
- Animation clips with looping flag set will automatically set their boundary clips to themselves when added to the animation library

## [0.5.0-preview.1] - 2020-05-05

### Fixed
- Fixed timeline inspectors not updating when manipulating tags in timeline
- Fixed warning in InputUtility
- Fixed issue where Boundary clips could not be cleared
- Fixed issue where small tags would be moved when selected
- Fixed an issue where using the AutoFrame (A) key would have inconsistent behaviour
- Fixed an issue where undo/redo was not correctly updating preview
- Fixed an issue where deleting a previewed game object or creating new scene during preview would cause console error spam
- Fixed missing label in Annotation property drawers
- Fixed possible event leaks relating to Marker manipulation guidelines
- Fixed an issue where some debug information in the task graph and in-scene weren't showing up properly on HDRP
- Fixed ReflectionTypeLoadException exception occuring when using Microsoft Analysis assembly
- Fixed exception thrown in debug frame info when synthesizer was invalid
- Fixed TrajectoryFragment node debug display which were displaying wrong information and throwing exception
- Fixed wrong SequenceTask documentation
- Fixed tag manipulation so users can re-order and change tag time in the same operation
- Fixed Metric track updating during undo/redo and when changing boundary clips
- Fixed constant repaint of the snapshot debugger window to only occur during play mode
- Fixed guideline flickering when creating tags while previewing
- Fixed the GameObject icon in the timeline when using the personal skin
- Fixed crash in Navigation sample happening after second click in game view
- Fixed tag detection in builder which were too lenient. Among other things, it was assuming boundary clips duration was as long as time horizon

### Added
- Added vertical guidelines and time labels when manipulating tags and markers
- Added new dropdown control to choose in-scene preview target
- Added sample showcasing Scene manipulators for manipulating position and rotation fields in annotations
- Added current clip name and frame debug information to TaskGraph TimeIndex nodes.
- Added mouse hover highlighting to Preview Selector element clarifying how it can be interacted with
- Added a Scene Hierarchy ping to the current Preview Target
- Added context menu option to the Animation library to manage boundary clips on multiple AnimationClips at the same time
- Added explicit loop and reset when executed options to SequenceTask to make it more intuitive
- Added "none" option to the preview/debug target dropdown
- Added readmes to samples

### Changed
- Changed the layout of the Builder Window to improve the use of space available and hideable elements
- Changed the styling of tags and markers in the Timeline view
- Changed the behaviour of the animation preview to behave more similarly to Unity Timeline
- Changed preview activation conditions. Preview will now turn on automatically if the playhead is moved and a valid target is selected
- Re-enabled animation frame debugger
- Updated dependency on com.unity.collections to 0.5.1-preview.11
- Updated dependency on com.unity.jobs to 0.2.4-preview.11
- Updated dependency on com.unity.burst to 1.2.3
- Gutter track toggles are now in a dropdown menu
- Active time field is smaller
- Samples given more unified look and some experience improvements
- Snapshot debugger's timeline now supports alt+LMB to pan the time area
- DebugDraw.Begin/End are now public

## [0.4.0-preview] - 2020-03-19
### This is the first release of *unity.com.kinematica*.
 - Kinematica Asset authoring - tagging, markers, metrics
 - Kinematica Runtime - motion matching, debugging, task graph, retargeting (Unity 2020.1+)
 - Snapshot Debugger - data agnostic debugging and playback, used by kinematic to provide debugging support.
