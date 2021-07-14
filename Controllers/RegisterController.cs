using Doan1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Doan1.Controllers
{
    public class RegisterController : Controller
    {
        // GET: Register
        
        public ActionResult Register(string username, string address, string password, string Email,string TenKH)
        {
            if (username==null)
            {
                return View();
            }
            using (var _context = new QuanLyCuaHangTraSuaEntities1())
            {

                try
                {
                    var user = (from u in _context.KhachHangs
                                where u.SoDienThoai == username || u.Email == Email
                                select u).Count();
                    if(user==0)
                    {
                        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                        KhachHang khachhang = new KhachHang();
                        khachhang.SoDienThoai = username;
                        khachhang.DiaChi = address;
                        khachhang.MatKhau = passwordHash;
                        khachhang.Email = Email;
                        khachhang.TenKH = TenKH;
                        khachhang.DaMua = 0;
                        khachhang.HoaDons.Clear();
                        _context.KhachHangs.Add(khachhang);
                        _context.SaveChanges();
                        return RedirectToAction("Login", "Login");
                    }
                    else
                    {
                        return View();
                    }

                }
                catch
                {
                    return View();
                }
            }
        }
    }
}