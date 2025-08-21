
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NCEZ.Simulator.Filters;

public sealed class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(kv => kv.Value is not null && kv.Value.Errors.Count > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Type = "https://http.dev/errors/validation",
                Title = "Validation failed",
                Status = StatusCodes.Status422UnprocessableEntity
            };
            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
