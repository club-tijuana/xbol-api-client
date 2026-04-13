using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Odasoft.XBOL.Business;

namespace Odasoft.XBOL.ClientAPI.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ApiException apiException)
        {
            return;
        }

        _logger.LogWarning(
            context.Exception,
            "Upstream API error {Status}",
            apiException.StatusCode);

        context.Result = context.Exception is ApiException<SeatsIoErrorResponse> typed
            ? new ObjectResult(typed.Result) { StatusCode = typed.StatusCode }
            : new ObjectResult(apiException.Response) { StatusCode = apiException.StatusCode };

        context.ExceptionHandled = true;
    }
}
