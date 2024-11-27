public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        DeltaExtensions.IncludeNo304ReasonInResponse = true;
        VerifierSettings.InitializePlugins();
    }
}