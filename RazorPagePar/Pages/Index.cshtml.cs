using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace RazorPagePar.Pages;

[Authorize]
public class IndexModel : PageModel
{
    public class Person
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }

    public void OnGet()
    {
    }
}