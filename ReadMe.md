## SimplifiedMoveset
###### Version: 0.68

This is a mod for Rain World v1.5.

### Description
Various movement changes. The main idea is to remove or simplify timings, making it easier to perform moves consistently. In addition, includes the ability to breath underwater and crawl on walls (disabled by default).  
  
  Here is a youtube video showing some of the changes: https://www.youtube.com/watch?v=Jp6UyUgoWB0

### Installation
1. (ModLoader) `BepInEx` and `BOI` can be downloaded from [RainDB](https://www.raindb.net/) under `Tools`.  
  **NOTE:** Rain World's BepInEx is a modified version. Don't download it from GitHub.  
2. (Dependency) The mod `ConfigMachine` can be downloaded from [RainDB](https://www.raindb.net/) under `Tools`.
3. Download the file  `SimplifiedMoveset.dll` from [Releases](https://github.com/SchuhBaum/SimplifiedMoveset/releases) and place it in the folder `[Steam]\SteamApps\common\Rain World\Mods`.  
4. Start `[Steam]\SteamApps\common\Rain World\BOI\BlepOutIn.exe`.  
5. Click `Select path` and enter the game's path `[Steam]\SteamApps\common\Rain World`. Enable the mod `SimplifiedMoveset.dll` and its dependencies. Then launch the game as normal. 

### Contact
If you have feedback, you can message me on Discord `@SchuhBaum#7246` or write an email to SchuhBaum71@gmail.com.  

### License  
There are two licenses available - MIT and Unlicense. You can choose which one you want to use.  
**NOTE:** WeakTables use their own license. This license is included in the folder SourceCode/WeakTables. Website: https://github.com/Dual-Iron/weak-tables

### Bugs
- Standing next to a wall, the belly slide can fail (not sure why). In this case the body mode index is set to CorridorClimb and goes into crawl afterwards. This bug seems to exist in vanilla as well.
- Pressing jump during the GetUpOnBeam animation will drop you afterwards. Intended is that nothing happens and you can only jump off after this (short) animation is finished.

### Changelog
v0.3:
- Extended the spear throw option to work with slopes and when hanging from beams.
- When the wall climb option is enabled, (mid-air) wall jumps have a higher priority than using grapple worms.


v0.4:
- Added support for AutoUpdate.
- Extended the climb option. Should be more consistent now.
- Added a ledge grab option (disabled by default). When enabled, you stay in the ledge grab animation until jump is pressed. Added the ability to mid-air wall jump off a ledge grab.


v0.5:
- Ledge grab option uses vanilla behaviour when grabbing a shortcut entrance ledge.
- Extended beam climb option. The main idea is to reduce cases where you accidentally drop from beams.
- When not in an animation, you can grab beams by holding down.
- While beam climbing, you lean from beams instead of falling from them. You can press down in some cases (to drop and grab beams below), or jump to drop without regrabbing.
- You can switch from horizontal to vertical beams and vice versa by holding the direction keys.
- Reworked the wall climb option. Simplified some cases.
- Added an option regarding tube worms. When enabled (default), tube/grapple worms auto-aim grapple to beams. They still prioritize solid tiles. Adjusts (starting) angle when diagonal up is pressed (vanilla uses straight up in that case).
- Restructured code.


v0.6:
- (beam climb) When hanging from a vertical beam and pressing jump, you drop instead of doing a mini-jump (for consistency).
- (beam climb) When hanging from a horizontal beam and pressing jump (without holding up), you drop instead of doing nothing (for consistency).
- (swim) Removed the ability to breath underwater while death rain.
- (beam climb) Small changes. Grabbing beams by holding down should no longer happen when "falling" inside corridors.
- (crawl) Renamed and extended the former crawl-turn option. Removed slowdown when holding down.
- (belly slide and crawl) Changed slope collision. The main idea is simplicity. Collision is (almost) identical with solid tiles. Only horizontal momentum gets adjusted. You are placed down (new) or up (already in vanilla) to the slope's surface. This makes moving down slopes at higher speed possible (useful for belly slides). Rolls from crawl turns are possible on slopes. These changes only affect player bodyChunks.
- (beam climb) Extended the GetUpOnBeam animation to work in both directions. This replaces the dropping and auto-regrabbing solution when pushing down while standing on a horizontal beam. This animation can now also be triggered while leaning.
- (wall climb) Fixed a bug where slugcat wouldn't stand up when crawling into a one-tile "wall".
- (belly slide) Removed the timing required for throwing spears while belly sliding.
- (belly slide and roll) Rocket jumps and flips are no longer canceled by bumping into ceilings.
- (spear throw) Simplified implementation. Fixed a bug where thrown spears would just fall when starving.


v0.68:
- (beam climb) When hanging from a horizontal beam and using a tube worm, retracting the tongue has priority when pressing jump (for consistency).
- (beam climb) Fixed a bug which prevented player from entering corridors during the GetUpToBeamTip animation.
- (belly slide and crawl) Horizontal momentum gets adjusted when on ceiling slopes (for consistency).
- (wall jump) Extracted the changes regarding wall jumps from the wall climb option. Enabled by default.
- (wall jump) Disabled wall sliding when not holding down.
- (wall jump) Jump inputs are spammed / buffered when pressing jump during wall climb (for 6 frames / 150ms). You now have 15 frames for pressing jump too late (mid-air wall jump) and 6 for too early.
- (wall jump) While wall climbing, you can now jump off horizontal beams and slopes below you. Before, you could only do it for solid tiles and water.
- Restructured code.
- (swim) You can eat fruit underwater.
- (rocket jump) Added option. When disabled, you can only perform normal jumps during rolls.
- (beam climb) Start wall climbing when touching a wall while walking on horizontal beams.
- Restructured code.
- Now a BepInEx plugin.
