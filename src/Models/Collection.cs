using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace src.Models
{
    public class Collection
    {
        [Key]
        public string userid { get; set; }
        public string collection_id { get; set; }
    }
}
