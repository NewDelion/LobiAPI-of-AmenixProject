using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace LobiAPI.Utils
{
    public enum ERROR_TYPE
    {
        /// <summary>
        /// 不明なエラー
        /// </summary>
        UNKNOWN                =   0,
        /// <summary>
        /// リクエストにパラメータが無いか不正
        /// </summary>
        BAD_REQUEST            = 400,
        /// <summary>
        /// tokenが不正
        /// </summary>
        INVALID_TOKEN          = 401,
        /// <summary>
        /// アクセス権限が無い
        /// </summary>
        FORBIDDEN              = 403,
        /// <summary>
        /// リソースが存在しない
        /// </summary>
        NOT_FOUND              = 404,
        /// <summary>
        /// HTTPメソッドが不正
        /// </summary>
        INVALID_HTTP           = 405,
        /// <summary>
        /// サーバに問題がある
        /// </summary>
        INTERNAL_SERVER_ERROR  = 500,
        /// <summary>
        /// サーバに問題がある
        /// </summary>
        BAD_GATEWAY            = 502,
        /// <summary>
        /// APIの利用制限中である
        /// </summary>
        API_LIMITATION         = 503
    }

    public class RequestAPIException : Exception
    {
        public ErrorObject ErrorObj { get; private set; }
        public RequestAPIException(ErrorObject error) : base(string.Join("\r\n", error.Message)) { ErrorObj = error; }
    }

    public class ErrorObject
    {
        protected class ErrorJson
        {
            [JsonProperty("error")]
            public List<string> Messages { get; set; }
        }

        public Uri RequestUri { get; private set; }
        public ERROR_TYPE ErrorType { get; private set; } = ERROR_TYPE.UNKNOWN;
        public HttpStatusCode StatusCode { get; private set; }
        public string Message { get; private set; } = "";

        public ErrorObject(HttpResponseMessage response)
        {
            RequestUri = response.RequestMessage.RequestUri;
            StatusCode = response.StatusCode;
            string content = response.Content.ReadAsStringAsync().Result;
            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    ErrorType = ERROR_TYPE.BAD_REQUEST;
                    Message = content;
                    break;
                case HttpStatusCode.Unauthorized:
                    ErrorType = ERROR_TYPE.INVALID_TOKEN;
                    Message = "tokenが不正";
                    break;
                case HttpStatusCode.Forbidden:
                    ErrorType = ERROR_TYPE.FORBIDDEN;
                    Message = "アクセス権限が無い";
                    break;
                case HttpStatusCode.NotFound:
                    ErrorType = ERROR_TYPE.NOT_FOUND;
                    Message = "リソースが存在しない";
                    break;
                case HttpStatusCode.MethodNotAllowed:
                    ErrorType = ERROR_TYPE.INVALID_HTTP;
                    Message = "HTTPメソッドが不正";
                    break;
                case HttpStatusCode.InternalServerError:
                    ErrorType = ERROR_TYPE.INTERNAL_SERVER_ERROR;
                    Message = "サーバに問題がある";
                    break;
                case HttpStatusCode.BadGateway:
                    ErrorType = ERROR_TYPE.BAD_GATEWAY;
                    Message = "サーバに問題がある";
                    break;
                case HttpStatusCode.ServiceUnavailable:
                    ErrorType = ERROR_TYPE.API_LIMITATION;
                    Message = "APIの利用制限中である";
                    break;
                default:
                    ErrorType = ERROR_TYPE.UNKNOWN;
                    Message = content;
                    break;
            }
        }
    }
}
