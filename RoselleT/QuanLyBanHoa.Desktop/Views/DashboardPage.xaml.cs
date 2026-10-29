using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using System;
using System.Linq;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            this.InitializeComponent();
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var orders = db.SalesOrders.AsNoTracking().ToList();
                    var orderDetails = db.OrderDetails.AsNoTracking().ToList();
                    var products = db.Products.AsNoTracking().ToList();
                    var customers = db.Customers.AsNoTracking().ToList();

                    // 1. Tính Doanh thu & Lợi nhuận từ các đơn không bị hủy
                    var validOrderIds = orders
                        .Where(o => !OrderPage.IsOrderCanceled(o.OrderStatus))
                        .Select(o => o.OrderID)
                        .ToHashSet();

                    var validDetails = orderDetails
                        .Where(od => validOrderIds.Contains(od.OrderID))
                        .ToList();

                    decimal totalRevenue = validDetails.Sum(od => od.Quantity * od.UnitPrice - od.Discount);
                    decimal totalProfit = validDetails.Sum(od => (od.UnitPrice - od.CostPrice) * od.Quantity - od.Discount);

                    TxtTotalRevenue.Text = $"{totalRevenue:N0} đ";
                    TxtTotalProfit.Text = $"{totalProfit:N0} đ";

                    // 2. Thống kê Đơn hàng mới
                    int newOrderCount = orders.Count(o => OrderPage.GetStatusOrderWeight(o.OrderStatus) == 1);
                    TxtNewOrders.Text = newOrderCount.ToString();
                    TxtTotalOrdersSub.Text = $"Trên tổng số {orders.Count} đơn hàng";

                    // 3. Thống kê Sản phẩm sắp hết hàng (StockQuantity <= MinStockLevel)
                    var lowStockItems = products
                        .Where(p => p.Status && p.StockQuantity <= p.MinStockLevel)
                        .OrderBy(p => p.StockQuantity)
                        .Take(6)
                        .ToList();

                    int totalLowStock = products.Count(p => p.Status && p.StockQuantity <= p.MinStockLevel);
                    TxtLowStockCount.Text = totalLowStock.ToString();
                    TxtTotalProductsSub.Text = $"Trên tổng số {products.Count} sản phẩm";

                    LowStockList.ItemsSource = lowStockItems;
                    TxtNoLowStock.Visibility = lowStockItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                    // 4. Lấy 5 đơn hàng gần đây nhất đồng bộ với trang Quản lý Đơn hàng
                    var recentOrders = orders
                        .OrderByDescending(o => o.CreatedDate)
                        .ThenByDescending(o => o.OrderID)
                        .Take(5)
                        .Select(o =>
                        {
                            var cust = customers.FirstOrDefault(c => c.CustomerID == o.CustomerID);
                            string custName = cust?.FullName ?? $"Khách #{o.CustomerID}";
                            string area = OrderPage.ExtractWardAndCity(o.DeliveryAddress, cust?.Address);

                            bool isCanceled = OrderPage.IsOrderCanceled(o.OrderStatus);
                            decimal totalAmt = isCanceled ? 0 : orderDetails
                                .Where(od => od.OrderID == o.OrderID)
                                .Sum(od => od.Amount > 0 ? od.Amount : (od.Quantity * od.UnitPrice - od.Discount));

                            return new RecentOrderViewModel
                            {
                                OrderCode = $"#ĐH{o.OrderID:D3}",
                                CustomerName = custName,
                                ShippingArea = area,
                                CreatedDateText = o.CreatedDate.ToString("dd/MM/yyyy"),
                                TotalAmountText = $"{totalAmt:N0} đ",
                                OrderStatus = o.OrderStatus,
                                StatusFg = OrderPage.GetStatusBrush(o.OrderStatus)
                            };
                        })
                        .ToList();

                    RecentOrdersList.ItemsSource = recentOrders;
                }
            }
            catch (Exception ex)
            {
                TxtTotalOrdersSub.Text = "Lỗi tải dữ liệu: " + ex.Message;
            }
        }
    }

    public class RecentOrderViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ShippingArea { get; set; } = string.Empty;
        public string CreatedDateText { get; set; } = string.Empty;
        public string TotalAmountText { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public SolidColorBrush StatusFg { get; set; } = new(Colors.Black);
    }
}