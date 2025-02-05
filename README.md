# YouShallNotPass - Safe sight prediction models
Safe sight prediction models of the CHI '25 paper "You Shall Not Pass: Warning Drivers of Unsafe Overtaking Maneuvers on Country Roads by Predicting Safe Sight Distance" (https://doi.org/10.1145/3706598.3713768). 

Main used and selfmade Unity scripts can be found in folder `unity-assets`.


Calculation of required overtaking distances is done using the scripts inside the `unity-assets/RequiredDist` folder.
Just call the respective `getRequiredDist(...)` function (see interface in `unity-assets/RequiredDist/RequiredDist.cs`).
See how it was used in `unity-assets/OvertakingAnalyzer.cs`.

To include this into your own Unity project you need to:
1. Create a road network using [RoadArchitect](https://github.com/MicroGSD/RoadArchitect)
2. Add `MapStud.cs` script to your map object (any object you want to be the map). It should show the custom editor script defined in `Map.cs`.
3. Press the "Create Map" button and wait for the mapping process to finish. Now your map data is generated and persistent.
4. Add sample auxilary cars and provide those to an instance of `CarSpawner.cs` to get opposing traffic on your map. See the `AutoTraffic` folder for useful scripts.
5. Prepare your main car and make it drivable. Use `OvertakingAnalyzer.cs` for automatic overtaking distance calculation and publishing (subscribe for the overtaking path using `AddPathListener()` first)
6. Improve the rest of your Unity project with all the rest of the scripts (add navigation system, HUD, logging, to-be-driven sections, ...)


Content:
```
-AutoTraffic: All autonomous cars related
 |-AutoSteer.cs: Control of autonomous cars
 |-Blinker.cs: Control of turn indicator
 |-Brakelight.cs: Control of brake light
 |-CarSpawner.cs: Automatic distribution of cars around the user car
 |-OpposingSpeedup.cs: Increasing speed of opposing cars
-HUD: Controlling all visuals in simulator
 |-AlphaFromTimescale.cs: Fade in end screen at end of session
 |-CrashText.cs: Showing a text in case of a crash
 |-OvertakingText.cs: Display distance until next opportunity in HUD
 |-SectionsText.cs: Text on pause screen
 |-ShowWarnImg.cs: Control of warn icon in HUD
 |-ShowWarnReason.cs: Control of warn explanation in HUD
-Interpolation: Different spline interpolations
 |-ArrayUtil.cs: Array to string formatted
 |-CubicSpline.cs: Simple cubic spline (by Ryan Seghers, modified)
 |-SightReason.cs: Find the reason for limited sight
 |-Spline3D.cs: 3D-cubic spline
-Logging: Logging of all data in the log file
 |-CarLogger.cs: Main logging skript, logging all ego car data
 |-CenterLineDist.cs: Calculation and logging of distance to road center line
 |-CollisionLogger.cs: Collision logging
 |-DataWriter.cs: Helper script for logging to file
 |-KeyPressLogger.cs: Logging of button presses
 |-OenLogger.cs: Logging of up to 5 cars to-be-overtaken (in front of ego car)
 |-OpposingTracker.cs: Logging of opposing cars
 |-Stopwatch.cs: Helper script for execution time measurement
-Map: Mapping of the simulation world and data persistence
 |-Map.cs: Automatic generation of map data from unity map
 |-MappedIntersection.cs: Data of a mapped intersection
 |-MappedRoad.cs: Data of a mapped road
 |-MapStud.cs: Map data access and persistence
 |-MapViz.cs: Map data visualization in Unity scene view (gizmos)
-Navigation: Navigation scripts
 |-AStar.cs: AStar algorithm
 |-IntersectionArrow.cs: Placement of turn indication behind next intersection
 |-Navigation.cs: Main script for calculation and publishing of the navigation path
 |-NavigationLine.cs: Blue navigation line in car display
 |-OvertakingLine.cs: Orange navigation line in car display where overtaking is possible 
-RequiredDist: Calculation of required sight distance and duration for overtaking
 |-RequiredDist.cs: Interface
 |-RequiredDistConst.cs: Calculation with constant acceleration
 |-RequiredDistDyn.cs: Definition of dynamic acceleration model
 |-RequiredDistIter.cs: Iterative Calculation
 |-RequiredDistLDM.cs: Definition of LDM
-Study: Study stuff
 |-SectionHandler.cs: Automatic control of sections
 |-StudyDisplay.cs: Display of current time for data and video synchronization
 |-StudyParameters.cs: Main study control
-CameraMouse.cs: Helper script to move the camera using the mouse without a VR headset
-Mirror.cs: Mirror control
-MoveOnStart.cs: Move the respective unity object as soon as unity is started
-OvertakingAnalyzer.cs: Main component of overtaking possibility analysis
-ResetCar.cs: Reset the car to the closest road
-ResetSeatedPosition.cs: Reset head postion inside the car
-SelectExtension.cs: Helper scripts for collection operations
-SetCenterOfMass.cs: Set referenced unity object's center of mass automatically
-TextureAnimation.cs: Animation of textures
```
