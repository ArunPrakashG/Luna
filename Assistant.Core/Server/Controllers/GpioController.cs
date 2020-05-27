using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Assistant.Gpio;
using Microsoft.AspNetCore.Mvc;

namespace Assistant.Core.Server.Controllers
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
