using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.Server.Security;

/// <summary>
/// GEÇİCİ KORUMA: Kişi 1'in rol tabanlı girişi (JWT) gelene kadar eğitmen API'lerini korur.
/// Eğitmen uygulaması her isteğe "X-Instructor-Key" başlığı ekler; değer appsettings.json'daki
/// "InstructorKey" ile aynı olmalı. JWT gelince bunun yerine [Authorize(Roles = "Instructor")] kullanın.
/// </summary>
public class InstructorKeyAttribute : Attribute, IAsyncActionFilter
{
    public static bool IsValid(IConfiguration config, string? given)
    {
        var expected = config["InstructorKey"];
        return !string.IsNullOrEmpty(expected) && given == expected;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var config = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var given = ctx.HttpContext.Request.Headers[ApiHeaders.InstructorKey].ToString();

        if (!IsValid(config, given))
        {
            ctx.Result = new UnauthorizedObjectResult("Bu işlem sadece eğitmen içindir.");
            return;
        }
        await next();
    }
}
