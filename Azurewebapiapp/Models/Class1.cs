using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Azurewebapiapp.Models
{
    public class Vmdata
    {
        public string VMname { get; set; }
        public string UserName { get; set; } 

        public string Password { get; set; }

       

        public int Disk1_Size { get; set; }
        public int Disk2_Size { get; set; }
        public string Resource_Grp_Name { get; set; }
        public string Vnet { get; set; }
        public string Availabilityset { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string TenentID { get; set; }

        public string NatRuleName { get; set; }
        public Int16 FrontEndPort { get; set; }
        public Int16 BackEndPort { get; set; }

    }
}