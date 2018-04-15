using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Zetalex.TableList.Example.Mvc.Models;

namespace TableList.Example.Mvc.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var model = new ProductModel
            {
                ID = 1,
                Initial = (decimal)547,
                BlockPrices = new List<ProductBlockPrice>
                {
                    new ProductBlockPrice { Initial = 600, CountFrom = 30 },
                    new ProductBlockPrice { Initial = 400, CountFrom = 10, Default = true }
                }
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Index(ProductModel model)
        {
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}