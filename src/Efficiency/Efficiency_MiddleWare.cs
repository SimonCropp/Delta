namespace Efficiency;

public static partial class Efficiency
{
    public static IApplicationBuilder UseEfficiency(this IApplicationBuilder builder) =>
        builder.UseMiddleware<Middleware>();
}