using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Doan1.Models.Factory_Method
{
    public class Tra : PhanLoaiMon
    {
        private QuanLyCuaHangTraSuaEntities1 db = new QuanLyCuaHangTraSuaEntities1();
        public override IQueryable<Menu> LayLoai()
        {
            string loai = "Trà";
            var temp = HttpContext.Current.Session["ChiNhanh"] as string;
            var menu = from l in db.Menus
                       where l.MaChiNhanh == temp && l.Loai == loai
                       select l;
            return menu;
        }
    }
}