using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

        public void LogRequestLoggingInfo(HttpWebRequest request, string requestBody)
        {
            string log = string.Format(
                "RequestUri: {0}" + Environment.NewLine +
                "Headers: \n{1}" + Environment.NewLine +
                "Method: {2}" + Environment.NewLine +
                "Body: {3}" + Environment.NewLine,
                request.RequestUri.ToString(),
                request.Headers.ToString(),
                request.Method,
                requestBody);

            writeToFile(log);
        }

        public void LogResponseLoggingInfo(HttpWebResponse response, string responseAsString)
        {
            string log = string.Format(
                "ResponseUri: {0}" + Environment.NewLine +
                "Headers: \n{1}" + Environment.NewLine +
                "Method: {2}" + Environment.NewLine +
                "Body: {3}" + Environment.NewLine,
                response.ResponseUri.ToString(),
                response.Headers.ToString(),
                response.Method,
                responseAsString);

            writeToFile(log);
        }


        public void writeToFile(string data)
        {
            data = stripPasswordFromXml(data);

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

        private string stripPasswordFromXml(string data)
        {
            return (new Regex("<password>.*</password>", RegexOptions.IgnoreCase)).Replace(data, "<Password>[stripped out]</Password>");
        }
    }
}
