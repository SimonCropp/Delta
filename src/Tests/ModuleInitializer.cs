public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyEntityFramework.Enable();
        VerifySqlServer.Enable();
        VerifyDiffPlex.Initialize();
    }
}