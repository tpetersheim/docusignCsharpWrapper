using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using DocusignEntity;
using System.Xml.Linq;

namespace DocusignLib
{
    public class DocusignMethods
    {
        static string Username = string.Empty;
        static string Password = string.Empty;
        static string IntegratorKey = string.Empty;
        static string Url = string.Empty;
        static string TemplateId = string.Empty;
        static string envelopeUri = string.Empty;
        static string docListUri = string.Empty;
        static string errorMessage = string.Empty;
        static string authenticateStr = string.Empty;
        private LoggingHandler logHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="docusignAuth"></param>
        public DocusignMethods(DocusignAuth docusignAuth, string logFilePath = null)
        {
            Username = docusignAuth.UserName;
            Password = docusignAuth.Password;
            IntegratorKey = docusignAuth.IntegratorKey;
            Url = docusignAuth.Url;
            TemplateId = docusignAuth.TemplateId;

            authenticateStr = "<DocuSignCredentials>" +
                "<Username>" + Username + "</Username>" +
                "<Password>" + Password + "</Password>" +
                "<IntegratorKey>" + IntegratorKey + "</IntegratorKey>" +
                "</DocuSignCredentials>";

            logHandler = new LoggingHandler(logFilePath);
        }

        /// <summary>
        /// Login and return a LoginResponse
        /// </summary>
        /// <returns></returns>
        private LoginResponse Login()
        {
            var loginResponse = new LoginResponse();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
            request.Accept = "application/xml";
            request.Method = "GET";
            logHandler.LogRequestLoggingInfo(request, "");
            HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(webResponse.GetResponseStream());
            string responseText = sr.ReadToEnd();
            logHandler.LogResponseLoggingInfo(webResponse, responseText);

            //Parse response xml to object
            XDocument xDoc = XDocument.Parse(responseText);
            loginResponse.AccountId = getXDocValue(xDoc, "accountId");
            loginResponse.BaseUrl = getXDocValue(xDoc, "baseUrl");
            loginResponse.Email = getXDocValue(xDoc, "email");
            bool isDefault;
            bool.TryParse(getXDocValue(xDoc, "isDefault"), out isDefault);
            loginResponse.IsDefault = isDefault;
            loginResponse.Name = getXDocValue(xDoc, "name");
            loginResponse.SiteDescription = getXDocValue(xDoc, "siteDescription");
            loginResponse.UserId = getXDocValue(xDoc, "userId");
            loginResponse.UserName = getXDocValue(xDoc, "userName");
    
            return loginResponse;
        }

        /// <summary>
        /// RequestSignatureFromTemplate request signature using Template
        /// </summary>
        /// <param name="requestData"></param>
        /// <returns></returns>
        public Response RequestSignatureFromTemplate(envelopeDefinition requestData)
        {
            errorMessage = string.Empty;
            try
            {
                // 
                // STEP 1 - Login
                //

                var loginResponse = Login();


                //
                // STEP 2 - 
                //

                requestData.accountId = loginResponse.AccountId;
                string requestBody = Helper.GetXMLFromObject(requestData);

                // append "/envelopes" to baseURL and use in the request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(loginResponse.BaseUrl + "/envelopes");
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.ContentLength = requestBody.Length;
                request.Accept = "application/xml";
                request.Method = "POST";
                // write the body of the request
                byte[] body = System.Text.Encoding.UTF8.GetBytes(requestBody);
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(body, 0, requestBody.Length);
                dataStream.Close();
                // read the response
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                string responseText = "";
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();

                return processSignatureResponse(responseText, webResponse.StatusCode.ToString());
            }
            catch (WebException e)
            {
                return handleWebException<Response>(e);
            }
        }

