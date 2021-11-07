using BepInEx;
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
        public static class Data
        {
            public class Locations
            {
                public string head { get; set; } = "0.0,0.7,0.0";
                public string hit { get; set; } = "0.0,0.5,0.0";
                public string spell { get; set; } = "0.0,0.5,0.0";
                public string torch { get; set; } = "0.0,0.5,0.0";
            }

            public class MeshAdjustments
            {
                public string size { get; set; } = "1.0,1.0,1.0";
                public string rotationOffset { get; set; } = "0.0,0.0,0.0";
                public string positionOffset { get; set; } = "0.0,0.0,0.0";
            }

            public class AssetInfo
            {
                public string kind { get; set; } = "";
                public string groupName { get; set; } = "";
                public string description { get; set; } = "";
                public string name { get; set; } = "";
                public string tags { get; set; } = "";
                public string id { get; set; } = "";
                public string author { get; set; } = "Anonymous";
                public string version { get; set; } = "Unversioned";
                public string comment { get; set; } = "";
                public float size { get; set; } = 1.0f;
                public string location { get; set; } = "";
                public string assetBase { get; set; } = "DEFAULT";
                public MeshAdjustments mesh { get; set; } = new MeshAdjustments();
                public Locations locations { get; set; } = new Locations();
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
}
