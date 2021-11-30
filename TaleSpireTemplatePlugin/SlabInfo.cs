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
            public class SlabInfo
            {
                public Vector3 position { get; set; } = Vector3.zero;
                public string code { get; set; } = "";
            }
        }
    }
}
