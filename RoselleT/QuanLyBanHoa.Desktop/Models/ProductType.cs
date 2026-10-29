namespace QuanLyBanHoa.Desktop.Models
{
    public class ProductType
    {
        public int ProductTypeID { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Status { get; set; } = true;
    }
}