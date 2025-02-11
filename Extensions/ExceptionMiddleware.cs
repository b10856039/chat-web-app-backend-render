using System.Net;
using System.Text.Json;
using static ChatAPI.Endpoints.AuthController;

namespace ChatAPI.Extensions;

public class ExceptionMiddleware
{   
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;


    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }


    public async Task InvokeAsync(HttpContext context)
    {
        try{
            await _next(context);
        }catch(Exception ex)
        {  
            _logger.LogError($"[ExceptionMiddleware] 捕獲未處理異常 : {ex.Message}");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse<string> response = new ApiResponse<string>(new List<string> { "伺服器內部錯誤，請稍後再試" });
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        if (exception is ModelStateValidationException modelStateException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            response = new ApiResponse<string>(modelStateException.Errors); // 使用 ModelState 驗證錯誤
        }
        else if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response = new ApiResponse<string>(new List<string> { "未授權的請求" });
        }
        else if (exception is ForbiddenAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            response = new ApiResponse<string>(new List<string> { "您無權存取此資源。" });
        }
        else if (exception is KeyNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            response = new ApiResponse<string>(new List<string> { "找不到資源。" });
        }

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }


    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message) : base(message) { }
    }

    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public ApiResponse(T? data)
        {
            Data = data;
            Errors = null;
        }

        public ApiResponse(List<string> errors)
        {
            Data = default;
            Errors = errors;
        }
    }

}
