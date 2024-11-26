using Microsoft.AspNetCore.Mvc.ModelBinding;
using ModelStateDictionary = Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary;

namespace WebApi.MinimalApi;

public static class Extensions
{
    public static bool IsInvalid(this ModelStateDictionary modelState, string key)
    {
        return modelState.GetValidationState(key) == ModelValidationState.Invalid;
    }
}