# Making Unity Asset Bundles

The follow is not a complete step by step set of instructions but a general work flow with notes about important steps or common mistakes...

Use Unity version ``2021.2.14f1`` or earlier.

## 1. Unity Project

Open a 3D project in Unity. While not absolutely necessary, I recommend creating some organizational folders before you start. Typically I
create the following hierarchy which makes it easier to add things to the assetBundle:

```
Contents
+ Meshes
+ Materials
+ Texture
```

Ensure that your project has the ability to create Asset Bundles. If not, you will need to create an Editor folder in your Unity Assets folder
and add the necessary script. 

## 2. Import Your Mesh

Import your mesh (OBJ, FBX or any other format accepted by Unity). If you are using the folder structure above, import into the Meshes folder.
Click on the mesh and ensure the following settings in the Inspector.

- Read/Write is checked (Required)

- Rig is set to None unless you are using animations (Recommended)

- Animations is unchecked unless you are using animations (Recommended)

## 3. Getting Materials/Textures

Depending on the imported objects you may be able to extract the materials and texture from the object or you may need to import them manually.
If you are using the recommended folder structure the import materials into the materials folder and textures into the texture folder.
Click on each Texture and ensure, in the Inspector, that:

- Read/Write is checked (Required)

- SRGB is checked for the diffuse texture

## 4. Place Asset In Scene

Take the mesh and pull it into the scene. Using the Inspector ensure that the position transform is 0,0,0. This ensures that the mini will be
located at the center of the base when spawned.

## 5. Switch Shaders

Unless you have a specific reason, click on the asset in the scene and in the inspector switch the Shader used by the asset from "Standard" to
"Standard (Specular Setup)". While this step in not necessary, it will ensure that your asset is rendered with more accurate colors than when
using the Standard shader.

## 6. Create A Prefab

Pull the asset from the scene back to the Content folder. A prompt will appear. Select the Create Original Prefab option.

## 7. Adding A Portrait (Optional)

If you want your asset to have a nice portrait in the library (when using EAR) then add a PNG file to the Content folder. The PNG file needs to
meet the following specifications:

- Name = Portrait.PNG

- Type = PNG

- Size = 128x128

After importing such a image file into Unity, using the Inspector ensure the following settings:

- Read/Write is checked.

- sRGB is checked.

- Compression is set to None.

The compression setting is critical for the portrait to work. By default compression is not set to None.

## 8. Adding An Info File (Optional)

If you want your asset to be placed in a specific folder in the library (when using EAR) you can add an optional text file which specifies such
details. The file is regular text file. Although it contains JSON content the extension is still TXT. The file has the following specifications:

- Name = Info.txt

- Type = Plain Text

- Content = JSON

The content of the file should be similar to this:

```{ "kind": "Creature", "groupName": "Fey", "description": "Rogue fairy", "name": "Trix", "tags": "Tiny, Fairy" }```

Kind: Can be any of the following - Creature, Transformation, Aura or Effect. The last 2 options are only available if CMP Integration is available.
See below for the difference between these kinds.

groupName: Indicates the group in the library in which the asset will be found. Please note that EAR settings can override this. Since one of the
EAR options is to limit the groupName to the core TS groups, it is highly recommened to use one of the core TS groups if possible and only use
custom group names if the asset doesn't fit into any of the existing core TS groups.

description: Describes the asset. Curently not used but could be used in the future.

name: The default name that is assigned to the mini when the mini is spawned. Can be renamed using the GM option.

tags: Key words used in searching assets. Not currently used. To be used in the future.

### Kind: Creature

This option is the default option. When a asset uses this option, when the asset is selected from the library the mouse pointer changes to the
asset and the asset can be dropped on the board. This is how the library works with regular core TS assets. This option does not require CMP
Integration to work.

This option is tpically used to make minis, props, etc.

### Kind: Transformation

This option can be used to transform an exsiting mini into another appearance. Select an existing mini and then select a transformation from
the library. The selected mini will be transformed into the library selected asset. This option requires CMP Integration (i.e. StatMessaging
plugin and CMP plugin active). This option passes a CMP Mini Transform request to CMP.

This option is typically used to make alternate apperance for a mini such as wild shapes, polymorph shapes, etc.

### Kind: Aura

This option can be used to add mesh to an exsiting mini. Select an existing mini and then select a aura from the library. The selected mini
will be augmented with the selected aura mesh (keeping the original mini mesh and adding the aura mesh). This option requires CMP Integration
(i.e. StatMessaging plugin and CMP plugin active). This option passes a CMP Mini Transform request to CMP.

This option is typically used to create auras which are attached to the caster.

### Kind: Effect

This option works similarly to transformation in that it replaces the original mini appearance with the selected one but the big difference is
that the Effects option allows use of features that Transformation does not. However, minis transformed with this method do not respect visibility.
The effect will remain visisble even when the mini is hidden. This option requires CMP Integration (i.e. StatMessaging plugin and CMP plugin active).
This option passes a CMP Mini Transform request of blank to the mini and then a CMP Effect request for the selected content.

This option is typically used to create effects which are not attached to the caster (e.g. fireball, flaming sphere, spiritual weapon, etc)


## 9. Making A Asset Bundle

Click on the prefab and in the bottom right of Unity select new and create a new asset bundle name. Please ensure that the name used for the
prefab (step 6) is the same as the assetBundle name. Otherwise your asset will not work. Repeat this step for each of the folders to include
them in the assetBundle. If you did not use folders, you will need to mark all components (meshes, materials and textures) for includion to
the assetBunle. Right click to access the context menu and Buuild Asset Bundles.

## 10. Putting The Files In The Right Place

Once you have your asset bundle(s) you need to put them in a File Access Plugin location (also known as FAP location) in order for EAR or
CMP to find it. The most common place to put custom data, that you are adding manually, is the TaleSpire_CustomData folder. This folder can
be found in the TaleSpire game directory. Please note that TaleSpire_CustomData is not a TS core folder. It is a folder that is used by
plugins but not the core game. If the folder does not exist create it. Asset bundles should be placed in a Minis sub-folder and a further
sub folder that matches the asset bundle name. For example, the assetbundle "smurf" would be located in:

```TaleSpire_CustomData/Minis/smurf/smurf```

Where the first smurf is a folder and the second smurf is the assetBundle file.
