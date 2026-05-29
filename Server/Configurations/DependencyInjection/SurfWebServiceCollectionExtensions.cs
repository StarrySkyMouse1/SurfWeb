using SurfWeb.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SurfWeb.Configurations;

public static class SurfWebServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSurfWebOptions(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<SurfWebOptions>()
            .Bind(builder.Configuration.GetSection(SurfWebOptions.SectionName))
            .PostConfigure(options =>
            {
                var configured = options.Styles.FirstOrDefault(s => s.Default);
                if (configured is not null)
                    options.DefaultStyleId = configured.Id;
            });
        return builder;
    }
}
