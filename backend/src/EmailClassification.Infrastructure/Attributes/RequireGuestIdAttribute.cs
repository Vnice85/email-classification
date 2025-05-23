using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Attributes
{
    public class RequireGuestIdAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Items.ContainsKey("guestId"))
            {
                context.Result = new BadRequestObjectResult("Missing guestId in header");
            }

        }
    }

}
