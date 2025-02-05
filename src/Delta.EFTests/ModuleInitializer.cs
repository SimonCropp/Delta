[assembly: NonParallelizable]

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        DeltaExtensions.UseResponseDiagnostics = true;
        VerifierSettings.InitializePlugins();
    }
}