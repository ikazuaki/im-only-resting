﻿/*
Copyright 2012 Stephen Swensen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Xml.Linq;
using Newtonsoft.Json;
using TidyManaged;

namespace Swensen.Ior.Core {
    public class ResponseModel {
        public IorContentType ContentType { get; private set;}

        /// <summary>
        /// Create an empty ResponseModel with ResponseStatus set to a loading message.
        /// </summary>
        public ResponseModel(string status="") {
            this.Status = status;
            this.ContentType = new IorContentType();
            initLazyFields();
        }

        /// <summary>
        /// Create a ResponseModel populated from an IRestResonse
        /// </summary>
        public ResponseModel(HttpResponseMessage response, DateTime start, DateTime end) {
            if (response == null)
                throw new ArgumentNullException("response");

                                 
            //response.ReasonPhrase
            //response.StatusCode

            this.Status = string.Format("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);

            if (start != null && end != null) {
                Start = start;
                End = end;
                ElapsedTime = (end - start).Milliseconds + " ms";
            }

            var readContentBytesTask = response.Content.ReadAsByteArrayAsync();
            readContentBytesTask.Wait();
            this.ContentBytes = readContentBytesTask.Result;

            var readContentStringTask = response.Content.ReadAsStringAsync();
            readContentStringTask.Wait();
            this.Content = readContentStringTask.Result;

            //note: not sure why response headers is String -> IEnumerable<String> map instead of just String -> String
            var contentType = response.Headers.FirstOrDefault(x => x.Key.ToUpper() == "CONTENT-TYPE").Value.FirstOrDefault();
            this.ContentType = new IorContentType(contentType);

            this.Headers = String.Join(Environment.NewLine, response.Headers.Select(p => p.Key + ": " + p.Value.FirstOrDefault()));

            //todo: either get rid of this (left over from RestSharp), or make some good use of it (i.e. exception messages).
            this.ErrorMessage = null;// response.ErrorMessage;

            initLazyFields();            
        }

        private void initLazyFields() {
            this.prettyPrintedContent = new Lazy<string>(() => IorContentType.GetPrettyPrintedContent(this.ContentType.MediaTypeCategory, this.Content));
            this.contentFileExtension = new Lazy<string>(() => IorContentType.GetFileExtension(this.ContentType.MediaTypeCategory, this.ContentType.MediaType));
            this.temporaryFile = new Lazy<string>(() => IorContentType.GetTemporaryFile(this.ContentBytes, this.ContentFileExtension));
        }

        public string ErrorMessage { get; private set; }

        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public string ElapsedTime { get; private set; }

        public string Status { get; private set; }

        public byte[] ContentBytes { get; private set; }
        public string Content { get; private set; }
        public string Headers { get; set; }

        private Lazy<string> prettyPrintedContent;
        public string PrettyPrintedContent { get { return prettyPrintedContent.Value; } }

        private Lazy<string> contentFileExtension;
        public string ContentFileExtension { get { return contentFileExtension.Value; } }

        private Lazy<string> temporaryFile;
        public string TemporaryFile { get { return temporaryFile.Value; } }

        public static ResponseModel Loading = new ResponseModel(status:"Loading...");
        public static ResponseModel Empty = new ResponseModel();
    }
}
