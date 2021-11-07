# Extra Assets Registration Plugin

This unofficial TaleSpire allows custom assets to be registered with the core TS library allowing custom content to be accessed
using the TS library like any core TS assets. Supports multiple settings for customizing the plugin.

## Change Log

2.0.0: Rewrite to support creature, effect, aura, transformation and audio directly in EAR without dependency on CMP.

2.0.0: Exposed a lot more properties in the info.txt file. Some of these are preparation for future use.

2.0.0: Slab support has been temporarily removed. Will be added back in next revision.

1.7.0: Added support for slabs. Doesn't support custom tiles yet but now you can pull stabs out from the library.

1.6.0: Reworked visibility code based on Hollo's TS base implementation

1.5.0: Added code to implement visibility for EAR spawned assets

1.3.1: Corrects spawned mini orientation so that facing is correct

1.3.0: Changed how CMP Integration works and added support for CMP Transformation, CMP Aura and CMP Effect

1.3.0: Added more logging during registry to identify issues with assetBundles that fail to register

1.2.0: Added Soft Dependency Interface Module (SDIM) to access StatMessaging to allow removal of StatMessaging as a dependency.
       if not using CMP integration then neither CMP nor StatMessaging is needed.

1.2.0: Added code to prevent registration failure of an asset to prevent other assets from being registered.

1.1.1: Added missing manifest dependency (StatMessaging). No plugin change.

1.1.0: Added integration with CMP support. Select mini and hold CTRL while selecting an asset in the library.

1.0.1: Added code to ignore non-asset bundle assets

1.0.0: Initial release

## Install

Use R2ModMan or similar installer to install this plugin. Set the desired setting using the R2ModMan config for the plugin or
keep the default settings.

## Usage

There are 4 different asset types: Creature, Effect, Aura, Audio plus Transformation.

There are 10 different hot keys: Animations 1 to 7, Prompt Animation, Audion Play, Stop.

There are 2 different application modes: By Asset and Override.

There are 3 different usage modes: Full Seek, New Assets Only, and Manual.

There are 4 different grouping modes: Custom, List Only, Core Only, Single Folder.

There are 2 different styles of audio assets.

### Asset Types

```
+----------------+-----------------------------------+----------+-------------+--------------+
| Type           | Description                       |  Shader  | Can Stealth | Transparency |
+----------------+-----------------------------------+----------+-------------+--------------+
| Creature       | Creates a new mini                |    TS    |     Yes     |      No      |
+----------------+----------------------------------------------+----------------------------+
| Effect         | Creates a new mini                |    AB    |     No      |      Yes     |
+----------------+----------------------------------------------+----------------------------+
| Aura           | Attaches new mini to current mini |    AB    |     No      |      Yes     |
+----------------+----------------------------------------------+----------------------------+
| Audio          | Creates a new stealthed mini      |    TS    |     Yes     |      No      |
+----------------+----------------------------------------------+----------------------------+
| Transformation | Replaces current mini with new    |    TS    |     Yes     |      No      |
+----------------+----------------------------------------------+----------------------------+
```

### Demo Assets

Under "Human" you will find an "Assasin" (icons with brown leather hooded outfit). The assasin has animations and poses.
The assasin starts in animation 1. The first two animations are very subtle and may look like poses. The third animation
is a static ready pose. Animation 4 is a kick attack. Animation 5 is a spell attack and animation 6 is a death animation.
You can access each one of these by selecting the assasin on the board and pressing LEFT ALT plus 1 thru 6.
The assasin also has a battle cry which you can play with LEFT ALT+9.

Under "Tavern Songs" you will find "Cockerel". When you place this asset on the board it will look like a speaker. You
can hide it using Stealth so that your player's don't see it. When selected on the board, you can press LEFT ALT + 9 to
play the music. It is a tavern song about a rooster. If you think the song is about anything else, you have a dirty mind.
You can use LEFT ALT + 0 to stop playing the music.

### Keyboard Hotkeys

