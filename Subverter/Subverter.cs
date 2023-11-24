﻿using System.Reflection;
using HarmonyLib;
using Robust.Shared.Utility;

public static class SubverterPatch
{
    public static string Name = "Subverter";
    public static string Description = "Helper patch for loading additional code";
}

public static class MarseyLogger
{
    // Info enums are identical to those in the loader however they cant be easily casted between the two
    public enum LogType
    {
        INFO,
        WARN,
        FATL,
        DEBG
    }

    // Delegate gets casted to Marsey::Utility::Log(AssemblyName, string) at runtime by the loader
    public delegate void Forward(AssemblyName asm, string message);
    
    public static Forward? logDelegate;
    
    public static void Log(LogType type, string message)
    {
        logDelegate?.Invoke(Assembly.GetExecutingAssembly().GetName(), $"[{type.ToString()}] {message}");
    }
}

public static class ModLoaderPatch
{
    [HarmonyPatch]
    public static class TryLoadModulesPatch
    {
        // Use method overloading to target the TryLoadModules method
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("Robust.Shared.ContentPack.ModLoader"), "TryLoadModules");
        }

        [HarmonyPrefix]
        public static bool Prefix(object __instance)
        {
            var loadGameAssemblyMethod = AccessTools.Method(AccessTools.TypeByName("Robust.Shared.ContentPack.ModLoader"), "LoadGameAssembly", new Type[] { typeof(string), typeof(bool) });
        
            foreach (var path in GetSubverters())
            {
                MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"Preloading {path}");
                loadGameAssemblyMethod.Invoke(__instance, new object[] { path.ToString(), false });
            }
            
            return true;
        }
        
        private static IEnumerable<string> GetSubverters()
        {
            string directoryPath = Directory.GetCurrentDirectory() + "Subversion";
            MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"Loading from {Directory.GetCurrentDirectory()}");
            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.dll"))
            {
                if (Path.GetFileName(filePath) != "Subverter.dll")
                {
                    yield return filePath;
                }
            }
        }
    }
}