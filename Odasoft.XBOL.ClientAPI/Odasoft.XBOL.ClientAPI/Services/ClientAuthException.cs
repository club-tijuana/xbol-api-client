namespace Odasoft.XBOL.ClientAPI.Services;

public sealed class ClientAuthException : Exception
{
    public ClientAuthException(string message, int statusCode, string code)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }

    public string Code { get; }
}
