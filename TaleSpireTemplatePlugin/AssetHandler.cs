using BepInEx;
using Bounce.TaleSpire.AssetManagement;
using Bounce.Unmanaged;
using DataModel;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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

            public static void Initialize(BaseUnityPlugin pluginRef, Data.AssetGroups groupStrategySetting)
            {
                pluginReference = pluginRef;
                groupStrategy = groupStrategySetting;
            }

            public static bool GetAssets(ref Dictionary<string, Data.AssetInfo> assetsByLocation)
            {
                Data.AssetInfo[] assetList = new Data.AssetInfo[] { };
                if (System.IO.File.Exists(ExtraAssetsRegistrationPlugin.Internal.pluginDirectory + "\\cache\\AssetInfo.cache"))
                {
                    assetList = JsonConvert.DeserializeObject<Data.AssetInfo[]>(FileAccessPlugin.File.ReadAllText(Internal.pluginDirectory + "\\cache\\AssetInfo.cache"));
                    foreach (Data.AssetInfo asset in assetList)
                    {
                        assetsByLocation.Add(asset.location, asset);
                    }
                }
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Found "+assetsByLocation.Count+" Previous Assets"); }
                bool newAssets = false;
                foreach (string location in FileAccessPlugin.File.Catalog(false))
                {
                    bool isKnownAssetType = (System.IO.Path.GetExtension(location) == "") || (System.IO.Path.GetExtension(location).ToUpper() == ".SLAB");
                    if (isKnownAssetType && !assetsByLocation.ContainsKey(location))
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

                            Data.AssetInfo info = JsonConvert.DeserializeObject<Data.AssetInfo>(txt);
                            if (code != "") { info.code = code; }
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Info = " + JsonConvert.SerializeObject(info)); }
                            info.id = ExtraAssetsLibrary.DTO.Asset.GenerateID(ExtraAssetsRegistrationPlugin.Guid + "." + location).ToString();
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Id = " + info.id.ToString()); }
                            info.location = location;
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

                            assetsByLocation.Add(info.location, info);
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Added To List"); }

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
                            if (portrait != null)
                            {
                                try
                                {
                                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration: Caching Portrait"); }
                                    System.IO.File.WriteAllBytes(Internal.pluginDirectory + "cache\\" + info.id.ToString() + ".png", portrait.EncodeToPNG());
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
                                ExtraAssetsRegistrationPlugin.Image.CreateTextImage(info.name, 128, 128, Internal.pluginDirectory + "Default.png").Save(Internal.pluginDirectory + "cache\\" + info.id.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        catch (Exception x)
                        {
                            Debug.LogWarning("Extra Assets Registration: Content " + location + " does not seem to be an valid asset or is corrupt\r\n" + x);
                        }
                        if (ab != null) { ab.Unload(true); }
                    }
                }
                return newAssets;
            }

            public static GameObject CreateAsset(Data.AssetInfo asset, NGuid nguid)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Creating Asset Of Type " + asset.kind); }
                if (asset.kind.ToUpper() == "SLAB")
                {
                    try
                    {
                        // Check if slab code is a serialized SlabInfo object
                        Data.SlabInfo[] slabs = JsonConvert.DeserializeObject<Data.SlabInfo[]>(asset.code);
                        // Resolve asset code as SlabInfo object
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Slabs [" + System.IO.Path.GetFileName(asset.location) + "]"); }
                        Internal.self.StartCoroutine("BuildMultipleSlabs", new object[] { slabs, Internal.delayPerSlab });
                    }
                    catch (Exception)
                    {
                        // Resolve asset code as Slab code
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Slab [" + System.IO.Path.GetFileName(asset.location) + "]"); }
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Code...\r\n"+asset.code); }
                        DirtyClipboardHelper.PushToClipboard(asset.code);
                        pluginReference.StartCoroutine("DisplayMessage", new object[] { "Presss CTRL+V To Paste The Selected Slab", 3.0f });
                    }
                    return null;
                }
                else if ((System.IO.Path.GetFileName(asset.location) == System.IO.Path.GetFileNameWithoutExtension(asset.location)) || (System.IO.Path.GetExtension(asset.location).ToUpper() == ".OBJ"))
                {
                    // Resolve AsetBundles and OBJ/MTL asset
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Asset (" + asset.kind + ") '" + System.IO.Path.GetFileName(asset.location) + "' From File '" + asset.location + "'"); }

                    GameObject model = null;
                    if (System.IO.Path.GetFileName(asset.location) == System.IO.Path.GetFileNameWithoutExtension(asset.location))
                    {
                        // Load from AssetBundle
                        AssetBundle ab = FileAccessPlugin.AssetBundle.Load(asset.location);
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: AB Null? " + (ab == null)); }
                        model = ab.LoadAsset<GameObject>(System.IO.Path.GetFileName(asset.location));
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Model Null? " + (model == null)); }
                        if(model==null)
                        {
                            Debug.LogWarning("Extra Assets Registration Plugin: Improper Asset Bundle detected. Asset Bundle at '" + asset.location + "' doesn't contain '" + System.IO.Path.GetFileName(asset.location) + "'. Trying to load '" + ab.GetAllAssetNames()[0] + "' instead.");
                            model = ab.LoadAsset<GameObject>(ab.GetAllAssetNames()[0]);
                        }
                        ab.Unload(false);
                    }
                    else if (System.IO.Path.GetExtension(asset.location).ToUpper()==".OBJ")
                    {
                        // Load from OBJ/MTL
                        Debug.LogWarning("Extra Assets Registration Plugin: OBJ/MTL files are not supported. Please create a Unity AssetBundle or a Slab file");
                        model = null;
                    }
                    model.transform.eulerAngles = new Vector3(0, 180, 0);

                    if ((asset.kind.ToUpper() != "AURA") && (asset.kind.ToUpper() != "EFFECT"))
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
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Keeping AssetBundle Specified Shader"); }
                    }

                    return model;
                }
                else
                {
                    Debug.LogWarning("Extra Assets Registration Plugin: Unsupported file type (" + asset.location + ")");
                    return null;
                }
            }

            public static GameObject CreateAssetBase(Data.AssetInfo asset, NGuid nguid)
            {
                string baseSetting = asset.assetBase.ToUpper();

                if (asset.kind.ToUpper() == "CREATURE" && Internal.baseForCreatures == Internal.BaseTypeTriState.alwaysNone) { baseSetting = "NONE"; }
                if (asset.kind.ToUpper() == "CREATURE" && Internal.baseForCreatures == Internal.BaseTypeTriState.alwaysBase && asset.assetBase=="NONE") { baseSetting = "DEFAULT"; }
                if (asset.kind.ToUpper() == "EFFECT" && Internal.baseForEffects == Internal.BaseTypeTriState.alwaysNone) { baseSetting = "NONE"; }
                if (asset.kind.ToUpper() == "EFFECT" && Internal.baseForEffects == Internal.BaseTypeTriState.alwaysBase && asset.assetBase=="NONE") { baseSetting = "DEFAULT"; }
                if (asset.kind.ToUpper() == "AUDIO" && Internal.baseForAudio == Internal.BaseTypeTriState.alwaysNone) { baseSetting = "NONE"; }
                if (asset.kind.ToUpper() == "AUDIO" && Internal.baseForAudio == Internal.BaseTypeTriState.alwaysBase && asset.assetBase == "NONE") { baseSetting = "DEFAULT"; }

                if (baseSetting == "NONE") { return ExtraAssetsLibrary.Handlers.BaseHelper.NoBase(); }
                if (baseSetting == "DEFAULT") { return ExtraAssetsLibrary.Handlers.BaseHelper.DefaultBase(); }
                Data.AssetInfo baseAsset = new Data.AssetInfo()
                {
                    kind = "CREATURE",
                    location = asset.assetBase,
                };
                return CreateAsset(baseAsset, nguid);
            }

            public static bool LibrarySelectionMade(NGuid nguid, AssetDb.DbEntry.EntryKind kind)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Library Selection Made"); }

                if (kind != AssetDb.DbEntry.EntryKind.Creature)
                {
                    // Props and Tile are currently pass-thru
                    return true;
                }

                Data.AssetInfo assetInfo = FindAssetInfo(nguid);

                string mode = assetInfo.kind.ToUpper();
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) { mode = "AURA"; }
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) { mode = "TRANSFORMATION"; }
                if (Input.GetKey(KeyCode.LeftShift)) { mode = "CREATURE"; }
                if (Input.GetKey(KeyCode.RightShift)) { mode = "EFFECT"; }

                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Handling '" + assetInfo.name + "' Using '" + mode + "' (" + assetInfo.kind + ", A" + (Input.GetKey(KeyCode.LeftAlt) | Input.GetKey(KeyCode.RightAlt)) + "C" + (Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl)) + "S" + (Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift)) + ")"); }

                if (mode == "CREATURE" || mode == "EFFECT")
                {
                    // Spawn new asset
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: " + assetInfo.kind + " Mode"); }
                    return true;
                }
                else if (mode == "AUDIO")
                {
                    // Spawn hidden speaker
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: A Mini Is Selected"); }
                    SpawnCreature(assetInfo, Vector3.zero, Vector3.zero, true);
                    return false;
                }
                else
                {
                    // Check to see if an asset is selected
                    CreatureBoardAsset asset;
                    CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                    if (asset != null)
                    {
                        Vector3 pos = asset.Creature.transform.position;
                        Vector3 rot = asset.Creature.transform.eulerAngles;
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: A Mini Is Selected"); }
                        if (mode == "TRANSFORMATION")
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
                            return false;
                        }
                        else if (mode == "AURA")
                        {
                            // Add To Currently Selected Asset
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Aura Mode"); }
                            string auraName = System.IO.Path.GetFileNameWithoutExtension(assetInfo.location);

                            if(GameObject.Find("CustomAura:" + asset.Creature.CreatureId + "." + auraName)==null)
                            {
                                // Add aura
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Request Aura '" + auraName + "' Creation on "+asset.Creature.CreatureId); }
                                StatMessaging.SetInfo(asset.Creature.CreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Aura."+auraName, nguid.ToString());
                            }
                            else
                            {
                                // Remove aura
                                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Requesting Aura '" + auraName + "' Removal on " + asset.Creature.CreatureId); }
                                StatMessaging.ClearInfo(asset.Creature.CreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Aura."+auraName);
                            }
                            return false;
                        }
                        else
                        {
                            Debug.LogWarning("Extra Assets Registration Plugin: Unknown Mode '" + mode + " (" + assetInfo.kind.ToUpper() + ")'");
                        }
                    }
                    else
                    {
                        // No creature selected - default to Spawn New Asset
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.none) { Debug.Log("Extra Assets Registration Plugin: " + mode + " (" + assetInfo.kind + ") Mode But No Selected Asset. Defaulting To Spawning Asset."); }
                        return true;
                    }
                }
                return true;
            }

            public static void LibrarySelectionMiniPlaced(NGuid nguid, CreatureGuid cid)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Library Selection Instance Made (" + cid + ")"); }
                Data.AssetInfo info = FindAssetInfo(nguid);
                if(info!=null)
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Time To Live is "+info.timeToLive); }
                    if (info.timeToLive>0f)
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Starting Self Destruct sequence"); }
                        pluginReference.StartCoroutine("DestroyAssetAfterTimeSpan", new object[] { cid, info.timeToLive });
                    }
                }
                else
                {
                    Debug.LogWarning("Extra Assets Registration Plugin: Unable To Find AssetInfo For "+nguid);
                }
            }

            public static CreatureGuid SpawnCreature(Data.AssetInfo asset, Vector3 pos, Vector3 rot, bool initalHidden = false)
            {
                NGuid transformNguid = new NGuid(asset.id);
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Content " + asset.location + " has Nguid = " + transformNguid.ToString()); }
                CreatureDataV1 creatureDataV1 = new CreatureDataV1(transformNguid);
                creatureDataV1.CreatureId = new CreatureGuid(new Bounce.Unmanaged.NGuid(System.Guid.NewGuid()));

                CreatureDataV2 creatureDataV2 = new CreatureDataV2(creatureDataV1);
                creatureDataV2.CreatureId = creatureDataV1.CreatureId;

                creatureDataV1.ExplicitlyHidden = initalHidden;
                creatureDataV2.ExplicitlyHidden = initalHidden;
                creatureDataV1.Flying = false;
                creatureDataV2.Flying = false;

                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Creating mini which " + (initalHidden ? "is" : "is not") + " hidden"); }
                CreatureManager.CreateAndAddNewCreature(creatureDataV2, pos, Quaternion.Euler(rot), false, initalHidden);

                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Registering mini for saving"); }
                BuildingBoardTool.RecordInBuildHistory(creatureDataV2.GetActiveBoardAssetId());

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
                            if (item.Name == contentName) { return item.Id; }
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
                        return seekAssetInfo;
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
        }

        public IEnumerator DestroyAssetAfterTimeSpan(object[] inputs)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Destroy Asset After " + (float)inputs[1] + " Seconds Started."); }
            yield return new WaitForSeconds((float)inputs[1]);
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset((CreatureGuid)inputs[0], out asset);
            if (asset != null)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: "+StatMessaging.GetCreatureName(asset)+" Self Destruct "); }
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
                if (BoardSessionManager.Board.PushStringToTsClipboard(slab.code) == PushStringToTsClipboardResult.Success)
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