using BepInEx;
using Bounce.Unmanaged;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LordAshes
{
    public partial class ExtraAssetsRegistrationPlugin : BaseUnityPlugin
    {
        public static class MessagingHandler
        {
            static List<AssetDataPlugin.DatumChange> backlog = new List<AssetDataPlugin.DatumChange>();

            public class SpawnRequest
            {
                public string kind { get; set; } = "Creature";
                public NGuid assetNguid { get; set; } = NGuid.Empty;
                public string name { get; set; } = "";
                public Vector3 pos { get; set; } = Vector3.zero;
                public Vector3 rot { get; set; } = Vector3.zero;
            }

            public static void MessagingRequest(AssetDataPlugin.DatumChange change)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { if (change != null) { Debug.Log("Extra Assets Registration Plugin: Action:" + change.action + "|Key:" + change.key + "|Source:" + change.source + "|Previous:" + change.previous + "|Value:" + change.value); } }

                // Add messages to backlog if board is not ready
                if (Internal.boardState != Internal.BoardState.BoardReady)
                {
                    // Add only non-null requests
                    if (change != null) { backlog.Add(change); }
                }
                // Null change when board is ready means process backlog
                else if(change==null)
                {
                    // Process each message in the backlog
                    foreach(AssetDataPlugin.DatumChange datumChange in backlog)
                    {
                        MessagingRequest(datumChange);
                    }
                    backlog.Clear();
                }
                // Porcess individual messages
                else
                {
                    if (change.key.StartsWith(ExtraAssetsRegistrationPlugin.Guid + ".Aura."))
                    {
                        if (change.action != AssetDataPlugin.ChangeAction.remove)
                        {
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Creating aura request"); }
                            CreateAura(new CreatureGuid(change.source), change.key.Substring(change.key.IndexOf(".Aura.") + ".Aura.".Length), new NGuid(change.value.ToString()));
                        }
                        else
                        {
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Remove aura reqeust"); }
                            DestroyAura(new CreatureGuid(change.source), change.key.Substring(change.key.IndexOf(".Aura.") + ".Aura.".Length));
                        }
                    }
                    else if (change.key.EndsWith(ExtraAssetsRegistrationPlugin.Guid + ".Anim"))
                    {
                        if (change.value.ToString() != "")
                        {
                            // Play Animation
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Play animation '" + change.value + "'"); }
                            PlayAnimation(new CreatureGuid(change.source), change.value.ToString());
                        }
                        else
                        {
                            // Stop Animation
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Stop animation"); }
                            StopAnimation(new CreatureGuid(change.source));
                        }
                    }
                    else if (change.key.EndsWith(ExtraAssetsRegistrationPlugin.Guid + ".Audio"))
                    {
                        if (change.value.ToString() != "")
                        {
                            // Play Audio
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Play audio"); }
                            PlayAudio(new CreatureGuid(change.source));
                        }
                        else
                        {
                            // Stop Audio
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Stop audio"); }
                            StopAudio(new CreatureGuid(change.source));
                        }
                    }
                    else if (change.key.EndsWith(ExtraAssetsRegistrationPlugin.Guid + ".Spawn") || change.key.EndsWith(ExtraAssetsRegistrationPlugin.Guid + ".Transform"))
                    {
                        try
                        {
                            SpawnRequest spawn = JsonConvert.DeserializeObject<SpawnRequest>(change.value.ToString());
                            Data.AssetInfo spawnInfo = null;
                            if (spawn.name != "")
                            {
                                spawn.assetNguid = AssetHandler.FindAssetId(spawn.name);
                            }
                            if (spawn.assetNguid != NGuid.Empty)
                            {
                                spawnInfo = AssetHandler.FindAssetInfo(spawn.assetNguid);
                                CreatureGuid cid = AssetHandler.SpawnCreature(spawnInfo, spawn.pos, spawn.rot, false);
                                if (change.key.EndsWith(ExtraAssetsRegistrationPlugin.Guid + ".Transform"))
                                {
                                    AssetHandler.LibrarySelectionMade(spawn.assetNguid, AssetDb.DbEntry.EntryKind.Creature, "TRANSFORMATION");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Extra Asssets Registration Plugin: Empty Spawn Request. Specify name or NGuid in request.");
                            }
                        }
                        catch (Exception)
                        {
                            Debug.LogWarning("Extra Asssets Registration Plugin: Unable to process remote spawn request (" + change.value + ").");
                        }
                    }
                }
            }
        }

        private static void CreateAura(CreatureGuid cid, string auraName, NGuid nguid)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Creating aura"); }
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Seeking NGuid match"); }
            foreach (Data.AssetInfo assetInfo in ExtraAssetsRegistrationPlugin.AssetHandler.AssetsByFileLocation.Values)
            {
                if (assetInfo.id == nguid.ToString())
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Found NGuid match"); }
                    if (GameObject.Find("CustomAura:" + cid.ToString() + "." + auraName))
                    {
                        DestroyAura(cid, auraName);
                    }
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Creating '" + auraName + "' aura on " + cid.ToString()); }
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Getting prefab"); }
                    GameObject prefab = ExtraAssetsRegistrationPlugin.AssetHandler.CreateAsset(assetInfo);
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Instancing model"); }
                    GameObject model = GameObject.Instantiate(prefab);
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Destroying prefab"); }
                    GameObject.Destroy(prefab);
                    if (asset != null)
                    {
                        Data.AssetInfo parentAssetInfo = AssetHandler.FindAssetInfo(asset.BoardAssetId);
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Applying aura to mini " + JsonConvert.SerializeObject(parentAssetInfo)); }
                        Vector3 pos = new Vector3(float.Parse(assetInfo.mesh.positionOffset.Split(',')[0]), float.Parse(assetInfo.mesh.positionOffset.Split(',')[1]), float.Parse(assetInfo.mesh.positionOffset.Split(',')[2]));
                        Vector3 rot = new Vector3(float.Parse(assetInfo.mesh.rotationOffset.Split(',')[0]), float.Parse(assetInfo.mesh.rotationOffset.Split(',')[1]), float.Parse(assetInfo.mesh.rotationOffset.Split(',')[2]));
                        Vector3 anchorPos = Vector3.zero;
                        Vector3 anchorRot = Vector3.zero;
                        Debug.Log("Extra Assets Registration Plugin: Anchor Type: " + assetInfo.anchor.ToUpper());
                        switch (assetInfo.anchor.ToUpper())
                        {
                            case "HEAD":
                                Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.head.Split(',')[0]), float.Parse(parentAssetInfo.locations.head.Split(',')[1]), float.Parse(parentAssetInfo.locations.head.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.head.Split(',')[3]), float.Parse(parentAssetInfo.locations.head.Split(',')[4]), float.Parse(parentAssetInfo.locations.head.Split(',')[5]));
                                break;
                            case "HIT":
                                Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.hit.Split(',')[0]), float.Parse(parentAssetInfo.locations.hit.Split(',')[1]), float.Parse(parentAssetInfo.locations.hit.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.hit.Split(',')[3]), float.Parse(parentAssetInfo.locations.hit.Split(',')[4]), float.Parse(parentAssetInfo.locations.hit.Split(',')[5]));
                                break;
                            case "SPELL":
                                Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.spell.Split(',')[0]), float.Parse(parentAssetInfo.locations.spell.Split(',')[1]), float.Parse(parentAssetInfo.locations.spell.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.spell.Split(',')[3]), float.Parse(parentAssetInfo.locations.spell.Split(',')[4]), float.Parse(parentAssetInfo.locations.spell.Split(',')[5]));
                                break;
                            case "TORCH":
                                Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.torch.Split(',')[0]), float.Parse(parentAssetInfo.locations.torch.Split(',')[1]), float.Parse(parentAssetInfo.locations.torch.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.torch.Split(',')[3]), float.Parse(parentAssetInfo.locations.torch.Split(',')[4]), float.Parse(parentAssetInfo.locations.torch.Split(',')[5]));
                                break;
                            case "HANDRIGHT":
                                Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.handRight.Split(',')[0]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[1]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.handRight.Split(',')[3]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[4]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[5]));
                                break;
                            case "HANDLEFT":
                                Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.handLeft.Split(',')[0]), float.Parse(parentAssetInfo.locations.handLeft.Split(',')[1]), float.Parse(parentAssetInfo.locations.handLeft.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.handLeft.Split(',')[3]), float.Parse(parentAssetInfo.locations.handLeft.Split(',')[4]), float.Parse(parentAssetInfo.locations.handLeft.Split(',')[5]));
                                break;
                            /*
                            case "2HANDS":
                                Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.handRight.Split(',')[0]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[1]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.handLeft.Split(',')[0]), float.Parse(parentAssetInfo.locations.handLeft.Split(',')[1]), float.Parse(parentAssetInfo.locations.handLeft.Split(',')[2])) - new Vector3(float.Parse(parentAssetInfo.locations.handRight.Split(',')[0]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[1]), float.Parse(parentAssetInfo.locations.handRight.Split(',')[2]));
                                break;
                            */
                            default:
                                if (assetInfo.anchor.ToUpper() == "ROOT")
                                {
                                    Debug.Log("Extra Assets Registration Plugin: Anchor Recognized: " + assetInfo.anchor.ToUpper());
                                }
                                else
                                {
                                    Debug.Log("Extra Assets Registration Plugin: Anchor " + assetInfo.anchor.ToUpper()+" Not Recognized. Using default ROOT anchor.");
                                }
                                anchorPos = new Vector3(float.Parse(parentAssetInfo.locations.root.Split(',')[0]), float.Parse(parentAssetInfo.locations.root.Split(',')[1]), float.Parse(parentAssetInfo.locations.root.Split(',')[2]));
                                anchorRot = new Vector3(float.Parse(parentAssetInfo.locations.root.Split(',')[3]), float.Parse(parentAssetInfo.locations.root.Split(',')[4]), float.Parse(parentAssetInfo.locations.root.Split(',')[5]));
                                break;
                        }
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high)
                        {
                            Debug.Log("Extra Assets Registration Plugin: Mini Position: " + asset.BaseLoader.LoadedAsset.transform.position.ToString());
                            Debug.Log("Extra Assets Registration Plugin: Anchor Offset: " + anchorPos.ToString());
                            Debug.Log("Extra Assets Registration Plugin: Mesh Offset:   " + pos.ToString());
                            Debug.Log("Extra Assets Registration Plugin: Mini Rotation:   " + asset.CreatureRoot.transform.eulerAngles.ToString());
                            Debug.Log("Extra Assets Registration Plugin: Anchor Rotation: " + anchorRot.ToString());
                            Debug.Log("Extra Assets Registration Plugin: Mesh Rotation:   " + rot.ToString());
                        }
                        model.transform.position = asset.BaseLoader.LoadedAsset.transform.position + anchorPos + pos;
                        Vector3 worldRot = asset.BaseLoader.LoadedAsset.transform.eulerAngles;
                        asset.BaseLoader.LoadedAsset.transform.eulerAngles = new Vector3(asset.BaseLoader.LoadedAsset.transform.eulerAngles.x, 270, asset.BaseLoader.LoadedAsset.transform.eulerAngles.z);
                        model.transform.localEulerAngles = anchorRot;
                        model.transform.SetParent(asset.BaseLoader.LoadedAsset.transform);
                        asset.BaseLoader.LoadedAsset.transform.eulerAngles = worldRot;
                    }
                    else
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Applying aura to camera"); }
                        Vector3 pos = new Vector3(float.Parse(assetInfo.mesh.positionOffset.Split(',')[0]), float.Parse(assetInfo.mesh.positionOffset.Split(',')[1]), float.Parse(assetInfo.mesh.positionOffset.Split(',')[2]));
                        Vector3 rot = new Vector3(float.Parse(assetInfo.mesh.rotationOffset.Split(',')[0]), float.Parse(assetInfo.mesh.rotationOffset.Split(',')[1]), float.Parse(assetInfo.mesh.rotationOffset.Split(',')[2]));
                        model.transform.position = (Camera.main.transform.position + pos);
                        model.transform.eulerAngles = (Camera.main.transform.eulerAngles + rot);
                        model.transform.SetParent(Camera.main.transform);
                    }
                    model.name = "CustomAura:" + cid.ToString() + "." + auraName;
                    break;
                }
            }
        }

        private static void DestroyAura(CreatureGuid cid, string auraName)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);
            if (asset != null)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Destroying previous '" + auraName + "' aura"); }
                GameObject aura = GameObject.Find("CustomAura:" + cid + "." + auraName);
                if (aura != null) { GameObject.Destroy(aura); }
            }
            else
            {
                Debug.Log("Extra Assets Registration Plugin: No valid creature selected from which to remove aura '" + auraName + "'");
            }
        }

        public static void PlayAudio(CreatureGuid cid)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Play Audio"); }
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);
            if (asset != null)
            {
                AudioSource source = asset.GetComponentInChildren<AudioSource>();
                if (source != null)
                {
                    source.Play();
                }
            }
        }

        public static void StopAudio(CreatureGuid cid)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Stop Audio"); }
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);
            if (asset != null)
            {
                AudioSource source = asset.GetComponentInChildren<AudioSource>();
                if (source != null)
                {
                    source.Stop();
                }
            }
        }

        public static void PlayAnimation(CreatureGuid cid, string animName)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Play Animation " + animName); }
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);
            if (asset != null)
            {
                Animation animation = asset.GetComponentInChildren<Animation>();
                if (animation != null)
                {
                    int animIndex = -1;
                    if (int.TryParse(animName, out animIndex))
                    {
                        List<AnimationState> anims = (new List<AnimationState>(animation.Cast<AnimationState>()));
                        animIndex--;
                        if (animIndex >= 0 && animIndex < anims.Count)
                        {
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high)
                            {
                                for (int a = 0; a < anims.Count; a++)
                                {
                                    Debug.Log("Extra Assets Registration Plugin: Animation " + (a + 1) + " is " + anims[a].name);
                                }
                            }
                            animName = anims[animIndex].name;
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Playing Animation " + animName); }
                        }
                        else
                        {
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Animation " + (animIndex + 1) + " Is Not Defined"); }
                            return;
                        }
                    }
                    animation.Play(animName);
                }
                else
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: The Selected Asset Has Not Animations"); }
                }
            }
        }

        public static void StopAnimation(CreatureGuid cid)
        {
            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Stop Animation "); }
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(cid, out asset);
            if (asset != null)
            {
                Animation animation = asset.GetComponentInChildren<Animation>();
                if (animation != null)
                {
                    animation.Stop();
                }
            }
        }
    }
}
