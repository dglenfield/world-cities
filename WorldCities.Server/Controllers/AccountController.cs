using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using WorldCities.Server.Data;
using WorldCities.Server.Data.Models;

namespace WorldCities.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly JwtHandler _jwtHandler;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(ApplicationDbContext context, JwtHandler jwtHandler, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _jwtHandler = jwtHandler;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Login(ApiLoginRequest loginRequest)
    {
        var user = await _userManager.FindByNameAsync(loginRequest.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password)) 
        {
            return Unauthorized(new ApiLoginResult()
            {
                Success = false,
                Message = "Invalid Email or Password."
            });
        }

        var secToken = await _jwtHandler.GetTokenAsync(user);
        var jwt = new JwtSecurityTokenHandler().WriteToken(secToken);
        return Ok(new ApiLoginResult()
        {
            Success = true,
            Message = "Login successful",
            Token = jwt
        });
    }
}
