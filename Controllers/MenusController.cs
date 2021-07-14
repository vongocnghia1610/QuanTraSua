using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Doan1.Models;
using PagedList;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Ajax.Utilities;
using EPPlusTest;
using OfficeOpenXml;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Configuration;
using Doan1.Models.Factory_Method;
using Microsoft.AspNetCore.Hosting;
using Firebase.Auth;
using System.Threading;
using Firebase.Storage;
using System.Threading.Tasks;
using System.Diagnostics;
using PayPal.Api;
namespace Doan1.Controllers
{
    public class MenusController : Controller
    {
        private QuanLyCuaHangTraSuaEntities1 db = new QuanLyCuaHangTraSuaEntities1();
        // GET: Menus
        public string err = "";
        static private string idtrans;
        public static int soluong;
        public class HttpParamActionAttribute : ActionNameSelectorAttribute
        {
            public override bool IsValidName(ControllerContext controllerContext, string actionName, MethodInfo methodInfo)
            {
                if (actionName.Equals(methodInfo.Name, StringComparison.InvariantCultureIgnoreCase))
                    return true;

                var request = controllerContext.RequestContext.HttpContext.Request;
                return request[methodInfo.Name] != null;
            }
        }
        [AuthorizeController]
        [HttpGet]
        // GET: 
        public ActionResult Index(int? size, int? page, string sortProperty, string sortOrder, string searchString)
        {

            // 1. Tạo biến ViewBag gồm sortOrder, searchValue, sortProperty và page
            if (sortOrder == "asc") ViewBag.sortOrder = "desc";
            if (sortOrder == "desc") ViewBag.sortOrder = "";
            if (sortOrder == "") ViewBag.sortOrder = "asc";
            ViewBag.searchValue = searchString;
            ViewBag.sortProperty = sortProperty;
            ViewBag.page = page;

            // 2. Tạo danh sách chọn số trang
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem { Text = "5", Value = "5" });
            items.Add(new SelectListItem { Text = "10", Value = "10" });
            items.Add(new SelectListItem { Text = "20", Value = "20" });


            // 2.1. Thiết lập số trang đang chọn vào danh sách List<SelectListItem> items
            foreach (var item in items)
            {
                if (item.Value == size.ToString()) item.Selected = true;
            }
            ViewBag.size = items;
            ViewBag.currentSize = size;

            // 3. Lấy tất cả tên thuộc tính của lớp Menu (MaMon, TenMon, Loai,...)
            var properties = typeof(Menu).GetProperties();
            List<Tuple<string, bool, int>> list = new List<Tuple<string, bool, int>>();
            foreach (var item in properties)
            {
                int order = 999;
                var isVirtual = item.GetAccessors()[0].IsVirtual;

                if (item.Name == "TenMon")
                {
                    order = 2;
                }
                if (item.Name == "MaMon") order = 1;
                if (item.Name == "Loai") order = 3;
                if (item.Name == "GiaTien") order = 4;
                if (item.Name == "MaChiNhanh") continue; // Không hiển thị cột này
                if (item.Name == "ChiNhanh") continue;
                if (item.Name == "SoLuongDaBan") order = 5;
                if (item.Name == "AnhMinhHoa") continue; // Không hiển thị cột này
                if (item.Name == "ChiTietHoaDons") continue;
                if (item.Name == "ChiTietHoaDons1") continue;
                if (item.Name == "ChiNhanh1") continue;

                Tuple<string, bool, int> t = new Tuple<string, bool, int>(item.Name, isVirtual, order);
                list.Add(t);
            }
            list = list.OrderBy(x => x.Item3).ToList();

            // 3.1. Tạo Heading sắp xếp cho các cột
            foreach (var item in list)
            {
                if (!item.Item2)
                {
                    if (sortOrder == "desc" && sortProperty == item.Item1)
                    {
                        ViewBag.Headings += "<th><a href='?page=" + page + "&size=" + ViewBag.currentSize + "&sortProperty=" + item.Item1 + "&sortOrder=" +
                            ViewBag.sortOrder + "&searchString=" + searchString + "'>" + item.Item1 + "<i class='fa fa-fw fa-sort-desc'></i></th></a></th>";
                    }
                    else if (sortOrder == "asc" && sortProperty == item.Item1)
                    {
                        ViewBag.Headings += "<th><a href='?page=" + page + "&size=" + ViewBag.currentSize + "&sortProperty=" + item.Item1 + "&sortOrder=" +
                            ViewBag.sortOrder + "&searchString=" + searchString + "'>" + item.Item1 + "<i class='fa fa-fw fa-sort-asc'></a></th>";
                    }
                    else
                    {
                        ViewBag.Headings += "<th><a href='?page=" + page + "&size=" + ViewBag.currentSize + "&sortProperty=" + item.Item1 + "&sortOrder=" +
                           ViewBag.sortOrder + "&searchString=" + searchString + "'>" + item.Item1 + "<i class='fa fa-fw fa-sort'></a></th>";
                    }

                }
                else ViewBag.Headings += "<th>" + item.Item1 + "</th>";
            }

            // 4. Truy vấn lấy tất cả đường dẫn
            var menu = from l in db.Menus
                       select l;

            // 5. Tạo thuộc tính sắp xếp mặc định là "MaMon"
            if (String.IsNullOrEmpty(sortProperty)) sortProperty = "MaMon";

