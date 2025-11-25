using Contoso.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Refit;
using System.ComponentModel.DataAnnotations;
using Contoso.WebApp.Extensions;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
 
namespace Contoso.WebApp.Pages.Account;
 
public class LoginModel : PageModel
{
    
 
    public IActionResult OnGet()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToPage("/Home/Home");
        }
        else
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Home/Home")
            }, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
 
}