using System;

namespace QuanLyBanHoa.Desktop.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Today;
        public int AccountID { get; set; }
    }
}