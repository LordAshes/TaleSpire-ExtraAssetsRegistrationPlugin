using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using DataModel;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LordAshes
{
    public partial class ExtraAssetsRegistrationPlugin : BaseUnityPlugin
    {
        public static class AssetHandler
        {
            private static Data.AssetGroups groupStrategy = Data.AssetGroups.custom;
            private static string groupStrategyList = "";
            private const string coreGroups = "Beast,Constructs,Demonic,Dragonfolk,Dwarf,Elementals,Elf,Fey,Giants,Gnome,Goblin,Half-Demon,Halfling,Half-Orc,Human,Humanoid,Monsterous,Orc,Undead,Aura,Effects,Audio";
            private static BaseUnityPlugin pluginReference = null;

            public static Dictionary<string, Data.AssetInfo> AssetsByFileLocation = new Dictionary<string, Data.AssetInfo>();

            public static bool chainLoaderInProgress = false;

            public static void Initialize(BaseUnityPlugin pluginRef, Data.AssetGroups groupStrategySetting)
            {
                pluginReference = pluginRef;
                groupStrategy = groupStrategySetting;
            }

            public static bool GetAssets(ref Dictionary<string, Data.AssetInfo> assetsByLocation)
            {
                Data.AssetInfo[] assetList = new Data.AssetInfo[] { };
                if (System.IO.File.Exists(ExtraAssetsRegistrationPlugin.Internal.cacheDirectory + "\\AssetInfo.cache"))
                {
                    string json = FileAccessPlugin.File.ReadAllText(Internal.cacheDirectory + "\\AssetInfo.cache");
                    json = RectifyToLocalFormat(json);
                    assetList = JsonConvert.DeserializeObject<Data.AssetInfo[]>(json);
                    foreach (Data.AssetInfo asset in assetList)
                    {
                        if(!assetsByLocation.ContainsKey(asset.location))
                        {
                            assetsByLocation.Add(asset.location, asset);
                        }
                        else
                        {
                            Debug.LogWarning("Extra Assets Registration Plugin: Repeated Existing Asset From Location "+ asset.location);
                        }
                    }
                }
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Found "+assetsByLocation.Count+" Previous Assets"); }
                bool newAssets = false;
                foreach (string location in FileAccessPlugin.File.Catalog(false))
                {
                    bool isKnownAssetType = "||.SLAB|.ENC|".Contains("|"+System.IO.Path.GetExtension(location).ToUpper()+"|");
                    if (isKnownAssetType && !assetsByLocation.ContainsKey(location+"."+System.IO.Path.GetFileNameWithoutExtension(location)))
                    {
                        // Add new asset to the asset cache
                        newAssets = true;
                        AssetBundle ab = null;
                        string code = "";
                        try
                        {
                            string txt = "";
                            if (System.IO.Path.GetExtension(location) == "")
                            {
                                // Asset Bundle
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: AssetBundle"); }
                                ab = FileAccessPlugin.AssetBundle.Load(location);
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: AssetBundle? " + (ab != null)); }
                                TextAsset ta = ab.LoadAsset<TextAsset>("Info.txt");
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Info.Text? " + (ta != null)); }
                                if (ta != null)
                                {
                                    txt = ta.text;
                                }
                                else
                                {
                                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Using default info"); }
                                    txt = "{\"kind\": \"Creature\",\"id\": \"\",\"groupName\": \"Custom Content\",\"description\": \"" + System.IO.Path.GetFileName(location) + "\",\"name\": \"" + System.IO.Path.GetFileNameWithoutExtension(location) + "\",\"tags\": \"\"}";
                                }
                            }
                            else if (System.IO.Path.GetExtension(location).ToUpper() == ".OBJ")
                            {
                                // OBJ/MTL
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: OBJ/MTL"); }
                                if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(location) + "/info.txt"))
                                {
                                    txt = System.IO.File.ReadAllText(System.IO.Path.GetDirectoryName(location) + "/info.txt");
                                }
                                else
                                {
                                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Using default info"); }
                                    txt = "{\"kind\": \"Creature\",\"id\": \"\",\"groupName\": \"Custom Content\",\"description\": \"" + System.IO.Path.GetFileName(location) + ".obj\",\"name\": \"" + System.IO.Path.GetFileNameWithoutExtension(location) + "\",\"tags\": \"\"}";
                                }
                            }
                            else if (System.IO.Path.GetExtension(location).ToUpper() == ".SLAB")
                            {
                                // SLAB
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: SLAB"); }
                                code = FileAccessPlugin.File.ReadAllText(location);
                                if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(location) + "/info.txt"))
                                {
                                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Using info.txt"); }
                                    txt = System.IO.File.ReadAllText(System.IO.Path.GetDirectoryName(location) + "/info.txt");
                                }
                                else
                                {
                                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Using default info"); }
                                    txt = "{\"kind\": \""+ System.IO.Path.GetExtension(location).Substring(1) + "\",\"id\": \"\",\"groupName\": \"Custom Content\",\"description\": \"" + System.IO.Path.GetFileName(location) + "\",\"name\": \"" + System.IO.Path.GetFileNameWithoutExtension(location) + "\",\"tags\": \"\"}";
                                }
                            }
                            else if (System.IO.Path.GetExtension(location).ToUpper() == ".ENC")
                            {
                                // ENC
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: ENCOUNTER ("+location+")"); }
                                txt = FileAccessPlugin.File.ReadAllText(location);
                            }

                            Data.AssetInfo info = JsonConvert.DeserializeObject<Data.AssetInfo>(txt);
                            if (code != "") { info.code = code; }
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Info = " + JsonConvert.SerializeObject(info)); }
                            info.location = (info.location=="") ? location : info.location;
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Location = " + info.location); }
                            switch (groupStrategy)
                            {
                                case Data.AssetGroups.custom:
                                    break;
                                case Data.AssetGroups.listOnly:
                                    if (!groupStrategyList.Contains(info.groupName)) { info.groupName = "Custom Content"; }
                                    break;
                                case Data.AssetGroups.coreOnly:
                                    if (!coreGroups.Contains(info.groupName)) { info.groupName = "Custom Content"; }
                                    break;
                                case Data.AssetGroups.singleFolder:
                                    info.groupName = "Custom Content";
                                    break;
                            }
                            if (info.name == "") { info.name = System.IO.Path.GetFileNameWithoutExtension(location); }
                            if (info.groupName == "") { info.groupName = "Custom Content"; }
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Group = " + info.groupName); }

                            Texture2D portrait = null;
                            if (System.IO.Path.GetExtension(location) == "")
                            {
                                portrait = ab.LoadAsset<Texture2D>("Portrait.png");
                            }
                            else
                            {
                                portrait = FileAccessPlugin.Image.LoadTexture(System.IO.Path.GetDirectoryName(location) + "/portrait.png");
                            }
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Portrait? " + (portrait != null)); }

                            List<string> variants = (info.variants != null) ? info.variants.ToList<string>() : new List<string>();
                            variants.Insert(0, "");
                            foreach (string variant in variants)
                            {
                                Data.AssetInfo variantInfo = info.Clone();
                                variantInfo.name = variantInfo.name + ((variant != "") ? " (" + variant + ")" : "");
                                variantInfo.id = ExtraAssetsLibrary.DTO.Asset.GenerateID(ExtraAssetsRegistrationPlugin.Guid + "." + variantInfo.location + variant).ToString();
                                variantInfo.location = variantInfo.location + "." + System.IO.Path.GetFileNameWithoutExtension(variantInfo.location) + variant;
                                if (variant != "")
                                {
                                    variantInfo.variants = null;
                                    variantInfo.chainLoad = null;
                                }
                                if (variant != "") { variantInfo.groupName = "[Variants]"; }
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Id = " + variantInfo.id.ToString()); }

                                if (!assetsByLocation.ContainsKey(variantInfo.location))
                                {
                                    assetsByLocation.Add(variantInfo.location, variantInfo);
                                }
                                else
                                {
                                    Debug.LogWarning("Extra Assets Registration Plugin: Repeated New Asset From Location " + variantInfo.location+" ("+ location + "." + System.IO.Path.GetFileNameWithoutExtension(location) + ")");
                                }

                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Added " + variantInfo.location + " To List (" + variantInfo.groupName + ")"); }

                                if (portrait != null)
                                {
                                    try
                                    {
                                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Caching Portrait"); }
                                        System.IO.File.WriteAllBytes(Internal.cacheDirectory + "\\" + variantInfo.id.ToString() + ".png", portrait.EncodeToPNG());
                                    }
                                    catch (Exception)
                                    {
                                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Failed To Cache Portrait"); }
                                        portrait = null;
                                    }
                                }
                                if (portrait == null)
                                {
                                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Creating Default Portrait"); }
                                    ExtraAssetsRegistrationPlugin.Image.CreateTextImage(variantInfo.name, 128, 128, Internal.pluginDirectory + "Default.png").Save(Internal.cacheDirectory + "\\" + variantInfo.id.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                }
                            }
                        }
                        catch (Exception x)
                        {
                            Debug.LogWarning("Extra Assets Registration: Content " + location + " does not seem to be an valid asset or is corrupt\r\n" + x);
                            Debug.LogException(x);
                        }
                        if (ab != null) { ab.Unload(true); }
                    }
                }
                return newAssets;
            }

            public static List<string> GetHiddenGroups()
            {
                List<string> hidden = Internal.hiddenGroups;
                foreach (Data.AssetInfo entry in AssetsByFileLocation.Values)
                {
                    if (entry.groupName.StartsWith("[") && !hidden.Contains(entry.groupName)) { hidden.Add(entry.groupName); }
                }
                return hidden;
            }

            public static GameObject CreateAssetBase(Data.AssetInfo asset)
            {
                string baseSetting = asset.assetBase.ToUpper();

                string mode = ApplyTranslations(asset.kind.ToUpper());

                if (mode == "CREATURE" && Internal.baseForCreatures == Internal.BaseTypeTriState.alwaysNone) { baseSetting = "NONE"; }
                if (mode == "CREATURE" && Internal.baseForCreatures == Internal.BaseTypeTriState.alwaysBase && asset.assetBase == "NONE") { baseSetting = "DEFAULT"; }
                if (mode == "EFFECT" && Internal.baseForEffects == Internal.BaseTypeTriState.alwaysNone) { baseSetting = "NONE"; }
                if (mode == "EFFECT" && Internal.baseForEffects == Internal.BaseTypeTriState.alwaysBase && asset.assetBase == "NONE") { baseSetting = "DEFAULT"; }
                if (mode == "AUDIO" && Internal.baseForAudio == Internal.BaseTypeTriState.alwaysNone) { baseSetting = "NONE"; }
                if (mode == "AUDIO" && Internal.baseForAudio == Internal.BaseTypeTriState.alwaysBase && asset.assetBase == "NONE") { baseSetting = "DEFAULT"; }
                if (mode == "FILTER") { baseSetting = "NONE"; }

                if (baseSetting == "NONE") { return ExtraAssetsLibrary.Handlers.BaseHelper.NoBase(); }
                if (baseSetting == "DEFAULT") { return ExtraAssetsLibrary.Handlers.BaseHelper.DefaultBase(); }
                Data.AssetInfo baseAsset = new Data.AssetInfo()
                {
                    kind = "CREATURE",
                    location = asset.assetBase,
                };
                return CreateAsset(baseAsset);
            }

            public static GameObject CreateAsset(Data.AssetInfo assetInfo)
            {
                string mode = ApplyTranslations(assetInfo.kind.ToUpper());
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Creating Asset Of Type " + assetInfo.kind); }
                if (mode != "SLAB")
                {
                    if (Input.GetKey(KeyCode.LeftAlt)) { mode = "AURA"; }
                    if (Input.GetKey(KeyCode.RightAlt)) { mode = "FILTER"; }
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) { mode = "TRANSFORMATION"; }
                    if (Input.GetKey(KeyCode.LeftShift)) { mode = "CREATURE"; }
                    if (Input.GetKey(KeyCode.RightShift)) { mode = "EFFECT"; }
                }

                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Treating Asset As Type " + mode); }
                if (mode == "SLAB")
                {
                    try
                    {
                        // Check if slab code is a serialized SlabInfo object
                        Data.SlabInfo[] slabs = JsonConvert.DeserializeObject<Data.SlabInfo[]>(assetInfo.code);
                        // Resolve asset code as SlabInfo object
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Slabs [" + System.IO.Path.GetFileName(assetInfo.location) + "]"); }
                        Internal.self.StartCoroutine("BuildMultipleSlabs", new object[] { slabs, Internal.delayPerSlab });
                    }
                    catch (Exception)
                    {
                        // Resolve asset code as Slab code
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Slab [" + System.IO.Path.GetFileName(assetInfo.location) + "]"); }
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Code...\r\n" + assetInfo.code); }
                        DirtyClipboardHelper.PushToClipboard(assetInfo.code);
                        pluginReference.StartCoroutine("DisplayMessage", new object[] { "Presss CTRL+V To Paste The Selected Slab", 3.0f });
                    }
                    return null;
                }
                else if (mode == "ENCOUNTER")
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Rerouting To DefaultEncounterPointer"); }
                    mode = "EFFECT";
                    assetInfo.name = "defaultencounterpointer";
                    assetInfo.location = "Minis/ladefaultencounterpointer/ladefaultencounterpointer.ladefaultencounterpointer";
                }

                // Resolve AsetBundles
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Name = "+assetInfo.name); }
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Location = "+assetInfo.location); }
                string assetName = System.IO.Path.GetExtension(assetInfo.location).Substring(1);
                string assetLocation = System.IO.Path.GetDirectoryName(assetInfo.location) + "/" + System.IO.Path.GetFileNameWithoutExtension(assetInfo.location);
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Name = " + assetName); }
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Location = " + assetLocation); }

                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Asset (" + assetInfo.kind + ") '" + assetName + "' From File '" + assetInfo.location + "'"); }

                GameObject model = null;
                // Load from AssetBundle
                AssetBundle ab = FileAccessPlugin.AssetBundle.Load(assetLocation);
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: AB Null? " + (ab == null)); }

                if (Internal.graphics == Internal.GraphicsCapabilities.HighPerformance)
                {
                    // Try loading a high res version of the asset if one exists. Default to low res version if a high res version is not available. 
                    model = ab.LoadAsset<GameObject>(assetName + "high");
                    if (model == null) { model = ab.LoadAsset<GameObject>(assetName); }
                }
                else // if (Internal.graphics == Internal.GraphicsMode.lowPerformance)
                {
                    // Try loading a low res version of the asset if one exists. Default to high red version if a low res version is not available. 
                    model = ab.LoadAsset<GameObject>(assetName);
                    if (model == null) { model = ab.LoadAsset<GameObject>(assetName + "high"); }
                }

                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Model Null? " + (model == null)); }
                if (model == null)
                {
                    // Prefab in asset bundle does not match asset bundle name. Try getting the first available prefab
                    Debug.LogWarning("Extra Assets Registration Plugin: Improper Asset Bundle detected. Asset Bundle at '" + assetInfo.location + "' doesn't contain '" + assetName + "'. Trying to load '" + ab.GetAllAssetNames()[0] + "' instead.");
                    model = ab.LoadAsset<GameObject>(ab.GetAllAssetNames()[0]);
                }
                ab.Unload(false);
                if (mode != "AURA") { model.transform.eulerAngles = Utility.GetV3(Internal.defaultMiniOrientation); } else { model.transform.eulerAngles = Utility.GetV3(Internal.defaultAuraOrientation); }

                if (mode != "AURA" && mode != "EFFECT" && mode != "FILTER")
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Applying 'Taleweaver/CreatureShader' Shader"); }
                    List<Renderer> renderers = new List<Renderer>();
                    renderers.AddRange(model.GetComponents<Renderer>());
                    renderers.AddRange(model.GetComponentsInChildren<Renderer>());
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.material.shader = Shader.Find("Taleweaver/CreatureShader");
                    }
                }
                else
                {
                    List<Renderer> renderers = new List<Renderer>();
                    renderers.AddRange(model.GetComponents<Renderer>());
                    renderers.AddRange(model.GetComponentsInChildren<Renderer>());
                    string shaderNames = "";
                    foreach (Renderer renderer in renderers)
                    {
                        if (!shaderNames.Contains(renderer.material.shader.name + ","))
                        {
                            shaderNames = shaderNames + renderer.material.shader.name + ",";
                        }
                    }
                    if (shaderNames.EndsWith(",")) { shaderNames = shaderNames.Substring(0, shaderNames.Length - 1); }

                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Keeping Asset Bundle Shader (" + shaderNames + ")"); }
                }
                Transform[] markers = model.transform.Children().ToArray();
                foreach (Transform marker in markers)
                {
                    if (marker.name == "HandRight")
                    {
                        AssetsByFileLocation[assetInfo.location].locations.handRight = marker.position.x + "," + marker.position.y + "," + marker.position.z + "," + marker.eulerAngles.x + "," + marker.eulerAngles.y + "," + marker.eulerAngles.z;
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Updated Right Hand Hook to " + AssetsByFileLocation[assetInfo.location].locations.handRight); }
                    }
                    else if (marker.name == "HandLeft")
                    {
                        AssetsByFileLocation[assetInfo.location].locations.handLeft = marker.position.x + "," + marker.position.y + "," + marker.position.z + "," + marker.eulerAngles.x + "," + marker.eulerAngles.y + "," + marker.eulerAngles.z;
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Updated Left Hand Hook to " + AssetsByFileLocation[assetInfo.location].locations.handLeft); }
                    }
                }

                return model;
            }

            public static bool LibrarySelectionMade(NGuid nguid, AssetDb.DbEntry.EntryKind kind)
            {
                return LibrarySelectionMade(nguid, kind, null);
            }

            public static bool LibrarySelectionMade(NGuid nguid, AssetDb.DbEntry.EntryKind kind, string modeOverride)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Library Selection Made"); }

                Data.AssetInfo assetInfo = FindAssetInfo(nguid);

                string mode = ApplyTranslations(assetInfo.kind.ToUpper());
                if (modeOverride != null) { mode = modeOverride; }
                else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) { mode = "AURA"; }
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) { mode = "TRANSFORMATION"; }
                else if (Input.GetKey(KeyCode.LeftShift)) { mode = "CREATURE"; }
                else if (Input.GetKey(KeyCode.RightShift)) { mode = "EFFECT"; }
                
                // Check to see if an asset is selected
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                Vector3 pos = (asset != null) ? asset.Creature.transform.position : Vector3.zero;
                Vector3 rot = (asset != null) ? asset.Creature.transform.eulerAngles : Vector3.zero;

                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Handling '" + assetInfo.name + "' ("+nguid.ToString()+") Using '" + mode + "' (" + assetInfo.kind + ", A" + (Input.GetKey(KeyCode.LeftAlt) | Input.GetKey(KeyCode.RightAlt)) + "C" + (Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl)) + "S" + (Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift)) + ")"); }

                if (mode == "CREATURE" || mode == "ENCOUNTER" || mode == "EFFECT" || mode == "SLAB" || mode == ApplyTranslations(AssetDb.DbEntry.EntryKind.Tile.ToString().ToUpper()) || mode == ApplyTranslations(AssetDb.DbEntry.EntryKind.Prop.ToString().ToUpper()))
                {
                    // Let Core TS spawn new asset
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: " + assetInfo.kind + " Mode"); }
                    return true;
                }
                else if (mode == "AUDIO")
                {
                    // Spawn hidden speaker
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: A Mini Is Selected"); }
                    SpawnCreature(assetInfo, Vector3.zero, Vector3.zero, true);
                }
                else if (asset != null && mode == "TRANSFORMATION")
                {
                    // Replace currently selected asset
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Transformation Mode"); }
                    foreach (Transform child in asset.Creature.transform.Children())
                    {
                        if (child.name.StartsWith("Custom"))
                        {
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Destroying '" + child.name + "' Of '" + Utility.GetCreatureName(asset) + "'"); }
                            GameObject.Destroy(child.gameObject);
                        }
                    }
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Destroying '" + Utility.GetCreatureName(asset) + "'"); }
                    asset.RequestDelete();
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Creating '" + assetInfo.name + "'"); }
                    SpawnCreature(assetInfo, pos, rot);
                }
                else if ((asset !=null && mode == "AURA") || (mode=="FILTER"))
                {
                    // Add To Currently Selected Asset
                    CreatureGuid cid = (asset != null) ? asset.Creature.CreatureId : CreatureGuid.Empty;
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Aura/Filter Mode"); }
                    string auraName = System.IO.Path.GetFileNameWithoutExtension(assetInfo.location);

                    if (GameObject.Find("CustomAura:" + cid + "." + auraName) == null)
                    {
                        // Add aura
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Request Aura/Filter '" + auraName + "' Creation on " + cid); }
                        AssetDataPlugin.SetInfo(cid.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Aura." + auraName, nguid.ToString());
                    }
                    else
                    {
                        // Remove aura
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Requesting Aura/Filter '" + auraName + "' Removal on " + cid); }
                        AssetDataPlugin.ClearInfo(cid.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Aura." + auraName);
                    }
                }
                else
                {
                    Debug.LogWarning("Extra Assets Registration Plugin: Unknown Mode '" + mode + " (" + assetInfo.kind.ToUpper() + ")'");
                }
                Debug.Log("Extra Assets Registration Plugin: Supressing Library Selection Asset Creation.");
                return false;
            }

            public static void LibrarySelectionMiniPlaced(NGuid nguid, CreatureGuid cid)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Mini "+cid+" Placed."); }
                Internal.self.StartCoroutine("LibrarySelectionMiniPlacedDelayed", new object[] { cid, chainLoaderInProgress, Internal.delayPostDrop });
            }

            public static CreatureGuid SpawnCreature(Data.AssetInfo asset, Vector3 pos, Vector3 rot, bool initalHidden = false)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Content " + asset.location + " has Nguid = " + asset.id.ToString()); }
                return SpawnCreature(new NGuid(asset.id), pos, rot, initalHidden);
            }

            public static CreatureGuid SpawnCreature(NGuid transformNguid, Vector3 pos, Vector3 rot, bool initalHidden = false)
            {
                CreatureDataV1 creatureDataV1 = default(CreatureDataV1);
                creatureDataV1.CreatureId = CreatureGuid.Empty;
                try
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Spawning Content Nguid" + transformNguid.ToString()); }
                    creatureDataV1 = new CreatureDataV1(transformNguid);
                    creatureDataV1.CreatureId = new CreatureGuid(new Bounce.Unmanaged.NGuid(System.Guid.NewGuid()));

                    CreatureDataV2 creatureDataV2 = new CreatureDataV2(creatureDataV1);
                    creatureDataV2.CreatureId = creatureDataV1.CreatureId;

                    creatureDataV1.ExplicitlyHidden = initalHidden;
                    creatureDataV2.ExplicitlyHidden = initalHidden;
                    creatureDataV1.Flying = false;
                    creatureDataV2.Flying = false;

                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Creating mini which " + (initalHidden ? "is" : "is not") + " hidden"); }
                    CreatureManager.CreateAndAddNewCreature(creatureDataV2, pos, Quaternion.Euler(rot), false, initalHidden);

                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Registering mini for saving"); }
                    BuildingBoardTool.RecordInBuildHistory(creatureDataV2.GetActiveBoardAssetId());
                }
                catch(Exception x)
                {
                    Debug.Log("Extra Assets Registration Plugin: Exception During SpawnCreature");
                    Debug.LogException(x);
                }
                return creatureDataV1.CreatureId;
            }

            public static NGuid FindAssetId(string contentName)
            {
                foreach ((AssetDb.DbEntry.EntryKind, List<AssetDb.DbGroup>) groups in AssetDb.GetAllGroups())
                {
                    foreach (AssetDb.DbGroup group in groups.Item2)
                    {
                        foreach (AssetDb.DbEntry item in group.Entries)
                        {
                            // if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Found '"+item.Name+"'. Looking For '"+contentName+"'"); }
                            if (item.Name.ToUpper() == contentName.ToUpper()) { return item.Id; }
                        }
                    }
                }
                return NGuid.Empty;
            }

            public static Data.AssetInfo FindAssetInfo(NGuid nguid)
            {
                foreach (Data.AssetInfo seekAssetInfo in AssetsByFileLocation.Values)
                {
                    if (seekAssetInfo.id == nguid.ToString())
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Custom Asset Recognized"); }
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Found Custom Asset "+JsonConvert.SerializeObject(seekAssetInfo)); }
                        return seekAssetInfo.Clone();
                    }
                }
                foreach ((AssetDb.DbEntry.EntryKind, List<AssetDb.DbGroup>) groups in AssetDb.GetAllGroups())
                {
                    foreach (AssetDb.DbGroup group in groups.Item2)
                    {
                        foreach (AssetDb.DbEntry item in group.Entries)
                        {
                            if (item.Id == nguid)
                            {
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Core Asset Recognized"); }
                                return new Data.AssetInfo()
                                {
                                    name = item.Name,
                                    description = item.Description,
                                    kind = item.Kind.ToString(),
                                    groupName = item.Group.Name,
                                    tags = String.Join(",", item.Tags),
                                    id = item.Id.ToString(),
                                    location = "Core"
                                };
                            }
                        }
                    }
                }
                Debug.LogWarning("Extra Assets Registration Plugin: Asset  NGuid " + nguid + " Not Recognized");
                return null;
            }

            public static string RectifyToLocalFormat(string json)
            {
                if(Internal.fractionalCharacter != ".")
                {
                    Debug.Log("Extra Assets Registration Plugin: Converting From '.' Format To '"+Internal.fractionalCharacter+"' Format");
                    json = json.Replace("0.", "0" + Internal.fractionalCharacter);
                    json = json.Replace("1.", "1" + Internal.fractionalCharacter);
                    json = json.Replace("2.", "2" + Internal.fractionalCharacter);
                    json = json.Replace("3.", "3" + Internal.fractionalCharacter);
                    json = json.Replace("4.", "4" + Internal.fractionalCharacter);
                    json = json.Replace("5.", "5" + Internal.fractionalCharacter);
                    json = json.Replace("6.", "6" + Internal.fractionalCharacter);
                    json = json.Replace("7.", "7" + Internal.fractionalCharacter);
                    json = json.Replace("8.", "8" + Internal.fractionalCharacter);
                    json = json.Replace("9.", "9" + Internal.fractionalCharacter);
                }
                return json;
            }

            public static string RectifyToStorageFormat(string json)
            {
                if (Internal.fractionalCharacter != ".")
                {
                    Debug.Log("Extra Assets Registration Plugin: Converting From '" + Internal.fractionalCharacter + "' Format To '.' Format");
                    json = json.Replace("0" + Internal.fractionalCharacter, "0.");
                    json = json.Replace("1" + Internal.fractionalCharacter, "1.");
                    json = json.Replace("2" + Internal.fractionalCharacter, "2.");
                    json = json.Replace("3" + Internal.fractionalCharacter, "3.");
                    json = json.Replace("4" + Internal.fractionalCharacter, "4.");
                    json = json.Replace("5" + Internal.fractionalCharacter, "5.");
                    json = json.Replace("6" + Internal.fractionalCharacter, "6.");
                    json = json.Replace("7" + Internal.fractionalCharacter, "7.");
                    json = json.Replace("8" + Internal.fractionalCharacter, "8.");
                    json = json.Replace("9" + Internal.fractionalCharacter, "9.");
                }
                return json;
            }

            private static string ApplyTranslations(string local)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Mode Translation: Local = '"+local+"'"); }
                foreach (KeyValuePair<string,string> replacement in Internal.languageModeTranslation)
                {
                    local = local.Replace(replacement.Key, replacement.Value);
                }
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Mode Translation: Standard = '" + local + "'"); }
                return local;
            }
        }

        public IEnumerator LibrarySelectionMiniPlacedDelayed(object[] inputs)
        {
            yield return new WaitForSeconds((float)inputs[2]);
            CreatureBoardAsset asset = null;
            CreaturePresenter.TryGetAsset((CreatureGuid)inputs[0], out asset);
            if (asset != null)
            {
                CreatureGuid cid = (CreatureGuid)asset.Creature.CreatureId;
                NGuid nguid = asset.BoardAssetId;
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Mini " + cid + " of type " + nguid + " Placed."); }

                Data.AssetInfo assetInfo = AssetHandler.FindAssetInfo(nguid);
                if (assetInfo != null)
                {
                    if (assetInfo.chainLoad != null && (bool)inputs[1] == false)
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Processing Chain Loader: " + assetInfo.chainLoad); }
                        Internal.self.StartCoroutine("ChainLoader", new object[] { assetInfo, cid });
                    }
                    if (assetInfo.linkRequests != null && assetInfo.linkRequests.Length > 0)
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Processing Link Request: " + JsonConvert.SerializeObject(assetInfo.linkRequests)); }
                        Internal.self.StartCoroutine("ProcessLinqRequests", new object[] { cid, assetInfo.linkRequests });
                    }
                }
            }
        }


        public IEnumerator DestroyAssetAfterTimeSpan(object[] inputs)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Destroy Asset After " + (float)inputs[1] + " Seconds Started."); }
            yield return new WaitForSeconds((float)inputs[1]);
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset((CreatureGuid)inputs[0], out asset);
            if (asset != null)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: "+AssetDataPlugin.Legacy.GetCreatureName(asset)+" Self Destruct "); }
                asset.RequestDelete();
            }
            else
            {
                Debug.LogWarning("Extra Assets Registration Plugin: Invalid Creature Id Provided To Destroy Asset After Timespan.");
            }
        }

        public IEnumerator BuildMultipleSlabs(object[] inputs)
        {
            Data.SlabInfo[] slabs = (Data.SlabInfo[])inputs[0];
            foreach (Data.SlabInfo slab in slabs)
            {
                Copied copied = default(Copied);
                if (BoardSessionManager.Board.PushStringToTsClipboard(slab.code, 0, slab.code.Length, out copied) == PushStringToTsClipboardResult.Success)
                {
                    Copied mostRecentCopied_LocalOnly = BoardSessionManager.Board.GetMostRecentCopied_LocalOnly();
                    if (mostRecentCopied_LocalOnly != null)
                    {
                        Debug.Log("Extra Assets Registration Plugin: Placing Slab. X:" + slab.position.x + " y:" + slab.position.x + " z:" + slab.position.z + " Slab: " + slab.code);
                        BoardSessionManager.Board.PasteCopied(slab.position, 0, 0UL);
                        Debug.Log("Extra Assets Registration Plugin: Post Slab Placement Delay = " + (float)inputs[1]);
                        yield return new WaitForSeconds((float)inputs[1]);
                    }
                }
            }
        }

        public IEnumerator ChainLoader(object[] inputs)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Surpressing Chain Loader"); }
            AssetHandler.chainLoaderInProgress = true;
            Data.AssetInfo assetInfo = (Data.AssetInfo)inputs[0];
            CreatureGuid cid = (CreatureGuid)inputs[1];
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Chain Loading..."); }
            CreatureBoardAsset asset = null;
            while (asset == null)
            {
                yield return new WaitForSeconds(0.5f);
                CreaturePresenter.TryGetAsset(cid, out asset);
            }
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Mini " + JsonConvert.SerializeObject(assetInfo)); }
            if (asset != null)
            {
                string[] chain = assetInfo.chainLoad.Split(',');
                for (int i = 0; i < chain.Length; i = i + 7)
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Spawning " + chain[i]); }
                    Vector3 pos = new Vector3(float.Parse(chain[i + 1]), float.Parse(chain[i + 2]), float.Parse(chain[i + 3]));
                    Vector3 rot = new Vector3(float.Parse(chain[i + 4]), float.Parse(chain[i + 5]), float.Parse(chain[i + 6]));
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Mini " + chain[i] + " Offset Pos " + pos.ToString() + " & Offset Rot " + rot.ToString()); }
                    if (chain[i].ToUpper() != "{VARIANT}")
                    {
                        NGuid nguid = AssetHandler.FindAssetId(chain[i]);
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Mini " + chain[i] + " Has Guid " + nguid.ToString()); }
                        Data.AssetInfo chainAssetInfo = AssetHandler.FindAssetInfo(nguid);
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Spawning " + JsonConvert.SerializeObject(chainAssetInfo)); }
                        AssetHandler.SpawnCreature(chainAssetInfo, asset.CreatureRoot.position + pos, asset.CreatureRoot.eulerAngles + rot);
                    }
                    else
                    {
                        SingletonBehaviour<BoardToolManager>.Instance.SwitchToTool<DefaultBoardTool>(BoardToolManager.Type.Normal);
                        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Activating Sub-Selection Menu At " + mouse.ToString()); }
                        Internal.menuOpenSubselectionPos = mouse;
                        Internal.menuSubselectionSpawnPos = asset.CreatureRoot.position;
                        Internal.menuSubselectionSpawnRot = asset.CreatureRoot.eulerAngles;
                        Internal.menuOpenSubselection = assetInfo.Clone();
                        Internal.menuOpenSubselection.comment = asset.Creature.CreatureId.ToString();
                    }
                }
            }
            if (assetInfo.chainLoad == null || !assetInfo.chainLoad.ToUpper().Contains("{VARIANT}"))
            {
                asset.RequestDelete();
                Internal.self.StartCoroutine("SupressChainLoad");
            }
        }

        public IEnumerator SupressChainLoad()
        {
            AssetHandler.chainLoaderInProgress = true;
            yield return new WaitForSeconds(Internal.delayChainLoaderSupression);
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Re-enabling Chain Loader"); }
            AssetHandler.chainLoaderInProgress = false;
        }

        public IEnumerator ProcessLinqRequests(object[] inputs)
        {
            yield return new WaitForSeconds(Internal.delayProcessingLinks);
            CreatureGuid cid = (CreatureGuid)inputs[0];
            Data.LinkRequest[] linkRequests = (Data.LinkRequest[])inputs[1];
            foreach (Data.LinkRequest request in linkRequests)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Processing Link: Cid:"+ cid.ToString()+", Key:"+request.key+", Value:"+ request.value+", Legacy:"+ request.legacy); }
                AssetDataPlugin.SetInfo(cid.ToString(), request.key, request.value, request.legacy);
            }
        }

        public IEnumerator DisplayMessage(object[] inputs)
        {
            string message = Convert.ToString(inputs[0]);
            float duration = (float)inputs[1];
            Internal.guiMessage = message;
            yield return new WaitForSeconds(duration);
            if (Internal.guiMessage == message) { Internal.guiMessage = ""; }
        }
    }
}