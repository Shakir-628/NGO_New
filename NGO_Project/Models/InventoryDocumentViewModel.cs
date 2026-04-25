using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NGO_Project.Models
{
    public class InventoryDocumentViewModel
    {
        public List<InventoryItem> InventoryItems { get; set; }
        public List<Document> Documents { get; set; }
    }
}