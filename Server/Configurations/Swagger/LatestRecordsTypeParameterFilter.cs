using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SurfWeb.Configurations.Swagger;

/// <summary>Swagger：<c>GET /api/v1/api/records/latest</c> 的 <c>type</c> 显示语义化选项。</summary>
internal sealed class LatestRecordsTypeParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (context.ParameterInfo?.Name != "type")
            return;

        parameter.Description = null;
        parameter.Schema = new OpenApiSchema
        {
            Type = "string",
            Enum =
            [
                new OpenApiString("all"),
                new OpenApiString("main"),
                new OpenApiString("bonus"),
                new OpenApiString("stage"),
            ],
            Default = new OpenApiString("all"),
            Extensions =
            {
                ["x-enum-descriptions"] = new OpenApiArray
                {
                    new OpenApiString("全部"),
                    new OpenApiString("主线 (track=0)"),
                    new OpenApiString("奖励 (track>0)"),
                    new OpenApiString("阶段"),
                },
            },
        };
        parameter.Example = new OpenApiString("all");
    }
}