            // 5. Sắp xếp tăng/giảm bằng phương thức OrderBy sử dụng trong thư viện Dynamic LINQ
            if (sortOrder == "desc") menu = menu.OrderBy(sortProperty + " desc");
            else if (sortOrder == "asc") menu = menu.OrderBy(sortProperty);
            else menu = menu.OrderBy("MaMon");

            // 5.1. Thêm phần tìm kiếm
            if (!String.IsNullOrEmpty(searchString))
            {
                menu = menu.Where(s => s.TenMon.Contains(searchString));
            }

            // 5.2. Nếu page = null thì đặt lại là 1.
            page = page ?? 1; //if (page == null) page = 1;

            // 5.3. Tạo kích thước trang (pageSize), mặc định là 5.
            int pageSize = (size ?? 5);

            ViewBag.pageSize = pageSize;

            // 6. Toán tử ?? trong C# mô tả nếu page khác null thì lấy giá trị page, còn
            // nếu page = null thì lấy giá trị 1 cho biến pageNumber. --- dammio.com
            int pageNumber = (page ?? 1);

            // 6.2 Lấy tổng số record chia cho kích thước để biết bao nhiêu trang
            int checkTotal = (int)(menu.ToList().Count / pageSize) + 1;
            // Nếu trang vượt qua tổng số trang thì thiết lập là 1 hoặc tổng số trang
            if (pageNumber > checkTotal) pageNumber = checkTotal;

            // 7. Trả về các menu được phân trang theo kích thước và số trang.
            return View(menu.ToPagedList(pageNumber, pageSize));
        }

        public void Xuat()
        {
            var a = from l in db.Menus
                    select l;
            var list = a.ToList();
            list = list.OrderBy(s => s.Loai).ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");
            Sheet.Cells["A1"].Value = "Mã món";
            Sheet.Cells["B1"].Value = "Tên món";
            Sheet.Cells["C1"].Value = "Loại";
            Sheet.Cells["D1"].Value = "Giá tiền";
            Sheet.Cells["E1"].Value = "Tên Chi Nhánh";
            Sheet.Cells["F1"].Value = "Link Ảnh";
            Sheet.Cells["G1"].Value = "Đã bán";
            int row = 2;

            foreach (var item in list)
            {
                Sheet.Cells[string.Format("A{0}", row)].Value = item.MaMon;
                Sheet.Cells[string.Format("B{0}", row)].Value = item.TenMon;
                Sheet.Cells[string.Format("C{0}", row)].Value = item.Loai;
                Sheet.Cells[string.Format("D{0}", row)].Value = item.GiaTien;
                Sheet.Cells[string.Format("E{0}", row)].Value = item.ChiNhanh.TenChiNhanh;
                Sheet.Cells[string.Format("F{0}", row)].Value = item.AnhMinhHoa;
                Sheet.Cells[string.Format("G{0}", row)].Value = item.SoLuongDaBan;
                row++;
            }


            Sheet.Cells["A:AZ"].AutoFitColumns();
            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment: filename=" + "Report.xlsx");
            Response.BinaryWrite(Ep.GetAsByteArray());
            Response.End();
        }


