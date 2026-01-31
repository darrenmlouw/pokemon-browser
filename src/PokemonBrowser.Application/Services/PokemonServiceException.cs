using System.Net;

namespace PokemonBrowser.Application.Services;

public sealed class PokemonServiceException : Exception
{
    public PokemonServiceException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}