LEFT ALT + 1 = Play animation called "Anim01" if the asset has such an animation.
LEFT ALT + 2 = Play animation called "Anim02" if the asset has such an animation.
LEFT ALT + 3 = Play animation called "Anim03" if the asset has such an animation.
LEFT ALT + 4 = Play animation called "Anim04" if the asset has such an animation.
LEFT ALT + 5 = Play animation called "Anim05" if the asset has such an animation.
LEFT ALT + 6 = Play animation called "Anim06" if the asset has such an animation.
LEFT ALT + 7 = Play animation called "Anim07" if the asset has such an animation.
LEFT ALT + 8 = Prompt for an animation name and then play that animation if the asset has it.
LEFT ALT + 9 = Play audio if the asset has audio associated with it.
LEFT ALT + 0 = Stop playing animations and audio.

### Application Mode: By Asset

In this mode, the type of asset (Creature, Effect, Aura, Audio, Transformation) is determine by the asset's info.txt file.
If the asset does not have such a file, the asset is assumed to be a Creature asset.

### Application Mode: Overide

In this mode, the user specifies how the asset should be treated (i.e. what type it should be treated as) by holding a specific key
when selecting the asset from the library.

LEFT SHIFT = The asset will be treated as a Creature regardless of the info.txt information (if present)
RIGHT SHIFT = The asset will be treated as a Effect regardless of the info.txt information (if present)
ANY CTRL  = The asset will be treated as a Transformation regardless of the info.txt information (if present)
ANY ALT = The asset will be treated as a Aura regardless of the info.txt information (if present)


### Usage Mode: Full Seek

This mode causes the Extra Assets Registration plugin to search for assets ignoring the cache of existing assets. This produces
the most accurate list of available assets, because it will catch both added and removed assets, but it also takes much longer
on startup if a user has lots of available assets.

The startup time will always be longer in this mode. How much longer depends on the number of assets available on the device. 

This mode does not need any manual interaction. Once TS is started, the assets will appear in the core TS library.


### Usage Mode: New Assets Only (Default Mode)

This mode causes the Extra Assets Registration plugin to look for new assets which have been added since the last check but not
to look for removed assets. In this mode, new assets are detected without the need for manual trigger and/or needing to restart
but the process is faster than a Full Seek because any already cached assets do not need to be investigated. The downside of this
mode is that if assets are removed from the device, they will still show up in the library and produce an error when one tries to
use them or when a board is loaded that contains them.

It should be noted that the first time TS is started using this mode it will take a longer time on startup because it will search
for all available assets (same as Full Seek mode). However, on startups after that, the startup time will be shorter because the
plugin does not need to study any assets that it already knows about.

This mode does not need any manual interaction. Once TS is started, the assets will appear in the core TS library.


### Usage Mode: Manual Mode

This mode is used if you don't want Extra Assets Registration plugin to look for assets on start-up. It will only register assets
which it had previous found. In this mode, the user needs to manually tell Extra Assets Registration plugin to look for new assets
by using the keyboard shortcut (default RCTRL+A for assets). 


### Grouping Mode: Custom (Default Grouping)

In custom grouping the plugin check to see if a info.txt file exists in the assetBundle. If so, it uses that information to determine
various infromation about the content including the group in which it should appear in the library. This allows the content creator
to specify the group but it can also lead to a lot of custom groups if different content creators use different group names.

If the content does not provide an info.txt file the Custom Content group is used.


### Grouping Mode: List Only

In list only grouping the plugin will check to see if a info.txt file exists in the assetBundle. If so, it uses that information to
determine various infromation about the content including the group. It then compares the group name against the configured list and
keeps the suggested group name only if it is on the list. If not the group is changed to Custom Content.

If the content does not provide an info.txt file the Custom Content group is used.


### Grouping Mode: Core Only

In core only grouping the plugin will check to see if a info.txt file exists in the assetBundle. If so, it uses that information to
determine various infromation about the content including the group. It then compares the group name against the list of core group
names and keeps the suggested group name only if it is on the list. If not the group is changed to Custom Content.

