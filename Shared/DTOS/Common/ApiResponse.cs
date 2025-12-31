namespace Shared.DTOS.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public int StatusCode { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "تمت العملية بنجاح")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 200
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Errors = errors ?? new List<string>()
            };
        }

        public static ApiResponse<T> NotFoundResponse(string message = "العنصر المطلوب غير موجود")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = 404
            };
        }

        public static ApiResponse<T> UnauthorizedResponse(string message = "غير مصرح بالوصول")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = 401
            };
        }
    }

    // For endpoints that don't return data
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public int StatusCode { get; set; }

        public static ApiResponse SuccessResponse(string message = "تمت العملية بنجاح")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                StatusCode = 200
            };
        }

        public static ApiResponse ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Errors = errors ?? new List<string>()
            };
        }

        public static ApiResponse NotFoundResponse(string message = "العنصر المطلوب غير موجود")
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                StatusCode = 404
            };
        }

        public static ApiResponse UnauthorizedResponse(string message = "غير مصرح بالوصول")
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                StatusCode = 401
            };
        }
    }
}

