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

            HttpWebRequest request = initializeRequest(Url, restVerb.GET);
            string response = getResponseBody(request);

            //Parse response xml to object
            XDocument xDoc = XDocument.Parse(response);
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
        public Response RequestSignatureFromEnvelope(envelopeDefinition requestData)
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
                // append "/envelopes" to baseURL and use in the request
                requestData.accountId = loginResponse.AccountId;
                string requestBody = Helper.GetXMLFromObject(requestData);
                string url = loginResponse.BaseUrl + "/envelopes";
                HttpWebRequest request = initializeRequest(url, restVerb.POST, requestBody);

                // read the response
                string responseStatusCode;
                string response = getResponseBody(request, out responseStatusCode);

                return processSignatureResponse(response, responseStatusCode);
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
                string url = loginResponse.BaseUrl + "/envelopes";
                string envDef = Helper.GetXMLFromObject(requestData);
                HttpWebRequest request = initializeRequest(url, restVerb.POST, envDef, file);

                // read the response
                string responseStatusCode;
                string response = getResponseBody(request, out responseStatusCode);

                return processSignatureResponse(response, responseStatusCode);
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
                string url = loginResponse.BaseUrl + "/envelopes/" + envelopeId;
                HttpWebRequest request = initializeRequest(url, restVerb.GET);

                // read the response
                string response = getResponseBody(request);

                return response;
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
            try
            {
                // 
                // STEP 1 - Login
                //
                var loginResponse = Login();


                //
                // STEP 2 - Get Envelope Recipient Info
                //
                // append "/envelopes/" + envelopeId + "/recipients" to baseUrl and use in the request
                string url = loginResponse.BaseUrl + "/envelopes/" + envelopeId + "/recipients?include_tabs=" + includeTab;
                HttpWebRequest request = initializeRequest(url, restVerb.GET);

                // read the response
                string response = getResponseBody(request);

                return response;
               
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
            try
            {
                // 
                // STEP 1 - Login
                //
                var loginResponse = Login();


                //
                // STEP 2 - Get Envelope Status(es)
                //
                // Append "/envelopes" and query string to baseUrl and use in the request
                string url = loginResponse.BaseUrl + "/envelopes?from_date=" + date + "&from_to_status=" + status;
                HttpWebRequest request = initializeRequest(url, restVerb.GET);

                // read the response
                string response = getResponseBody(request);

                return response;
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
            List<string> uriList = new List<string>();

            try
            {
                // 
                // STEP 1 - Login
                //
                var loginResponse = Login();
               

                //
                // STEP 2 - Get Envelope Document List
                //
                // append docListUri to the baseUrl and use in the request
                string url = loginResponse.BaseUrl + "/envelopes/" + envelopeId + "/documents";
                HttpWebRequest request = initializeRequest(url, restVerb.GET);

                // read the response
                string responseText = getResponseBody(request);

                // grab the document uris
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
                        request = (HttpWebRequest)WebRequest.Create(loginResponse.BaseUrl + uri);
                        request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
                        request.Accept = "application/pdf";
                        request.Method = "GET";
                        // read the response
                        HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
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
        public string EmbedSendingUX(envelopeDefinition envData, string returnUrl)
        {
            string envelopeId = string.Empty;
            string uri = string.Empty;
            try
            {
                // 
                // STEP 1 - Login
                //
                var loginResponse = Login();


                //
                // STEP 2 - Create an Envelope with an Embedded Recipient
                //
                // Construct an outgoing XML request body
                envData.accountId = loginResponse.AccountId;
                string requestBody = Helper.GetXMLFromObject(envData);
                // append "/envelopes" to baseUrl and use in the request
                string url = loginResponse.BaseUrl + "/envelopes";
                HttpWebRequest request = initializeRequest(url, restVerb.POST, requestBody);

                // read the response
                string response = getResponseBody(request);
                var xDoc = XDocument.Parse(response);
                envelopeId = getXDocValue(xDoc, "envelopeId");
                uri = getXDocValue(xDoc, "uri");


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
                url = loginResponse.BaseUrl + uri + "/views/sender";
                request = initializeRequest(url, restVerb.POST, reqBody);

                // read the response
                response = getResponseBody(request);

                return response;
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
        public Response EmbedSigningUX(envelopeDefinition envData,string returnUrl)
        {
            string envelopeId = string.Empty;
            string uri = string.Empty;
            try
            {
                // 
                // STEP 1 - Login
                //
                var loginResponse = Login();


                //
                // STEP 2 - Request Envelope Result
                //
                // Construct an outgoing XML request body
                envData.accountId = loginResponse.AccountId;
                string requestBody = Helper.GetXMLFromObject(envData);

                // append "/envelopes" to baseUrl and use in the request
                string url = loginResponse.BaseUrl + "/envelopes";
                HttpWebRequest request = initializeRequest(url, restVerb.POST, requestBody);

                // read the response
                string response = getResponseBody(request);
                var xDoc = XDocument.Parse(response);
                envelopeId = getXDocValue(xDoc, "envelopeId");
                uri = getXDocValue(xDoc, "uri");


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
                url = loginResponse.BaseUrl + uri + "/views/recipient";
                request = initializeRequest(url, restVerb.POST, reqBody);

                // read the response
                response = getResponseBody(request);

                return new RequestSignatureResponse() {
                      EnvelopeId = response  
                };
            }
            catch (WebException e)
            {
                return handleWebException<Response>(e);
            }
        }

        /// <summary>
        /// EmbeddedDocusignConsoleView provides ah Embedded Console View
        /// </summary>
        /// <returns></returns>
        public string EmbeddedConsoleView()
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
        public Response EmbeddedEnvelopeView(DocusignMethods.EmbeddedEnvelopeViews view, string envelopeId)
        {
            if (!(view == EmbeddedEnvelopeViews.sender || view == EmbeddedEnvelopeViews.correct))
                throw new InvalidOperationException("EmbeddedEnvelopeView must be of type sender or correct.");

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
                StringBuilder body = new StringBuilder();
                body.Append("<returnUrlRequest xmlns=\"http://www.docusign.com/restapi\">");
                body.Append("<returnUrl>" + "www.docusign.com" + "</returnUrl>");
                body.Append("</returnUrlRequest>");

                // append "/envelopes/[envelopeId]/views/" to baseUrl and use in the request
                string url = string.Format("{0}/envelopes/{1}/views/{2}", loginResponse.BaseUrl, envelopeId, view.ToString());
                HttpWebRequest request = initializeRequest(url, restVerb.POST, body.ToString());

                // read the response
                string responseStatusCode;
                string response = getResponseBody(request, out responseStatusCode);

                return processEnvelopeViewResponse(response, responseStatusCode);
            }
            catch (WebException e)
            {
                return handleWebException<Response>(e);
            }
        }

        /// <summary>
        /// EmbeddedDocusignEnvelopeView provides an Embedded Envelope View of recipient
        /// </summary>
        /// <returns></returns>
        public Response EmbeddedEnvelopeView(DocusignMethods.EmbeddedEnvelopeViews view, string envelopeId, string clientUserId, string email, string username, string authenticationMethod = "Email", string returnUrl = "http://demo.docusign.com")
        {
            if (view != EmbeddedEnvelopeViews.recipient)
                throw new InvalidOperationException("EmbeddedEnvelopeView must be of type sender or correct.");

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
                StringBuilder body = new StringBuilder();
                body.Append("<recipientViewRequest xmlns=\"http://www.docusign.com/restapi\">");
                body.Append("<authenticationMethod>" + authenticationMethod + "</authenticationMethod>");
                body.Append("<email>" + email + "</email>");
                body.Append("<returnUrl>" + returnUrl + "</returnUrl>");
                body.Append("<clientUserId>" + clientUserId + "</clientUserId>");
                body.Append("<userName>" + username + "</userName>");
                body.Append("</recipientViewRequest>");

                // append "/envelopes/[envelopeId]/views/" to baseUrl and use in the request
                string url = string.Format("{0}/envelopes/{1}/views/{2}", loginResponse.BaseUrl, envelopeId, EmbeddedEnvelopeViews.recipient);
                HttpWebRequest request = initializeRequest(url, restVerb.POST, body.ToString());

                // read the response
                string responseStatusCode;
                string response = getResponseBody(request, out responseStatusCode);

                return processEnvelopeViewResponse(response, responseStatusCode);
            }
            catch (WebException e)
            {
                return handleWebException<Response>(e);
            }
        }
        
        
        //***********************************************************************************************
        // --- HELPER FUNCTIONS ---
        //***********************************************************************************************
        private HttpWebRequest initializeRequest(string url, restVerb method, string body = null, FileInfo file = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method.ToString();
            addRequestHeaders(request);
            if (file != null)
                addRequestBodyFile(request, body, file);
            else if (body != null)
                addRequestBody(request, body);
            logHandler.LogRequestLoggingInfo(request, body);
            return request;
        }

        private void addRequestHeaders(HttpWebRequest request)
        {
            // authentication header can be in JSON or XML format.  XML used for this walkthrough:
            string authenticateStr =
                "<DocuSignCredentials>" +
                    "<Username>" + Username + "</Username>" +
                    "<Password>" + Password + "</Password>" +
                    "<IntegratorKey>" + IntegratorKey + "</IntegratorKey>" +
                    "</DocuSignCredentials>";
            request.Headers.Add("X-DocuSign-Authentication", authenticateStr);
            request.Accept = "application/xml";
            request.ContentType = "application/xml";
        }

        private void addRequestBody(HttpWebRequest request, string requestBody)
        {
            // create byte array out of request body and add to the request object
            request.ContentLength = requestBody.Length;
            byte[] body = System.Text.Encoding.UTF8.GetBytes(requestBody);
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(body, 0, requestBody.Length);
            dataStream.Close();
        }

        private void addRequestBodyFile(HttpWebRequest request, string body, FileInfo file)
        {
                // build the multipart request body
                string requestBodyStart = "\r\n\r\n--BOUNDARY\r\n" +
                        "Content-Type: application/xml\r\n" +
                        "Content-Disposition: form-data\r\n" +
                        "\r\n" +
                        body + "\r\n\r\n--BOUNDARY\r\n" + 	// our xml formatted envelopeDefinition
                        "Content-Type: application/pdf\r\n" +
                        "Content-Disposition: file; filename=\"" + file.Name + "\"; documentId=1\r\n" +
                        "\r\n";

                string requestBodyEnd = "\r\n--BOUNDARY--\r\n\r\n";

                using (FileStream fileStream = File.OpenRead(file.FullName))
                {
                    request.ContentType = "multipart/form-data; boundary=BOUNDARY";
                    request.ContentLength = requestBodyStart.ToString().Length + fileStream.Length + requestBodyEnd.ToString().Length;

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

        }

        private string getResponseBody(HttpWebRequest request)
        {
            string throwAway;
            return getResponseBody(request, out throwAway);
        }

        private string getResponseBody(HttpWebRequest request, out string statusCode)
        {
            // read the response stream into a local string
            HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(webResponse.GetResponseStream());
            string responseText = sr.ReadToEnd();
            logHandler.LogResponseLoggingInfo(webResponse, responseText);
            statusCode = webResponse.StatusCode.ToString();
            return responseText;
        }

        private string parseDataFromResponse(string response, string searchToken)
        {
            // look for "searchToken" in the response body and parse its value
            using (XmlReader reader = XmlReader.Create(new StringReader(response)))
            {
                while (reader.Read())
                {
                    if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == searchToken))
                        return reader.ReadString();
                }
            }
            return null;
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
            var envelope = new envelopeDefinition()
            {
                emailSubject = emailSubject,
                status = signatureStatus.created,    // "sent" to send immediately, "created" to save as draft in your account
                documents = new DocDetail()
                {
                    document = new Document()
                    {
                        documentId = "1",
                        name = documentName
                    }
                },
                recipients = new Recipients()
                {
                    signers = new List<signer>()
                    {
                        new signer()
                        {
                            recipientId = "1",
                            email = emailAddress,
                            name = name,
                            tabs = null                                
                        }
                    }
                }
            };

            return envelope;
        }

        public envelopeDefinition CreateEmbeddedTemplateEnvelopeDraft(string roleOneName, string emailSubject, string roleOneEmail, string roleOneClientUserId, string templateId, List<textTabs> tabs = null)
        {
            var envelope = new envelopeDefinition()
            {
                emailSubject = emailSubject,
                status = signatureStatus.sent,    // "sent" to send immediately, "created" to save as draft in your account
                templateId = templateId,
                templateRoles = new List<templateRole>()
                {
                    new templateRole()
                    {
                        roleName = "RoleOne",
                        name = roleOneName,
                        email = roleOneEmail,
                        clientUserId = roleOneClientUserId, //This property set determines if template is embedded
                        tabs = tabs
                    }
                }

            };

            return envelope;
        }

        public List<textTabs> DictionaryToTextTabs(Dictionary<string, string> dictionary)
        {
            return dictionary.Select(d => new textTabs() { tabLabel = d.Key, value = d.Value }).ToList();
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

        private enum restVerb
        {
            GET,
            PUT,
            POST,
            DELETE
        }

        public enum EmbeddedEnvelopeViews
        {
            correct,
            sender,
            recipient
        }
    }
}
