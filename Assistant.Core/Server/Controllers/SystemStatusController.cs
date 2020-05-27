using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Assistant.Core.Server.Controllers {
    public class SystemStatusController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
