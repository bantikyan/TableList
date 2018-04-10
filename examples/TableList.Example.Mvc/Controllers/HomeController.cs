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
                //Options = new List<InnerModel>
                //             {
                //                 new InnerModel { ID = 1, FirstName = "item1", LastName = "last1", TL_AllowDelete = false },
                //                 new InnerModel { ID = 2, FirstName = "item2", LastName = "last2", TL_AllowModify = false },
                //                 new InnerModel { ID = 3, FirstName = "item3", LastName = "last3", BirthDate = new DateTime(1988, 11, 30) },
                //             },
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