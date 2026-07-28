namespace tms_template_net8.Models.DTOs
{
    /// <summary>
    /// Standard API response model
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; }
        public DateTime Timestamp { get; set; }

        public ApiResponse()
        {
            Errors = new List<string>();
            Timestamp = DateTime.UtcNow;
        }

        public static ApiResponse<T> SuccessResponse(T? data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = new List<string>()
            };
        }

        public static ApiResponse<T> FailureResponse(string message = "Operation failed", List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string>()
            };
        }

        public static ApiResponse<T> FailureResponse(T? data, string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data,
                Errors = errors ?? new List<string>()
            };
        }
    }

    /// <summary>
    /// Non-generic API response model
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; }
        public DateTime Timestamp { get; set; }

        public ApiResponse()
        {
            Errors = new List<string>();
            Timestamp = DateTime.UtcNow;
        }

        public static ApiResponse FailureResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
