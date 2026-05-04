using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TriPla.Backend.Application.Common;

namespace TriPla.Backend.Api.Extensions;

public static class ControllerExtensions
{
    public static Guid GetUserId(this ControllerBase controller)
    {
        var sub = controller.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? controller.User.FindFirstValue("sub");

        if (Guid.TryParse(sub, out var id))
            return id;

        throw new UnauthorizedAccessException("User identity not found in the token.");
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return new BadRequestObjectResult(new { error = result.Error });
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return new BadRequestObjectResult(new { error = result.Error });
    }

    public static IActionResult ToCreatedResult<T>(this Result<T> result, string actionName, object routeValues)
    {
        if (result.IsSuccess)
            return new CreatedAtActionResult(actionName, null, routeValues, result.Value);

        return new BadRequestObjectResult(new { error = result.Error });
    }
}
