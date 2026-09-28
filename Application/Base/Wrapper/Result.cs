namespace Application.Base.Wrapper
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public static Result<T> Success(T? data, string? message = null)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message ?? ResourcesLocalizationKeys.Success
            };
        }

        public static Result<T> Falid(T? data, string? message = null)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Data = data,
                Message = message ?? ResourcesLocalizationKeys.Failed
            };
        }

        public static Result<T> Info(T? data, string? message = null)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Data = data,
                Message = message
            };
        }
    }
}