        /// <summary>
        /// RequestSignatureFromDocument request signature using Document
        /// </summary>
        /// <param name="file"></param>
        /// <param name="requestData"></param>
        /// <returns></returns>
        public Response RequestSignatureFromDocument(FileInfo file, envelopeDefinition requestData)
        {
            errorMessage = string.Empty;
            try
            {
                // 
                // STEP 1 - Login
                //

                var loginResponse = Login();
               

                //
                // STEP 2 - Create an Envelope with one recipient and one tab and send
                //

                string envDef = Helper.GetXMLFromObject(requestData);


                // read contents of document into the request stream
                HttpWebRequest request;
                using (FileStream fileStream = File.OpenRead(file.FullName))
                {

                    // build the multipart request body
                    string requestBodyStart = "\r\n\r\n--BOUNDARY\r\n" +
                            "Content-Type: application/xml\r\n" +
                            "Content-Disposition: form-data\r\n" +
                            "\r\n" +
                            envDef + "\r\n\r\n--BOUNDARY\r\n" + 	// our xml formatted envelopeDefinition
                            "Content-Type: application/pdf\r\n" +
                            "Content-Disposition: file; filename=\"" + file.Name + "\"; documentId=1\r\n" +
                            "\r\n";

                    string requestBodyEnd = "\r\n--BOUNDARY--\r\n\r\n";

                    // use baseURL value + "/envelopes" for url of this request
                    request = (HttpWebRequest)WebRequest.Create(loginResponse.BaseUrl + "/envelopes");
                    request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                    request.ContentType = "multipart/form-data; boundary=BOUNDARY";
                    request.Accept = "application/xml";
                    request.ContentLength = requestBodyStart.ToString().Length + fileStream.Length + requestBodyEnd.ToString().Length;
                    request.Method = "POST";
                    logHandler.LogRequestLoggingInfo(request, requestBodyStart + requestBodyEnd);
                    // write the body of the request
                    byte[] bodyStart = System.Text.Encoding.UTF8.GetBytes(requestBodyStart.ToString());
                    byte[] bodyEnd = System.Text.Encoding.UTF8.GetBytes(requestBodyEnd.ToString());
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(bodyStart, 0, requestBodyStart.ToString().Length);

                    // Read the file contents and write them to the request stream
                    byte[] buf = new byte[4096];
                    int len;
                    while ((len = fileStream.Read(buf, 0, 4096)) > 0)
                    {
                        dataStream.Write(buf, 0, len);
                    }

                    dataStream.Write(bodyEnd, 0, requestBodyEnd.ToString().Length);
                    dataStream.Close();
                }

                // read the response
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                string responseText = "";
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();
                logHandler.LogResponseLoggingInfo(webResponse, responseText);

                return processSignatureResponse(responseText, webResponse.StatusCode.ToString());
            }
            catch (WebException e)
            {
                return handleWebException<Response>(e);
            }

        }
        
        /// <summary>
        /// GetDocusignEnvelopeInformation fetches envelope information
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <returns></returns>
        public string GetDocusignEnvelopeInformation(string envelopeId)
        {
            string url = Url;

            envelopeUri = "/envelopes/" + envelopeId;

            errorMessage = string.Empty;

            try
            {
                // 
                // STEP 1 - Login
                //
                var loginResponse = Login();

                //
                // STEP 2 - Get Envelope Info
                //
                // use baseURL value + envelopeUri for url of this request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(loginResponse.BaseUrl + envelopeUri);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.Method = "GET";
                // read the response
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                string responseText = "";
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();

                return responseText;
            }
            catch (WebException e)
            {
                return handleWebException<string>(e);
            }
        }

