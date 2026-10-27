using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roselle.Web.Models
{
    [Table("PAYMENT")]
    public class Payment
    {
        [Key] // BẮT BUỘC PHẢI CÓ DÒNG NÀY
        [Column("PaymentID")]
        public int PaymentID { get; set; }

        [Column("OrderID")]
        public int OrderID { get; set; }

        [Column("Method")]
        public string Method { get; set; } = "";

        [Column("Status")]
        public string Status { get; set; } = "";

        [Column("PaymentDate")]
        public DateTime? PaymentDate { get; set; }
    }
}