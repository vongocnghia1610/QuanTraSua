using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Doan1.Models.Factory_Method
{
    public class PhanLoaiFactory
    {
        
        private PhanLoaiFactory()
        {
        }
        public static PhanLoaiMon  getLoai(LoaiThucUong Loai)
        {
            switch (Loai)
            {
                case LoaiThucUong.Tra:
                    return new Tra();
                case LoaiThucUong.TraSua:
                    return new TraSua();
                case LoaiThucUong.MonKhac:
                    return new MonKhac();
                default:
                    throw new ArgumentException("Invalid type", "type");
            }
        }
    }
}