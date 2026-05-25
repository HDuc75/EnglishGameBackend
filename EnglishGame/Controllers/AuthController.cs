
using EnglishGame.Dtos;
using EnglishGame.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnglishGame.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        try { return Ok(await _auth.RegisterAsync(req)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        try { return Ok(await _auth.LoginAsync(req)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }



    [HttpPost("forgot-password")]
    public async Task<ActionResult<object>> ForgotPassword(ForgotPasswordRequest req)
    {
        try { return Ok(await _auth.ForgotPasswordAsync(req)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<object>> ResetPassword(ResetPasswordRequest req)
    {
        try { return Ok(await _auth.ResetPasswordAsync(req)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

}
