using Microsoft.AspNetCore.Mvc;

namespace VentaFacil.web.Controllers
{
    public class BaseController : Controller
    {
        /// <summary>
        /// Sets a flash message to be displayed in the next view using the _Alert partial.
        /// </summary>
        /// <param name="message">The message body.</param>
        /// <param name="type">The type of alert: success, error, warning, info, primary.</param>
        /// <param name="title">The title of the alert (optional).</param>
        protected void SetAlert(string message, string type = "primary", string title = "")
        {
            TempData["AlertMessage"] = message;
            TempData["AlertType"] = type;
            TempData["AlertTitle"] = title;
        }
    }
}
