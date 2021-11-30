using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ExtraAssetsLibrary;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.StatMessaging.Guid)]
    [BepInDependency(ExtraAssetPlugin.Guid)]
    public partial class ExtraAssetsRegistrationPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Extra Assets Registration Plug-In";
        public const string Guid = "org.lordashes.plugins.extraassetsregistration";
        public const string Version = "2.4.1.0";

        private static class Internal
        {
            public enum BaseTypeTriState
            {
                alwaysNone = 0,
                alwaysBase = 1,
                asPerAsset = 2
            }

            public enum DiagnosticSelection
            {
                none = 0,
                low = 1,
                high = 2
            }

            // Settings
            public static string pluginDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";
            public static Data.AutomaticAssetsSeekSetting seekSetting = Data.AutomaticAssetsSeekSetting.newAssetsOnly;
            public static ConfigEntry<KeyboardShortcut> triggerRegistration;
            public static ConfigEntry<KeyboardShortcut> triggerSlabImport;

            public static float auraSolidifcationDelay = 5f;

            public static BaseTypeTriState baseForCreatures = BaseTypeTriState.asPerAsset;
            public static BaseTypeTriState baseForEffects = BaseTypeTriState.asPerAsset;
            public static BaseTypeTriState baseForAudio = BaseTypeTriState.asPerAsset;

            public static DiagnosticSelection showDiagnostics = DiagnosticSelection.none;

            public static float delayAuraApplication = 5f;
            public static KeyboardShortcut triggerManualAuraApplication;
            public static System.Guid subscriptionStatMessaging = System.Guid.Empty;

            public static string guiMessage = "";

            public static bool subscriptionStarted = false;

            public static BaseUnityPlugin self = null;
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

            Internal.self = this;

            Internal.triggerRegistration = Config.Bind("Keyboard Shortcuts", "Manual Seek For Assets", new KeyboardShortcut(KeyCode.A, KeyCode.RightControl));
            Internal.triggerSlabImport = Config.Bind("Keyboard Shortcuts", "Slab or Multi Slab Importer", new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl));
            Internal.triggerManualAuraApplication = Config.Bind("Keyboard Shortcuts", "Manual Aura Application Keyboard Shortcut", new KeyboardShortcut(KeyCode.R, KeyCode.RightControl)).Value;

            AssetHandler.Initialize(this, Config.Bind("Settings", "Organization Settings", Data.AssetGroups.custom).Value);

            Internal.seekSetting = Config.Bind("Settings", "Detect New Assets Settings", Data.AutomaticAssetsSeekSetting.newAssetsOnly).Value;

            Internal.baseForCreatures = Config.Bind("Settings", "Base For Creatures", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.baseForEffects = Config.Bind("Settings", "Base For Effects", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.baseForAudio = Config.Bind("Settings", "Base For Audio", Internal.BaseTypeTriState.asPerAsset).Value;

            Internal.delayAuraApplication = Config.Bind("Startup", "Aura Application Delay In Seconds", 5.0f).Value;

            Internal.showDiagnostics = Config.Bind("Troubleshooting", "Log Addition Diagnostic Data", Internal.DiagnosticSelection.none).Value;

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
                Debug.Log("Extra Assets Registration Plugin: Deleting Cache");
                foreach (string item in System.IO.Directory.EnumerateFiles(Internal.pluginDirectory + "\\cache"))
                {
                    System.IO.File.Delete(item);
                }
                RegisterAssets();
            }

            if (Utility.StrictKeyCheck(Internal.triggerSlabImport.Value))
            {
                Data.AssetInfo asset = new Data.AssetInfo()
                {
                    id = NGuid.Empty.ToString(),
                    kind = "Slab",
                    name = "CustomStab(s)",
                    groupName = "Runtime Import",
                    assetBase = "None",
                    author = "Clipboard",
                    comment = "Clipboard Import",
                    description = "Slab(s) imported from clipboard",
                    location = "Clipboard",
                    version = ExtraAssetPlugin.Version,
                    code = DirtyClipboardHelper.PullFromClipboard()
                };
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Slab(s) Importer Code...\r\n"+asset.code); }
                AssetHandler.CreateAsset(asset, NGuid.Empty);
            }

            if (Utility.isBoardLoaded())
            {
                if (!Internal.subscriptionStarted)
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Board Is Ready. Starting Delayed Aura Application"); }
                    StartCoroutine("DelayedAuraApplication", new object[] { Internal.delayAuraApplication });
                    Internal.subscriptionStarted = true;
                }

                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "1"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "2"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "3"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "4"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "5"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha6, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "6"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha7, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "7"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha8, KeyCode.LeftAlt)))
                {
                    SystemMessage.AskForTextInput("Play Animation", "Animation Name:", "OK", (userAnim) => { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", userAnim); }, null, "Cancel", null);
                }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha9, KeyCode.LeftAlt))) { StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Audio", "On"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha0, KeyCode.LeftAlt)))
                {
                    StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Anim", "");
                    StatMessaging.SetInfo(LocalClient.SelectedCreatureId, ExtraAssetsRegistrationPlugin.Guid + ".Audio", "");
                }
                if (Utility.StrictKeyCheck(Internal.triggerManualAuraApplication))
                {
                    StartCoroutine("DisplayMessage", new object[] { "Aura Application Started", 3.0f });
                    StartCoroutine("DelayedAuraApplication", new object[] { 0.1f });
                }
            }
            else
            {
                if (Internal.subscriptionStatMessaging != System.Guid.Empty)
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Unsubscribing To Aura Stat Messages"); }
                    StatMessaging.Unsubscribe(Internal.subscriptionStatMessaging);
                    Internal.subscriptionStatMessaging = System.Guid.Empty;
                }
                Internal.subscriptionStarted = false;
            }
        }

        private void OnGUI()
        {
            if (Internal.guiMessage != "")
            {
                GUIStyle gs1 = new GUIStyle();
                gs1.alignment = TextAnchor.MiddleCenter;
                gs1.fontSize = 24;
                gs1.normal.textColor = Color.black;
                GUIStyle gs2 = new GUIStyle();
                gs2.alignment = TextAnchor.MiddleCenter;
                gs2.fontSize = 24;
                gs2.normal.textColor = Color.yellow;

                GUI.Label(new Rect(0, 30, 1920, 35), Internal.guiMessage, gs1);
                GUI.Label(new Rect(0, 32, 1920, 37), Internal.guiMessage, gs2);
            }
        }

        private void RegisterAssets()
        {
            // Get new assets
            bool newAssets = false;
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Look For New Assets Set To " + Internal.seekSetting.ToString()); }
            if (Internal.seekSetting != Data.AutomaticAssetsSeekSetting.manual)
            {
                // For full seek, delete cache 
                if (Internal.seekSetting == Data.AutomaticAssetsSeekSetting.fullSeek)
                {
                    // Delete cached assets
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Deleting Cache"); }
                    foreach (string item in System.IO.Directory.EnumerateFiles(Internal.pluginDirectory + "\\cache"))
                    {
                        System.IO.File.Delete(item);
                    }
                }
                // Seek registerable assets
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Getting Registerable Assets"); }
                newAssets = AssetHandler.GetAssets(ref AssetHandler.AssetsByFileLocation);
            }

            // Register Assets
            ExtraAssetsLibrary.ExtraAssetPlugin.CoreAssetPrefixCallbacks.Add(ExtraAssetsRegistrationPlugin.Guid, (nguid, kind) => AssetHandler.LibrarySelectionMade(nguid, kind));
            foreach (Data.AssetInfo asset in AssetHandler.AssetsByFileLocation.Values)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Registering [" + asset.location + "] in [" + asset.groupName + "] as [" + asset.id + "] lives of ["+asset.timeToLive+"]"); }
                try
                {
                    ExtraAssetsLibrary.DTO.Asset extraAsset = new ExtraAssetsLibrary.DTO.Asset()
                    {
                        Id = new NGuid(asset.id),
                        GroupName = asset.groupName,
                        Name = asset.name,
                        Description = asset.description,
                        // Kind = ResolveKind(asset.kind),
                        CustomKind = ResolveCustomKind(asset.kind),
                        Icon = FileAccessPlugin.Image.LoadSprite(Internal.pluginDirectory + "cache\\" + asset.id + ".png"),
                        tags = asset.tags.Split(','),
                        BaseCallback = (nguid) => AssetHandler.CreateAssetBase(asset, nguid),
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
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high)
                    {
                        Debug.Log("Extra Assets Registration Plugin: Id: "+extraAsset.Id+" ("+asset.location+")");
                        Debug.Log("Extra Assets Registration Plugin: Name: " + extraAsset.Name);
                        Debug.Log("Extra Assets Registration Plugin: Description: " + extraAsset.Description);
                        // Debug.Log("Extra Assets Registration Plugin: Kind: " + extraAsset.Kind);
                        Debug.Log("Extra Assets Registration Plugin: CustomKind: " + extraAsset.CustomKind);
                        Debug.Log("Extra Assets Registration Plugin: GroupName: " + extraAsset.GroupName);
                        Debug.Log("Extra Assets Registration Plugin: Tags: " + extraAsset.tags);
                        Debug.Log("Extra Assets Registration Plugin: Icon: " + (extraAsset.Icon!=null));
                        Debug.Log("Extra Assets Registration Plugin: Base Handler: " + (extraAsset.BaseCallback != null));
                        Debug.Log("Extra Assets Registration Plugin: Model Handler: " + (extraAsset.ModelCallback != null));
                        Debug.Log("Extra Assets Registration Plugin: Pre Handler: " + true);
                        Debug.Log("Extra Assets Registration Plugin: Post Handler: " + (extraAsset.PostCallback != null));
                        Debug.Log("Extra Assets Registration Plugin: DefaultScale: " + extraAsset.DefaultScale);
                        Debug.Log("Extra Assets Registration Plugin: Scale: " + extraAsset.Scale.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Offset: " + extraAsset.TransformOffset.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Rotation: " + extraAsset.Rotation.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Head: " + extraAsset.headPos.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Hit: " + extraAsset.hitPos.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Spell: " + extraAsset.spellPos.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Torch: " + extraAsset.torchPos.ToString());
                    }
                }
                catch (Exception)
                {
                    Debug.LogWarning("Extra Assets Registration Plugin: Failed To Register [" + System.IO.Path.GetFileName(asset.location) + "]");
                }
            }

            // Write out new AssetInfo cache file
            if (newAssets)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Updating Asset Cache File"); }
                FileAccessPlugin.File.WriteAllText(Internal.pluginDirectory + "cache\\AssetInfo.cache", "  " + JsonConvert.SerializeObject(AssetHandler.AssetsByFileLocation.Values.ToArray<Data.AssetInfo>(), Formatting.Indented));
            }
        }

        private AssetDb.DbEntry.EntryKind ResolveKind(string kind)
        {
            foreach (AssetDb.DbEntry.EntryKind option in (AssetDb.DbEntry.EntryKind[])Enum.GetValues(typeof(AssetDb.DbEntry.EntryKind)))
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Identifying Kind '" + kind.ToUpper() + "'. Comparing To '" + option.ToString().ToUpper() + "' ("+(int)option+")"); }
                if (kind.ToUpper() == option.ToString().ToUpper()) 
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Setting Kind To '"+option.ToString()+"' ("+(int)option+")"); }
                    return option;
                }
            }
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Setting Kind To Default Type Of Creature ("+(int)AssetDb.DbEntry.EntryKind.Creature + ")"); }
            return AssetDb.DbEntry.EntryKind.Creature;
        }

        private ExtraAssetsLibrary.DTO.CustomEntryKind ResolveCustomKind(string kind)
        {
            foreach (ExtraAssetsLibrary.DTO.CustomEntryKind option in (ExtraAssetsLibrary.DTO.CustomEntryKind[])Enum.GetValues(typeof(ExtraAssetsLibrary.DTO.CustomEntryKind)))
            {
                // if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Identifying CustomKind '" + kind.ToUpper() + "'/'"+ kind.ToUpper() + "S'. Comparing To '" + option.ToString().ToUpper() + "'"); }
                if (kind.ToUpper() == option.ToString().ToUpper()) { return option; }
                if (kind.ToUpper().Substring(0,kind.Length-1) == option.ToString().ToUpper()) { return option; }
            }
            return ExtraAssetsLibrary.DTO.CustomEntryKind.Creature;
        }

        private IEnumerator DelayedAuraApplication(object[] inputs)
        {
            StartCoroutine("DisplayMessage", new object[] { "Preparing Aura Processing System", (float)inputs[0] });
            yield return new WaitForSeconds((float)inputs[0]);
            StartCoroutine("DisplayMessage", new object[] { "Aura Processing System Started", 3.0f });
            if (Internal.subscriptionStatMessaging != System.Guid.Empty)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Unsubscribing To Aura Stat Messages"); }
                StatMessaging.Unsubscribe(Internal.subscriptionStatMessaging);
                Internal.subscriptionStatMessaging = System.Guid.Empty;
            }
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Applying Current Auras"); }
            foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
            {
                List<StatMessaging.Change> changes = new List<StatMessaging.Change>();
                string json = asset.Creature.Name.Substring(asset.Creature.Name.IndexOf(">") + 1);
                Debug.Log("Extra Assets Registration Plugin: " + json);
                Dictionary<string, string> keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                foreach (KeyValuePair<string, string> entry in keyValuePairs)
                {
                    if (entry.Key.StartsWith(ExtraAssetsRegistrationPlugin.Guid + ".Aura.") ||
                        entry.Key.StartsWith(ExtraAssetsRegistrationPlugin.Guid + ".Anim") ||
                        entry.Key.StartsWith(ExtraAssetsRegistrationPlugin.Guid + ".Audio"))
                    {
                        changes.Add(new StatMessaging.Change()
                        {
                            action = StatMessaging.ChangeType.added,
                            cid = asset.Creature.CreatureId,
                            key = entry.Key,
                            previous = "",
                            value = entry.Value
                        });
                    }
                }
                if (changes.Count > 0) { StatMessagingHandler.StatMessagingRequest(changes.ToArray()); }
            }
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Subscribing Stat Messages"); }
            Internal.subscriptionStatMessaging = StatMessaging.Subscribe("*", StatMessagingHandler.StatMessagingRequest);
        }
    }
}