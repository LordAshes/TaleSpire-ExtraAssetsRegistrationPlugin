using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LordAshes
{
    public partial class ExtraAssetsRegistrationPlugin : BaseUnityPlugin
    {
        public static class SDIM
        {
            public enum InvokeResult
            {
                success = 0,
                missingFile = 1,
                missingMethod = 2,
                invalidParameters = 3
            }

            public static object InvokeReturn = null;

            public static InvokeResult InvokeMethod(string pluginFile, string methodName, object[] parameters)
            {
                InvokeReturn = null;
                Type type = FindPlugin(pluginFile);
                if (type == null) { return InvokeResult.missingFile; }
                MethodInfo method = type.GetMethod(methodName);
                if (method == null) { return InvokeResult.missingMethod; }
                try
                {
                    InvokeReturn = method.Invoke(null, parameters);
                }
                catch (Exception)
                {
                    return InvokeResult.invalidParameters;
                }
                return InvokeResult.success;
            }

            private static Type FindPlugin(string pluginFile)
            {
                Assembly assembly = Assembly.LoadFrom(BepInEx.Paths.PluginPath + "/" + pluginFile);
                if (assembly == null) { return null; }
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(BaseUnityPlugin))) { return type; }
                }
                return null;
            }
        }
    }
}

