using BepInEx;
using Bounce.Unmanaged;
using HarmonyLib;
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
        public static class Patches
        {
            [HarmonyPatch(typeof(PlaceableManager), "ProcessAssetForPlaceable")]
            internal class PlaceableManagerProcessAssetForPlaceablePatch
            {
                public static bool Prefix(NGuid assetPackId, string fullyQualifiedAssetId, ref GameObject asset)
                {
                    Debug.Log("Extra Assets Registration Plugin: Patch: Nguid=" + assetPackId.ToString() + ", string=" + fullyQualifiedAssetId + ", " + asset.ToString());
                    return true;
                }
            }
        }
    }
}