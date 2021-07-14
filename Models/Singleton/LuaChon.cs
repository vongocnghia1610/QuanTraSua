using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Doan1.Models.Singleton
{
    public class LuaChon
    {
        private LuaChon() { }
        private static LuaChon instance = null;
        public string selected ;
        public static LuaChon Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LuaChon();
                }
                return instance;
            }
        }

        public void TatCa()
        {
            selected = "Tất cả";
        }
        public void  ChoXacNhan()
        {
            selected = "Chờ xác nhận";
        }
        public void ChoLayHang()
        {
            selected = "Chờ lấy hàng";
        }
        public void DangGiao()
        {
            selected = "Đang giao";
        }
        public void DaGiao()
        {
            selected = "Đã giao";
        }
        public void DaHuy()
        {
            selected = "Đã hủy";
        }
    }
}