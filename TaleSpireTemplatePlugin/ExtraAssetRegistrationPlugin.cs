using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ExtraAssetsLibrary;
using System;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(ExtraAssetPlugin.Guid)]
    public partial class ExtraAssetsRegistrationPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Extra Assets Registration Plug-In";
        public const string Guid = "org.lordashes.plugins.extraassetsregistration";
        public const string Version = "1.6.0.0";

        public string dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"\\";

        // Soft Dependency
        public const string cmpGuid = "org.lordashes.plugins.custommini";

        // Settings
        private AutomaticAssetsSeekSetting seekSetting = AutomaticAssetsSeekSetting.newAssetsOnly;
        private AssetGroups groupStrategy = AssetGroups.custom;
        private string groupStrategyList = "";
        private const string coreGroups = "Beast,Constructs,Demonic,Dragonfolk,Dwarf,Elementals,Elf,Fey,Giants,Gnome,Goblin,Half-Demon,Halfling,Half-Orc,Human,Humanoid,Monsterous,Orc,Undead";
        private ConfigEntry<KeyboardShortcut> triggerRegistration;

        // Local variables
        private Dictionary<string, AssetInfo> assetsByLocation = new Dictionary<string, AssetInfo>();

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            // Not required but good idea to log this state for troubleshooting purpose
            UnityEngine.Debug.Log("Template Plugin: Lord Ashes Template Plugin Is Active.");

            if (!System.IO.Directory.Exists(dir+"\\cache")) { System.IO.Directory.CreateDirectory(dir+"\\cache"); }

            seekSetting = Config.Bind("Settings", "Detect New Assets Settings", AutomaticAssetsSeekSetting.newAssetsOnly).Value;
            groupStrategy = Config.Bind("Settings", "Organization Settings", AssetGroups.custom).Value;
            triggerRegistration = Config.Bind("Shortcuts", "Manual Seek For Assets", new KeyboardShortcut(KeyCode.A, KeyCode.RightControl));

            // Get new assets
            bool newAssets = false;
            Debug.Log("Extra Asset Registration Plugin: Look For New Assets Set To " + seekSetting.ToString());
            if (seekSetting!=AutomaticAssetsSeekSetting.manual)
            {
                // For full seek, delete cache 
                if (seekSetting==AutomaticAssetsSeekSetting.fullSeek)
                {
                    // Delete cached assets
                    Debug.Log("Extra Asset Registration Plugin: Deleting Cache");
                    foreach (string item in System.IO.Directory.EnumerateFiles(dir + "\\cache"))
                    {
                        System.IO.File.Delete(item);
                    }
                }
                // Seek registerable assets
                Debug.Log("Extra Asset Registration Plugin: Getting Registerable Assets");
                newAssets = GetAssets(ref assetsByLocation);
            }

            // Register Assets
            foreach (AssetInfo asset in assetsByLocation.Values)
            {
                Debug.Log("Extra Asset Registration Plugin: Registering [" + asset.location + "] in ["+ asset.groupName + "] as [" + asset.id + "]");
                try
                {
                    ExtraAssetsLibrary.DTO.Asset extraAsset = new ExtraAssetsLibrary.DTO.Asset()
                    {
                        Id = new NGuid(asset.id),
                        GroupName = asset.groupName,
                        Name = asset.name,
                        Description = asset.description,
                        Kind = AssetDb.DbEntry.EntryKind.Creature,
                        Icon = FileAccessPlugin.Image.LoadSprite(dir + "cache\\" + asset.id + ".png"),
                        BaseCallback = null,
                        ModelCallback = (nguid) =>
                        {
                            CreatureBoardAsset transformationTarget = null;
                            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out transformationTarget);
                            Debug.Log("Extra Asset Registration Plugin: [" + ((transformationTarget != null) ? "Y" : "N") + "] Creature Selected");
                            if(asset.kind.ToUpper()=="TRANSFORMATION" && (transformationTarget!=null))
                            {
                                Debug.Log("Extra Asset Registration Plugin: CMP Transformation Request [" + System.IO.Path.GetFileName(asset.location) + "]");
                                SDIM.InvokeResult success = SDIM.InvokeMethod("LordAshes-StatMessagingPlugin/StatMessaging.dll", "SetInfo", new object[] { LocalClient.SelectedCreatureId, cmpGuid, System.IO.Path.GetFileName(asset.location) });
                                if (success == SDIM.InvokeResult.success) { return null; }
                            }
                            else if (asset.kind.ToUpper() == "AURA"  && (transformationTarget != null))
                            {
                                Debug.Log("Extra Asset Registration Plugin: CMP Effect Request [" + System.IO.Path.GetFileName(asset.location) + "]");
                                SDIM.InvokeResult success = SDIM.InvokeMethod("LordAshes-StatMessagingPlugin/StatMessaging.dll", "SetInfo", new object[] { LocalClient.SelectedCreatureId, cmpGuid + ".effect", System.IO.Path.GetFileName(asset.location) });
                                if (success == SDIM.InvokeResult.success) { return null; }
                            }
                            else if (asset.kind.ToUpper() == "EFFECT"  && (transformationTarget != null))
                            {
                                Debug.Log("Extra Asset Registration Plugin: CMP Blank Mini And Effect Request [" + System.IO.Path.GetFileName(asset.location) + "]");
                                SDIM.InvokeResult success1 = SDIM.InvokeMethod("LordAshes-StatMessagingPlugin/StatMessaging.dll", "SetInfo", new object[] { LocalClient.SelectedCreatureId, cmpGuid, "blank" });
                                SDIM.InvokeResult success2 = SDIM.InvokeMethod("LordAshes-StatMessagingPlugin/StatMessaging.dll", "SetInfo", new object[] { LocalClient.SelectedCreatureId, cmpGuid + ".effect", System.IO.Path.GetFileName(asset.location) });
                                if (success1 == SDIM.InvokeResult.success && success2 == SDIM.InvokeResult.success) { return null; }
                            }
                            Debug.Log("Extra Asset Registration Plugin: Loading [" + System.IO.Path.GetFileName(asset.location) + "] From AssetBundle [" + asset.location + "]");
                            
                            AssetBundle ab = FileAccessPlugin.AssetBundle.Load(asset.location);
                            GameObject model = ab.LoadAsset<GameObject>(System.IO.Path.GetFileName(asset.location));
                            model.transform.eulerAngles = new Vector3(0,180,0);

                            List<Renderer> renderers = new List<Renderer>();
                            renderers.AddRange(model.GetComponents<Renderer>());
                            renderers.AddRange(model.GetComponentsInChildren<Renderer>());

                            foreach (Renderer renderer in renderers)
                            {
                                renderer.material.shader = Shader.Find("Taleweaver/CreatureShader");
                            }

                            ab.Unload(false);

                            return model;
                        }
                    };
                    ExtraAssetPlugin.AddAsset(extraAsset);
                }
                catch(Exception)
                {
                    Debug.Log("Extra Asset Registration Plugin: Failed To Register [" + System.IO.Path.GetFileName(asset.location) + "]");
                }
            }

            // Write out new AssetInfo cache file
            if (newAssets)
            {
                Debug.Log("Extra Asset Registration Plugin: Updating Asset Cache File");
                FileAccessPlugin.File.WriteAllText(dir + "cache\\AssetInfo.cache", "  " + JsonConvert.SerializeObject(assetsByLocation.Values.ToArray<AssetInfo>(),Formatting.Indented));
            }
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            // Check for manual asset search
            if (Utility.StrictKeyCheck(triggerRegistration.Value))
            {
                SystemMessage.DisplayInfoText("Looking For New Assets...");
                Debug.Log("Extra Asset Registration Plugin: Deleting Cache");
                foreach (string item in System.IO.Directory.EnumerateFiles(dir + "\\cache"))
                {
                    System.IO.File.Delete(item);
                }
                GetAssets(ref assetsByLocation);
                SystemMessage.DisplayInfoText("Sorry But You Will Need To Restart TS\r\nTo Get Access To New Assets");
            }
        }

        public bool GetAssets(ref Dictionary<string, AssetInfo> assetsByLocation)
        {
            AssetInfo[] assetList = new AssetInfo[] { };
            if (System.IO.File.Exists(dir + "\\cache\\AssetInfo.cache"))
            {
                assetList = JsonConvert.DeserializeObject<AssetInfo[]>(FileAccessPlugin.File.ReadAllText(dir + "\\cache\\AssetInfo.cache"));
                foreach (AssetInfo asset in assetList)
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
                        AssetInfo info = JsonConvert.DeserializeObject<AssetInfo>(txt);
                        Debug.Log("Extra Asset Registration: Info = "+ JsonConvert.SerializeObject(info));
                        info.id = ExtraAssetsLibrary.DTO.Asset.GenerateID(ExtraAssetsRegistrationPlugin.Guid + "." + location).ToString();
                        Debug.Log("Extra Asset Registration: Id = " + info.id.ToString());
                        info.location = location;
                        Debug.Log("Extra Asset Registration: Location = " + info.location);
                        switch (groupStrategy)
                        {
                            case AssetGroups.custom:
                                break;
                            case AssetGroups.listOnly:
                                if (!groupStrategyList.Contains(info.groupName)) { info.groupName = "Custom Content"; }
                                break;
                            case AssetGroups.coreOnly:
                                if (!coreGroups.Contains(info.groupName)) { info.groupName = "Custom Content"; }
                                break;
                            case AssetGroups.singleFolder:
                                info.groupName = "Custom Content";
                                break;
                        }
                        if (info.name == "") { info.name = System.IO.Path.GetFileName(location); }
                        if (info.groupName == "") { info.groupName = "Custom Content"; }
                        Debug.Log("Extra Asset Registration: Group = " + info.groupName);

                        if (info.kind.ToUpper() == "TRANSFORMATION") { info.groupName = info.groupName + " (CMP)"; }
                        else if (info.kind.ToUpper() == "AURA") { info.groupName = info.groupName + " (CMP)"; }
                        else if (info.kind.ToUpper() == "EFFECT") { info.groupName = info.groupName + " (CMP)"; }
                        else if (info.location.ToUpper() == "TRANSFORMATION") { info.groupName = info.groupName + " (CMP)"; info.kind = "Transformation"; }
                        else if (info.location.ToUpper() == "AURA") { info.groupName = info.groupName + " (CMP)"; info.kind = "Aura"; }
                        else if (info.location.ToUpper() == "EFFECT") { info.groupName = info.groupName + " (CMP)"; info.kind = "Effect"; }
                        Debug.Log("Extra Asset Registration: Revised Group = " + info.groupName);

                        assetsByLocation.Add(info.location, info);
                        Debug.Log("Extra Asset Registration: Added To List");

                        Texture2D portrait = ab.LoadAsset<Texture2D>("Portrait.png");
                        Debug.Log("Extra Asset Registration: Portrait? "+(portrait != null));
                        if (portrait != null)
                        {
                            try
                            {
                                Debug.Log("Extra Asset Registration: Caching Portrait");
                                System.IO.File.WriteAllBytes(dir + "cache\\" + info.id.ToString() + ".png", portrait.EncodeToPNG());
                            }
                            catch(Exception)
                            {
                                Debug.Log("Extra Asset Registration: Failed To Cache Portrait");
                                portrait = null;
                            }
                        }
                        if (portrait == null)
                        {
                            Debug.Log("Extra Asset Registration: Creating Default Portrait");
                            ExtraAssetsRegistrationPlugin.Image.CreateTextImage(info.name, 128, 128, dir + "Default.png").Save(dir + "cache\\" + info.id.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    catch (Exception x)
                    {
                        Debug.Log("Extra Asset Registration: Content " + location + " does not seem to be an assetBundle or is corrupt\r\n"+x);
                    }
                    if (ab != null) { ab.Unload(true); }
                }
            }
            return newAssets;
        }

        public bool ReplaceGameObjectMesh(GameObject source, GameObject destination)
        {
            if (destination == null) { Debug.Log("Destination Is Null"); return false; }
            MeshFilter dMF = destination.GetComponent<MeshFilter>();
            MeshRenderer dMR = destination.GetComponent<MeshRenderer>();
            if (dMF == null || dMR == null) { Debug.LogWarning("Unable get destination MF or MR."); return false; }

            destination.transform.position = new Vector3(0, 0, 0);
            destination.transform.rotation = Quaternion.Euler(0, 0, 0);
            destination.transform.eulerAngles = new Vector3(0, 0, 0);
            destination.transform.localPosition = new Vector3(0, 0, 0);
            destination.transform.localRotation = Quaternion.Euler(0, 0, 0);
            destination.transform.localEulerAngles = new Vector3(0, 180, 0);
            destination.transform.localScale = new Vector3(1f, 1f, 1f);

            dMF.transform.position = new Vector3(0, 0, 0);
            dMF.transform.rotation = Quaternion.Euler(0, 0, 0);
            dMF.transform.eulerAngles = new Vector3(0, 0, 0);
            dMF.transform.localPosition = new Vector3(0, 0, 0);
            dMF.transform.localRotation = Quaternion.Euler(0, 0, 0);
            dMF.transform.localEulerAngles = new Vector3(0, 0, 0);
            dMF.transform.localScale = new Vector3(1, 1, 1);

            dMR.transform.position = new Vector3(0, 0, 0);
            dMR.transform.rotation = Quaternion.Euler(0, 0, 0);
            dMR.transform.eulerAngles = new Vector3(0, 0, 0);
            dMR.transform.localPosition = new Vector3(0, 0, 0);
            dMR.transform.localRotation = Quaternion.Euler(0, 0, 0);
            dMR.transform.localEulerAngles = new Vector3(0, 0, 0);
            dMR.transform.localScale = new Vector3(1, 1, 1);

            MeshFilter sMF = (source.GetComponent<MeshFilter>() != null) ? source.GetComponent<MeshFilter>() : source.GetComponentInChildren<MeshFilter>();
            if (sMF != null)
            {
                Debug.Log("Copying MF->MF");
                Debug.Log("Mesh From " + sMF.mesh.name + " / " + sMF.sharedMesh.name + " (" + (sMF.mesh.triangles.Length / 3).ToString() + " Polygons / " + (sMF.mesh.triangles.Length / 3).ToString() + " Polygons)");
                dMF.mesh = sMF.mesh;
                dMF.sharedMesh = sMF.sharedMesh;
            }

            MeshRenderer sMR = (source.GetComponent<MeshRenderer>() != null) ? source.GetComponent<MeshRenderer>() : source.GetComponentInChildren<MeshRenderer>();
            if (sMR != null)
            {
                Debug.Log("Copying MR->MR");
                Debug.Log("Material From " + sMR.material.name + " / " + sMR.sharedMaterial.name);
                Shader shaderSave = dMR.material.shader;  // Shader must be maintained in order for the Stealth mode to work automatically
                dMR.sharedMaterials = sMR.sharedMaterials;
                dMR.material.shader = shaderSave;
            }

            SkinnedMeshRenderer sSMR = (source.GetComponent<SkinnedMeshRenderer>() != null) ? source.GetComponent<SkinnedMeshRenderer>() : source.GetComponentInChildren<SkinnedMeshRenderer>();
            if (sSMR != null)
            {
                Debug.Log("Copying SMR->MF/MR");
                Debug.Log("Mesh From " + sSMR.sharedMesh.name + " (" + (sSMR.sharedMesh.triangles.Length / 3).ToString() + " Polygons)");
                dMF.sharedMesh = sSMR.sharedMesh;
                Shader shaderSave = dMR.material.shader; // Shader must be maintained in order for the Stealth mode to work automatically
                Debug.Log("Material From " + sSMR.material.name);
                dMR.material = sSMR.material;
                dMR.material.shader = shaderSave;
            }

            return true;
        }

        public class AssetInfo
        {
            public string kind { get; set; } = "";
            public string groupName { get; set; } = "";
            public string description { get; set; } = "";
            public string name { get; set; } = "";
            public string tags { get; set; } = "";
            public string id { get; set; } = "";
            public string location { get; set; } = "";
        }

        public enum AutomaticAssetsSeekSetting
        {
            manual = 0,
            newAssetsOnly = 1,
            fullSeek = 2
        }

        public enum AssetGroups
        {
            custom = 0,
            listOnly = 1,
            coreOnly = 2,
            singleFolder = 3
        }
    }
}
