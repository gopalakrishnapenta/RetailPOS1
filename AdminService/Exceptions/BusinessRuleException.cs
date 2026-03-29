using System.Net;

namespace AdminService.Exceptions
{
    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string message) 
            : base(message, HttpStatusCode.Forbidden)
        {
        }
    }

    public class BusinessRuleException : DomainException
    {
        public BusinessRuleException(string message) 
            : base(message, HttpStatusCode.UnprocessableEntity)
        {
        }
    }
}
