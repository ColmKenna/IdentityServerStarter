using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("product")]
[Authorize]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyConstants.CanViewProductsScope)]
    public IActionResult Get()
    {
        return new JsonResult("Product List");
    }
    
    [HttpGet("{id}")]
    [Authorize(Policy = PolicyConstants.CanViewProductsScope)]
    public IActionResult Get(int id)
    {
        return new JsonResult($"Product {id}");
    }
    
    [HttpPost]
    [Authorize(Policy = PolicyConstants.CanAmendProductScope)]
    public IActionResult Post()
    {
        return new JsonResult("Product Created");
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = PolicyConstants.CanAmendProductScope)]
    public IActionResult Put(int id)
    {
        return new JsonResult($"Product {id} Updated");
    }
    
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyConstants.CanAmendProductScope)]
    public IActionResult Delete(int id)
    {
        return new JsonResult($"Product {id} Deleted");
    }
}