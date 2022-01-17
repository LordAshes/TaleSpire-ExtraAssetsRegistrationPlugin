using BepInEx;
using Bounce.Unmanaged;
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
        public static class StatMessagingHandler
        {
            public static void StatMessagingRequest(StatMessaging.Change[] changes)
            {
                if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: "+changes.Length+" Changes Stat Message"); }
                foreach (StatMessaging.Change change in changes)
                {
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: " + change.action + ":" + change.key + ":" + change.cid + ":" + change.previous + ":" + change.value); }

                    if (change.key.StartsWith(ExtraAssetsRegistrationPlugin.Guid + ".Aura."))
                    {
                        if (change.action != StatMessaging.ChangeType.removed)
                        {
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Creating aura request"); }
                            CreateAura(change.cid, change.key.Substring(change.key.IndexOf(".Aura.")+".Aura.".Length), new NGuid(change.value));
                        }
                        else
                        {
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Remove aura reqeust"); }
                            DestroyAura(change.cid, change.key.Substring(change.key.IndexOf(".Aura.") + ".Aura.".Length));
                        }
                    }
                    else if (change.key.EndsWith(ExtraAssetsRegistrationPlugin.Guid + ".Anim"))
                    {
                        if(change.value!="")
                        {
                            // Play Animation
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Play animation '"+change.value+"'"); }
                            PlayAnimation(change.cid, change.value);
                        }
                        else
                        {
                            // Stop Animation
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Stop animation"); }
                            StopAnimation(change.cid);
                        }
                    }
                    else if (change.key.EndsWith(ExtraAssetsRegistrationPlugin.Guid + ".Audio"))
                    {
                        if (change.value != "")
                        {
                            // Play Audio
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Play audio"); }
                            PlayAudio(change.cid);
                        }
                        else
                        {
                            // Stop Audio
                            if (Internal.showDiagnostics >= Internal.DiagnosticSelection.low) { Debug.Log("Extra Assets Registration Plugin: Stop audio"); }
                            StopAudio(change.cid);
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
                    assetInfo.kind = "AURA";
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Getting prefab"); }
                    GameObject prefab = ExtraAssetsRegistrationPlugin.AssetHandler.CreateAsset(assetInfo);
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Instancing model"); }
                    GameObject model = GameObject.Instantiate(prefab);
                    if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Destroying prefab"); }
                    GameObject.Destroy(prefab);
                    if (asset != null)
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Applying aura to mini"); }
                        model.transform.position = asset.BaseLoader.LoadedAsset.transform.position;
                        model.transform.eulerAngles = new Vector3(0f, asset.BaseLoader.LoadedAsset.transform.eulerAngles.y, 0f);
                        model.transform.SetParent(asset.BaseLoader.LoadedAsset.transform);
                        model.name = "CustomAura:" + cid.ToString() + "." + auraName;
                    }
                    else
                    {
                        if (Internal.showDiagnostics >= Internal.DiagnosticSelection.high) { Debug.Log("Extra Assets Registration Plugin: Applying aura to camera"); }
                        model.transform.position = Camera.main.transform.position;
                        model.transform.eulerAngles = new Vector3(0f, Camera.main.transform.eulerAngles.y, 0f);
                        model.transform.SetParent(Camera.main.transform);
                        model.name = "CustomAura:" + cid.ToString() + "." + auraName;
                    }
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
