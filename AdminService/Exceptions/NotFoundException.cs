using System.Net;

namespace AdminService.Exceptions
{
    public class NotFoundException : DomainException
    {
        public NotFoundException(string message) 
            : base(message, HttpStatusCode.NotFound)
        {
        }
    }

    public class ValidationException : DomainException
    {
        public ValidationException(string message) 
            : base(message, HttpStatusCode.UnprocessableEntity)
        {
        }
    }

    public class ConflictException : DomainException
    {
        public ConflictException(string message) 
            : base(message, HttpStatusCode.Conflict)
        {
        }
    }
}
