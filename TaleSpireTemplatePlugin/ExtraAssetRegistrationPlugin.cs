using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using Newtonsoft.Json;
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
        public const string Version = "2.0.0.0";

        private static class Internal
        {
            public enum BaseTypeBiState
            {
                alwaysNone = 0,
                alwaysBase = 1
            }

            public enum BaseTypeTriState
            {
                alwaysNone = 0,
                alwaysBase = 1,
                asPerAsset = 2
            }

            // Soft Dependency
            public const string cmpGuid = "org.lordashes.plugins.custommini";

            // Settings
            public static string pluginDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";
            public static Data.AutomaticAssetsSeekSetting seekSetting = Data.AutomaticAssetsSeekSetting.newAssetsOnly;
            public static ConfigEntry<KeyboardShortcut> triggerRegistration;

            public static float auraSolidifcationDelay = 5f;
            public static bool auraLinkApplied = false;

            public static BaseTypeTriState baseForCreatures = BaseTypeTriState.asPerAsset;
            public static BaseTypeTriState baseForEffects = BaseTypeTriState.asPerAsset;
            public static BaseTypeTriState baseForAudio = BaseTypeTriState.asPerAsset;
            public static BaseTypeBiState baseForAura = BaseTypeBiState.alwaysNone;
        }

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            // Not required but good idea to log this state for troubleshooting purpose
            UnityEngine.Debug.Log("Extra Assets Registration Plugin: Is Active.");

            if (!System.IO.Directory.Exists(Internal.pluginDirectory + "\\cache")) { System.IO.Directory.CreateDirectory(Internal.pluginDirectory + "\\cache"); }

            AssetHandler.Initialize(this, Config.Bind("Settings", "Organization Settings", Data.AssetGroups.custom).Value);

            Internal.seekSetting = Config.Bind("Settings", "Detect New Assets Settings", Data.AutomaticAssetsSeekSetting.newAssetsOnly).Value;
            Internal.triggerRegistration = Config.Bind("Shortcuts", "Manual Seek For Assets", new KeyboardShortcut(KeyCode.A, KeyCode.RightControl));
            Internal.auraSolidifcationDelay = Config.Bind("Settings", "Delay In Seconds After Start When Auras Are Attached", 5f).Value;

            Internal.baseForCreatures = Config.Bind("Settings", "Base For Creatures", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.baseForEffects = Config.Bind("Settings", "Base For Effects", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.baseForAudio = Config.Bind("Settings", "Base For Audio", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.baseForAura = Config.Bind("Settings", "Base For Auras", Internal.BaseTypeBiState.alwaysNone).Value;

            RegisterAssets();
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            // Check for manual asset search
            if (Utility.StrictKeyCheck(Internal.triggerRegistration.Value))
            {
                SystemMessage.DisplayInfoText("Looking For New Assets...");
                Debug.Log("Extra Asset Registration Plugin: Deleting Cache");
                foreach (string item in System.IO.Directory.EnumerateFiles(Internal.pluginDirectory + "\\cache"))
                {
                    System.IO.File.Delete(item);
                }
                RegisterAssets();
            }

            if (Utility.isBoardLoaded())
            {
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation("Anim01"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation("Anim02"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation("Anim03"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation("Anim04"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation("Anim05"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha6, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation("Anim06"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha7, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation("Anim07"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha8, KeyCode.LeftAlt))) { AssetHandler.PlayAnimation(null); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha9, KeyCode.LeftAlt))) { AssetHandler.PlayAudio(); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha0, KeyCode.LeftAlt))) { AssetHandler.StopAnimation(); AssetHandler.StopAudio(); }

                if (!Internal.auraLinkApplied)
                {
                    Internal.auraLinkApplied = true;
                    StartCoroutine("LinkAuras", Internal.auraSolidifcationDelay);
                }
            }
            else
            {
                Internal.auraLinkApplied = false;
            }
        }

        private void RegisterAssets()
        {
            // Get new assets
            bool newAssets = false;
            Debug.Log("Extra Asset Registration Plugin: Look For New Assets Set To " + Internal.seekSetting.ToString());
            if (Internal.seekSetting != Data.AutomaticAssetsSeekSetting.manual)
            {
                // For full seek, delete cache 
                if (Internal.seekSetting == Data.AutomaticAssetsSeekSetting.fullSeek)
                {
                    // Delete cached assets
                    Debug.Log("Extra Asset Registration Plugin: Deleting Cache");
                    foreach (string item in System.IO.Directory.EnumerateFiles(Internal.pluginDirectory + "\\cache"))
                    {
                        System.IO.File.Delete(item);
                    }
                }
                // Seek registerable assets
                Debug.Log("Extra Asset Registration Plugin: Getting Registerable Assets");
                newAssets = AssetHandler.GetAssets(ref AssetHandler.AssetsByFileLocation);
            }

            // Register Assets
            ExtraAssetsLibrary.ExtraAssetPlugin.CoreAssetPrefixCallbacks.Add(ExtraAssetsRegistrationPlugin.Guid, (nguid, kind) => AssetHandler.LibrarySelectionMade(nguid, kind));
            foreach (Data.AssetInfo asset in AssetHandler.AssetsByFileLocation.Values)
            {
                Debug.Log("Extra Asset Registration Plugin: Registering [" + asset.location + "] in [" + asset.groupName + "] as [" + asset.id + "]");
                try
                {
                    ExtraAssetsLibrary.DTO.Asset extraAsset = new ExtraAssetsLibrary.DTO.Asset()
                    {
                        Id = new NGuid(asset.id),
                        GroupName = asset.groupName,
                        Name = asset.name,
                        Description = asset.description,
                        CustomKind = ExtraAssetsLibrary.DTO.CustomEntryKind.Creature,
                        Kind = AssetDb.DbEntry.EntryKind.Creature,
                        Icon = FileAccessPlugin.Image.LoadSprite(Internal.pluginDirectory + "cache\\" + asset.id + ".png"),
                        tags = asset.tags.Split(','),
                        BaseCallback = null, // (nguid) => AssetHandler.CreateAsset(asset, nguid),
                        ModelCallback = (nguid) => AssetHandler.CreateAsset(asset, nguid),
                        PostCallback = (nguid, cid) => AssetHandler.LibrarySelectionMiniPlaced(nguid, cid),
                        DefaultScale = asset.size,
                        Scale = Utility.GetV3(asset.mesh.size),
                        TransformOffset = Utility.GetV3(asset.mesh.rotationOffset),
                        Rotation = Quaternion.Euler(Utility.GetV3(asset.mesh.rotationOffset)),
                        headPos = Utility.GetV3(asset.locations.head),
                        hitPos = Utility.GetV3(asset.locations.hit),
                        spellPos = Utility.GetV3(asset.locations.spell),
                        torchPos = Utility.GetV3(asset.locations.torch),
                    };
                    ExtraAssetPlugin.AddAsset(extraAsset);
                }
                catch (Exception)
                {
                    Debug.Log("Extra Asset Registration Plugin: Failed To Register [" + System.IO.Path.GetFileName(asset.location) + "]");
                }
            }

            // Write out new AssetInfo cache file
            if (newAssets)
            {
                Debug.Log("Extra Asset Registration Plugin: Updating Asset Cache File");
                FileAccessPlugin.File.WriteAllText(Internal.pluginDirectory + "cache\\AssetInfo.cache", "  " + JsonConvert.SerializeObject(AssetHandler.AssetsByFileLocation.Values.ToArray<Data.AssetInfo>(), Formatting.Indented));
            }
        }
    }
}
