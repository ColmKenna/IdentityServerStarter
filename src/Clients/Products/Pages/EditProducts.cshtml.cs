using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Products.Pages;

[Authorize(Policy = "CanAmendProducts")]
public class EditProductsModel : PageModel
{
    [BindProperty]
    [Required]
    [Display(Name = "Product name")]
    public string ProductName { get; set; } = "Road Bike";

    [BindProperty]
    [Range(0.01, 100000)]
    public decimal Price { get; set; } = 1299.00m;

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        StatusMessage = $"Saved {ProductName} at {Price:C}.";
        return RedirectToPage();
    }
}