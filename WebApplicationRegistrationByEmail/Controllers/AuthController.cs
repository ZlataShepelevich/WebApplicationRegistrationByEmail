using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;
using WebApplicationRegistrationByEmail.Services;

namespace WebApplicationRegistrationByEmail.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IStringLocalizer _localizer;
        private readonly MessageQueueService _messageQueueService;
        private readonly IMemoryCache _cache;

        public AuthController(MessageQueueService messageQueueService, IMemoryCache cache, IStringLocalizerFactory localizerFactory)
        {
            _messageQueueService = messageQueueService;
            _cache = cache;
            var type = typeof(Program);
            _localizer = localizerFactory.Create("Messages", type.Assembly.FullName);
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] string email)
        {
            if (!IsValidEmail(email))
            {
                return BadRequest(_localizer["InvalidEmail"]);
            }

            var cacheKey = $"LastSend_{email}";
            if (_cache.TryGetValue(cacheKey, out DateTime lastSend) && DateTime.UtcNow - lastSend < TimeSpan.FromMinutes(1))
            {
                return BadRequest(_localizer["PleaseWait"]);
            }

            var code = new Random().Next(1000, 9999).ToString();
            _messageQueueService.SendEmailMessage(email, code);

            _cache.Set(email, code, TimeSpan.FromMinutes(5));
            _cache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromMinutes(1));

            return Ok(_localizer["VerificationSent"]);
        }

        [HttpPost("verify")]
        public IActionResult VerifyCode([FromBody] VerificationRequest request)
        {
            if (_cache.TryGetValue(request.Email, out string storedCode) && storedCode == request.Code)
            {
                return Ok(_localizer["VerificationSuccess"]);
            }

            return BadRequest(_localizer["VerificationFailed"]);
        }

        private bool IsValidEmail(string email)
        {
            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailRegex, RegexOptions.IgnoreCase);
        }
    }

    public class VerificationRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
