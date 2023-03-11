public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyEntityFramework.Initialize();
        VerifySqlServer.Initialize();
        VerifyDiffPlex.Initialize();
        VerifyMicrosoftLogging.Initialize();
        VerifyAspNetCore.Initialize();
    }
}