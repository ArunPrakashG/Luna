using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Assistant.Extensions.Attributes {
	public class PreventSpamAttribute : ActionFilterAttribute {

		// This stores the time between Requests (in seconds)
		public int DelayRequest = 10;
		// The Error Message that will be displayed in case of 
		// excessive Requests
		public string ErrorMessage = "Excessive Request Attempts Detected.";
		// This will store the URL to Redirect errors to
		public string RedirectURL { get; set; }

		public override void OnActionExecuting(ActionExecutingContext filterContext) {
			
			base.OnActionExecuting(filterContext);
		}
	}
}
