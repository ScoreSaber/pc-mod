using SiraUtil.Web;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Core.Api {
    internal class SiraUtilHttpMessageHandler : HttpMessageHandler {
        private const int RequestTimeoutSeconds = 30;
        private readonly IHttpService _httpService;

        internal SiraUtilHttpMessageHandler(IHttpService httpService) {
            _httpService = httpService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            string body = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            var headers = GetHeaders(request);

            Plugin.Log.Debug($"ScoreSaber API {request.Method} {request.RequestUri.AbsolutePath}");
            IHttpResponse siraResponse = await _httpService.SendAsync(
                ToMethod(request.Method),
                request.RequestUri.ToString(),
                RequestTimeoutSeconds,
                body,
                headers,
                null,
                cancellationToken);

            if (siraResponse == null) {
                Plugin.Log.Debug($"ScoreSaber API {request.RequestUri.AbsolutePath} completed: no response");
                return CreateResponse(request, 0, Encoding.UTF8.GetBytes("ScoreSaber request failed before receiving an HTTP response"));
            }

            Plugin.Log.Debug($"ScoreSaber API {request.RequestUri.AbsolutePath} completed: {siraResponse.Code}");
            if (siraResponse.Code <= 0) {
                return CreateResponse(request, siraResponse.Code, Encoding.UTF8.GetBytes("ScoreSaber request timed out before receiving an HTTP response"));
            }

            byte[] responseBody = await ReadResponseBody(siraResponse);

            HttpResponseMessage response = CreateResponse(request, siraResponse.Code, responseBody);
            CopyHeaders(response, siraResponse);
            return response;
        }

        private static HttpResponseMessage CreateResponse(HttpRequestMessage request, int statusCode, byte[] responseBody) {
            return new HttpResponseMessage(GetStatusCode(statusCode)) {
                Content = new ByteArrayContent(responseBody),
                RequestMessage = request
            };
        }

        private static void CopyHeaders(HttpResponseMessage response, IHttpResponse siraResponse) {
            siraResponse.CopyHeadersTo(response);
        }

        private static async Task<byte[]> ReadResponseBody(IHttpResponse response) {
            try {
                return await response.ReadAsByteArrayAsync() ?? new byte[0];
            } catch (Exception ex) {
                string message = string.IsNullOrEmpty(ex.Message) ? "ScoreSaber response body could not be read" : ex.Message;
                return Encoding.UTF8.GetBytes(message);
            }
        }

        private static Dictionary<string, string> GetHeaders(HttpRequestMessage request) {
            var headers = new Dictionary<string, string>();
            foreach (var header in request.Headers) {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            if (request.Content == null) {
                return headers;
            }

            foreach (var header in request.Content.Headers) {
                if (!ShouldSkipHeader(header.Key)) {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
            }

            return headers;
        }

        private static bool ShouldSkipHeader(string name) {
            return string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase);
        }

        private static HTTPMethod ToMethod(HttpMethod method) {
            if (method == HttpMethod.Get) {
                return HTTPMethod.GET;
            }

            if (method == HttpMethod.Post) {
                return HTTPMethod.POST;
            }

            if (method == HttpMethod.Put) {
                return HTTPMethod.PUT;
            }

            if (method.Method == "PATCH") {
                return HTTPMethod.PATCH;
            }

            if (method == HttpMethod.Delete) {
                return HTTPMethod.DELETE;
            }

            throw new NotSupportedException($"Unsupported HTTP method {method.Method}");
        }

        private static HttpStatusCode GetStatusCode(int statusCode) {
            return statusCode <= 0 ? HttpStatusCode.RequestTimeout : (HttpStatusCode)statusCode;
        }
    }
}
