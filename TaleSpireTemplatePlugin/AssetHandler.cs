using BepInEx;
using Bounce.Unmanaged;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
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
                if (System.IO.File.Exists(ExtraAssetsRegistrationPlugin.Internal.pluginDirectory + "\\cache\\Data.AssetInfo.cache"))
                {
                    assetList = JsonConvert.DeserializeObject<Data.AssetInfo[]>(FileAccessPlugin.File.ReadAllText(Internal.pluginDirectory + "\\cache\\Data.AssetInfo.cache"));
                    foreach (Data.AssetInfo asset in assetList)
                    {
                        assetsByLocation.Add(asset.location, asset);
                    }
                }
                bool newAssets = false;
                foreach (string location in FileAccessPlugin.File.Catalog(false))
                {
                    bool isAssetBundle = (System.IO.Path.GetExtension(location) == "");
                    if (isAssetBundle && !assetsByLocation.ContainsKey(location))
                    {
                        // Add new asset to the asset cache
                        newAssets = true;
                        AssetBundle ab = null;
                        try
                        {
                            ab = FileAccessPlugin.AssetBundle.Load(location);
                            Debug.Log("Extra Asset Registration Plugin: AssetBundle? " + (ab != null));
                            TextAsset ta = ab.LoadAsset<TextAsset>("Info.txt");
                            Debug.Log("Extra Asset Registration Plugin: Info.Text? " + (ta != null));
                            string txt = "";
                            if (ta != null) { txt = ta.text; } else { txt = "{\"kind\": \"Creature\",\"id\": \"\",\"groupName\": \"Custom Content\",\"description\": \"" + System.IO.Path.GetFileName(location) + "\",\"name\": \"" + System.IO.Path.GetFileName(location) + "\",\"tags\": \"\"}"; }
                            Data.AssetInfo info = JsonConvert.DeserializeObject<Data.AssetInfo>(txt);
                            Debug.Log("Extra Asset Registration: Info = " + JsonConvert.SerializeObject(info));
                            info.id = ExtraAssetsLibrary.DTO.Asset.GenerateID(ExtraAssetsRegistrationPlugin.Guid + "." + location).ToString();
                            Debug.Log("Extra Asset Registration: Id = " + info.id.ToString());
                            info.location = location;
                            Debug.Log("Extra Asset Registration: Location = " + info.location);
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
                            if (info.name == "") { info.name = System.IO.Path.GetFileName(location); }
                            if (info.groupName == "") { info.groupName = "Custom Content"; }
                            Debug.Log("Extra Asset Registration: Group = " + info.groupName);

                            assetsByLocation.Add(info.location, info);
                            Debug.Log("Extra Asset Registration: Added To List");

                            Texture2D portrait = ab.LoadAsset<Texture2D>("Portrait.png");
                            Debug.Log("Extra Asset Registration: Portrait? " + (portrait != null));
                            if (portrait != null)
                            {
                                try
                                {
                                    Debug.Log("Extra Asset Registration: Caching Portrait");
                                    System.IO.File.WriteAllBytes(Internal.pluginDirectory + "cache\\" + info.id.ToString() + ".png", portrait.EncodeToPNG());
                                }
                                catch (Exception)
                                {
                                    Debug.Log("Extra Asset Registration: Failed To Cache Portrait");
                                    portrait = null;
                                }
                            }
                            if (portrait == null)
                            {
                                Debug.Log("Extra Asset Registration: Creating Default Portrait");
                                ExtraAssetsRegistrationPlugin.Image.CreateTextImage(info.name, 128, 128, Internal.pluginDirectory + "Default.png").Save(Internal.pluginDirectory + "cache\\" + info.id.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        catch (Exception x)
                        {
                            Debug.Log("Extra Asset Registration: Content " + location + " does not seem to be an assetBundle or is corrupt\r\n" + x);
                        }
                        if (ab != null) { ab.Unload(true); }
                    }
                }
                return newAssets;
            }

            public static GameObject CreateAsset(Data.AssetInfo asset, NGuid nguid)
            {
                Debug.Log("Extra Asset Registration Plugin: Loading (" + asset.kind + ") '" + System.IO.Path.GetFileName(asset.location) + "' From AssetBundle '" + asset.location + "'");

                AssetBundle ab = FileAccessPlugin.AssetBundle.Load(asset.location);
                Debug.Log("Extra Asset Registration Plugin: AB Null? " + (ab == null));
                GameObject model = ab.LoadAsset<GameObject>(System.IO.Path.GetFileName(asset.location));
                Debug.Log("Extra Asset Registration Plugin: Model Null? " + (model == null));
                model.transform.eulerAngles = new Vector3(0, 180, 0);

                if ((asset.kind.ToUpper() != "AURA") && (asset.kind.ToUpper() != "EFFECT"))
                {
                    Debug.Log("Extra Asset Registration Plugin: Applying 'Taleweaver/CreatureShader' Shader");
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
                    Debug.Log("Extra Asset Registration Plugin: Keeping AssetBundle Specified Shader");
                }

                ab.Unload(false);

                return model;
            }

            public static GameObject CreateAssetBase(Data.AssetInfo asset, NGuid nguid)
            { 
                if (asset.assetBase == "") { return ExtraAssetsLibrary.Handlers.BaseHelper.DefaultBase(); }
                if (asset.assetBase.ToUpper() == "NONE") if (asset.assetBase == "") { return ExtraAssetsLibrary.Handlers.BaseHelper.NoBase(); }
                Data.AssetInfo baseAsset = new Data.AssetInfo()
                {
                    kind = "CREATURE",
                    location = asset.assetBase,
                };
                return CreateAsset(baseAsset,nguid);
            }

            public static bool LibrarySelectionMade(NGuid nguid, AssetDb.DbEntry.EntryKind kind)
            {
                Debug.Log("Extra Asset Registration Plugin: Library Selection Made");

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

                Debug.Log("Extra Asset Registration Plugin: Handling '" + assetInfo.name + "' Using '" + mode + "' (" + assetInfo.kind + ", A" + (Input.GetKey(KeyCode.LeftAlt)|Input.GetKey(KeyCode.RightAlt)) + "C" + (Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl)) + "S" + (Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift)) + ")");

                if (mode == "CREATURE" || mode == "EFFECT")
                {
                    // Spawn new asset
                    Debug.Log("Extra Asset Registration Plugin: " + assetInfo.kind + " Mode");
                    return true;
                }
                else if (mode == "AUDIO")
                {
                    // Spawn hidden speaker
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
                        Debug.Log("Extra Asset Registration Plugin: A Mini Is Selected");
                        if (mode == "TRANSFORMATION")
                        {
                            // Replace currently selected asset
                            Debug.Log("Extra Asset Registration Plugin: Transformation Mode");
                            foreach (Transform child in asset.Creature.transform.Children())
                            {
                                if (child.name.StartsWith("Custom"))
                                {
                                    Debug.Log("Extra Asset Registration Plugin: Destroying '" + child.name + "' Of '" + Utility.GetCreatureName(asset) + "'");
                                    GameObject.Destroy(child.gameObject);
                                }
                            }
                            Debug.Log("Extra Asset Registration Plugin: Destroying '" + Utility.GetCreatureName(asset) + "'");
                            asset.RequestDelete();
                            Debug.Log("Extra Asset Registration Plugin: Creating '" + assetInfo.name + "'");
                            SpawnCreature(assetInfo, pos, rot);
                            return false;
                        }
                        else if (mode == "AURA")
                        {
                            // Add To Currently Selected Asset
                            Debug.Log("Extra Asset Registration Plugin: Aura Mode");
                            string auraName = System.IO.Path.GetFileNameWithoutExtension(assetInfo.location);

                            bool auraExists = false;
                            foreach (CreatureBoardAsset check in CreaturePresenter.AllCreatureAssets)
                            {
                                if (check.Creature.Name.StartsWith("CustomAura:" + asset.Creature.CreatureId + "." + auraName)) { auraExists = true; break; }
                            }

                            if (!auraExists)
                            {
                                // Add aura
                                Debug.Log("Extra Asset Registration Plugin: Adding Aura '" + auraName + "'");
                                CreatureGuid auraCid = SpawnCreature(assetInfo, pos, rot);
                                pluginReference.StartCoroutine("LinkAura", new object[] { auraName, asset.Creature.CreatureId, auraCid });
                            }
                            else
                            {
                                // Remove Aura
                                Debug.Log("Extra Asset Registration Plugin: Removing Aura '" + auraName + "'");
                                foreach (CreatureBoardAsset checkAsset in CreaturePresenter.AllCreatureAssets)
                                {
                                    if (checkAsset.Creature.Name.StartsWith("CustomAura:" + asset.Creature.CreatureId.ToString() + "." + auraName))
                                    {
                                        checkAsset.RequestDelete();
                                        break;
                                    }
                                }
                            }

                            return false;
                        }
                        else
                        {
                            Debug.Log("Extra Asset Registration Plugin: Unknown Mode '" + mode + " (" + assetInfo.kind.ToUpper() + ")'");
                        }
                    }
                    else
                    {
                        // No creature selected - default to Spawn New Asset
                        Debug.Log("Extra Asset Registration Plugin: " + mode + " (" + assetInfo.kind + ") Mode But No Selected Asset. Defaulting To Spawning Asset.");
                        return true;
                    }
                }
                return true;
            }

            public static void LibrarySelectionMiniPlaced(NGuid nguid, CreatureGuid cid)
            {
                Debug.Log("Extra Asset Registration Plugin: Library Selection Instance Made (" + cid + ")");
            }

            public static void PlayAudio()
            {
                Debug.Log("Extra Asset Registration Plugin: Play Audio");
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if (asset != null)
                {
                    AudioSource source = asset.GetComponentInChildren<AudioSource>();
                    if (source!=null)
                    {
                        source.Play();
                    }
                }
            }

            public static void StopAudio()
            {
                Debug.Log("Extra Asset Registration Plugin: Stop Audio");
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if (asset != null)
                {
                    AudioSource source = asset.GetComponentInChildren<AudioSource>();
                    if (source != null)
                    {
                        source.Stop();
                    }
                }
            }

            public static void PlayAnimation(string animName)
            {
                Debug.Log("Extra Asset Registration Plugin: Play Animation "+animName);
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if (asset != null)
                {
                    Animation animation = asset.GetComponentInChildren<Animation>();
                    if (animation != null)
                    {
                        if (animName == null)
                        {
                            SystemMessage.AskForTextInput("Play Animation", "Animation Name:", "OK", (userName) => { animation.Play(userName); }, null, "Cancel", null);
                        }
                        else
                        {
                            animation.Play(animName);
                        }
                    }
                }
            }

            public static void StopAnimation()
            {
                Debug.Log("Extra Asset Registration Plugin: Stop Animation ");
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if (asset != null)
                {
                    Animation animation = asset.GetComponentInChildren<Animation>();
                    if (animation != null)
                    {
                        animation.Stop();
                    }
                }
            }

            public static void SwitchPose(string poseName)
            {
                Debug.Log("Extra Asset Registration Plugin: Switch Pose To "+poseName);
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if (asset != null)
                {
                    SystemMessage.AskForTextInput("Play Animation", "Animation Name:", "OK", (pose) =>
                    {
                        string source = pose.Substring(0, pose.IndexOf("."));
                        pose = pose.Substring(pose.IndexOf(".")+1);
                        AssetBundle ab = FileAccessPlugin.AssetBundle.Load(source+"/"+source);
                        if(ab!=null)
                        {
                            GameObject prefab = ab.LoadAsset<GameObject>(pose);
                            if(prefab!=null)
                            {
                                MeshFilter assetMF = asset.GetComponentInChildren<MeshFilter>();
                                MeshFilter prefabMF = prefab.GetComponentInChildren<MeshFilter>();
                                Renderer assetR = asset.GetComponentInChildren<Renderer>();
                                Renderer prefabR = prefab.GetComponentInChildren<Renderer>();
                                if (assetMF!=null && prefabMF!=null)
                                {
                                    assetMF.mesh = prefabMF.mesh;
                                }
                                if (assetR != null && prefabR != null)
                                {
                                    assetR.material = prefabR.material;
                                    assetR.material.shader = Shader.Find("Taleweaver/CreatureShader");
                                }
                            }
                        }
                    
                    }, null, "Cancel", null);
                }
            }

            public static CreatureGuid SpawnCreature(Data.AssetInfo asset, Vector3 pos, Vector3 rot, bool initalHidden = false)
            {
                NGuid transformNguid = new NGuid(asset.id);
                Debug.Log("Content " + asset.location + " has Nguid = " + transformNguid.ToString());
                Debug.Log("Creating CreatureV1 data");
                CreatureDataV1 creatureDataV1 = new CreatureDataV1(transformNguid);
                creatureDataV1.CreatureId = new CreatureGuid(new Bounce.Unmanaged.NGuid(System.Guid.NewGuid()));

                Debug.Log("Creating CreatureV2 data");
                CreatureDataV2 creatureDataV2 = new CreatureDataV2(creatureDataV1);
                creatureDataV2.CreatureId = creatureDataV1.CreatureId;

                creatureDataV1.ExplicitlyHidden = initalHidden;
                creatureDataV2.ExplicitlyHidden = initalHidden;
                creatureDataV1.Flying = false;
                creatureDataV2.Flying = false;

                Debug.Log("Creating mini which "+(initalHidden ? " is" : " is not") +" hidden");
                CreatureManager.CreateAndAddNewCreature(creatureDataV2, pos, Quaternion.Euler(rot), false, initalHidden);

                Debug.Log("Registering mini for saving");
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
                        Debug.Log("Extra Asset Registration Plugin: Custom Asset Recognized");
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
                                Debug.Log("Extra Asset Registration Plugin: Core Asset Recognized");
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
                Debug.Log("Extra Asset Registration Plugin: Asset  NGuid " + nguid + " Not Recognized");
                return null;
            }
        }

        public IEnumerator LinkAura(object[] inputs)
        {
            CreatureBoardAsset asset = null;
            CreatureBoardAsset parentAsset = null;
            string auraName = inputs[0].ToString();
            CreatureGuid parentCid = (CreatureGuid)inputs[1];
            CreatureGuid auraCid = (CreatureGuid)inputs[2];
            Debug.Log("Extra Asset Registration Plugin: LinkAura Co-Routine: Getting Aura");
            while (asset == null)
            {
                // Get Aura
                yield return new WaitForSeconds(0.1f);
                CreaturePresenter.TryGetAsset(auraCid, out asset);
            }
            Debug.Log("Extra Asset Registration Plugin: LinkAura Co-Routine: Getting Parent");
            while (parentAsset == null)
            {
                // Get Parent
                yield return new WaitForSeconds(0.1f);
                CreaturePresenter.TryGetAsset(parentCid, out parentAsset);
            }
            // Parent aura
            Debug.Log("Extra Asset Registration Plugin: LinkAura Co-Routine: Linking");
            asset.transform.SetParent(parentAsset.CreatureLoaders[0].LoadedAsset.transform);
            // Update the aura mini name so that we can find it later (to implement toggle action)
            string suffix = asset.Creature.Name;
            if (suffix.IndexOf("<size=0") > -1) { suffix = suffix.Substring(suffix.IndexOf("<size=0")); } else { suffix = ""; }
            CreatureManager.SetCreatureName(auraCid, "CustomAura:" + parentCid.ToString() + "." + auraName + suffix); ;
            // Remove aura base
            if (Internal.baseForAura == Internal.BaseTypeBiState.alwaysNone)
            {
                asset.BaseLoader.GetComponentInChildren<MeshFilter>().mesh.triangles = new int[0];
            }
        }

        public IEnumerator LinkAuras(float initialDelay)
        {
            Debug.Log("Extra Asset Registration Plugin: LinkAuras Co-Routine: Started");
            yield return new WaitForSeconds(initialDelay);
            foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
            {
                if(asset.Creature.Name.StartsWith("CustomAura:"))
                {
                    string cid = asset.Creature.Name.Substring(asset.Creature.Name.IndexOf("CustomAura:")+ "CustomAura:".Length);
                    cid = cid.Substring(0, cid.IndexOf("."));
                    CreatureBoardAsset parentAsset = null;
                    CreaturePresenter.TryGetAsset(new CreatureGuid(cid), out parentAsset);
                    Debug.Log("Extra Asset Registration Plugin: Relinking Aura '"+asset.Creature.CreatureId+"' With Mini '"+parentAsset.Creature.CreatureId+"'");
                    asset.transform.SetParent(parentAsset.CreatureLoaders[0].LoadedAsset.transform);
                }
            }
            Debug.Log("Extra Asset Registration Plugin: LinkAuras Co-Routine: Ended");
        }
    }
}