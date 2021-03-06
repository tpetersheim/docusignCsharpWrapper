﻿using DocusignEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocusignEntity
{

    public class Response
    {
        public string WebResponseStatusCode { get; set; }
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RequestSignatureResponse : Response
    {
        public string EnvelopeId { get; set; }
        public signatureStatus Status { get; set; }
        public DateTime StatusDateTime { get; set; }
        public string Uri { get; set; }
    }

    public class EnvelopeViewResponse : Response
    {
        public string Url { get; set; }
    }
}
