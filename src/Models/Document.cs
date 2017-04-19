using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace src.Models
{
    public class Document
    {
        public string filename { get; set; }
        [Key]
        public string document_id { get; set; }
    }
}
