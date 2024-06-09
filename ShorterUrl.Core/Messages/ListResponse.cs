using ShorterUrl.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShorterUrl.Core.Messages
{
    public class ListResponse
    {
        public List<ShortUrlEntity> UrlList { get; set; }

        public ListResponse() { }
        public ListResponse(List<ShortUrlEntity> list)
        {
            UrlList = list;
        }
    }
}
