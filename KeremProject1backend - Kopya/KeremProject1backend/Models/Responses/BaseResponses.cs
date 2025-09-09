using System.Text.Json.Serialization;
using System.Xml;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using System.Collections.Generic;
using System;

namespace KeremProject1backend.Models.Responses
{
    public class BaseResponse
    {
        public object? Response { get; set; } = null;
        public long ReturnValue { get; set; } = 0;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool Errored { get; set; } = false;
        [JsonIgnore]
        public int _userid = 0;
        public int SetUserID(int input) => _userid = input;
     
        public BaseResponse GenerateError(int errorcode, string errorMessage)
        {
            ReturnValue = errorcode;
            ErrorMessage = errorMessage;
            Errored = true;
            return this;
        }

        public BaseResponse GenerateSuccess(long returnvalue, string Message)
        {
            ReturnValue = returnvalue;
            ErrorMessage = Message;
            return this;
        }
        public BaseResponse GenerateSuccess(string Message)
        {
            ErrorMessage = Message;
            return this;
        }
        public BaseResponse GenerateSuccess()
        {
            return this;
        }

    }
    public class DummyNULLResponse
    {

    }
    public class SessionInfos
    {
        public long LastAccessTime { get; set; }
        public int Userid { get; set; }
        public DateTime LastAcsessRealTime { get; set; }
    }

    public class ErrorDetails
    {
        public int Code { get; set; }
        public string Desc { get; set; } = string.Empty;
    }

    public class Err
    {
        public int Id { get; set; }
        public string? Desc { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

}
