# Extra Assets Registration Plugin

This unofficial TaleSpire allows custom assets to be registered with the core TS library allowing custom content to be accessed
using the TS library like any core TS assets. Supports multiple settings for customizing the plugin.

## Change Log

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

Optional: If you want to be able to do CMP Integration (transform existing minis using the TS asset library) then you will need
          to install the following plugins: LordAshes-StatMessaging and LordAshes-CustomMiniPlugin. If these plugins are not
		  installed, EAR will as normal but with no CMP integration functionality.

## Usage

There are 2 application modes: New Mini and CMP Integration
There are 3 different usage modes: Full Seek, New Assets Only, and Manual.
There are 4 different grouping modes: Custom, List Only, Core Only, Single Folder.

### Application Mode: New Mini

In this mode the user click on a library selection, the corresponding asset will be picked up and can be dropped on the board
one or more tiles. This is the same action as one would usually bring out core assets from the library.

### Application Mode: Transform Mini

In this mode the selected library transformation, aura or effect will be applied to selected mini. Thus first a mini needs to
be selected and then a transformation, aura or effect can be selected from the library. This functionality will only work if the
optional StatMessaging plugin and CMP are installed. Transformation, aura and effect groups in the library have a "(CMP)" after
them to indicated that they are only available if CMP integration is installed.

Normally the Kind setting in the asset's bundle is used to determine which kind of asset it is: Character, Transformation, Aura
or Effect. However, EAR also looks at the location of the file and looks for Transformation, Aura or Effect in the full location
of the file.

Character: The mini is spawned like any other core TS mini. Drop one or more copies of the mini on the board.
Transformation: The minis appearance is replaced with the selected content. Mesh and materials only. Supports hide and fly modes.
Aura: The selected aura is added to the selected mini. Aura does not support hide and fly mode.
Effect: The selected mini apperance is replaced by the selected content. Similar to Transformation but supports effects but does
        not support hide and fly mode.
		
For CMP users:

Transformation => CMP Mini Transformation, Aura => CMP Effect, Effect => CMP Mini Transformation to blank + CMP Effect.


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
by using the keyboard shortcut (default RCTRL+A for assets). Currently, doing this requires a restart of TS in order for the new
found assets to be available. So the steps are:

1. Start TS
2. Trigger the Manual Seek. Wait for it to complete.
3. Restart TS

This mode does not need any manual interaction to use the collected assets. Only requesting the plugin to update the list of assets
required the above manual interaction.


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
  "kind": "Creature",
  "groupName": "Fey",
  "description": "Rogue fairy",
  "name": "Trix",
  "tags": "Tiny, Fairy"
}

Where "kind" is always "Creature" at this point. This will be used in the future for things like Props and Tiles.
Where "groupName" is the name of the group in the library that contains the asset. See Note 1 below.
Where "description" is a description of the asset. Not currently used. To be used in the future. 
Where "name" is the name of the asset. See Note 2 below.
Where "tags" is a list of tags associated with the asset. Not currently used. To be used in the future. 

Note 1: Depending on the Extra Asset Registration plugin group settings (see above) the groupName may be ignored
        when determining the folder in which the asset will be placed in the library. In such case, as noted above,
		the asset will be placed in a Custom Content folder.
		
Note 2:	The Name (or partial name if too long) is displayed on the asset portrait if the default portrait is used.

## Update Setting

A setting in the configuration for this plugin in R2ModMan has been added. This setting indicates how many update cycles
are skipped before resyncing visibility. The default is 10. Decreasing this number puts a larger stress on the CPU but
updates visibility faster. Increasing the number lowers the stress on the CPU but visibility make take longer to update.
