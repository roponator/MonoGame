using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using System.IO;

namespace RopoMonogameEventLogger
{

    /* 
        Logs a custom event to server, automaticaly also stores: 
          - app name 
          - app version
          - time (local to server)

        Your data:
         - tag (max 400 chars): the title or something you can search/filter easily, eg "MyFile.cs line 40 bug test"
         - message (max 60k chars): the body of the message, callstack etc...

        Limitations:
        - tag: max 400 chars, will be cut if longer.
        - message: max 60k chars, will be cut if longer.
        - app name is stored with max 400 chars, will be cut if longer.

        Usage example:
          SaladEventLogging.LogBlocking("MyFunc bla test bug", "dsfsdfsfsdf");
          SaladEventLogging.LogAsync("MyFunc bla test bug", "sdfsdfsdfsdf");

    */
    public class SaladEventLogging
    {
        public static string GAME_NAME = "NOT_PROPERLY_SET_DOMINOES_DELUXE_WIN8";
        public static string GAME_VERSION = "FAIL_NOT_SET";

        public static void LogBlocking(string tag, string message)
        {

            try
            {
                var request = HttpWebRequest.Create(LOG_SERVLET_ADDRESS);
                request.Method = "PUT";
                request.ContentType = "application/json";

                var requestTask = request.GetRequestStreamAsync();
                requestTask.Wait();

                StreamWriter requestWriter = new StreamWriter(requestTask.Result);
                requestWriter.Write(makeJsonStringForRequest(tag, message));
                requestWriter.Flush();

                var responseTask = request.GetResponseAsync();
                responseTask.Wait();

                var response = (HttpWebResponse)responseTask.Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
#if DEBUG
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var requestResult = streamReader.ReadToEnd();
                        System.Diagnostics.Debug.WriteLine("log error: " + response.StatusCode.ToString() + ", details: " + requestResult);
                    }
#endif
                }
                response.Dispose();

            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.ToString());
#endif
            }
        }

        public static void LogAsync(string tag, string message)
        {
            try
            {
                var request = HttpWebRequest.Create(LOG_SERVLET_ADDRESS);
                request.Method = "PUT";
                request.ContentType = "application/json";

                request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), new object[] { request, tag, message });
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.ToString());
#endif
            }

        }


        private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                object[] myData = (object[])asynchronousResult.AsyncState;

                HttpWebRequest request = (HttpWebRequest)myData[0];

                // End the operation
                Stream postStream = request.EndGetRequestStream(asynchronousResult);

                StreamWriter requestWriter = new StreamWriter(postStream);
                requestWriter.Write(makeJsonStringForRequest((string)myData[1], (string)myData[2]));
                requestWriter.Flush();

                // Start the asynchronous operation to get the response
                request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.ToString());
#endif
            }
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

                // End the operation
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                Stream streamResponse = response.GetResponseStream();

                if (response.StatusCode != HttpStatusCode.OK)
                {
#if DEBUG
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var requestResult = streamReader.ReadToEnd();
                        System.Diagnostics.Debug.WriteLine("log error: " + response.StatusCode.ToString() + ", details: " + requestResult);
                    }
#endif

                }

                response.Dispose();
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.ToString());
#endif
            }
        }

        private static string makeJsonStringForRequest(string tag, string message)
        {
            // manually create the string since jsons are very slow on android + to prevent json dependency and we may need to send this fast before app crashes and closes
            string s= " { \"appName\":\"" + cleanForJSON(GAME_NAME) + "\",    \"appVersion\":\"" + cleanForJSON(GAME_VERSION) + 
                "\",    \"tag\":\"" + cleanForJSON(tag) + "\",    \"message\":\"" + cleanForJSON(message) + "\" }";
            return s;
        }

        public static string cleanForJSON(string s)
        {
            if (s == null || s.Length == 0)
            {
                return "";
            }

            char c = '\0';
            int i;
            int len = s.Length;
            StringBuilder sb = new StringBuilder(len + 4);
            String t;

            for (i = 0; i < len; i += 1)
            {
                c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                        {
                            //t = "000" + String.Format("X", c);
                            //sb.Append("\\u" + t.Substring(t.Length - 4));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private const string LOG_SERVLET_ADDRESS = "http://ec2-34-192-207-0.compute-1.amazonaws.com/customeventslog";
    }
}
