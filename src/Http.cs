using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ScoreSaber {
    internal struct HttpOptions {
        public string applicationName { get; set; }
        public Version version { get; set; }
        public string baseURL { get; set; }
    }

    internal sealed class Http {
        internal Dictionary<string, string> PersistentRequestHeaders { get; private set; }
        internal HttpOptions options;

        internal Http(HttpOptions _options = new HttpOptions()) {

            options = _options;
            PersistentRequestHeaders = new Dictionary<string, string>();

            if ((_options.applicationName != null && _options.version == null) || (_options.applicationName == null && _options.version != null)) {
                throw new ArgumentException("You must specify either both or none of ApplicationName and Version");
            }

            string libVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string userAgent = $"Default/{libVersion}";

            if (_options.applicationName != null) {
                userAgent = $"{_options.applicationName}/{_options.version}";
            }

            PersistentRequestHeaders.Add("User-Agent", userAgent);
        }

        internal async Task SendHttpAsyncRequest(UnityWebRequest request) {

            foreach (var header in PersistentRequestHeaders) {
                request.SetRequestHeader(header.Key, header.Value);
            }

            AsyncOperation asyncOperation = request.SendWebRequest();
            while (!asyncOperation.isDone) {
                await Task.Delay(100);
            }
        }

        internal async Task<string> GetRawAsync(string url) {
            using (UnityWebRequest request = UnityWebRequest.Get(url)) {
                request.timeout = 5;
                await SendHttpAsyncRequest(request);
                if (request.IsConnectionError() || request.IsProtocolError()) {
                    throw ThrowHttpException(request);
                }

                return Encoding.UTF8.GetString(request.downloadHandler.data);
            }
        }

        internal async Task<byte[]> DownloadRawAsync(string url) {
            using (UnityWebRequest request = UnityWebRequest.Get(url)) {
                await SendHttpAsyncRequest(request);
                if (request.IsConnectionError() || request.IsProtocolError()) {
                    throw ThrowHttpException(request);
                }

                return request.downloadHandler.data;
            }
        }

        internal async Task<string> GetAsync(string url) {
            url = $"{options.baseURL}{url}";
            using (UnityWebRequest request = UnityWebRequest.Get(url)) {
                request.timeout = 5;
                await SendHttpAsyncRequest(request);
                if (request.IsConnectionError() || request.IsProtocolError()) {
                    throw ThrowHttpException(request);
                }

                return Encoding.UTF8.GetString(request.downloadHandler.data);
            }
        }

        internal async Task<byte[]> DownloadAsync(string url) {
            url = $"{options.baseURL}{url}";
            return await DownloadRawAsync(url);
        }

        internal Task<string> PostAsync(string url, WWWForm form) => PostAsync(url, form, null);

        internal async Task<string> PostAsync(string url, WWWForm form, IDictionary<string, string> headers) {
            url = $"{options.baseURL}{url}";
            using (UnityWebRequest request = UnityWebRequest.Post(url, form)) {
                if (headers != null) {
                    foreach (var header in headers) {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }

                request.timeout = 120;
                await SendHttpAsyncRequest(request);
                if (request.IsConnectionError() || request.IsProtocolError()) {
                    throw ThrowHttpException(request);
                }

                return Encoding.UTF8.GetString(request.downloadHandler.data);
            }
        }

        internal HttpErrorException ThrowHttpException(UnityWebRequest request) {
            string errorBody = request.downloadHandler.data == null ? string.Empty : Encoding.UTF8.GetString(request.downloadHandler.data);
            return new HttpErrorException(
                request.IsConnectionError(),
                request.IsProtocolError(),
                (int)request.responseCode,
                errorBody
            );
        }
    }

    internal class HttpErrorException : Exception {
        internal bool isNetworkError { get; set; }
        internal bool isHttpError { get; set; }
        internal bool isScoreSaberError { get; set; }
        internal int statusCode { get; set; }
        internal string errorBody { get; set; }
        internal ScoreSaberError scoreSaberError { get; set; }
        internal HttpErrorException(bool _isNetworkError, bool _isHttpError, int _statusCode, string _scoreSaberErrorMessage = "") {
            isNetworkError = _isNetworkError;
            isHttpError = _isHttpError;
            statusCode = _statusCode;
            errorBody = _scoreSaberErrorMessage;
            if (_scoreSaberErrorMessage != string.Empty) {
                try {
                    scoreSaberError = JsonConvert.DeserializeObject<ScoreSaberError>(_scoreSaberErrorMessage);
                    isScoreSaberError = true;
                } catch (Exception) { }
            }
        }
    }

    internal class ScoreSaberError {
        [JsonProperty("errorMessage")]
        internal string ErrorMessage { get; set; }
        [JsonProperty("message")]
        internal string Message { get; set; }
    }
}
