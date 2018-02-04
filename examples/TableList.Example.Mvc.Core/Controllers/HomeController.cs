using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TableList.Example.Mvc.Core.Models;
using Zetalex.TableList.Example.Mvc.Core.Models;

namespace TableList.Example.Mvc.Core.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new ProductModel
            {
                ID = 1,
                Initial = (decimal)547,
                BlockPrices = new List<ProductBlockPrice>
                {
                    new ProductBlockPrice { Initial = 600, CountFrom = 30 },
                    new ProductBlockPrice { Initial = 400, CountFrom = 50 },
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

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
