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
using HarmonyLib;
using ExtraAssetsLibrary.DTO;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.AssetDataPlugin.Guid)]
    [BepInDependency(ExtraAssetPlugin.Guid)]
    public partial class ExtraAssetsRegistrationPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Extra Assets Registration Plug-In";
        public const string Guid = "org.lordashes.plugins.extraassetsregistration";
        public const string Version = "3.5.5.0";

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

            public enum GraphicsCapabilities
            {
                LowPerformance = 1,
                HighPerformance = 2
            }

            public enum BoardState
            {
                BoardNotReady = 0,
                BoardMiniDelay = 1,
                BoardReady = 2
            }

            // Settings
            public static string pluginDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";
            public static string cacheDirectory = pluginDirectory + "CustomData\\cache";
            public static Data.AutomaticAssetsSeekSetting seekSetting = Data.AutomaticAssetsSeekSetting.newAssetsOnly;
            
            public static ConfigEntry<KeyboardShortcut> triggerRegistration;
            public static ConfigEntry<KeyboardShortcut> triggerSlabImport;
            public static ConfigEntry<KeyboardShortcut> triggerManualAuraApplication;

            public static float auraSolidifcationDelay = 5f;

            public static BaseTypeTriState baseForCreatures = BaseTypeTriState.asPerAsset;
            public static BaseTypeTriState baseForEffects = BaseTypeTriState.asPerAsset;
            public static BaseTypeTriState baseForAudio = BaseTypeTriState.asPerAsset;

            public static DiagnosticSelection showDiagnostics = DiagnosticSelection.none;

            public static float delayChainLoaderSupression = 3f;
            public static float delayAuraApplication = 5f;
            public static float delayProcessingLinks = 1f;

            public static string defaultEncounterPointer = null;

            public static float delayPerSlab = 0.1f;

            public static string guiMessage = "";
            public static Data.AssetInfo menuOpenSubselection = null;
            public static Vector2 menuOpenSubselectionPos = Vector2.zero;
            public static Vector3 menuSubselectionSpawnPos = Vector3.zero;
            public static Vector3 menuSubselectionSpawnRot = Vector3.zero;

            public static bool subscriptionStarted = false;

            public static BaseUnityPlugin self = null;

            public static BoardState boardState = BoardState.BoardNotReady;

            public static string fractionalCharacter = ".";

            public static GraphicsCapabilities graphics = GraphicsCapabilities.LowPerformance;

            public static List<string> hiddenGroups = new List<string>();

            public static Vector3 defaultMiniOrientation = new Vector3(0, 180, 0);
            public static Vector3 defaultAuraOrientation = new Vector3(0, 180, 0);
        }

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Extra Assets Registration Plugin: Is Active.");

            if (!System.IO.Directory.Exists(Internal.cacheDirectory)) { System.IO.Directory.CreateDirectory(Internal.cacheDirectory); }

            Internal.self = this;

            Internal.graphics = Config.Bind("Settings", "Device Graphics Capabilities", Internal.GraphicsCapabilities.HighPerformance).Value;

            Internal.triggerRegistration = Config.Bind("Keyboard Shortcuts", "Manual Seek For Assets", new KeyboardShortcut(KeyCode.A, KeyCode.RightControl));
            Internal.triggerSlabImport = Config.Bind("Keyboard Shortcuts", "Slab or Multi Slab Importer", new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl));
            Internal.triggerManualAuraApplication = Config.Bind("Keyboard Shortcuts", "Manual Aura Application Keyboard Shortcut", new KeyboardShortcut(KeyCode.R, KeyCode.RightControl));

            AssetHandler.Initialize(this, Config.Bind("Settings", "Organization Settings", Data.AssetGroups.custom).Value);

            Internal.seekSetting = Config.Bind("Settings", "Detect New Assets Settings", Data.AutomaticAssetsSeekSetting.newAssetsOnly).Value;

            Internal.baseForCreatures = Config.Bind("Settings", "Base For Creatures", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.baseForEffects = Config.Bind("Settings", "Base For Effects", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.baseForAudio = Config.Bind("Settings", "Base For Audio", Internal.BaseTypeTriState.asPerAsset).Value;
            Internal.delayPerSlab = Config.Bind("Settings", "Delay Between Slab In Multi Slab Asset", 0.1f).Value;
            Internal.delayChainLoaderSupression = Config.Bind("Settings", "Chain Loader Suppression Delay", 3.0f).Value;
            Internal.defaultEncounterPointer = Config.Bind("Settings", "Default Encounter Pointer", "Minis/laDefaultEncounterPointer/laDefaultEncounterPointer.laDefaultEncounterPointer").Value;
            Internal.fractionalCharacter = Config.Bind("Settings", "Fractional Character", ".").Value;
            Internal.delayAuraApplication = Config.Bind("Startup", "Aura Application Delay In Seconds", 5.0f).Value;
            Internal.delayProcessingLinks = Config.Bind("Startup", "Link Processing Delay In Seconds", 1.0f).Value;

            Internal.showDiagnostics = Config.Bind("Troubleshooting", "Log Addition Diagnostic Data", Internal.DiagnosticSelection.none).Value;

            var harmony = new Harmony(Guid);
            harmony.PatchAll();

            AssetDataPlugin.Subscribe(ExtraAssetsRegistrationPlugin.Guid + "*", MessagingHandler.MessagingRequest);

            StartCoroutine("RegisterAssets");

            Utility.PostOnMainPage(this.GetType());
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
                foreach (string item in System.IO.Directory.EnumerateFiles(Internal.cacheDirectory))
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
                AssetHandler.CreateAsset(asset);
            }

            if (Utility.isBoardLoaded())
            {
                if(Internal.boardState==Internal.BoardState.BoardNotReady)
                {
                    Internal.boardState = Internal.BoardState.BoardMiniDelay;
                    StartCoroutine("DelayedAuraApplication", new object[] { Internal.delayAuraApplication });
                }

                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "1"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "2"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "3"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "4"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "5"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha6, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "6"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha7, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "7"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha8, KeyCode.LeftAlt)))
                {
                    SystemMessage.AskForTextInput("Play Animation", "Animation Name:", "OK", (userAnim) => { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", userAnim); }, null, "Cancel", null);
                }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha9, KeyCode.LeftAlt))) { AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Audio", "On"); }
                if (Utility.StrictKeyCheck(new KeyboardShortcut(KeyCode.Alpha0, KeyCode.LeftAlt)))
                {
                    AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Anim", "");
                    AssetDataPlugin.SetInfo(LocalClient.SelectedCreatureId.ToString(), ExtraAssetsRegistrationPlugin.Guid + ".Audio", "");
                }
                if (Utility.StrictKeyCheck(Internal.triggerManualAuraApplication.Value))
                {
                    StartCoroutine("DisplayMessage", new object[] { "Aura Application Started", 3.0f });
                    StartCoroutine("DelayedAuraApplication", new object[] { 0.1f });
                }
            }
            else
            {
                if (Internal.boardState != Internal.BoardState.BoardNotReady)
                {
                    Debug.Log("Extra Assets Registration Plugin: Board Unloaded");
                    Internal.boardState = Internal.BoardState.BoardNotReady;
                }
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

            GUIStyle gsss = new GUIStyle();
            gsss.alignment = TextAnchor.MiddleCenter;
            gsss.fontSize = 12;
            gsss.normal.textColor = Color.white;
            if (Internal.menuOpenSubselection != null)
            {
                for (int i = 0; i<Internal.menuOpenSubselection.variants.Length; i++)
                {
                    GUI.DrawTexture(new Rect(Internal.menuOpenSubselectionPos.x, Internal.menuOpenSubselectionPos.y + i * 20, 200, 18), Texture2D.grayTexture, ScaleMode.StretchToFill);
                    if(GUI.Button(new Rect(Internal.menuOpenSubselectionPos.x, Internal.menuOpenSubselectionPos.y + i * 20, 200, 18), Internal.menuOpenSubselection.variants[i], gsss))
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Sub-Selection Spawning "+ Internal.menuOpenSubselection.name + " ("+Internal.menuOpenSubselection.variants[i]+")"); }
                        NGuid nguid = AssetHandler.FindAssetId(Internal.menuOpenSubselection.name + " ("+Internal.menuOpenSubselection.variants[i]+")");
                        AssetHandler.SpawnCreature(nguid, Internal.menuSubselectionSpawnPos, Internal.menuSubselectionSpawnRot, false);
                        StartCoroutine("SupressChainLoad");
                        CreatureBoardAsset asset;
                        CreaturePresenter.TryGetAsset(new CreatureGuid(Internal.menuOpenSubselection.comment), out asset);
                        if (asset != null) { asset.RequestDelete(); }
                        Internal.menuOpenSubselection = null;
                        break;
                    }
                }
            }
        }

        private IEnumerator RegisterAssets()
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
                    foreach (string item in System.IO.Directory.EnumerateFiles(Internal.cacheDirectory))
                    {
                        System.IO.File.Delete(item);
                    }
                    yield return new WaitForSeconds(0.1f);
                }
                // Seek registerable assets
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Getting Registerable Assets"); }
                newAssets = AssetHandler.GetAssets(ref AssetHandler.AssetsByFileLocation);
                yield return new WaitForSeconds(0.1f);
            }

            // Register Assets
            ExtraAssetPlugin.CoreAssetPrefixCallbacks.Add(ExtraAssetsRegistrationPlugin.Guid, (nguid, kind) => AssetHandler.LibrarySelectionMade(nguid, kind));
            if (Config.Bind("Setting", "Hide Groups", true).Value == true)
            {
                ExtraAssetPlugin.HiddenGroups = AssetHandler.GetHiddenGroups(); 
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high)
                { 
                    Debug.Log("Extra Assets Registration Plugin: Hiding The Following Groups = " + String.Join(",", Internal.hiddenGroups)); 
                }
            }
            foreach (Data.AssetInfo asset in AssetHandler.AssetsByFileLocation.Values)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Registering [" + asset.location + "] in [" + asset.groupName + "] as [" + asset.id + "] lives of [" + asset.timeToLive + "]"); }
                try
                {
                    CustomEntryKind customKind = CustomEntryKind.Creature;
                    Category category = Category.Creature;

                    if ((asset.kind != null && asset.kind != "") && (asset.category == null || asset.category == ""))
                    {
                        customKind = ResolveCustomKind(asset.kind);
                        switch (customKind)
                        {
                            case CustomEntryKind.Creature:
                                category = Category.Creature;
                                break;
                            case CustomEntryKind.Tile:
                                category = Category.Tile;
                                break;
                            case CustomEntryKind.Projectile:
                                category = Category.AuraAndEffects;
                                break;
                            case CustomEntryKind.Aura:
                                category = Category.AuraAndEffects;
                                break;
                            case CustomEntryKind.Effects:
                                category = Category.AuraAndEffects;
                                break;
                            case CustomEntryKind.Prop:
                                category = Category.Prop;
                                break;
                            case CustomEntryKind.Audio:
                                category = Category.Audio;
                                break;
                            case CustomEntryKind.Slab:
                                category = Category.Slab;
                                break;
                            default:
                                category = Category.Creature;
                                break;
                        }
                    }
                    else if ((asset.kind == null || asset.kind == "") && (asset.category != null && asset.category != ""))
                    {
                        category = ResolveCategory(asset.category);
                        switch (category)
                        {
                            case Category.Creature:
                                customKind = CustomEntryKind.Creature;
                                break;
                            case Category.Tile:
                                customKind = CustomEntryKind.Tile;
                                break;
                            case Category.AuraAndEffects:
                                customKind = CustomEntryKind.Aura;
                                break;
                            case Category.Prop:
                                customKind = CustomEntryKind.Prop;
                                break;
                            case Category.Audio:
                                customKind = CustomEntryKind.Audio;
                                break;
                            case Category.Slab:
                                customKind = CustomEntryKind.Slab;
                                break;
                            default:
                                customKind = CustomEntryKind.Creature;
                                break;
                        }
                    }

                    ExtraAssetsLibrary.DTO.Asset extraAsset = new ExtraAssetsLibrary.DTO.Asset();
                    try { extraAsset.Id = new NGuid(asset.id); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set Id"); }
                    try { extraAsset.GroupName = asset.groupName; } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set groupName tro " + asset.groupName); }
                    try { extraAsset.Name = asset.name; } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set name to " + asset.name); }
                    try { extraAsset.Description = asset.description; } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set description to " + asset.description); }
                    try { extraAsset.Category = category; } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set category to " + asset.category); }
                    try { extraAsset.CustomKind = customKind; } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set kind to " + asset.kind); }
                    try { extraAsset.Icon = FileAccessPlugin.Image.LoadSprite(Internal.cacheDirectory + "\\" + asset.id + ".png"); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set icon"); }
                    try { extraAsset.tags = asset.tags.Split(','); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set tags to " + asset.tags); }
                    try { extraAsset.BaseCallback = (nguid) => AssetHandler.CreateAssetBase(asset); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set base callback"); }
                    try { extraAsset.ModelCallback = (nguid) => AssetHandler.CreateAsset(asset); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set model callback"); }
                    try { extraAsset.PostCallback = (nguid, cid) => AssetHandler.LibrarySelectionMiniPlaced(nguid, cid); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set post callback"); }
                    try { extraAsset.DefaultScale = asset.size; } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set default scale to " + asset.size.ToString()); }
                    try { extraAsset.Scale = Utility.GetV3(AssetHandler.RectifyToLocalFormat(asset.mesh.size)); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set scale to " + asset.mesh.size.ToString()); }
                    try { extraAsset.TransformOffset = Utility.GetV3(AssetHandler.RectifyToLocalFormat(asset.mesh.rotationOffset)); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set position to " + asset.mesh.rotationOffset.ToString()); }
                    try { extraAsset.Rotation = Quaternion.Euler(Utility.GetV3(asset.mesh.rotationOffset)); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set rotation to " + asset.mesh.rotationOffset.ToString()); }
                    try { extraAsset.headPos = Utility.GetV3(AssetHandler.RectifyToLocalFormat(asset.locations.head)); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set head location to " + asset.locations.head.ToString()); }
                    try { extraAsset.hitPos = Utility.GetV3(AssetHandler.RectifyToLocalFormat(asset.locations.hit)); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to set hit location to " + asset.locations.hit.ToString()); }
                    try { extraAsset.spellPos = Utility.GetV3(AssetHandler.RectifyToLocalFormat(asset.locations.spell)); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to spell location to " + asset.locations.spell.ToString()); }
                    try { extraAsset.torchPos = Utility.GetV3(AssetHandler.RectifyToLocalFormat(asset.locations.torch)); } catch (Exception) { Debug.Log("Extra Assets Registration Plugin: Unable to torch location to " + asset.locations.torch.ToString()); }
                    ExtraAssetPlugin.AddAsset(extraAsset);
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high)
                    {
                        Debug.Log("Extra Assets Registration Plugin: Id: " + extraAsset.Id + " (" + asset.location + ")");
                        Debug.Log("Extra Assets Registration Plugin: Name: " + extraAsset.Name);
                        Debug.Log("Extra Assets Registration Plugin: Description: " + extraAsset.Description);
                        Debug.Log("Extra Assets Registration Plugin: Category: " + extraAsset.Category);
                        Debug.Log("Extra Assets Registration Plugin: CustomKind: " + extraAsset.CustomKind);
                        Debug.Log("Extra Assets Registration Plugin: GroupName: " + extraAsset.GroupName);
                        Debug.Log("Extra Assets Registration Plugin: Tags: " + String.Join(",",extraAsset.tags));
                        Debug.Log("Extra Assets Registration Plugin: Icon: " + (extraAsset.Icon != null)+" ("+ Internal.cacheDirectory + "\\" + asset.id + ".png)");
                        Debug.Log("Extra Assets Registration Plugin: Base Handler: " + (extraAsset.BaseCallback != null));
                        Debug.Log("Extra Assets Registration Plugin: Model Handler: " + (extraAsset.ModelCallback != null));
                        Debug.Log("Extra Assets Registration Plugin: Pre Handler: " + true);
                        Debug.Log("Extra Assets Registration Plugin: Post Handler: " + (extraAsset.PostCallback != null));
                        Debug.Log("Extra Assets Registration Plugin: DefaultScale: " + extraAsset.DefaultScale);
                        Debug.Log("Extra Assets Registration Plugin: Scale: " + extraAsset.Scale.ToString());
                        Debug.Log("Extra Assets Registration Plugin: TransformOffset: " + extraAsset.TransformOffset.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Rotation: " + extraAsset.Rotation.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Head: " + extraAsset.headPos.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Hit: " + extraAsset.hitPos.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Spell: " + extraAsset.spellPos.ToString());
                        Debug.Log("Extra Assets Registration Plugin: Torch: " + extraAsset.torchPos.ToString());
                    }
                }
                catch (Exception x)
                {
                    Debug.LogWarning("Extra Assets Registration Plugin: Failed To Register [" + System.IO.Path.GetFileName(asset.location) + "]");
                    Debug.LogException(x);
                }
                yield return new WaitForSeconds(0.1f);
            }

            // Write out new AssetInfo cache file
            if (newAssets)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Updating Asset Cache File"); }
                string json = JsonConvert.SerializeObject(AssetHandler.AssetsByFileLocation.Values.ToArray<Data.AssetInfo>(), Formatting.Indented);
                AssetHandler.RectifyToStorageFormat(json);
                FileAccessPlugin.File.WriteAllText(Internal.cacheDirectory + "\\AssetInfo.cache", json);
            }
        }

        private Category ResolveCategory(string category)
        {
            foreach (Category option in (Category[])Enum.GetValues(typeof(Category)))
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Identifying Category '" + category.ToUpper() + "'. Comparing To '" + option.ToString().ToUpper() + "'"); }
                if (category.ToUpper() == option.ToString().ToUpper()) { return option; }
            }
            return Category.Creature;
        }

        private CustomEntryKind ResolveCustomKind(string kind)
        {
            foreach (CustomEntryKind option in (CustomEntryKind[])Enum.GetValues(typeof(CustomEntryKind)))
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Identifying CustomKind '" + kind.ToUpper() + "'/'"+ kind.ToUpper() + "S'. Comparing To '" + option.ToString().ToUpper() + "'"); }
                if (kind.ToUpper() == "FILTER") { return CustomEntryKind.Creature; }
                if (kind.ToUpper() == option.ToString().ToUpper()) { return option; }
                if (kind.ToUpper()+"S" == option.ToString().ToUpper()) { return option; }
            }
            return CustomEntryKind.Creature;
        }

        private IEnumerator DelayedAuraApplication(object[] inputs)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Board Loaded. Message Processing Delay Started."); }
            yield return new WaitForSeconds((float)inputs[0]);
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Message Processing Started."); }
            // Stop using backlog 
            Internal.boardState = Internal.BoardState.BoardReady;
            // Process backlog
            MessagingHandler.MessagingRequest(null);
        }
    }
}