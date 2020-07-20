using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Luna.Core.Server.Controllers {
	public class DashboardController : Controller {
		private readonly ILogger<DashboardController> _logger;

		public DashboardController(ILogger<DashboardController> logger) {
			_logger = logger;			
		}

		public IActionResult Index() {
			return View();
		}
	}
}
