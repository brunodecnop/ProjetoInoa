using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoInoa
{
    class Config
    {
        public string SenderEmail { get; set; }
        public string SenderPW { get; set; }
        public string TargetEmail { get; set; }
        public string SMTPAddress { get; set; }
        public int SMTPPort { get; set; }
        public bool EnableSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string APIKey { get; set; }

    }
}
