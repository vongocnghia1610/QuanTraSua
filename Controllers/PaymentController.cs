using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Doan1.Controllers
{
    public class PaymentController : Controller
    {
        // GET: Paypal
        public ActionResult Index()
        {
            return View();
        }
    }
}