## SimplifiedMoveset
###### Version: 2.4.9

This is a mod for Rain World v1.9.

### Description
Various movement changes. The main idea is to remove or simplify timings, making it easier to perform advanced moves consistently. In addition, includes the ability to breath underwater and crawl on walls (disabled by default).  
  
Here is a youtube video showing Rain World v1.5 + some of the changes in action:  
https://www.youtube.com/watch?v=Jp6UyUgoWB0

### Installation
0. Update Rain World to version 1.9 if needed.
1. Download the file `SimplifiedMoveset.zip` from [Releases](https://github.com/SchuhBaum/SimplifiedMoveset/releases/tag/v2.4.9).
2. Extract its content in the folder `[Steam]\SteamApps\common\Rain World\RainWorld_Data\StreamingAssets\mods`.
3. Start the game as normal. In the main menu select `Remix` and enable the mod. 

### Bug reports & FAQ
See the corresponding sections on the [Steam Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2928752589) for the mod `SBCameraScroll`.

### Contact
If you have feedback, you can message me on Discord `@schuhbaum` or write an email to SchuhBaum71@gmail.com.  

### License  
There are two licenses available - MIT and Unlicense. You can choose which one you want to use.

### Changelog
#### (Rain World v1.9)
v2.4.9:
- (belly slide) Reduced the duration of the normal belly slide back to vanilla. The overall distance is unintentionally increased otherwise (like +20%). Not sure why since the speed is decreased and the same as the long belly slide. From what I tested the distance is as in vanilla now. I might need to double check later since this seems somewhat odd.
- (belly slide) Reduced the speed and increased the duration of the belly slide for Gourmand. This way it matches better with his rocket jump.
- (gourmand) Added this option. Enabled by default. Exhaust only when throwing spears. Stun creatures with rocket jumps. Slides, rocket jumps and rolls only stun and deal no damage.
- (belly slide) Increased the duration for Rivulet's belly slide slightly to better match vanilla's slide distance.
- (gourmand) Allow rocket jumps to deal damage if you have enough speed (similar to Option_Roll_2).
- (tube worm) Restored that wall jumps are priotized over using tube worms.
- (wall jump) Restored that inputs are buffered for 6 frames when pressing jump early.
- (beam climb) Added some cases where you can drop down (by holding/pressing down and/or pressing jump). Prevent beam hopping when holding down. Otherwise you might trigger it accidentally.
- (beam climb) Fixed a case where you would switch from standing on beam to hanging from beam prematurely.
- (gourmand) Fixed a bug where you would not get exhausted when throwing a spear shortly after getting stun grabbed.
- (swim) Gourmand can recover from exhaustion while being underwater.
- Potentially fixed some issues with the new update (v1.9.15).
- (wall climb + jump) Modified how friction is applied when touching walls. This way you slide down a bit and don't stop immediately when you are falling fast.

v2.4.0:
- (crawl) Forgot to add an option check for the last change.
- Changed the hook initialization logic. This should reduce the log spam from IL hooks. Instead of doing it every cycle while in-game they are initialized when starting the game or when changing the options.
- Forgot to change some cases to reduce spam from IL hooks.
- (beam climb) Made grabbing vertical beams less sensitive.
- Changed mod id back to original.
- (wall climb) Fixed a bug where the hand movement would not be executed. Added that slugcat looks up or down when wall climbing.
- Added a pdb file for debugging.
- (beam climb) Changed vertical collision checks to be less sensible for slugcat's upper body chunk. This can help in situations where you try to jump off beams and have solid blocks above. This way you bonk your head less often.
- (beam climb) Fixed a bug where the collision change would interfere with entering pipes close to the water surface. Added more general restrictions.
- (beam climb) Beam hopping is allowed while holding up. This makes this a no risk + low reward move instead.

v2.3.0:
- (beam climb) Potentially fixed a bug where wall jumps would mess with climbing straight up beams.
- (roll 2) Allows to initiate rolls from rocket jumps but only with enough speed. The intention is not be able to spam roll + rocket jumps not to prevent it entirely.
- (beam climb) Fixed a bug where you would grab horizontal beams inside vertical corridors.
- (wall jump) Allow to mid-air wall jump even when changing directions.
- (stand up) Added this option. Various situations where you would stand up are now transfered to this option and some additional are added.
- (belly slide) You can no longer rocket jump from belly slides out of shortcuts.
- (tube worm) Added a short cooldown when leaving shortcuts.
- (belly slide / spear throw) Fixed a vanilla bug that could cause a freeze when carrying a player on the back.
- Potentially fixed a bug where non-existing sprites were requested for certain animations.
- Restructured code.
- (crawl) Wrapped the code for the crawl option in UpdateBodyMode() inside an IL-Hook to improve compatibility with the mod "The Friend".
- Some more IL-Hook wrapping to improve compatibility in general.
- (roll_1) Rocket jumps from rolls are adjusted for Rivulet and slug pups (for consistency).
- (crawl) Fixed a bug that would break the movement sequence in the Hunter cutscene.
- Logging the mod options should be more reliable now.

v2.2.0:
- (tube worm) Retracing Saint's tongue during beam climbing is prioritized over jumping off.
- (crawl) Some adjustments for crawl turns on ledges.
- (tube worm) Wall jumps are priotized over using tongues.
- (spear throw) Spears and other weapons cannot change directions after they being thrown.
- (tube worm) Fixed two bugs where wall jumps were not correctly prioritized.
- (tube worm) Some implementation adjustments.
- Restructured code.
- (tube worm) Retracting tongues is prioritized over jumping in corridors.
- (beam climb) Fixed a bug where Saint would grab beams unintentionally.
- (tube worm) Fixed a bug where wall jumps were not correctly detected.
- Fixed a bug that could crash Artificer dreams.
- (tube worm) Improved consistency for using tongues after wall jumps.
- (grab) Fixed a bug where one-handed creatures would not be grabbed unless the player was crouching.

v2.1.0:
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
- (tube worm) This option extends now to Saint's tongue.
- Adjusted the speed of belly slides.
- (crawl) Crawl turns can only be initiated when on (semi-)solid tiles or slopes.
- (ledge grab) Removed this option. That what I was trying to do is related to crawl turns from ledges.

#### (Rain World v1.5)
v0.86:
- (roll_2) Changed this option. When disabled, removes the ability to initiate rolls from rocket jumps.
- (beam climb) Removed the time that you need to wait before you can grab beams after leaving a corridor. Not sure what the purpose of this was since you only needed to wait under certain conditions (being upside down?).
- (grab) Fixed a bug where grabability was not set correctly.
- (beam climb) You can exit the ClimbUpToBeamTip-animation by pressing down. In rare cases you can get stuck in this animation otherwise.
- (belly slide) Fixed a bug where belly slides would be canceled early when being close to a wall.
- (beam climb) Fixed a bug where you would not be able to grab beams for too long when jumping during the StandOnBeam animation.
- (beam climb) Fixed a bug where you would jump off too early before reaching the BeamTip animation.

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

v0.50:
- Ledge grab option uses vanilla behaviour when grabbing a shortcut entrance ledge.
- Extended beam climb option. The main idea is to reduce cases where you accidentally drop from beams.
- When not in an animation, you can grab beams by holding down.
- While beam climbing, you lean from beams instead of falling from them. You can press down in some cases (to drop and grab beams below), or jump to drop without regrabbing.
- You can switch from horizontal to vertical beams and vice versa by holding the direction keys.
- Reworked the wall climb option. Simplified some cases.
- Added an option regarding tube worms. When enabled (default), tube/grapple worms auto-aim grapple to beams. They still prioritize solid tiles. Adjusts (starting) angle when diagonal up is pressed (vanilla uses straight up in that case).
- Restructured code.

v0.40:
- Added support for AutoUpdate.
- Extended the climb option. Should be more consistent now.
- Added a ledge grab option (disabled by default). When enabled, you stay in the ledge grab animation until jump is pressed. Added the ability to mid-air wall jump off a ledge grab.

v0.30:
- Extended the spear throw option to work with slopes and when hanging from beams.
- When the wall climb option is enabled, (mid-air) wall jumps have a higher priority than using grapple worms.