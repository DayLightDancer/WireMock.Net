// This source file is based on mock4net by Alexandre Victoor which is licensed under the Apache 2.0 License.
// For more details see 'mock4net/LICENSE.txt' and 'mock4net/readme.md' in this project root.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if USE_ASPNETCORE
using System.Security.Cryptography.X509Certificates;
#endif
using Stef.Validation;
using WireMock.Http;
using WireMock.Models;
using WireMock.Owin;
using WireMock.Types;
using WireMock.Util;

namespace WireMock;

/// <summary>
/// The RequestMessage.
/// </summary>
public class RequestMessage : IRequestMessage
{
    /// <inheritdoc />
    public string ClientIP { get; }

    /// <inheritdoc />
    public string Url { get; }

    /// <inheritdoc />
    public string AbsoluteUrl { get; }

    /// <inheritdoc />
    public string? ProxyUrl { get; set; }

    /// <inheritdoc />
    public DateTime DateTime { get; set; }

    /// <inheritdoc />
    public string Path { get; }

    /// <inheritdoc />
    public string AbsolutePath { get; }

    /// <inheritdoc />
    public string[] PathSegments { get; }

    /// <inheritdoc />
    public string[] AbsolutePathSegments { get; }

    /// <inheritdoc />
    public string Method { get; }

    /// <inheritdoc />
    public IDictionary<string, WireMockList<string>>? Headers { get; }

    /// <inheritdoc />
    public IDictionary<string, string>? Cookies { get; }

    /// <inheritdoc />
    public IDictionary<string, WireMockList<string>>? Query { get; }

    /// <inheritdoc />
    public IDictionary<string, WireMockList<string>>? QueryIgnoreCase { get; }

    /// <inheritdoc />
    public string RawQuery { get; }

    /// <inheritdoc />
    public IBodyData? BodyData { get; }

    /// <inheritdoc />
    public string? Body { get; }

    /// <inheritdoc />
    public object? BodyAsJson { get; }

    /// <inheritdoc />
    public byte[]? BodyAsBytes { get; }

#if MIMEKIT
    /// <inheritdoc />
    public object? BodyAsMimeMessage { get; }
#endif

    /// <inheritdoc />
    public string? DetectedBodyType { get; }

    /// <inheritdoc />
    public string? DetectedBodyTypeFromContentType { get; }

    /// <inheritdoc />
    public string? DetectedCompression { get; }

    /// <inheritdoc />
    public string Host { get; }

    /// <inheritdoc />
    public string Protocol { get; }

    /// <inheritdoc />
    public int Port { get; }

    /// <inheritdoc />
    public string Origin { get; }

#if USE_ASPNETCORE
    /// <inheritdoc />
    public X509Certificate2? ClientCertificate { get; }
#endif

    /// <summary>
    /// Used for Unit Testing
    /// </summary>
    public RequestMessage(
        UrlDetails urlDetails,
        string method,
        string clientIP,
        IBodyData? bodyData = null,
        IDictionary<string, string[]>? headers = null,
        IDictionary<string, string>? cookies = null) : this(null, urlDetails, method, clientIP, bodyData, headers, cookies)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestMessage"/> class.
    /// </summary>
    /// <param name="options">The<seealso cref="IWireMockMiddlewareOptions"/>.</param>
    /// <param name="urlDetails">The original url details.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="clientIP">The client IP Address.</param>
    /// <param name="bodyData">The BodyData.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cookies">The cookies.</param>
#if USE_ASPNETCORE
    /// <param name="clientCertificate">The client certificate</param>
#endif
    internal RequestMessage(
        IWireMockMiddlewareOptions? options,
        UrlDetails urlDetails, string method,
        string clientIP,
        IBodyData? bodyData = null,
        IDictionary<string, string[]>? headers = null,
        IDictionary<string, string>? cookies = null
#if USE_ASPNETCORE
        , X509Certificate2? clientCertificate = null
#endif
    )
    {
        Guard.NotNull(urlDetails);
        Guard.NotNull(method);
        Guard.NotNull(clientIP);

        AbsoluteUrl = urlDetails.AbsoluteUrl.ToString();
        Url = urlDetails.Url.ToString();
        Protocol = urlDetails.Url.Scheme;
        Host = urlDetails.Url.Host;
        Port = urlDetails.Url.Port;
        Origin = $"{Protocol}://{Host}:{Port}";

        AbsolutePath = WebUtility.UrlDecode(urlDetails.AbsoluteUrl.AbsolutePath);
        Path = WebUtility.UrlDecode(urlDetails.Url.AbsolutePath);
        PathSegments = Path.Split('/').Skip(1).ToArray();
        AbsolutePathSegments = AbsolutePath.Split('/').Skip(1).ToArray();

        Method = method;
        ClientIP = clientIP;

        BodyData = bodyData;

        // Convenience getters for e.g. Handlebars
        Body = BodyData?.BodyAsString;
        BodyAsJson = BodyData?.BodyAsJson;
        BodyAsBytes = BodyData?.BodyAsBytes;

        DetectedBodyType = BodyData?.DetectedBodyType.ToString();
        DetectedBodyTypeFromContentType = BodyData?.DetectedBodyTypeFromContentType.ToString();
        DetectedCompression = BodyData?.DetectedCompression;

#if MIMEKIT
        try
        {
            BodyAsMimeMessage = MimeKitUtils.GetMimeMessage(BodyData, headers![HttpKnownHeaderNames.ContentType].First());
        }
        catch
        {
            // Ignore exception from MimeMessage.Load
        }
#endif

        Headers = headers?.ToDictionary(header => header.Key, header => new WireMockList<string>(header.Value));
        Cookies = cookies;
        RawQuery = urlDetails.Url.Query;
        Query = QueryStringParser.Parse(RawQuery, options?.QueryParameterMultipleValueSupport);
        QueryIgnoreCase = new Dictionary<string, WireMockList<string>>(Query, StringComparer.OrdinalIgnoreCase);
#if USE_ASPNETCORE
        ClientCertificate = clientCertificate;
#endif
    }

    /// <inheritdoc />
    public WireMockList<string>? GetParameter(string key, bool ignoreCase = false)
    {
        if (Query == null)
        {
            return null;
        }

        var query = !ignoreCase ? Query : new Dictionary<string, WireMockList<string>>(Query, StringComparer.OrdinalIgnoreCase);

        return query.ContainsKey(key) ? query[key] : null;
    }
}