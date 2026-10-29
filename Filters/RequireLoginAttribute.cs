using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BloodConnect.Filters;

/// <summary>
/// Simple session-based guard. Actions marked with [RequireLogin] redirect to the
/// login page unless the admin has signed in (state stored in Session).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (string.IsNullOrEmpty(context.HttpContext.Session.GetString("Admin")))
        {
            // Only GET pages can be returned to after login (POST-only URLs have no GET page)
            var req = context.HttpContext.Request;
            var returnUrl = HttpMethods.IsGet(req.Method) ? req.Path + req.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }
        base.OnActionExecuting(context);
    }
}
