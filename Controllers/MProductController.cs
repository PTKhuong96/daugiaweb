﻿using AuctionWeb.Filters;
using AuctionWeb.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AuctionWeb.Helpers;

namespace AuctionWeb.Controllers
{
    public class MProductController : Controller
    {
        // GET: MProduct
        [CheckLogin(RequiredPermission = 1)]
        public ActionResult Index()
        {
            return View();
        }

        // GET: MProduct/Add
        [CheckLogin(RequiredPermission = 1)]
        public ActionResult Add()
        {
            using (var ctx = new AuctionSiteDBEntities())
            {
                var list = ctx.Categories.ToList();
                ViewBag.Categories = list;
            }

            return View();
        }
        
        [HttpPost]
        [CheckLogin(RequiredPermission = 1)]
        public ActionResult Add(Product vm, HttpPostedFileBase Firstimg, HttpPostedFileBase Secondimg
            , HttpPostedFileBase Thirdimg)
        {
            using (var ctx = new AuctionSiteDBEntities())
            {
                vm.CurrentPrice = null;
                vm.HighestKeeper = null;
                vm.EvaluationPoint = null;
                vm.TimePost = DateTime.Now;
                ctx.Products.Add(vm);
                ctx.SaveChanges();

                if(Firstimg != null && Firstimg.ContentLength > 0 && (Secondimg != null && Secondimg.ContentLength > 0) &&
                    (Thirdimg != null && Thirdimg.ContentLength > 0))
                {
                    string spDirPath = Server.MapPath("~/Img/products");
                    string targetDirPath = Path.Combine(spDirPath, vm.ID.ToString());
                    Directory.CreateDirectory(targetDirPath);

                    string mainFileName = Path.Combine(targetDirPath, "main.jpg");
                    Firstimg.SaveAs(mainFileName);

                    string mainFileName2 = Path.Combine(targetDirPath, "main2.jpg");
                    Secondimg.SaveAs(mainFileName2);

                    string mainFileName3 = Path.Combine(targetDirPath, "main3.jpg");
                    Thirdimg.SaveAs(mainFileName3);
                }

                var list = ctx.Categories.ToList();
                ViewBag.Categories = list;
            }

            return View();
        }

        // GET: MProduct/InTime
        [CheckLogin(RequiredPermission = 1)]
        public ActionResult InTime()
        {
            using (var ctx = new AuctionSiteDBEntities())
            {
                int Iduser = CurrentContext.GetCurUser().ID;
                var list = ctx.Products.Where(p => ((p.UserID == Iduser) 
                && (DateTime.Now <= EntityFunctions.AddDays(p.TimePost, p.IntervalTime))))
                .ToList();
                return View(list);
            }          
        }

        // GET: MProduct/WatchedList
        public ActionResult WatchList()
        {
            using (var ctx = new AuctionSiteDBEntities())
            {
                var iduser = CurrentContext.GetCurUser().ID;
                var list = (from p in ctx.Products
                            join f in ctx.FavoriteProducts
                            on p.ID equals f.IDProducts
                            where f.IDUsers == iduser
                            select new ProductVM
                            {
                                ID = p.ID,
                                IDCat = p.IDCat,
                                Name = p.Name,
                                Description = p.Description,
                                StartPrice = p.StartPrice,
                                StepPrice = p.StepPrice,
                                EndPrice = p.EndPrice,
                                IntervalTime = p.IntervalTime,
                                ExtendTime = p.ExtendTime,
                                EvaluationPoint = p.EvaluationPoint,
                                HighestKeeper = p.HighestKeeper,
                                TimePost = p.TimePost,
                                CurrentPrice = p.CurrentPrice,
                                UserID = p.UserID,
                                Bought = p.Bought,
                            }).ToList();

                return View(list);
            }
        }

        // POST: MProduct/WatchedList
        [HttpPost]
        public ActionResult WatchList(Product vm)
        {           
            using (var ctx = new AuctionSiteDBEntities())
            {
                var iduser = CurrentContext.GetCurUser().ID;
                var fr = new FavoriteProduct()
                {
                    IDProducts = vm.ID,
                    IDUsers = iduser,
                    
                };

                ctx.FavoriteProducts.Add(fr);
                ctx.SaveChanges();
            }
            return RedirectToAction("Index","Home");
        }
    }
}