        // GET: Menus/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Menu menu = db.Menus.Find(id);
            if (menu == null)
            {
                return HttpNotFound();
            }
            return View(menu);
        }
        [AuthorizeController]

        // GET: Menus/Create
        public ActionResult Create()
        {
            ViewBag.MaChiNhanh = new SelectList(db.ChiNhanhs, "MaChiNhanh", "TenChiNhanh");

            return View();
        }

        // POST: Menus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //   [AuthorizeController]

        private static string ApiKey = "AIzaSyBlLXc5rc1YM8Mj__gVzi0sc4VYl_AWeA4";
        private static string Bucket = "quantrasua-71104.appspot.com";
        private static string AuthEmail = "nguyenphannhattu@gmail.com";
        private static string AuthPassword = "123456";
        public string linkAnh = "";

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "MaMon,TenMon,GiaTien,MaChiNhanh")] Menu menu, HttpPostedFileBase postedFile, String Loai)
        {

            try
            {
                var dem = db.Menus.Count();
                dem = dem + 1;
                menu.MaMon = dem.ToString();
                FileStream stream;
                if (postedFile.ContentLength > 0)
                {
                    string path = Path.Combine(Server.MapPath("~/images"), postedFile.FileName);
                    postedFile.SaveAs(path);
                    stream = new FileStream(Path.Combine(path), FileMode.Open);
                    var auth = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
                    var a = await auth.SignInWithEmailAndPasswordAsync(AuthEmail, AuthPassword);
                    // you can use CancellationTokenSource to cancel the upload midway
                    var cancellation = new CancellationTokenSource();

                    var task = new FirebaseStorage(
                        Bucket,
                        new FirebaseStorageOptions
                        {
                            AuthTokenAsyncFactory = () => Task.FromResult(a.FirebaseToken),
                            ThrowOnCancel = true // when you cancel the upload, exception is thrown. By default no exception is thrown
                        })
                        .Child("images")
                        .Child(postedFile.FileName + menu.MaMon)
                        .PutAsync(stream, cancellation.Token);

                    task.Progress.ProgressChanged += (s, e) => Console.WriteLine($"Progress: {e.Percentage} %");

                    // cancel the upload
                    // cancellation.Cancel();

                    try
                    {
                        // error during upload will be thrown when you await the task
                        linkAnh = await task;
                    }
                    catch (Exception ex)
                    {

                        linkAnh = "null";
                    }
                }
                if (ModelState.IsValid)
                {
                    menu.Loai = Loai;
                    menu.AnhMinhHoa = linkAnh;
                    menu.SoLuongDaBan = 0;
                    db.Menus.Add(menu);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

                ViewBag.MaChiNhanh = new SelectList(db.ChiNhanhs, "MaChiNhanh", "TenChiNhanh", menu.MaChiNhanh);
                return View(menu);
            }
            catch
            {
                Session["Check"] = "Mã Món đã có";
                return RedirectToAction("Create");
            }
        }


        [HttpPost]
        public async Task<ActionResult> uphinh(HttpPostedFileBase postedFile)
        {
            FileStream stream;
            if (postedFile.ContentLength > 0)
            {
                string path = Path.Combine(Server.MapPath("~/images"), postedFile.FileName);
                postedFile.SaveAs(path);
                stream = new FileStream(Path.Combine(path), FileMode.Open);
                await Task.Run(() => uploadhinh(stream, postedFile.FileName));
            }
            return RedirectToAction("Create");
        }
        public async void uploadhinh(FileStream stream, string fileName)
        {
            var auth = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
            var a = await auth.SignInWithEmailAndPasswordAsync(AuthEmail, AuthPassword);
            // you can use CancellationTokenSource to cancel the upload midway
            var cancellation = new CancellationTokenSource();

            var task = new FirebaseStorage(
                Bucket,
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(a.FirebaseToken),
                    ThrowOnCancel = true // when you cancel the upload, exception is thrown. By default no exception is thrown
                })
                .Child("images")
                .Child(fileName)
                .PutAsync(stream, cancellation.Token);

            task.Progress.ProgressChanged += (s, e) => Console.WriteLine($"Progress: {e.Percentage} %");

            // cancel the upload
            // cancellation.Cancel();

            try
            {
                // error during upload will be thrown when you await the task
                linkAnh = await task;
            }
            catch (Exception ex)
            {

                linkAnh = "null";
            }
        }

        // GET: Menus/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Menu menu = db.Menus.Find(id);
            if (menu == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaChiNhanh = new SelectList(db.ChiNhanhs, "MaChiNhanh", "TenChiNhanh", menu.MaChiNhanh);
            return View(menu);
        }

        // POST: Menus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "MaMon,TenMon,GiaTien,MaChiNhanh,AnhMinhHoa")] Menu menu, HttpPostedFileBase postedFile, string Loai)
        {
            if (postedFile != null)
            {
                FileStream stream;
                if (postedFile.ContentLength > 0)
                {
                    string path = Path.Combine(Server.MapPath("~/images"), postedFile.FileName);
                    postedFile.SaveAs(path);
                    stream = new FileStream(Path.Combine(path), FileMode.Open);
                    var auth = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
                    var a = await auth.SignInWithEmailAndPasswordAsync(AuthEmail, AuthPassword);
                    // you can use CancellationTokenSource to cancel the upload midway
                    var cancellation = new CancellationTokenSource();

                    var task = new FirebaseStorage(
                        Bucket,
                        new FirebaseStorageOptions
                        {
                            AuthTokenAsyncFactory = () => Task.FromResult(a.FirebaseToken),
                            ThrowOnCancel = true // when you cancel the upload, exception is thrown. By default no exception is thrown
                        })
                        .Child("images")
                        .Child(postedFile.FileName + menu.MaMon)
                        .PutAsync(stream, cancellation.Token);

                    task.Progress.ProgressChanged += (s, e) => Console.WriteLine($"Progress: {e.Percentage} %");

                    // cancel the upload
                    // cancellation.Cancel();

                    try
                    {
                        // error during upload will be thrown when you await the task
                        linkAnh = await task;
                    }
                    catch (Exception ex)
                    {

                        linkAnh = "null";
                    }
                }
            }
            if (ModelState.IsValid)
            {
                if (postedFile != null)
                    menu.AnhMinhHoa = linkAnh;
                menu.Loai = Loai;
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaChiNhanh = new SelectList(db.ChiNhanhs, "MaChiNhanh", "TenChiNhanh", menu.MaChiNhanh);
            return View(menu);
        }

        // GET: Menus/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Menu menu = db.Menus.Find(id);
            if (menu == null)
            {
                return HttpNotFound();
            }
            return View(menu);
        }

        // POST: Menus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Menu menu = db.Menus.Find(id);
            db.Menus.Remove(menu);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult MenuKhachHang(int? size, int? page, string searchString)
        {
            // 1. Tạo biến ViewBag gồm sortOrder, searchValue, sortProperty và page

            ViewBag.searchValue = searchString;
            ViewBag.page = page;

            // 2. Tạo danh sách chọn số trang
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem { Text = "4", Value = "4" });
            items.Add(new SelectListItem { Text = "8", Value = "8" });
            items.Add(new SelectListItem { Text = "12", Value = "12" });


            // 2.1. Thiết lập số trang đang chọn vào danh sách List<SelectListItem> items
            foreach (var item in items)
            {
                if (item.Value == size.ToString()) item.Selected = true;
            }
            ViewBag.size = items;
            ViewBag.currentSize = size;



            // 4. Truy vấn lấy tất cả đường dẫn
            var menu = from l in db.Menus
                       select l;
            if ((string)Session["QuyenUser"] == "2")
            {
                var MaNhanVien = (string)Session["MaNV"];
                var nhanvien = (from l in db.NhanViens
                                where l.MaNV == MaNhanVien
                                select l).Single();
                var chinhanh = nhanvien.MaChiNhanh;
                Session["ChiNhanh"] = chinhanh;
                menu = from l in db.Menus
                       where l.MaChiNhanh == chinhanh
                       select l;
            }
            else
            {
                if (((string)Session["ChiNhanh"]) == null)
                    Session["ChiNhanh"] = "1";
                var chinhanh = (string)Session["ChiNhanh"];
                var a = MenuSingleton.Instance.selectedMenu;
                if (a == "Trà Sữa")
                {
                    PhanLoaiMon menu1 = PhanLoaiFactory.getLoai(LoaiThucUong.TraSua);
                    menu = menu1.LayLoai();
                }
                else if (a == "Trà")
                {
                    PhanLoaiMon menu1 = PhanLoaiFactory.getLoai(LoaiThucUong.Tra);
                    menu = menu1.LayLoai();
                }
                else if (a == "Món Khác")
                {
                    PhanLoaiMon menu1 = PhanLoaiFactory.getLoai(LoaiThucUong.MonKhac);
                    menu = menu1.LayLoai();
                }
                else
                {
                    menu = from l in db.Menus
                           where l.MaChiNhanh == chinhanh
                           select l;
                }



            }
            string checkchinhanh = (string)Session["ChiNhanh"];
            var temp = (from l in db.Menus
                        where l.MaChiNhanh == checkchinhanh
                        orderby l.SoLuongDaBan descending
                        select l).ToArray();
            soluong = temp[1].SoLuongDaBan;
            
            // 5.1. Thêm phần tìm kiếm
            if (!String.IsNullOrEmpty(searchString))
            {
                menu = menu.Where(s => s.TenMon.Contains(searchString));
            }

            // 5.2. Nếu page = null thì đặt lại là 1.
            page = page ?? 1; //if (page == null) page = 1;

            // 5.3. Tạo kích thước trang (pageSize), mặc định là 4.
            int pageSize = (size ?? 4);

            ViewBag.pageSize = pageSize;

            // 6. Toán tử ?? trong C# mô tả nếu page khác null thì lấy giá trị page, còn
            // nếu page = null thì lấy giá trị 1 cho biến pageNumber. --- dammio.com
            int pageNumber = (page ?? 1);

            // 6.2 Lấy tổng số record chia cho kích thước để biết bao nhiêu trang
            int checkTotal = (int)(menu.ToList().Count / pageSize) + 1;
            // Nếu trang vượt qua tổng số trang thì thiết lập là 1 hoặc tổng số trang
            if (pageNumber > checkTotal) pageNumber = checkTotal;

            // 7. Trả về các menu được phân trang theo kích thước và số trang.
            return View(menu.OrderByDescending(x => x.MaMon).ToPagedList(pageNumber, pageSize));

        }
        public ActionResult btnTra(object sender, EventArgs e)
        {

            //Session["PhanLoai"] = LuaChon.Instance.ChoXacNhan() ;
            MenuSingleton.Instance.Tra();
            return RedirectToAction("MenuKhachHang", "Menus");

        }
        public ActionResult btnTraSua(object sender, EventArgs e)
        {

            //Session["PhanLoai"] = LuaChon.Instance.ChoXacNhan() ;
            MenuSingleton.Instance.TraSua();
            return RedirectToAction("MenuKhachHang", "Menus");

        }
        public ActionResult btnMonKhac(object sender, EventArgs e)
        {

            //Session["PhanLoai"] = LuaChon.Instance.ChoXacNhan() ;
            MenuSingleton.Instance.MonKhac();
            return RedirectToAction("MenuKhachHang", "Menus");

        }
        public ActionResult btnTatCa(object sender, EventArgs e)
        {

            //Session["PhanLoai"] = LuaChon.Instance.ChoXacNhan() ;
            MenuSingleton.Instance.TatCa();
            return RedirectToAction("MenuKhachHang", "Menus");

        }
        [AuthorizeController]
        public ActionResult ThongKe(object sender, EventArgs e)
        {
            //if ((string)Session["ThongKe"] == "2")
            //{

            var chinhanh = (string)Session["ThongKe"];
            if (chinhanh == null)
            {
                Session["ThongKe"] = "1";
                chinhanh = "1";
            }
            var menu = from l in db.Menus
                       where l.MaChiNhanh == chinhanh
                       orderby l.SoLuongDaBan descending
                       select l;
            List<Menu> lst = new List<Menu>();
            lst = menu.Take(3).ToList();
            if (lst.Count != 0)
            {

                var sLTS1 = db.Menus.Where(n => n.Loai == "Trà Sữa" && n.ChiNhanh.MaChiNhanh == chinhanh).Sum(n => n.SoLuongDaBan);
                ViewBag.sLTS1 = sLTS1;
                //Gán vào ViewBag
                var doanhThuCN1 = db.HoaDons.Where(n => n.ChiNhanh.MaChiNhanh == chinhanh && n.TrangThai == "Đã giao").Sum(n => n.TongGia);
                ViewBag.doanhThuCN1 = doanhThuCN1;



                //List Đá Xay 
                //var lstDX = db.MENUs.Where(n => n.MaLoai == 2).OrderByDescending(n => n.TenMon).Count();
                var sLT1 = db.Menus.Where(n => n.Loai == "Trà" && n.ChiNhanh.MaChiNhanh == chinhanh).Sum(n => n.SoLuongDaBan);
                //Gán vào ViewBag
                ViewBag.sLT1 = sLT1;

                //List TOPPING 
                //var lstTP = db.MENUs.Where(n => n.MaLoai == 3).OrderByDescending(n => n.TenMon).Count();
                var sLTO1 = db.Menus.Where(n => n.Loai == "Món Khác" && n.ChiNhanh.MaChiNhanh == chinhanh).Sum(n => n.SoLuongDaBan);
                //Gán vào ViewBag
                ViewBag.sLTO1 = sLTO1;
            }
            else
            {
                var sLTS1 = 0;
                ViewBag.sLTS1 = sLTS1;
                var doanhThuCN1 = 0;
                ViewBag.doanhThuCN1 = doanhThuCN1;
                var sLT1 = 0;
                ViewBag.sLT1 = sLT1;
                var sLTO1 = 0;
                ViewBag.sLTO1 = sLTO1;
            }

            return View(lst);
            //}
            //else
            //{

            //    var menu = from l in db.Menus
            //               where l.MaChiNhanh == "1"
            //               orderby l.SoLuongDaBan descending
            //               select l;
            //    List<Menu> lst = new List<Menu>();
            //    lst = menu.Take(3).ToList();
            //    var sLTS1 = db.Menus.Where(n => n.Loai == "Trà Sữa" && n.ChiNhanh.MaChiNhanh == "1").Sum(n => n.SoLuongDaBan);
            //    ViewBag.sLTS1 = sLTS1;
            //    //Gán vào ViewBag
            //    var doanhThuCN1 = db.HoaDons.Where(n => n.ChiNhanh.MaChiNhanh == "1" && n.TrangThai == "Đã giao").Sum(n => n.TongGia);
            //    ViewBag.doanhThuCN1 = doanhThuCN1;



            //    //List Đá Xay 
            //    //var lstDX = db.MENUs.Where(n => n.MaLoai == 2).OrderByDescending(n => n.TenMon).Count();
            //    var sLT1 = db.Menus.Where(n => n.Loai == "Trà" && n.ChiNhanh.MaChiNhanh == "1").Sum(n => n.SoLuongDaBan);
            //    //Gán vào ViewBag
            //    ViewBag.sLT1 = sLT1;

            //    //List TOPPING 
            //    //var lstTP = db.MENUs.Where(n => n.MaLoai == 3).OrderByDescending(n => n.TenMon).Count();
            //    var sLTO1 = db.Menus.Where(n => n.Loai == "Món Khác" && n.ChiNhanh.MaChiNhanh == "1").Sum(n => n.SoLuongDaBan);
            //    //Gán vào ViewBag
            //    ViewBag.sLTO1 = sLTO1;

            //    return View(lst);

            //}
        }
        [AuthorizeController]
        public ActionResult ThongKeChiNhanh(int chinhanh)
        {
            Session["ThongKe"] = chinhanh+"";
            return RedirectToAction("ThongKe", "Menus");
        }
        //[AuthorizeController]
        //public ActionResult ThongKe2(object sender, EventArgs e)
        //{
        //    Session["ThongKe"] = "2";
        //    return RedirectToAction("ThongKe", "Menus");
        //}
        [AuthorizeController]
        public ActionResult ImportFromExcel(HttpPostedFileBase postedFile)
        {
            if (postedFile != null)
            {
                if (postedFile != null && postedFile.ContentLength > (1024 * 1024 * 50))  // 50MB limit  
                {
                    ModelState.AddModelError("postedFile", "Your file is to large. Maximum size allowed is 50MB !");
                }

                else
                {
                    string filePath = string.Empty;
                    string path = Server.MapPath("~/Uploads/");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    filePath = path + Path.GetFileName(postedFile.FileName);
                    string extension = Path.GetExtension(postedFile.FileName);
                    postedFile.SaveAs(filePath);

                    string conString = string.Empty;
                    switch (extension)
                    {
                        case ".xls": //For Excel 97-03.  
                            conString = ConfigurationManager.ConnectionStrings["Excel03ConString"].ConnectionString;
                            break;
                        case ".xlsx": //For Excel 07 and above.  
                            conString = ConfigurationManager.ConnectionStrings["Excel07ConString"].ConnectionString;
                            break;
                    }

                    try
                    {
                        DataTable dt = new DataTable();
                        conString = string.Format(conString, filePath);

                        using (OleDbConnection connExcel = new OleDbConnection(conString))
                        {
                            using (OleDbCommand cmdExcel = new OleDbCommand())
                            {
                                using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                                {
                                    cmdExcel.Connection = connExcel;

                                    //Get the name of First Sheet.  
                                    connExcel.Open();
                                    DataTable dtExcelSchema;
                                    dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                                    string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                    connExcel.Close();

                                    //Read Data from First Sheet.  
                                    connExcel.Open();
                                    cmdExcel.CommandText = "SELECT * From [" + sheetName + "]";
                                    odaExcel.SelectCommand = cmdExcel;
                                    odaExcel.Fill(dt);
                                    connExcel.Close();
                                }
                            }
                        }
                        var dem = db.Menus.Count();
                        foreach (var item in dt.AsEnumerable())
                        {
                            if (ModelState.IsValid)
                            {
                                dem = dem + 1;
                                Menu luutru = new Menu();
                                luutru.AnhMinhHoa = item.Field<string>("Link Ảnh");
                                luutru.MaMon = dem.ToString();
                                luutru.TenMon = item.Field<string>("Tên món");
                                luutru.Loai = item.Field<string>("Loại");
                                luutru.GiaTien = item.Field<double>("Giá tiền");
                                luutru.MaChiNhanh = item.Field<double>("Mã Chi Nhánh").ToString();
                                luutru.SoLuongDaBan = 0;
                                //  try
                                //{
                                db.Menus.Add(luutru);
                                db.SaveChanges();
                                //}
                                //catch
                                //{
                                //    db.Menus.Remove(luutru);
                                //    db.SaveChanges();

                                //    db.Menus.Add(luutru);
                                //    db.SaveChanges();
                                //}
                            }
                        }
                        return RedirectToAction("Index", "Menus");

                        //conString = ConfigurationManager.ConnectionStrings["QuanLyCuaHangTraSuaEntities1"].ConnectionString;
                        //using (SqlConnection con = new SqlConnection(conString))
                        //{
                        //    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                        //    {
                        //        //Set the database table name.  
                        //        sqlBulkCopy.DestinationTableName = "Menus";
                        //        con.Open();
                        //        sqlBulkCopy.WriteToServer(dt);
                        //        con.Close();
                        //        return Json("File uploaded successfully");
                        //    }
                        //}
                    }

                    //catch (Exception ex)  
                    //{  
                    //    throw ex;  
                    //}  
                    catch (Exception e)
                    {
                        return Json("error" + e.Message);
                    }
                    //return RedirectToAction("Index");  
                }
            }
            //return View(postedFile);  
            return Json("no files were selected !");

        }
        [HttpPost]
        public ActionResult themgiohang(string mamon)
        {

            Menu menu = db.Menus.Find(mamon);
            Session["Mon"] = menu;
            return Content("True");
        }
        public void Size(string size)
        {
            if (size == "M")
            {

            }
            else if (size == "L")
            {

            }
        }
        public ActionResult DatMon(string size, string Duong, string Da, int SoLuong)
        {
            if (((Menu)(Session["Mon"])).MaMon != null)
            {
                List<GioHang> giohang;
                if (Session["Cart"] != null)
                {
                    giohang = ((List<GioHang>)Session["Cart"]);

                }
                else
                {
                    giohang = new List<GioHang>();
                }
                GioHang mon = new GioHang(((Menu)(Session["Mon"])).MaMon, size, Duong, Da, SoLuong, ((Menu)(Session["Mon"])).AnhMinhHoa);
                GioHang temp = giohang.SingleOrDefault(n => n.MaMon == ((Menu)(Session["Mon"])).MaMon && n.Size == size && n.Duong == Duong && n.Da == Da);
                if (temp != null)
                {
                    giohang.SingleOrDefault(n => n.MaMon == ((Menu)(Session["Mon"])).MaMon && n.Size == size && n.Duong == Duong && n.Da == Da).SoLuong += SoLuong;
                    giohang.SingleOrDefault(n => n.MaMon == ((Menu)(Session["Mon"])).MaMon && n.Size == size && n.Duong == Duong && n.Da == Da).ThanhTien += mon.ThanhTien;

                }
                else
                {
                    giohang.Add(mon);
                }
                Session["Cart"] = giohang;
                Session["SoLuong"] = TinhTongSoLuong();
                Session["TongTien"] = TinhTongTien();
                return Content("true");
            }
            return Content("false");
        }

        public ActionResult PaymentWithPaypal(string Cancel = null)
        {
            APIContext apiContext = PaypalConfiguration.GetAPIContext();
            try
            {
                //A resource representing a Payer that funds a payment Payment Method as paypal  
                //Payer Id will be returned when payment proceeds or click to pay  
                string payerId = Request.Params["PayerID"];
                if (string.IsNullOrEmpty(payerId))
                {
                    double tien = (double)Session["TongTien"];
                    //this section will be executed first because PayerID doesn't exist  
                    //it is returned by the create function call of the payment class  
                    // Creating a payment  
                    // baseURL is the url on which paypal sendsback the data.  
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/Menus/PaymentWithPayPal?";
                    //here we are generating guid for storing the paymentID received in session  
                    //which will be used in the payment execution  
                    var guid = Convert.ToString((new Random()).Next(100000));
                    //CreatePayment function gives us the payment approval url  
                    //on which payer is redirected for paypal account payment  
                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid, tien);
                    //get links returned from paypal in response to Create function call  
                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            //saving the payapalredirect URL to which user will be redirected for payment  
                            paypalRedirectUrl = lnk.href;
                        }
                    }
                    // saving the paymentID in the key guid  
                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    // This function exectues after receving all parameters for the payment  
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);
                    var tr = executedPayment.payment_instruction;
                    idtrans = executedPayment.transactions[0].related_resources[0].sale.id.Trim();
                    //If executed payment failed then we will show payment failure message to user  
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                return View();
            }
            //on successful payment, show success page to user.  
            return RedirectToAction("ThanhToan", "Menus");
        }
        private PayPal.Api.Payment payment;
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };
            this.payment = new Payment()
            {
                id = paymentId
            };
            return this.payment.Execute(apiContext, paymentExecution);
        }
        public double FormatDollar(double tienDo)
        {
            double tienDo2f = Math.Round(tienDo * 100) / 100;
            double tienDo3f = Math.Round(tienDo * 1000) / 1000;
            if (tienDo % tienDo2f == 0)
                return tienDo2f;
            else if (tienDo2f > tienDo3f)
                return tienDo2f;
            else
                return tienDo2f + 0.01;
        }
        private Payment CreatePayment(APIContext apiContext, string redirectUrl, double Tien)
        {
            //create itemlist and add item objects to it  
            var itemList = new ItemList()
            {
                items = new List<Item>()
            };
            //Adding Item Details like name, currency, price etc
            double tienViet = Tien;
            var tienDo = tienViet / 23045;
            tienDo = FormatDollar(tienDo);
            itemList.items.Add(new Item()
            {
                name = "Don Tra Sua",
                currency = "USD",
                price = tienDo.ToString(),
                quantity = "1",
                sku = "sku"
            });
            var payer = new Payer()
            {
                payment_method = "paypal"
            };
            // Configure Redirect Urls here with RedirectUrls object  
            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };
            // Adding Tax, shipping and Subtotal details  
            var details = new Details()
            {
                tax = "0",
                shipping = "0",
                subtotal = tienDo.ToString()
            };
            //Final amount with details
            var tongTien = tienDo ;
            var amount = new Amount()
            {
                currency = "USD",
                total = tongTien.ToString(), // Total must be equal to sum of tax, shipping and subtotal.  
                details = details
            };
            var transactionList = new List<Transaction>();
            string sohoadon = (db.HoaDons.Count() + 1).ToString();
            // Adding description about the transaction  
            transactionList.Add(new Transaction()
            {
                description = "Transaction description",
                invoice_number = sohoadon, //Generate an Invoice No  
                amount = amount,
                item_list = itemList
            }); ;
            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };
            // Create a payment using a APIContext  
            return this.payment.Create(apiContext);
        }
        public ActionResult refundPayPal(string iddon)
        {

            Dictionary<string, string> payPalConfig = new Dictionary<string, string>();
            payPalConfig.Add("mode", "sandbox");
            OAuthTokenCredential tokenCredential = new OAuthTokenCredential("AT2GliMEkGCdmb5wkSLGXIFdrHfnLGIc1ktrVGMObm-rmjxnnhEy9uKEKb4QnirC4i2YHzW6RDVO_wLT", "EHsXCZLryoU8o_c86dttYWFrJd7LQfRu_z36EQ4WhQ0EL2leayv_JFuTQfT10CiayKJ_lJro4yOyAftC", payPalConfig);
            string accessToken = tokenCredential.GetAccessToken();
            APIContext apicontext = new APIContext(accessToken);
            var hd = db.HoaDons.Find(iddon);
            if(hd.TrangThai == "Đã hủy")
            {
                string idsale = hd.ThanhToan;
                var tongTien = hd.TongGia;
                tongTien = tongTien / 23045;
                var tienDo = FormatDollar(tongTien);
                Sale sale = Sale.Get(apicontext, idsale);
                Amount amount = new Amount();
                amount.total = tienDo.ToString();
                amount.currency = "USD";
                Refund refund = new Refund();
                refund.amount = amount;
                Refund newRefund = sale.Refund(apicontext, refund);
                hd.ThanhToan = "Đã Refund Payment";
                db.SaveChanges();
            }  
            return RedirectToAction("Index", "Hoadons");
        }
        public  ActionResult PaymentWithVNPay()
        {
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhan ket qua tra ve 
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Ma website
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuoi bi mat
            double tien = (double)Session["TongTien"];
            idtrans = "Thanh toán vnpay";
            //Get payment input
            OrderInfo order = new OrderInfo();
            //Save order to db
            order.OrderId = DateTime.Now.Ticks;
            order.Amount = Convert.ToDecimal(tien);
            order.OrderDescription = "Trà Sữa";
            order.CreatedDate = DateTime.Now;
            //Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.0.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString());
            vnpay.AddRequestData("vnp_OrderInfo", order.OrderDescription);
            vnpay.AddRequestData("vnp_OrderType", "Payment"); //default value: other
            vnpay.AddRequestData("vnp_Amount", (order.Amount * 100).ToString());
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_CreateDate", order.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_BankCode", "NCB");
            vnpay.AddRequestData("vnp_Locale", "vn");

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }
        public int TinhTongSoLuong()
        {
            //Lấy giỏ hàng
            List<GioHang> lstGioHang = Session["Cart"] as List<GioHang>;
            if (lstGioHang == null)
            {
                return 0;
            }
            return lstGioHang.Sum(n => n.SoLuong);
        }
        public double TinhTongTien()
        {
            //Lấy giỏ hàng
            List<GioHang> lstGioHang = Session["Cart"] as List<GioHang>;
            if (lstGioHang == null)
            {
                return 0;
            }
            return lstGioHang.Sum(n => n.ThanhTien);
        }
        public ActionResult GioHang()
        {
            return View();
        }
        public ActionResult Xoa(string mamon,string da,string duong,string size,int soluong)
        {
            List<GioHang> giohang;

            giohang = ((List<GioHang>)Session["Cart"]);
            GioHang mon = giohang.SingleOrDefault(n => n.MaMon == mamon && n.Size == size && n.Duong == duong && n.Da == da && n.SoLuong==soluong);

            giohang.Remove(mon);
            Session["Cart"] = giohang;
            Session["SoLuong"] = TinhTongSoLuong();
            Session["TongTien"] = TinhTongTien();
            return View("GioHang");
        }
        public ActionResult Sua(string mamon,string duong,string da,string size)
        {
            List<GioHang> giohang;

            giohang = ((List<GioHang>)Session["Cart"]);
            GioHang mon = giohang.SingleOrDefault(n => n.MaMon == mamon && n.Size == size && n.Duong == duong && n.Da == da );

            
            Session["Mon"] = mon;
            return Content("True");
        }
        public ActionResult SuaMon(string size, string Duong, string Da, int SoLuong)
        {
            if (((GioHang)(Session["Mon"])).MaMon != null)
            {
                List<GioHang> giohang = ((List<GioHang>)Session["Cart"]);
                
                GioHang mon = new GioHang(((GioHang)(Session["Mon"])).MaMon, size, Duong, Da, SoLuong, ((GioHang)(Session["Mon"])).Anh);
                if (mon == ((GioHang)(Session["Mon"])))
                    return Content("true");
                GioHang temp = giohang.SingleOrDefault(n => n.MaMon == mon.MaMon && n.Size == size && n.Duong == Duong && n.Da == Da);
                if (temp != null)
                {
                    if (((GioHang)(Session["Mon"])).MaMon == temp.MaMon && ((GioHang)(Session["Mon"])).Size == temp.Size && ((GioHang)(Session["Mon"])).Duong == temp.Duong && ((GioHang)(Session["Mon"])).Da == temp.Da)
                    {
                        temp.SoLuong = SoLuong;
                        temp.ThanhTien = mon.ThanhTien;
                    }
                    else
                    {
                        giohang.Remove((GioHang)(Session["Mon"]));
                        temp.SoLuong += SoLuong;
                        temp.ThanhTien += mon.ThanhTien;
                        
                    }
                }
                else
                {
                    giohang.Remove((GioHang)(Session["Mon"]));
                    giohang.Add(mon);
                }
                                  
                Session["Cart"] = giohang;
                Session["SoLuong"] = TinhTongSoLuong();
                Session["TongTien"] = TinhTongTien();
                return Content("true");
            }
            return Content("false");
        }
        public ActionResult ThanhToan()
        {
            List<GioHang> giohang = ((List<GioHang>)Session["Cart"]);
            string mahoadonmoi;
            if (db.HoaDons.Count() != 0)
            {
                var hoadonmoi = (from l in db.HoaDons
                                 select l).ToList();
                var hd = hoadonmoi.OrderByDescending(x => int.Parse(x.MaHD)).ToList();
                mahoadonmoi = hd[0].MaHD;
                mahoadonmoi = (int.Parse(mahoadonmoi) + 1).ToString();
            }
            else
                mahoadonmoi = "0";
            HoaDon hoadon = new HoaDon();
            string manv ;
            string makh;
            if (((string)Session["QuyenUser"]) == "1")
            {
                manv = null;
                makh = null;
                hoadon.TrangThai = "Đã giao";
                hoadon.ThanhToan = "Đã thanh toán";
            }
            else if (((string)Session["QuyenUser"]) == "2")
            {
                manv = (string)Session["MaNV"];
                makh = null;
                hoadon.TrangThai = "Đã giao";
                hoadon.ThanhToan = "Đã thanh toán";
            }
            else 
            {
                manv = null;
                makh = (string)Session["SDT"];
                hoadon.TrangThai = "Chờ xác nhận";
                hoadon.ThanhToan = idtrans;
                idtrans = null ;
            }
            hoadon.MaHD = mahoadonmoi;
            hoadon.SoDienThoai = makh;
            hoadon.MaNV = manv;
            hoadon.NgayBan = DateTime.Now;
            hoadon.MaChiNhanh = (string)Session["ChiNhanh"];
            hoadon.TongGia = (double)Session["Tongtien"];
            db.HoaDons.Add(hoadon);
            db.SaveChanges();
            foreach(var item in giohang)
            {
                ChiTietHoaDon chitiet = new ChiTietHoaDon();
                chitiet.Da = item.Da;
                chitiet.MaHD = mahoadonmoi.ToString();
                chitiet.Size = item.Size;
                chitiet.Duong = item.Duong;
                chitiet.MaMon = item.MaMon;
                chitiet.SoLuong = item.SoLuong;
                chitiet.Gia = item.ThanhTien;
                db.ChiTietHoaDons.Add(chitiet);
                db.SaveChanges();
            }
            Session.Remove("TongTien");
            Session.Remove("Mon");
            Session.Remove("Cart");
            Session.Remove("SoLuong");
            return RedirectToAction("Index", "HoaDons");
        }
        public ActionResult chinhanh(int chinhanh)
        {
            Session["ChiNhanh"] = chinhanh+"";
            Session.Remove("Cart");
            Session.Remove("TongTien");
            Session.Remove("SoLuong");
            return RedirectToAction("MenuKhachHang");
        }
        
    }
}
