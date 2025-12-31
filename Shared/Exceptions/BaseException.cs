namespace Shared.Exceptions
{
    public abstract class BaseException : Exception
    {
        public int StatusCode { get; set; }
        public List<string> Errors { get; set; } = new();

        protected BaseException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }

        protected BaseException(string message, List<string> errors, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
            Errors = errors;
        }
    }

    public class NotFoundException : BaseException
    {
        public NotFoundException(string message = "العنصر المطلوب غير موجود") 
            : base(message, 404)
        {
        }
    }

    public class BadRequestException : BaseException
    {
        public BadRequestException(string message = "طلب غير صحيح") 
            : base(message, 400)
        {
        }

        public BadRequestException(string message, List<string> errors) 
            : base(message, errors, 400)
        {
        }
    }

    public class UnauthorizedException : BaseException
    {
        public UnauthorizedException(string message = "غير مصرح بالوصول") 
            : base(message, 401)
        {
        }
    }

    public class ForbiddenException : BaseException
    {
        public ForbiddenException(string message = "ليس لديك الصلاحية للوصول إلى هذا المورد") 
            : base(message, 403)
        {
        }
    }

    public class ValidationException : BaseException
    {
        public ValidationException(string message = "البيانات المرسلة غير صحيحة") 
            : base(message, 400)
        {
        }

        public ValidationException(List<string> errors) 
            : base("التحقق من صحة البيانات فشل", errors, 400)
        {
        }
    }

    public class BusinessLogicException : BaseException
    {
        public BusinessLogicException(string message) 
            : base(message, 400)
        {
        }
    }
}

