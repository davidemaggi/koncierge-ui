using Koncierge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeActionResultDto
    {


        public KonciergeActionResult Result { get; set; } = KonciergeActionResult.PENDING;
        public string Message { get; set; } = "";



        public bool IsOk()=> new List<KonciergeActionResult>() { KonciergeActionResult.WARNING, KonciergeActionResult.SUCCESS }.Contains(Result);
        public bool IsSuccess()=> new List<KonciergeActionResult>() { KonciergeActionResult.SUCCESS }.Contains(Result);
        public bool HasFailure()=> new List<KonciergeActionResult>() { KonciergeActionResult.FAILURE }.Contains(Result);

        public static KonciergeActionResultDto Fail(string msg) => new KonciergeActionResultDto { Message = msg,  Result = KonciergeActionResult.FAILURE };
        public static KonciergeActionResultDto Success(string msg) => new KonciergeActionResultDto { Message = msg ?? "", Result = KonciergeActionResult.SUCCESS };
        public static KonciergeActionResultDto Success() => new KonciergeActionResultDto {  Result = KonciergeActionResult.SUCCESS };
        public static KonciergeActionResultDto Warning(string msg) => new KonciergeActionResultDto { Message = msg,  Result = KonciergeActionResult.WARNING };



    }
    public class KonciergeActionDataResultDto<T>: KonciergeActionResultDto
    {


        public T? Data { get; set; }


        public static KonciergeActionDataResultDto<T> Fail(string msg, T? data) => new KonciergeActionDataResultDto<T> { Message = msg, Data = data, Result=KonciergeActionResult.FAILURE };
        public static KonciergeActionDataResultDto<T> Success(T data, string msg) => new KonciergeActionDataResultDto<T> { Message = msg??"", Data = data, Result=KonciergeActionResult.SUCCESS };
        public static KonciergeActionDataResultDto<T> Success(T data) => new KonciergeActionDataResultDto<T> {  Data = data, Result=KonciergeActionResult.SUCCESS };
        public static KonciergeActionDataResultDto<T> Warning(string msg, T? data) => new KonciergeActionDataResultDto<T> { Message = msg, Data = data, Result=KonciergeActionResult.WARNING };

        
    }

}
