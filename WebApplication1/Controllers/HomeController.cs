using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private const string Path = @"\Doc\personel.json";
        private const string SuccessText = "Hurraaa bak sana tabiki Sercan şaka şaka {0} çıktı hadi koş çizime :)";
        private const string NobodyExists = "Kimse kalmamışki";
        private const string UserNotExists = "Nasıl yaa seni bulamadım";
        
        private static readonly Lazy<List<Personel>> People = new Lazy<List<Personel>>(() =>
        {
            var fullPath= Directory.GetCurrentDirectory() + Path;
            var document = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Personel>>(
                System.IO.File.ReadAllText(fullPath));

            var dict = new List<Personel>();

            if (document != null)
            {
                foreach (var re in document)
                {
                    dict.Add(re);
                }   
            }

            return dict;
        });

        public IActionResult Index()
        {
            return View();
        }

        private static readonly object Locker = new Object();

        [HttpPost]
        public IActionResult Raffle(Personel personItem)
        {
            var person = People.Value.FirstOrDefault(x => x.Email == personItem.Email);

            if (person != null)
            {
                ViewData["Personel"] = person;
                ViewData["Personels"] = People.Value.ToList();
                return View(personItem);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Raffle()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        [HttpPost("select/person")]
        public IActionResult Select(Personel personel)
        {
            string result;
            var fullPath = Directory.GetCurrentDirectory() + Path;

            lock (Locker)
            {
                var selectedPerson = People.Value.FirstOrDefault(x => x.Email == personel.Email);

                if (selectedPerson != null)
                {
                    var availablePerson = People.Value.Where(x =>
                            People.Value.ToList().All(y => y.RelatedPersonel != x.Email) && x.Email != selectedPerson.Email)
                        .ToList();

                    if (availablePerson.Any())
                    {
                        string name;
                        var random = new Random();
                        var index = random.Next(0, availablePerson.Count);
                        if (availablePerson.Count == 2)
                        {
                            var item = People.Value.First(y => !y.BlackList && y.Email != selectedPerson.Email);
                            var selected = availablePerson.FirstOrDefault(x => x.Email == item.Email) ??
                                           availablePerson[index];
                            selectedPerson.RelatedPersonel = selected.Email;
                            name = selected.Name;
                        }
                        else
                        {
                            selectedPerson.RelatedPersonel = availablePerson[index].Email;
                            name = availablePerson[index].Name;
                        }

                        selectedPerson.BlackList = true;
                        System.IO.File.WriteAllText(fullPath,
                            Newtonsoft.Json.JsonConvert.SerializeObject(People.Value.ToList()));
                        result = string.Format(SuccessText,name);
                    }
                    else
                    {
                        result = NobodyExists;
                    }
                }
                else
                {
                    result = UserNotExists;
                }
            }

            ViewData["Result"] = result;
            return View("Selected");
            //return View(result);
        }
    }
}