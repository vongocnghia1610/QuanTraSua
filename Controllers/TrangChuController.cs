using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Doan1.Models;

namespace Doan1.Controllers
{
    public class TrangChuController : Controller
    {
        private QuanLyCuaHangTraSuaEntities1 db = new QuanLyCuaHangTraSuaEntities1();
        // GET: TrangChu
        public ActionResult TrangChu()
        {
            List<Menu> lst = new List<Menu>();
            foreach (var item in db.ChiNhanhs)
            {
                var menu = from l in db.Menus
                           where l.MaChiNhanh == item.MaChiNhanh
                           orderby l.SoLuongDaBan descending
                           select l;
                lst.AddRange(menu.Take(4).ToList());
            }
            return View(lst);
        }
        public ActionResult LienHe()
        {
            return View(db.LienHes.ToList());
        }
    }
}