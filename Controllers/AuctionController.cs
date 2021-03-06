﻿using AuctionWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AuctionWeb.Models;
using AuctionWeb.Helpers;
using System.Text;

namespace AuctionWeb.Controllers
{
    [CheckLogin]
    public class AuctionController : Controller
    {
        // GET: Auction
        public ActionResult Index(BargainVM vm)
        {
            using (var ctx = new AuctionSiteDBEntities())
            {
                var list_product = ctx.Products.ToList();
                var list_auction = ctx.Auctions.ToList();
                var user = CurrentContext.GetCurUser();
                var pro = ctx.Products.Where(p => p.ID == vm.ID).FirstOrDefault<Product>();
                //check if this user just set a price for this product
                if (pro.lastuser != CurrentContext.GetCurUser().ID
                   && (vm.Price >= pro.PriceDisplay + pro.StepPrice))
                {
                    //check if each product has been setted a price once time
                    if ((list_auction.Any(l => l.IDPro == pro.ID) == false))
                    {
                        var ac = new Auction()
                        {
                            IDPro = pro.ID,
                            IDUser = user.ID,
                            Username = user.Username,
                            Fullname = user.Name,
                            Time = DateTime.Now,
                            MaxPrice = vm.Price,
                            own = true,
                        };
                        pro.lastuser = user.ID;
                        pro.PriceDisplay = ac.MaxPrice;
                        ctx.Auctions.Add(ac);
                        ctx.SaveChanges();
                        ViewBag.info = "successfully!!!";
                    }
                    //check if we have many set price already
                    else
                    {
                        var takeowner = ctx.Auctions.Where(t => t.own == true && t.IDPro == pro.ID).FirstOrDefault();
                        var ac = new Auction()
                        {
                            IDPro = pro.ID,
                            IDUser = pro.UserID,
                            Username = user.Username,
                            Fullname = user.Name,
                            Time = DateTime.Now,
                            MaxPrice = vm.Price,
                        };
                        if(takeowner == null)
                        {
                            ac.own = true;                           
                            pro.lastuser = user.ID;
                            pro.PriceDisplay = ac.MaxPrice;
                            ctx.Auctions.Add(ac);                           
                            ctx.SaveChanges();
                            ViewBag.info = "successfully!!!";
                        }
                        else if(takeowner.MaxPrice < ac.MaxPrice)
                        {
                            ac.own = true;
                            //mark lastuser has set price for product
                            pro.lastuser = user.ID;
                            pro.PriceDisplay = ac.MaxPrice;
                            takeowner.own = false;
                            ctx.Auctions.Add(ac);
                            ctx.SaveChanges();
                            ViewBag.info = "successfully!!!";
                        }                    
                    }
                }
                else
                {
                    ViewBag.info = "You just nailled a price for it";
                }
            }
            return RedirectToAction("Details", "Products", new { id = vm.ID });
        }

        // GET: Auction/SettedBid
        public ActionResult SettedBid(Product vm)
        {
            using (var ctx = new AuctionSiteDBEntities())
            {
                    var list = ctx.Auctions.Where(u => u.IDPro == vm.ID).ToList();
                    //
                    //
                    foreach (var item in list)
                    {
                    //if we just have 1 user is racing for this product, Encryption is no needed
                    if (item.own == false)
                        {
                            string temp = new string('*', item.Fullname.Count());
                            var aStringBuilder = new StringBuilder(temp);
                            aStringBuilder.Replace('*', item.Fullname[item.Fullname.Length - 1], temp.Count() - 1, 1);
                            item.Fullname = aStringBuilder.ToString();
                        }
                        var hm = new DateTime(item.Time.Value.Year, item.Time.Value.Month, item.Time.Value.Day, item.Time.Value.Hour, item.Time.Value.Minute, 0);
                        item.Time = hm;
                    }
                    return View(list);
                }                         
        }

        // GET: Auction/BanUser
        public ActionResult BanUser(int iduser, int idpro)
        {
            using (var ctx = new AuctionSiteDBEntities())
            {
                var user = ctx.Users.Where(u => u.ID == iduser).FirstOrDefault();
                var ban = new BannedUser()
                {
                    IDProduct = idpro,
                    IDUser = iduser,
                };
                ctx.BannedUsers.Add(ban);
                ctx.SaveChanges();
            }

            using (var update = new AuctionSiteDBEntities())
            {
                var currentowner = update.Auctions.Where(a => a.own == true && a.IDPro == idpro).FirstOrDefault();
                currentowner.own = false;
                var secondMax = update.Auctions.OrderByDescending(a => a.MaxPrice)
                    .Where(p => p.IDPro == idpro)
                    .Skip(1).FirstOrDefault();
                //more than 1 price setting
                if(secondMax != null)
                {
                    secondMax.own = true;
                    //udpate maxprice of product
                    Product pro = update.Products.Where(p => p.ID == idpro).FirstOrDefault<Product>();
                    pro.PriceDisplay = secondMax.MaxPrice;
                    pro.lastuser = secondMax.IDUser;
                    update.SaveChanges();                  
                }
                //if we just have 1 setting price for this product then second will be turnned into null 
                else
                {
                    //udpate maxprice of product
                    Product pro = update.Products.Where(p => p.ID == idpro).FirstOrDefault<Product>();
                    pro.PriceDisplay = pro.StartPrice;
                    pro.lastuser = null;
                    update.SaveChanges();                   
                }
                return RedirectToAction("SettedBid", "Auction", new { id = idpro });
            }
        }
    }
}