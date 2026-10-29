using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuanLyBanHoa.Desktop.Data;
using System;
using System.Linq;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class StockWarningPage : Page
    {
        private bool _isShowingDialog = false;
        private bool _isInternalChange = false;

        public StockWarningPage()
        {
            this.InitializeComponent();
            LoadWarningData();
        }

        private async void TxtThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalChange) return;

            string raw = TxtThreshold.Text.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            // Kiểm tra chặn số âm
            if (raw.StartsWith("-") || (int.TryParse(raw, out int val) && val < 0))
            {
                _isInternalChange = true;
                TxtThreshold.Text = "10";
                TxtThreshold.SelectionStart = TxtThreshold.Text.Length;
                _isInternalChange = false;

                await ShowErrorDialog("Ngưỡng cảnh báo tồn kho không được phép nhập số âm! Hệ thống đã đặt lại về 10.");
                LoadWarningData();
                return;
            }

            if (!int.TryParse(raw, out _))
            {
                _isInternalChange = true;
                TxtThreshold.Text = "10";
                TxtThreshold.SelectionStart = TxtThreshold.Text.Length;
                _isInternalChange = false;

                await ShowErrorDialog("Vui lòng chỉ nhập số nguyên không âm cho ngưỡng cảnh báo!");
                LoadWarningData();
                return;
            }

            LoadWarningData();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string raw = TxtThreshold.Text.Trim();
            if (raw.StartsWith("-") || !int.TryParse(raw, out int val) || val < 0)
            {
                _isInternalChange = true;
                TxtThreshold.Text = "10";
                _isInternalChange = false;
                await ShowErrorDialog("Ngưỡng cảnh báo không hợp lệ (không được nhập số âm)!");
            }

            LoadWarningData();
        }

        private async System.Threading.Tasks.Task ShowErrorDialog(string message)
        {
            if (_isShowingDialog || this.XamlRoot == null) return;
            _isShowingDialog = true;
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Lỗi dữ liệu đầu vào",
                    Content = message,
                    CloseButtonText = "Đã hiểu",
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Light
                };
                await dialog.ShowAsync();
            }
            finally
            {
                _isShowingDialog = false;
            }
        }

        private void LoadWarningData()
        {
            try
            {
                int threshold = int.TryParse(TxtThreshold.Text.Trim(), out int t) && t >= 0 ? t : 10;

                using (var db = new ShopDbContext())
                {
                    var types = db.ProductTypes.AsNoTracking().ToDictionary(pt => pt.ProductTypeID, pt => pt.ProductTypeName);

                    var lowStockProducts = db.Products
                        .AsNoTracking()
                        .Where(p => p.StockQuantity <= threshold && p.Status)
                        .ToList()
                        .Select(p =>
                        {
                            string img = p.Image ?? string.Empty;
                            string imagePath = string.IsNullOrWhiteSpace(img)
                                ? "ms-appx:///images/default.png"
                                : (img.StartsWith("http") || img.Contains(":\\") ? img : $"ms-appx:///{img.TrimStart('/', '\\')}");

                            types.TryGetValue(p.ProductTypeID, out var categoryName);

                            return new StockWarningItemViewModel
                            {
                                ProductName = p.ProductName,
                                CategoryName = categoryName ?? "Khác",
                                StockQuantity = p.StockQuantity,
                                Unit = p.Unit,
                                ImagePath = imagePath
                            };
                        }).ToList();

                    WarningListView.ItemsSource = lowStockProducts;
                    TxtSummaryCount.Text = $"Có {lowStockProducts.Count} sản phẩm cần nhập thêm hàng.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi load cảnh báo tồn kho: " + ex.Message);
            }
        }
    }

    public class StockWarningItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
    }
}