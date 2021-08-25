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
        public const string Version = "1.0.0.0";

        public string dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"\\";

        private AutomaticAssetsSeekSetting seekSetting = AutomaticAssetsSeekSetting.newAssetsOnly;
        private AssetGroups groupStrategy = AssetGroups.custom;
        private string groupStrategyList = "";
        private const string coreGroups = "Beast,Constructs,Demonic,Dragonfolk,Dwarf,Elementals,Elf,Fey,Giants,Gnome,Goblin,Half-Demon,Halfling,Half-Orc,Human,Humanoid,Monsterous,Orc,Undead";
        private ConfigEntry<KeyboardShortcut> triggerRegistration;

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
                // assets.Add(asset.id, asset);
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
                        Debug.Log("Extra Asset Registration Plugin: Loading [" + System.IO.Path.GetFileName(asset.location) + "] From AssetBundle [" + asset.location + "]");
                        AssetBundle ab = FileAccessPlugin.AssetBundle.Load(asset.location);
                        GameObject model = ab.LoadAsset<GameObject>(System.IO.Path.GetFileName(asset.location));
                        ab.Unload(false);
                        return model;
                    }
                };
                ExtraAssetPlugin.AddAsset(extraAsset);
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
            if(Utility.StrictKeyCheck(triggerRegistration.Value))
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
                    AssetBundle ab = FileAccessPlugin.AssetBundle.Load(location);
                    TextAsset ta = ab.LoadAsset<TextAsset>("Info.txt");
                    string txt = "";
                    if (ta != null) { txt = ta.text; } else { txt = "{\"kind\": \"Creature\",\"id\": \"\",\"groupName\": \"Custom Content\",\"description\": \"" + System.IO.Path.GetFileName(location) + "\",\"name\": \"" + System.IO.Path.GetFileName(location) + "\",\"tags\": \"\"}"; Debug.Log("Using Default Text"); }
                    AssetInfo info = JsonConvert.DeserializeObject<AssetInfo>(txt);
                    info.id = ExtraAssetsLibrary.DTO.Asset.GenerateID(ExtraAssetsRegistrationPlugin.Guid + "." + location).ToString();
                    info.location = location;
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
                    assetsByLocation.Add(info.location, info);
                    Texture2D portrait = ab.LoadAsset<Texture2D>("Portrait.png");
                    if (portrait != null)
                    {
                        System.IO.File.WriteAllBytes(dir + "cache\\" + info.id.ToString() + ".png", portrait.EncodeToPNG());
                    }
                    else
                    {
                        ExtraAssetsRegistrationPlugin.Image.CreateTextImage(info.name, 128, 128, dir + "Default.png").Save(dir + "cache\\" + info.id.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                    ab.Unload(true);
                }
            }
            return newAssets;
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
            fullSeek =2
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
