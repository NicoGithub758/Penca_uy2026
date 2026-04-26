using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Models;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
	[ApiController]
	[Route("api/admin-plataforma")]
	public class AdminAuthController : ControllerBase
	{
		private readonly AuthService _authService;
		public AdminAuthController(AuthService authService) => _authService = authService;

		[HttpPost("login")]
		public IActionResult Login([FromBody] LoginRequest request)
		{
			var token = _authService.LoginPlataforma(request);
			return token == null ? Unauthorized() : Ok(new { Token = token });
		}
	}
}