If the content does not provide an info.txt file the Custom Content group is used.

### Grouping Mode: Single Folder

In single folder grouping all content is placed in a single Custom Content folder regardless if the content does or does not specify
a group name.


## Portraits and Asset Info

While assetBundles without portraits and/or an info file are acceptable and defaults will be generated for both, you can include
either or both of these to allow the Extra Asset Registration plugin to use that information instead of the defaults.

### Portrait

An assetBundle can include a custom portrait by including a Portrait.PNG file in the assetBundle. The file must be PNG and make,
after importing it into Unity, to make it read/write (by checking the corresponding checkbox) and ensure that it does NOT use
compression (by setting the compression setting to None). If these settings are not set properly, the Portrait may not be readable
by the Extra Asset Registration plugin. The portrait will be used for both the image in the library and the player badge and
initiative order symbol.

### Info File

An assetBundle can also include a info.txt file which contains a JSON string of information about the asset. Please note that
while the content is JSON, the extension of the file remains txt. The conent should follow the format:

{
  "name": "Assasin",
  "kind": "Creature",
  "groupName": "Human",
  "description": "Assasin",
  "tags": "Human,Rogue,Assasin",
  "author": "Lord Ashes",
  "version": "1.0",
  "comment": "Maximo source",
  "size": 1.0,
  "assetBase": "Default",
  "locations":
  {
    "head": "0.0,0.7,0.0",
    "hit": "0.0,0.5,0.0",
    "spell": "0.0,0.5,0.0",
    "torch": "0.0,0.5,0.0"
  },
  "meshAdjustments":
  {
    "size": "1.0,1.0,1.0",
    "rotationOffset": "0.0,0.0,0.0",
    "positionOffset": "0.0,0.0,0.0"
  }
}

Where "name" is the name of the asset. See Note 2 below.
Where "kind" is always "Creature" at this point. This will be used in the future for things like Props and Tiles.
Where "groupName" is the name of the group in the library that contains the asset. See Note 1 below.
Where "description" is a description of the asset. Not currently used. To be used in the future. 
Where "tags" is a list of tags associated with the asset. Not currently used. To be used in the future. 

Where "author", "version", and "comment" are user strings ignored by EAR.
Where "size" determine the GM Tools Size of the asset (e.g. 0.5, 1, 2, 4). *For future use.
Where "assetBase" determines the "recommended" base setting for the asset (None, Default or a base file). *For future use.
Where "locations" defines the offset for various points on the asset. *For future use.
Where "meshAdjustments" defines the mesh size adjustments and mesh offsets.

Note 1: Depending on the Extra Asset Registration plugin group settings (see above) the groupName may be ignored
        when determining the folder in which the asset will be placed in the library. In such case, as noted above,
		the asset will be placed in a Custom Content folder.
		
Note 2:	The Name (or partial name if too long) is displayed on the asset portrait if the default portrait is used.

### Making Audio Assets

There are two different styles of audio assets each of which have two options for a total of four combinations.

#### Audio Asset Vs Creature Asset

If an asset is of the kind Audio then, when selected, it will be automatically spawned hidden (stealth mode) but it will
always spawn to position 0,0,0 (from where the GM can move it if desired). If an audio asset is of the type Creature then
the audio asset will not automatically start hidden but the GM will be able to place it instead of having it spawn at the
default location of 0,0,0. The GM can then manually hide the asset. The asset can have any shape (just like a creature)
but I recommend a speaker shape (or similar) which clearly identifies it as a audio asset.

#### Auto Play Vs Manual Play

To create an audio asset, in Unity add a Audio Source component to the asset and provide it with a audio file for the
audio source. No other components need to be added to make the audio work. This, however, is where the second option
applies. The audio source has a checkbox to "play on wake". If this checkbox is checked, the audio will play as soon as
the asset is selected from the library (even before it is placed on the board). Once placed on the board, the audio
restarts. If the checkbox is not checked, the audio will not start on its own and must be started manually with the
LEFT ALT+9 keyboard shortcut. 
