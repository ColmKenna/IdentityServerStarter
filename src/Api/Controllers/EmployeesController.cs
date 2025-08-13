using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("employee")]
[Authorize]
public class EmployeesController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyConstants.CanViewEmployeesScope)]
    public IActionResult Get()
    {
        return new JsonResult("Employee List");
    }
    
    [HttpGet("{id}")]
    [Authorize(Policy = PolicyConstants.CanViewEmployeesScope)]
    public IActionResult Get(int id)
    {
        return new JsonResult($"Employee {id}");
    }
    
    [HttpPost]
    [Authorize(Policy = PolicyConstants.CanAmendEmployeeScope)]
    public IActionResult Post()
    {
        return new JsonResult("Employee Created");
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = PolicyConstants.CanAmendEmployeeScope)]
    public IActionResult Put(int id)
    {
        return new JsonResult($"Employee {id} Updated");
    }
    
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyConstants.CanAmendEmployeeScope)]
    public IActionResult Delete(int id)
    {
        return new JsonResult($"Employee {id} Deleted");
    }
}