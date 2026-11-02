using System;

namespace QuanLyBanHoa.Desktop.Models
{
    public class Account
    {
        public int AccountID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Today;
    }
}