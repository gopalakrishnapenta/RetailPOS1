using System.Net;

namespace OrdersService.Exceptions
{
    public abstract class DomainException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        protected DomainException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) 
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
