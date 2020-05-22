using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Assistant.Gpio;
using Microsoft.AspNetCore.Mvc;

namespace Assistant.Web.Controllers
{
    public class GpioController : Controller
    {
		private readonly GpioCore GpioCore;

		public GpioController(GpioCore _gpioCore) {
			GpioCore = _gpioCore;
			Debug.WriteLine($"{nameof(_gpioCore)} assigned!");
		}

        public IActionResult Index()
        {
            return View();
        }
    }
}
