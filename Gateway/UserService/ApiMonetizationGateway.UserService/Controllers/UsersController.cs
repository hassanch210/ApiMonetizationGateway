using Microsoft.AspNetCore.Mvc;
using ApiMonetizationGateway.Shared.DTOs;
using ApiMonetizationGateway.UserService.Services;

namespace ApiMonetizationGateway.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        
        if (result.Success)
            return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result);
        
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto?>>> GetUser(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        
        if (result.Success && result.Data == null)
            return NotFound();
        
        return Ok(result);
    }

    // Removed API key lookup endpoint

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<ApiResponse<UserDto?>>> GetUserByEmail(string email)
    {
        var result = await _userService.GetUserByEmailAsync(email);
        
        if (result.Success && result.Data == null)
            return NotFound();
        
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers()
    {
        var result = await _userService.GetAllUsersAsync();
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] CreateUserRequest request)
    {
        var result = await _userService.UpdateUserAsync(id, request);
        
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.Contains("not found")) == true)
                return NotFound(result);
            return BadRequest(result);
        }
        
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateUser(int id)
    {
        var result = await _userService.DeactivateUserAsync(id);
        
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.Contains("not found")) == true)
                return NotFound(result);
            return BadRequest(result);
        }
        
        return Ok(result);
    }

    // Removed API key regeneration endpoint

    // Removed API key validation endpoint
}