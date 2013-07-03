using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ctstone.Json
{
    public class JsonClient
    {
        private Uri _baseUri;
        public event EventHandler<HttpRequestEventArgs> HttpRequesting;

        public JsonClient(Uri baseUri)
        {
            _baseUri = baseUri;
        }

        public T GET<T>(string relUri, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            HttpWebRequest request = CreateRequest_GET(relUri, parameters);
            return ReadJson<T>(request);
        }
        public dynamic GET(string relUri, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            HttpWebRequest request = CreateRequest_GET(relUri, parameters);
            return ReadJson(request);
        }

        private HttpWebRequest CreateRequest_GET(string relUri, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            Uri uri = GetUri(relUri, parameters);
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            if (HttpRequesting != null)
                HttpRequesting(this, new HttpRequestEventArgs(request));
            request.Method = "GET";
            request.Accept = "application/json";
            return request;
        }

        private Uri GetUri(string relUri)
        {
            return new Uri(_baseUri, new Uri(relUri, UriKind.Relative));
        }

        private Uri GetUri(string relUri, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            string querystring = GetQuerystring(parameters);
            if (!String.IsNullOrEmpty(querystring))
                querystring = '?' + querystring;
            Uri uri = new Uri(_baseUri, new Uri(relUri + querystring, UriKind.Relative));
            Trace.WriteLine("URI: " + uri);
            return uri;
        }

        private static string GetQuerystring(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if (parameters == null)
                return String.Empty;
            return String.Join("&", parameters
                .Where(x => x.Value != null)
                .Select(x => String.Format("{0}={1}", x.Key, x.Value)));
        }

        private static dynamic ReadJson(HttpWebRequest request)
        {
            return JsonTokenizer.Parse(ReadResponse(request));
        }

        private static T ReadJson<T>(HttpWebRequest request)
        {
            return JsonTokenizer.Parse<T>(ReadResponse(request));
        }

        private static string ReadResponse(HttpWebRequest request)
        {
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                string text = sr.ReadToEnd();
                Trace.WriteLine("Got response: " + text);
                return text;
            }
        }
    }

    public class QueryParameters : IEnumerable<KeyValuePair<string, object>>
    {
        private List<KeyValuePair<string, object>> _queryParameters;

        public QueryParameters()
        {
            _queryParameters = new List<KeyValuePair<string, object>>();
        }

        public QueryParameters(IEnumerable<KeyValuePair<string, object>> baseParameters)
        {
            if (baseParameters == null)
                _queryParameters = new List<KeyValuePair<string, object>>();
            else 
                _queryParameters = new List<KeyValuePair<string, object>>(baseParameters);
        }

        public void Add(string key, object value)
        {
            _queryParameters.Add(new KeyValuePair<string, object>(key, value));
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _queryParameters.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class HttpRequestEventArgs : EventArgs
    {
        public HttpWebRequest Request { get; private set; }

        public HttpRequestEventArgs(HttpWebRequest request)
        {
            Request = request;
        }
    }
}
