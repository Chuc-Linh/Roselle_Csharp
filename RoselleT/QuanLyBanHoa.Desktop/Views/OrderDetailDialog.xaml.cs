using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class OrderDetailDialog : ContentDialog
    {
        private int _orderId;

        public OrderDetailDialog(int orderId)
        {
            this.InitializeComponent();
            _orderId = orderId;
            LoadOrderDetail();
        }

        private void LoadOrderDetail()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var order = db.SalesOrders.AsNoTracking().FirstOrDefault(o => o.OrderID == _orderId);
                    if (order == null) return;

                    int custId = order.CustomerID;
                    var customer = db.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerID == custId);

                    // ĐÃ SỬA: Lấy đúng CreatedDate của đơn hàng
                    DateTime orderDate = order.CreatedDate;
                    string rawStatus = order.OrderStatus;

                    bool isCanceled = OrderPage.IsOrderCanceled(rawStatus);

                    TxtDialogTitle.Text = $"Chi tiết đơn hàng #{_orderId}";
                    TxtOrderStatus.Text = rawStatus.ToUpper();
                    TxtOrderStatus.Foreground = OrderPage.GetStatusBrush(rawStatus);

                    string recipientName = customer?.FullName ?? $"Khách hàng #{custId}";
                    string phone = customer?.PhoneNumber ?? "—";

                    string rawAddr = !string.IsNullOrWhiteSpace(order.DeliveryAddress) ? order.DeliveryAddress : (customer?.Address ?? "—");
                    string wardCity = OrderPage.ExtractWardAndCity(order.DeliveryAddress, customer?.Address);
                    string displayAddress = rawAddr.Contains(',') ? rawAddr : $"{rawAddr} {wardCity}";

                    TxtRecipientName.Text = recipientName;
                    TxtRecipientPhone.Text = phone;
                    TxtShippingAddress.Text = displayAddress;
                    TxtOrderNote.Text = "Không có ghi chú";
                    TxtOrderDate.Text = orderDate.ToString("dd/MM/yyyy");

                    var details = db.OrderDetails.AsNoTracking().Where(d => d.OrderID == _orderId).ToList();
                    var products = db.Products.AsNoTracking().ToDictionary(p => p.ProductID);

                    var itemList = details.Select(d =>
                    {
                        products.TryGetValue(d.ProductID, out var prod);
                        decimal price = d.UnitPrice > 0 ? d.UnitPrice : (prod?.SellingPrice ?? 0);
                        decimal subTotal = d.Amount > 0 ? d.Amount : (price * d.Quantity - d.Discount);

                        string img = prod?.Image ?? string.Empty;
                        string imagePath = string.IsNullOrWhiteSpace(img)
                            ? "ms-appx:///images/default.png"
                            : (img.StartsWith("http") || img.Contains(":\\") ? img : $"ms-appx:///{img.TrimStart('/', '\\')}");

                        return new OrderDetailItemViewModel
                        {
                            ProductCode = prod?.ProductCode ?? $"SP#{d.ProductID}",
                            ProductName = prod?.ProductName ?? $"Sản phẩm #{d.ProductID}",
                            Unit = prod?.Unit ?? "Bó",
                            ImagePath = imagePath,
                            Quantity = d.Quantity,
                            Price = price,
                            PriceText = $"{price:N0} VNĐ",
                            SubTotal = subTotal,
                            SubTotalText = $"{subTotal:N0} VNĐ"
                        };
                    }).ToList();

                    OrderDetailListView.ItemsSource = itemList;
                    TxtItemCount.Text = $"{itemList.Sum(x => x.Quantity)} sản phẩm";

                    decimal finalTotal = isCanceled ? 0 : itemList.Sum(x => x.SubTotal);
                    TxtGrandTotal.Text = $"{finalTotal:N0} VNĐ";
                }
            }
            catch (Exception) { }
        }
    }

    public class OrderDetailItemViewModel
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string PriceText { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public string SubTotalText { get; set; } = string.Empty;
    }
}