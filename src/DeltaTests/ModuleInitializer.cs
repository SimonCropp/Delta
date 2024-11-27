public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        #region UseResponseDiagnostics

        DeltaExtensions.UseResponseDiagnostics = true;

        #endregion
        VerifierSettings.InitializePlugins();
    }
}