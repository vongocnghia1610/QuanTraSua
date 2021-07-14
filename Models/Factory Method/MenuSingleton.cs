using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Doan1.Models.Factory_Method
{
    public class MenuSingleton
    {
        private MenuSingleton() { }
        private static MenuSingleton instance = null;
        public string selectedMenu;
        public static MenuSingleton Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MenuSingleton();
                }
                return instance;
            }
        }
        public void Tra()
        {
            selectedMenu = "Trà";
        }
        public void TraSua()
        {
            selectedMenu = "Trà Sữa";
        }
        public void MonKhac()
        {
            selectedMenu = "Món Khác";
        }
        public void TatCa()
        {
            selectedMenu = "Tất Cả";
        }
    }
}