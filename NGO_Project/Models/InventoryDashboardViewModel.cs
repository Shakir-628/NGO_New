using System.Collections.Generic;

namespace NGO_Project
{
    public class InventoryDashboardViewModel
    {
        public InventoryItem NewItem { get; set; }
        public IEnumerable<InventoryItem> InventoryItems { get; set; }
    }
}