        /// <summary>
        /// GetEnvelopeRecipientStatus fetches envelope recipient status
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <param name="includeTab"></param>
        /// <returns></returns>
        public string GetEnvelopeRecipientStatus(string envelopeId,bool includeTab)
        {
            string url = Url;
            string baseURL = "";	// we will retrieve this
            string accountId = "";	// will retrieve

            

            // 
            // STEP 1 - Login
            //
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.Accept = "application/xml";
                request.Method = "GET";
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string responseText = sr.ReadToEnd();
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {	// Parse the xml response body
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "accountId"))
                            accountId = reader.ReadString();
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "baseUrl"))
                            baseURL = reader.ReadString();
                    }
                }

               

                //
                // STEP 2 - Get Envelope Recipient Info
                //

                // append "/envelopes/" + envelopeId + "/recipients" to baseUrl and use in the request
                url = baseURL + "/envelopes/" + envelopeId + "/recipients?include_tabs=" + includeTab;
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.Method = "GET";
                // read the response
                webResponse = (HttpWebResponse)request.GetResponse();
                sr.Close();
                responseText = "";
                sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();
                return responseText;
               
            }
            catch (WebException e)
            {
                return handleWebException<string>(e);
            }
        }

        /// <summary>
        /// GetEnvelopeStatus fetches envelope status
        /// </summary>
        /// <param name="date"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public string GetEnvelopeStatus(string date,from_to_status status)
        {
            string url = Url;
            string baseURL = "";	// we will retrieve this
            string accountId = "";	// will retrieve

           

            // 
            // STEP 1 - Login
            //
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.Accept = "application/xml";
                request.Method = "GET";
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string responseText = sr.ReadToEnd();
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {	// Parse the xml response body
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "accountId"))
                            accountId = reader.ReadString();
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "baseUrl"))
                            baseURL = reader.ReadString();
                    }
                }

                //--- display results
                Console.WriteLine("accountId = " + accountId + "\nbaseUrl = " + baseURL);

                //
                // STEP 2 - Get Envelope Status(es)
                //

                // Append "/envelopes" and query string to baseUrl and use in the request
                request = (HttpWebRequest)WebRequest.Create(baseURL + "/envelopes?from_date=" + date + "&from_to_status=" + status);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.Accept = "application/xml";
                request.Method = "GET";
                // read the response
                webResponse = (HttpWebResponse)request.GetResponse();
                sr.Close();
                responseText = "";
                sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();

                return responseText;
            }
            catch (WebException e)
            {
                return handleWebException<string>(e);
            }
        }

        /// <summary>
        /// GetEnvelopeDocList gets the document in envelope and downloads them
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <param name="download"></param>
        /// <param name="downloadToFolder"></param>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public List<string> GetEnvelopeDocList(string envelopeId,bool download,string downloadToFolder,out string errorMsg)
        {
            errorMsg = string.Empty;
            string baseURL = "";	// we will retrieve this
            string accountId = "";	// will retrieve
            docListUri = "/envelopes/" + envelopeId + "/documents";
            List<string> uriList = new List<string>();
            

            // 
            // STEP 1 - Login
            //
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.Accept = "application/xml";
                request.Method = "GET";
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string responseText = sr.ReadToEnd();
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {	// Parse the xml response body
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "accountId"))
                            accountId = reader.ReadString();
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "baseUrl"))
                            baseURL = reader.ReadString();
                    }
                }

               

                //
                // STEP 2 - Get Envelope Document List
                //

                // append docListUri to the baseUrl and use in the request
                request = (HttpWebRequest)WebRequest.Create(baseURL + docListUri);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.Method = "GET";
                // read the response
                webResponse = (HttpWebResponse)request.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();
                // grab the document uris
                
                int cnt = 0;
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "uri"))
                        {
                            uriList.Add(reader.ReadString());
                          //  Console.WriteLine("document uri {0} is:  {1}", ++cnt, uriList[uriList.Count - 1]);
                        }
                    }
                }

                //
                // STEP 3 - Download the Document(s)
                //
                if (download)
                {
                    int fileId = 1;
                    foreach (string uri in uriList)
                    {
                        // append document uris to the baseUrl
                        request = (HttpWebRequest)WebRequest.Create(baseURL + uri);
                        request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                        request.Accept = "application/pdf";
                        request.Method = "GET";
                        // read the response
                        webResponse = (HttpWebResponse)request.GetResponse();
                        using (MemoryStream ms = new MemoryStream())
                        using (FileStream outfile = new FileStream(downloadToFolder + "/document_" + fileId++ + ".pdf", FileMode.Create))
                        {
                            webResponse.GetResponseStream().CopyTo(ms);
                            if (ms.Length > int.MaxValue)
                            {
                                throw new NotSupportedException("Cannot write a file larger than 2GB.");
                            }
                            outfile.Write(ms.GetBuffer(), 0, (int)ms.Length);
                        }
                    }
                }
                
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    errorMsg = "Error code:: " + httpResponse.StatusCode;
                    using (Stream data = response.GetResponseStream())
                    {
                        string text = new StreamReader(data).ReadToEnd();
                        errorMsg += text;
                    }
                }
            }
            return uriList;
        }

        /// <summary>
        /// EmbedSendingUX embeds the sending UX
        /// </summary>
        /// <param name="envData"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public string EmbedSendingUX(envelopeDefinition envData,string returnUrl)
        {
            string accountId = string.Empty;
            string baseURL = string.Empty;
            string envelopeId = string.Empty;
            string uri = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.Accept = "application/xml";
                request.Method = "GET";
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string responseText = sr.ReadToEnd();
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {	// Parse the xml response body
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "accountId"))
                            accountId = reader.ReadString();
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "baseUrl"))
                            baseURL = reader.ReadString();
                    }
                }



                //
                // STEP 2 - Create an Envelope with an Embedded Recipient
                //

                // Construct an outgoing XML request body


                envData.accountId = accountId;
                string requestBody = Helper.GetXMLFromObject(envData);

                // append "/envelopes" to baseUrl and use in the request
                request = (HttpWebRequest)WebRequest.Create(baseURL + "/envelopes");
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.ContentLength = requestBody.Length;
                request.Method = "POST";
                // write the body of the request
                byte[] body = System.Text.Encoding.UTF8.GetBytes(requestBody);
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(body, 0, requestBody.Length);
                dataStream.Close();
                // read the response
                webResponse = (HttpWebResponse)request.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {	// Parse the xml response body
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "envelopeId"))
                            envelopeId = reader.ReadString();
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "uri"))
                            uri = reader.ReadString();
                    }
                }



                //
                // STEP 3 - Get the Embedded Console Send View
                //

                // construct another outgoing XML request body
                string reqBody = "<returnUrlRequest xmlns=\"http://www.docusign.com/restapi\">" +
                    "<authenticationMethod>email</authenticationMethod>" +
                    "<email>" + Username + "</email>" +	 				// NOTE: Use different email address if username provided in non-email format!
                    "<returnUrl>" + returnUrl + "</returnUrl>" +  // username can be in email format or an actual ID string
                    "<userName>Name</userName>" +
                    "<clientUserId>1</clientUserId>" +
                    "</returnUrlRequest>";

                // append uri + "/views/sender" to the baseUrl and use in the request
                request = (HttpWebRequest)WebRequest.Create(baseURL + uri + "/views/sender");
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.ContentLength = reqBody.Length;
                request.Method = "POST";
                // write the body of the request
                byte[] body2 = System.Text.Encoding.UTF8.GetBytes(reqBody);
                Stream dataStream2 = request.GetRequestStream();
                dataStream2.Write(body2, 0, reqBody.Length);
                dataStream2.Close();
                // read the response
                webResponse = (HttpWebResponse)request.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();
                return responseText;


            }
            catch (WebException e)
            {
                return handleWebException<string>(e);
            }
        }

        /// <summary>
        /// EmbedSigningUX embeds the signing UX
        /// </summary>
        /// <param name="envData"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public string EmbedSigningUX(envelopeDefinition envData,string returnUrl)
        {
            string accountId = string.Empty;
            string baseURL = string.Empty;
            string envelopeId = string.Empty;
            string uri = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.Accept = "application/xml";
                request.Method = "GET";
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string responseText = sr.ReadToEnd();
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {	// Parse the xml response body
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "accountId"))
                            accountId = reader.ReadString();
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "baseUrl"))
                            baseURL = reader.ReadString();
                    }
                }

         

                //
                // STEP 2 - Request Envelope Result
                //

                // Construct an outgoing XML request body
                

                envData.accountId = accountId;
                string requestBody = Helper.GetXMLFromObject(envData);

                // append "/envelopes" to baseUrl and use in the request
                request = (HttpWebRequest)WebRequest.Create(baseURL + "/envelopes");
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.ContentLength = requestBody.Length;
                request.Method = "POST";
                // write the body of the request
                byte[] body = System.Text.Encoding.UTF8.GetBytes(requestBody);
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(body, 0, requestBody.Length);
                dataStream.Close();
                // read the response
                webResponse = (HttpWebResponse)request.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();
                using (XmlReader reader = XmlReader.Create(new StringReader(responseText)))
                {
                    while (reader.Read())
                    {	// Parse the xml response body
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "envelopeId"))
                            envelopeId = reader.ReadString();
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "uri"))
                            uri = reader.ReadString();
                    }
                }

               

                //
                // STEP 3 - Get the Embedded Console Sign View
                //

                // construct another outgoing XML request body
                string reqBody = "<recipientViewRequest xmlns=\"http://www.docusign.com/restapi\">" +
                    "<authenticationMethod>email</authenticationMethod>" +
                        "<email>" + Username + "</email>" +	 	// NOTE: Use different email address if username provided in non-email format!
                        "<returnUrl>" + returnUrl + "</returnUrl>" +  // username can be in email format or an actual ID string
                        "<clientUserId>1</clientUserId>" +
                        "<userName>Name</userName>" +
                        "</recipientViewRequest>";

                // append uri + "/views/recipient" to baseUrl and use in the request
                request = (HttpWebRequest)WebRequest.Create(baseURL + uri + "/views/recipient");
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.ContentLength = reqBody.Length;
                request.Method = "POST";
                // write the body of the request
                byte[] body2 = System.Text.Encoding.UTF8.GetBytes(reqBody);
                Stream dataStream2 = request.GetRequestStream();
                dataStream2.Write(body2, 0, reqBody.Length);
                dataStream2.Close();
                // read the response
                webResponse = (HttpWebResponse)request.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                responseText = sr.ReadToEnd();
                return responseText;
            }
            catch (WebException e)
            {
                return handleWebException<string>(e);
            }
        }

        /// <summary>
        /// EmbeddedDocusignConsoleView provides ah Embedded Console View
        /// </summary>
        /// <returns></returns>
        public string EmbeddedDocusignConsoleView()
        {
            try
            {
                // 
                // STEP 1 - Login
                //

                var loginResponse = Login();


                //
                // STEP 2 - Launch the DocuSign Console in an authenticated view.
                //

                // Construct an outgoing XML request body
                StringBuilder xml = new StringBuilder();
                xml.Append("<consoleViewRequest xmlns=\"http://www.docusign.com/restapi\">");
                xml.Append("<accountId>" + loginResponse.AccountId + "</accountId>");
                xml.Append("</consoleViewRequest>");

                // append "/views/console" to baseUrl and use in the request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(loginResponse.BaseUrl + "/views/console");
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.ContentLength = xml.ToString().Length;
                request.Method = "POST";
                // write the body of the request
                byte[] body = System.Text.Encoding.UTF8.GetBytes(xml.ToString());
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(body, 0, xml.ToString().Length);
                dataStream.Close();
                // read the response
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string responseText = sr.ReadToEnd();
                return responseText;
            }
            catch (WebException e)
            {
                return handleWebException<string>(e);
            }
        }

        /// <summary>
        /// EmbeddedDocusignEnvelopeView provides an Embedded Envelope View of correct or sender
        /// </summary>
        /// <returns></returns>
        public Response EmbeddedDocusignEnvelopeView(EmbeddedDocusignEnvelopeViews view, string envelopeId, string returnUrl = null)
        {
            try
            {
                // 
                // STEP 1 - Login
                //

                var loginResponse = Login();


                // 
                // STEP 2 - 
                //

                // Construct an outgoing XML request body
                StringBuilder xml = new StringBuilder();
                xml.Append("<returnUrlRequest xmlns=\"http://www.docusign.com/restapi\">");
                xml.Append("<accountId>" + loginResponse.AccountId + "</accountId>");
                xml.Append("<envelopeId>" + envelopeId + "</envelopeId>");
                xml.Append("<returnUrl>" + "www.docusign.com" + "</returnUrl>");
                xml.Append("</returnUrlRequest>");

                // append "/envelopes/[envelopeId]/views/" to baseUrl and use in the request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/envelopes/{1}/views/{2}", loginResponse.BaseUrl, envelopeId, view.ToString()));
                request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.ContentLength = xml.ToString().Length;
                request.Method = "POST";
                // write the body of the request
                byte[] body = System.Text.Encoding.UTF8.GetBytes(xml.ToString());
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(body, 0, xml.ToString().Length);
                dataStream.Close();
                logHandler.LogRequestLoggingInfo(request, xml.ToString());
                // read the response
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string responseText = sr.ReadToEnd();
                logHandler.LogResponseLoggingInfo(webResponse, responseText);

                return processEnvelopeViewResponse(responseText, webResponse.StatusCode.ToString());
            }
            catch (WebException e)
            {
                return handleWebException<Response>(e);
            }
        }

        /// <summary>
        /// Create an envelopeDefinition of signatureStatus created with one document and one signer.
        /// </summary>
        /// <param name="emailSubject"></param>
        /// <param name="documentName"></param>
        /// <param name="emailAddress"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public envelopeDefinition CreateEnvelopeDraft(string emailSubject, string emailAddress, string name, string documentName)
        {
            var docDetail = new DocDetail()
                {
                    document = new Document()
                    {
                        documentId = "1",
                        name = documentName
                    }
                };

            //var tabs = new Tab()
            //{
            //    signHereTabs = new SignHereTab()
            //    {
            //        signHere = new SignHere()
            //        {
            //            documentId = "1",
            //            pageNumber = "1",
            //            xPosition = "100",
            //            yPosition = "100"
            //        }
            //    }
            //};
            var signers = new List<signer>()
            {
                new signer()
                {
                    recipientId = "1",
                    email = emailAddress,
                    name = name,
                    tabs = null
                                
                }
            };

            return CreateEnvelopeDefinition(emailSubject, signatureStatus.created, docDetail, signers);
        }

        /// <summary>
        /// Helps piece together an Envelope Definition
        /// </summary>
        /// <param name="emailSubject"></param>
        /// <param name="signatureStatus"></param>
        /// <param name="docDetail"></param>
        /// <param name="signers"></param>
        /// <returns></returns>
        public envelopeDefinition CreateEnvelopeDefinition(string emailSubject, signatureStatus signatureStatus, DocDetail docDetail, List<signer> signers)
        {
            var envelope = new envelopeDefinition()
            {
                emailSubject = emailSubject,
                status = signatureStatus,    // "sent" to send immediately, "created" to save as draft in your account
                documents = docDetail,
                recipients = new Recipients()
                {
                    signers = signers
                }
            };

            return envelope;
        }

        private T handleWebException<T>(WebException e)
        {
            string statusCode;
            using (WebResponse response = e.Response)
            {
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                //errorMessage = "Error code:: " + httpResponse.StatusCode;
                statusCode = httpResponse.StatusCode.ToString();
                using (Stream data = response.GetResponseStream())
                {
                    string text = new StreamReader(data).ReadToEnd();
                    logHandler.LogResponseLoggingInfo(httpResponse, text);
                    errorMessage += text;
                }
            }
            if (typeof(T) != typeof(string))
                return (T)((object)processErrorResponse(errorMessage, statusCode));
            else
                return (T)((object)errorMessage);
        }

        private Response processErrorResponse(string xml, string webResponseStatusCode)
        {
            var response = new Response() { WebResponseStatusCode = webResponseStatusCode };

            //Parse resulting xml
            XDocument resultXDoc = XDocument.Parse(xml);
            var namespaceName = resultXDoc.Root.Name.NamespaceName;

            var errorDetailsXEl = resultXDoc.Descendants(XName.Get("errorDetails", namespaceName));
            if (errorDetailsXEl.Any())
            {
                //Parse error elements
                response.Success = false;
                response.ErrorMessage = getXDocValue(resultXDoc, "message", "There was an unknown error sending the PDF to DocuSign.");
                response.ErrorCode = getXDocValue(resultXDoc, "errorCode");

            }
            return response;
        }

        private EnvelopeViewResponse processEnvelopeViewResponse(string xml, string webResponseStatusCode)
        {
            var response = new EnvelopeViewResponse() { WebResponseStatusCode = webResponseStatusCode };

            XDocument resultXDoc = XDocument.Parse(xml);
            var namespaceName = resultXDoc.Root.Name.NamespaceName;
            
            response.Success = true;
            response.Url = getXDocValue(resultXDoc, "url");

            return response;
        }

        /// <summary>
        /// Process the HttpWebResponse xml
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="webResponseStatusCode"></param>
        /// <returns></returns>
        private RequestSignatureResponse processSignatureResponse(string xml, string webResponseStatusCode)
        {
            var response = new RequestSignatureResponse() { WebResponseStatusCode = webResponseStatusCode };

            //Parse resulting xml
            XDocument resultXDoc = XDocument.Parse(xml);
            var namespaceName = resultXDoc.Root.Name.NamespaceName;

            //This is success xml. Parse success elements
            response.Success = true;
            response.EnvelopeId = getXDocValue(resultXDoc, "envelopeId");
            response.Status = stringToEnum<signatureStatus>(getXDocValue(resultXDoc, "status"));
            DateTime statusDate;
            DateTime.TryParse(getXDocValue(resultXDoc, "statusDateTime"), out statusDate);
            response.StatusDateTime = statusDate;
            response.Uri = getXDocValue(resultXDoc, "uri");

            return response;
        }

        private string getXDocValue(XDocument xDoc, string elementName, string defaultValue = null)
        {
            var elementValue = defaultValue;

            var messageXEl = xDoc.Descendants(XName.Get(elementName, xDoc.Root.Name.NamespaceName));
            if (messageXEl.Any())
                elementValue = messageXEl.First().Value;

            return elementValue;
        }


        private T stringToEnum<T>(string enumString)
        {
            return (T)Enum.Parse(typeof(T), enumString);
        }

    }

    public enum EmbeddedDocusignEnvelopeViews
    {
        correct,
        sender,
    }
}
