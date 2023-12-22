using System.Reflection;
using HarmonyLib;
using Robust.Shared.Utility;
using static Marserializer;

public static class SubverterPatch
{
    public static string Name = "Subverter";
    public static string Description = "Helper patch for loading additional code";

    public delegate void Forward(Assembly asm);

    public static Forward? hideDelegate;

    public static void Hide(Assembly asm)
    {
        hideDelegate?.Invoke(asm);
    }
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
            var loadGameAssemblyMethod = AccessTools.Method(AccessTools.TypeByName("Robust.Shared.ContentPack.BaseModLoader"), "InitMod");
        
            foreach (var path in GetSubverters())
            {
                Assembly subvAsm = Assembly.LoadFrom(path);
                MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"Preloading {path}");
                loadGameAssemblyMethod.Invoke(__instance, new object[] { subvAsm });
                SubverterPatch.Hide(subvAsm);
            }
            
            return true;
        }
        
        private static IEnumerable<string> GetSubverters()
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Marsey");
            MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"Loading from {directoryPath}");

            var patches = Deserialize(new string[] { directoryPath }, "subversion.marsey") ?? new List<string>();

            foreach (var filePath in patches)
            {
                if (Path.GetFileName(filePath) != "Subverter.dll")
                {
                    yield return filePath;
                }
            }
        }
    }
}