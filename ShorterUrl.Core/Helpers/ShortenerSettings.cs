using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShorterUrl.Core.Helpers
{
    public class ShortenerSettings
    {
        public string DefaultRedirectUrl { get; set; }
        public string CustomDomain { get; set; }
        public string DataStorage { get; set; }
    }
}
