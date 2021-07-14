using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doan1.Models.Factory_Method
{
    
    abstract public class PhanLoaiMon
    {
        public abstract IQueryable<Menu> LayLoai();

    }
}
