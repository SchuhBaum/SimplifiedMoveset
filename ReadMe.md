## SimplifiedMoveset
###### Version: 2.0.8

This is a mod for Rain World v1.9.

### Description
Various movement changes. The main idea is to remove or simplify timings, making it easier to perform advanced moves consistently. In addition, includes the ability to breath underwater and crawl on walls (disabled by default).  
  
Here is a youtube video showing Rain World 1.5 + some of the changes in action:  
https://www.youtube.com/watch?v=Jp6UyUgoWB0

### Installation
0. Update Rain World to version 1.9 if needed.
1. Download the file  `SimplifiedMoveset.zip` from [Releases](https://github.com/SchuhBaum/SimplifiedMoveset/releases).
2. Extract its content in the folder `[Steam]\SteamApps\common\Rain World\RainWorld_Data\StreamingAssets\mods`.
3. Start the game as normal. In the main menu select `Remix` and enable the mod. 

### Contact
If you have feedback, you can message me on Discord `@SchuhBaum#7246` or write an email to SchuhBaum71@gmail.com.  

### License  
There are two licenses available - MIT and Unlicense. You can choose which one you want to use.

### Changelog
#### (Rain World v1.9)
v2.0.8:
- Added support for Rain World 1.9.
- Removed AutoUpdate.
- (wall climb / jump) Fixed a bug where you would slightly slide down without pressing down.
- (swim) Fixed a bug where you could still dash underwater (and by doing that consume air). I included an exception when Rain World Remixes "No swim boost penalty" option is used.
- (roll_2) This option needs to be enabled to add changes to vanilla behavior (for consistency). Before it was reversed.
- (wall jump) Remove jumping off horizontal beams while wall climbing. I found it too hard to anticipate when it happens.
- (wall jump) Instead standing on beams has higher priority than wall climbing. You can do a normal jump off them now even when hugging a wall.
- (swim) Fixed a bug where rivulet would be slowed down.
- (swim) Fixed a bug that could crash Artificer dreams.
- (ledge grab) Changed this option. When enabled, ledge grabs are removed. These can mess your wall jumps up and give you extra downward momentum.
- Removed some inconsistencies with vanilla code regarding new characters.

#### (Rain World v1.5)
v0.30:
- Extended the spear throw option to work with slopes and when hanging from beams.
- When the wall climb option is enabled, (mid-air) wall jumps have a higher priority than using grapple worms.

v0.40:
- Added support for AutoUpdate.
- Extended the climb option. Should be more consistent now.
- Added a ledge grab option (disabled by default). When enabled, you stay in the ledge grab animation until jump is pressed. Added the ability to mid-air wall jump off a ledge grab.

v0.50:
- Ledge grab option uses vanilla behaviour when grabbing a shortcut entrance ledge.
- Extended beam climb option. The main idea is to reduce cases where you accidentally drop from beams.
- When not in an animation, you can grab beams by holding down.
- While beam climbing, you lean from beams instead of falling from them. You can press down in some cases (to drop and grab beams below), or jump to drop without regrabbing.
- You can switch from horizontal to vertical beams and vice versa by holding the direction keys.
- Reworked the wall climb option. Simplified some cases.
- Added an option regarding tube worms. When enabled (default), tube/grapple worms auto-aim grapple to beams. They still prioritize solid tiles. Adjusts (starting) angle when diagonal up is pressed (vanilla uses straight up in that case).
- Restructured code.

v0.60:
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

v0.70:
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
- Buffered wall jumps have priority over using tube worms.
- (beam climb) GetUpToBeamTip should not be canceled anymore by holding left or right. Fixed a bug where canceling beam climb would be spammed when climbing into a corner.
- Restructered code.

v0.80:
- (grab) Added this option (disabled by default). You can only grab dead large creatures when crouching. Fixed a bug where you could not grab them in vertical corridors. Fixed a bug where you could not grab when lying on top of the creature.
- (beam climb) Fixed a bug where bumping your head would be considered wall climbing and cancel the StandOnBeam animation.
- (beam climb) Reworked switching beams from horizontal to vertical and vice versa. Excluded some cases.
- Restructured code.
- (wall jump) Don't wall jump when jumping into a wall from beam climbing.
- (roll_1) End a roll always standing. Otherwise you can sometimes chain rolls on slopes when using the crawl option.
- (wall jump / climb) Fixed a bug where cicadas would slowly lift the player up during wall climbs.
- (beam climb) Fixed a bug where you would regrab the same horizontal beam when using a cicada while holding down + jump.
- (crouch jump) Don't stand up when jumping during the DownOnFours animation and pressing down. Otherwise this can mess up a super launch jump. Fixed a bug where you would not stand up when jumping out of shortcuts.
- (beam climb) Changed implementation for grabbing beams by holding down. Added that you can prevent grabbing beams by holding down and jump. This way you can go faster down like in corridors.

v0.86:
- (roll_2) Changed this option. When disabled, removes the ability to initiate rolls from rocket jumps.
- (beam climb) Removed the time that you need to wait before you can grab beams after leaving a corridor. Not sure what the purpose of this was since you only needed to wait under certain conditions (being upside down?).
- (grab) Fixed a bug where grabability was not set correctly.
- (beam climb) You can exit the ClimbUpToBeamTip-animation by pressing down. In rare cases you can get stuck in this animation otherwise.
- (belly slide) Fixed a bug where belly slides would be canceled early when being close to a wall.
- (beam climb) Fixed a bug where you would not be able to grab beams for too long when jumping during the StandOnBeam animation.
- (beam climb) Fixed a bug where you would jump off too early before reaching the BeamTip animation.