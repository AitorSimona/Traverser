# Kinematica (com.unity.kinematica)

![Kinematica](/Documentation~/images/kinematica-banner.png "Kinematica")

Kinematica is a next-generation character animation system.

Producing animated characters for video games in an actual production environment is subject to a large number of requirements and demands. Trying to find a solution that doesn’t suffer from the ever-growing complexity of animation graphs while meeting production constraints is a tremendously difficult proposition.

We have seen a large number of exciting new research from the machine learning community over the last couple of years that tries to address this problem. In general, these approaches face a trade-off between automation and control, and different approaches position themselves at different places with respect to this dilemma. And although most them deliver on the promise of automatically generating animations, they come with various drawbacks. Many approaches limit themselves to a particular type of movement (for example locomotion) to achieve automation. Some are expensive in terms of runtime performance, limiting their usage to off-line applications. Others require prohibitively long data preparation and/or machine learning processes resulting in slow turn-around times. To the best of our knowledge no approach exists today that could rival animation graph based systems as the default solution for character animation in games.

Kinematica is a tremendously ambitious attempt to provide a solution for character animation that does not rely on animation graphs while achieving a higher level of quality and at the same time offering the same level of control, versatility and flexibility. At the heart of Kinematica lies a considerably improved version of [Motion Matching](https://www.youtube.com/watch?v=z_wpgHFSWss) that achieves a near constant time lookup for arbitrary complex motion databases and does not reply on parameter tweaking or magic numbers.

This package is still in an experimental stage and we require the help and feedback of the community to verify the ideas and workflows presented here. Even though the intended idea is to replace animation graphs with this system we haven't achieved feature parity yet and none of the ideas here are set in stone. Do not hesitate to let us know what you think - we would love to incoorporate any kind of constructive feedback into this solution.

## Required Software

Unity 2019.3

## Documentation

Documentation for Kinematica is available in [unpublished](Documentation~/index.md) and [published](https://docs.unity3d.com/Packages/) form.

For further discussion, [please visit the forum](https://forum.unity.com/forums/).