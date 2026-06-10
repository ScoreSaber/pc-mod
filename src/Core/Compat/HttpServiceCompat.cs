using SiraUtil.Web;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Core.Compat {
    // old SiraUtil can't time out individual requests so we're faking it with a linked token
    internal static class HttpServiceCompat {
        internal static Task<IHttpResponse> SendWithTimeoutAsync(this IHttpService httpService, HTTPMethod method, string url, int timeoutSeconds, string body, IDictionary<string, string> headers, CancellationToken cancellationToken) {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
            return SendWithLinkedTimeoutAsync(httpService, method, url, timeoutSeconds, body, headers, cancellationToken);
#else
            return httpService.SendAsync(method, url, timeoutSeconds, body, headers, null, cancellationToken);
#endif
        }

#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
        private static async Task<IHttpResponse> SendWithLinkedTimeoutAsync(IHttpService httpService, HTTPMethod method, string url, int timeoutSeconds, string body, IDictionary<string, string> headers, CancellationToken cancellationToken) {
            // old SiraUtil reports timeout cancels as code <= 0
            using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                return await httpService.SendAsync(method, url, body, headers, null, timeoutSource.Token);
            }
        }
#endif
    }
}
