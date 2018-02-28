# Sphere Ray-cast using Unity
--------------------------

## Project Description

This project provides scripts needed to implement Sphere Ray-casting and an example scene that shows how it works.

Sphere Ray-casting is a wide-range 3D raycasting method that uses Unity [Physics.SphereCast()](https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html) and [Physics.RayCast()](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html) to detect the best GameObject that can be interacted.

## Table of Content

<!--ts-->
* [How to Setup](#how-to-setup)
  * [Requirements](#requirements)
  * [Deployment](#deployment)
* [Example Overview](#example-overview)
  * [Requirement and Deployment](#requirement-and-deployment)
  * [Running the Test](#running-the-test)
* [Performance Overview](#performance-overview)
* [License](#license)
<!--te-->

## How to Setup

These explanation will get you through implementing sphere raycast on any Unity Project with versions that allow [Physics.SphereCast()](https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html) or [Physics.RayCast()](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html).

### Requirements

* 3D Unity scene
* Any kind of FPS control with camera attached

### Deployment

1. Enabling Ray-cast
* Attach either __DetectInteractableObject.cs__ or __DetectInteractableObjectComparative.cs__ to the FPS character. If main camera is attached to the child, attach it to that child.
* Attach __InteractWithSelectedObject.cs__ to the same GameObject.
* Adjust editor fields to acquire desired range. See comments on scripts for detail.
2. Making Detectable GameObject
* Create a MonoBehaviour class that implements IInteractable
* Add code for desired interaction inside Interact() method, which will be called when __Interact__ input is pressed while this object is detected.
* Attach the created MonoBehaviour script on GameObjects you want player to interact with.

## Example Overview

These explanation describes the provided example scene [Assets/Scenes/SphereCastTest.unity](https://github.com/ALee1303/Sphere-Raycasting/tree/master/Assets/Scenes).

### Requirement and Deployment
* Project Version: Unity2017.3.1
* To avoid version conflict, import package __SphereCastTest.unitypackage__ into an existing project and open the scene.
* I recommend running test with editor screen on to see full functionality.

### Running the Test
* Scene consit of Unity FirstPersonCharacter controller that implements both versions of raycast.
* move around the scene and click left-mouse to see which interactables are dectected on various state.
* Enable one of the Detect scripts attached to FPSController's child to check each functionality.
* White, blue, and pink objects have interactable implemented and will display message on Console Log when interacted.
* red objects are not interactable and will block the interactable objects from being detected.
* Gizmos will be shown on editor screen for details:
  * White lines will be displayed on every object checked by sphere ray-cast.
  * Green line shows which object will be interacted when left-mouse is clicked.
  * Yellow spheres represent __DetectInteractableObject.cs__ range.
  * Red spheres represent __DetectInteractableObjectComparative.cs__ range.

## Performance Overview

Sphere Ray-cast allows wider ray-casting method in first-person game by first gathering all objects inside a range created by sweeping a sphere in front of character and determining if they're blocked by any object.

It uses Physics.SphereCast() to query every objects collided by SphereCast. This returns an array of RayCastHit sorted by distance from player.

<<<<<<< HEAD
```
=======
```C#
>>>>>>> 2cc4416... Update README.md
allHits = Physics.SphereCastAll(this.transform.position, castRadius,
				this.transform.forward, castDistance); // spherecast to find the objects.
```

Then out of these objects it uses angle comparison and to determine the best object to interact with. It also uses Physics.RayCast() to check if theres anything blocking the object from the player.

There are two versions of sphere ray-cast:

1. DetectInteractableObject.cs:
* This script simply sorts all objects collected by angle from center then returns the closest one to the center that is not blocked.
<<<<<<< HEAD

2.DetectInteractableObjectComparative.cs:
<<<<<<< HEAD
- This scrit compares the angle between each objects collected and returns the most optimal one. This may not be an object with the smallest angle from the center.
- This script uses greedy algorithm and will have better runtime-complexity than the other one.
- It either returns an object with the closest angle from the camera or closest distance from the player. It also allows a light-weight Observer(Event) patter in designing GameObject interaction.
 - If an object is close enough to center by set angle and is also closest object by distance from player to be so, it becomes an object picked.
 - If no such object was found, the closest object to the center will be returned.
- Check comments on script for more detail.
=======
=======
2. DetectInteractableObjectComparative.cs:
>>>>>>> cc49163... Update README.md
* This scrit compares the angle between each objects collected and returns the most optimal one. This may not be an object with the smallest angle from the center.
* This script uses greedy algorithm and will have better runtime-complexity than the other one.
* It either returns an object with the closest angle from the camera or closest distance from the player. It also allows a light-weight Observer(Event) patter in designing GameObject interaction.
  * If an object is close enough to center by set angle and is also closest object by distance from player to be so, it becomes an object picked.
  * If no such object was found, the closest object to the center will be returned.
* Check comments on script for more detail.
>>>>>>> 9935479... Update README.md

_1 is easier to implement than 2, but 2 has better control and performance._

**_1 Has <img src="https://latex.codecogs.com/gif.latex?O(n^2)" title="O(n^2)" /> worstcase runtime. 2 has <img src="https://latex.codecogs.com/gif.latex?O(nlgn)" title="O(nlgn)" />_**

<<<<<<< HEAD
<<<<<<< HEAD
In order for GameObjects to be detected by this ray-cast, it must implement "IInteractable" interface, also provided by this project.
<<<<<<< HEAD

## Example Overview

These explanation describes the provided example scene [Assets/Scenes/SphereCastTest.unity](https://github.com/ALee1303/Sphere-Raycasting/tree/master/Assets/Scenes).

### Prerequisite
* Project Version: Unity2017.3.1
* To avoid version conflict, import package "SphereCastTest.unitypackage" into an existing project and open the scene.
* I recommend running test with editor screen on to see full functionality.

### Running the Test
<<<<<<< HEAD
- Scene consit of Unity FirstPersonCharacter controller that implements both versions of raycast.
- move around the scene and click left-mouse to see which interactables are dectected on various state.
- Enable one of the Detect scripts attached to FPSController's child to check each functionality.
- White, blue, and pink objects have interactable implemented and will display message on Console Log when interacted.
- red objects are not interactable and will block the interactable objects from being detected.
- Gizmos will be shown on editor screen for details:
 - White lines will be displayed on every object checked by sphere ray-cast.
 - Green line shows which object will be interacted when left-mouse is clicked.
 - Yellow spheres represent "DetectInteractableObject.cs" range.
 - Red spheres represent "DetectInteractableObjectComparative.cs" range.
=======
* Scene consit of Unity FirstPersonCharacter controller that implements both versions of raycast.
* move around the scene and click left-mouse to see which interactables are dectected on various state.
* Enable one of the Detect scripts attached to FPSController's child to check each functionality.
* White, blue, and pink objects have interactable implemented and will display message on Console Log when interacted.
* red objects are not interactable and will block the interactable objects from being detected.
* Gizmos will be shown on editor screen for details:
  * White lines will be displayed on every object checked by sphere ray-cast.
  * Green line shows which object will be interacted when left-mouse is clicked.
  * Yellow spheres represent "DetectInteractableObject.cs" range.
  * Red spheres represent "DetectInteractableObjectComparative.cs" range.
>>>>>>> 9935479... Update README.md
=======
>>>>>>> cf3b691... Update README.md
=======
__In order for GameObjects to be detected by this ray-cast, it must implement "IInteractable" interface, also provided by this project.__
>>>>>>> 3818ab6... Update README.md
=======
__In order for GameObjects to be detected by this ray-cast, it must implement _IInteractable_ interface, also provided by this project.__

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
>>>>>>> cc49163... Update README.md
