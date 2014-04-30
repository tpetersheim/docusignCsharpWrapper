using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

namespace DocusignLib
{
    public class LoggingHandler//: DelegatingHandler
    {
        private FileInfo logFilePath;

        public LoggingHandler(string logFilePath)
        {
            if (!string.IsNullOrEmpty(logFilePath))
                this.logFilePath = new FileInfo(logFilePath);
        }

        //protected override Task<HttpRequestMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        //{
        //    // Log the request information
        //    LogRequestLoggingInfo(request);

        //    // Execute the request
        //    return base.SendAsync(request, cancellationToken).ContinueWith(task =>
        //    {
        //        var response = task.Result;
        //        // Extract the response logging info then persist the information
        //        LogResponseLoggingInfo(response);
        //        return response;
        //    });
        //}

        public void LogRequestLoggingInfo(HttpWebRequest request, string requestAsString)
        {
            //StreamReader sr = new StreamReader(request.GetRequestStream());
            //string requestAsString = sr.ReadToEnd();

            string log = string.Format(
                "RequestUri: {0}" + Environment.NewLine +
                "Headers: \n{1}" + Environment.NewLine +
                //"ContenType: {2}" + Environment.NewLine +
                //"ContentLength: {3}" + Environment.NewLine +
                //"Accept: {4}" + Environment.NewLine +
                "Method: {2}" + Environment.NewLine +
                "Body: {3}" + Environment.NewLine,
                request.RequestUri.ToString(),
                request.Headers.ToString(),
                //request.ContentType,
                //request.ContentLength, 
                //request.Accept,
                request.Method,
                requestAsString);

            writeToFile(log);
        }

        //private void LogResponseLoggingInfo(HttpWebResponse response)
        //{
        //    if (response.Content != null)
        //    {
        //        response.Content.ReadAsByteArrayAsync()
        //            .ContinueWith(task =>
        //            {
        //                var responseMsg = Encoding.UTF8.GetString(task.Result);
        //                // Log it somewhere
        //            });
        //    }
        //}


        public void writeToFile(string data)
        {
            if (logFilePath != null)
            {
                try
                {
                    if (!Directory.Exists(logFilePath.DirectoryName))
                    {
                        Directory.CreateDirectory(logFilePath.DirectoryName);
                    }
                    File.AppendAllText(logFilePath.FullName, DateTime.Now.ToString() + Environment.NewLine + data + Environment.NewLine);
                }
                catch (Exception excep) { } //Ignore any errors
            }
        }
    }
}
