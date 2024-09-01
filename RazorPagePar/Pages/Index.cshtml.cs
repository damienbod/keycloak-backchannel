using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagePar.Pages;

[Authorize]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}