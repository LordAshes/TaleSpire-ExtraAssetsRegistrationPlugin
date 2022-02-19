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
        public static partial class Data
        {
            public class Locations
            {
                public readonly string root = "0.0,0.0,0.0,0.0,0.0,0.0";
                public string head { get; set; } = "0.0,0.7,0.0,0.0,0.0,0.0";
                public string hit { get; set; } = "0.0,0.5,0.0,0.0,0.0,0.0";
                public string spell { get; set; } = "0.0,0.5,0.0,0.0,0.0,0.0";
                public string torch { get; set; } = "0.0,0.5,0.0,0.0,0.0,0.0";
                public string handRight { get; set; } = "0.3,1.25,0.0,0.0,0.0,0.0";
                public string handLeft { get; set; } = "-0.3,1.25,0.0,0.0,0.0,0.0";
            }

            public class MeshAdjustments
            {
                public string size { get; set; } = "1.0,1.0,1.0";
                public string rotationOffset { get; set; } = "0.0,0.0,0.0";
                public string positionOffset { get; set; } = "0.0,0.0,0.0";
            }

            public class AssetInfo
            {
                public string id { get; set; } = "";
                public string name { get; set; } = "";
                public string kind { get; set; } = "";
                public string category { get; set; } = "";
                public string groupName { get; set; } = "";
                public string description { get; set; } = "";
                public string tags { get; set; } = "";
                public string author { get; set; } = "Anonymous";
                public string version { get; set; } = "Unversioned";
                public string comment { get; set; } = "";
                public string[] variants { get; set; } = null;
                public string chainLoad { get; set; } = null;
                public string anchor { get; set; } = "root";
                public float timeToLive { get; set; } = 0f;
                public float size { get; set; } = 1.0f;
                public string code { get; set; } = "";
                public string location { get; set; } = "";
                public string assetBase { get; set; } = "DEFAULT";
                public MeshAdjustments mesh { get; set; } = new MeshAdjustments();
                public Locations locations { get; set; } = new Locations();

                public Data.AssetInfo Clone()
                {
                    return new AssetInfo()
                    {
                        id = this.id,
                        name = this.name,
                        kind = this.kind,
                        category = this.category,
                        groupName = this.groupName,
                        description = this.description,
                        tags = this.tags,
                        variants = this.variants,
                        chainLoad = this.chainLoad,
                        anchor = this.anchor,
                        author = this.author,
                        version = this.version,
                        comment = this.comment,
                        timeToLive = this.timeToLive,
                        size = this.size,
                        code = this.code,
                        location = this.location,
                        assetBase = this.assetBase,
                        mesh = this.mesh,
                        locations = this.locations
                    };
                }
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
