using IOU.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace IOU.Web.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IHttpClientFactory clientFactory, ILogger<PaymentController> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }
  

    }
}
