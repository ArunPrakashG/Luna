using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Luna.Gpio;
using Microsoft.AspNetCore.Mvc;

namespace Luna.Server.Controllers
{
    public class GpioController : Controller
    {
		public GpioController() {			
		}

        public IActionResult Index()
        {
            return View();
        }
    }
}
