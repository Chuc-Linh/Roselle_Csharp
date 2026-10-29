using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class PriceManagementPage : Page
    {
        private List<PriceItemViewModel> _allPrices = new();

        public PriceManagementPage()
        {
            this.InitializeComponent();
            LoadPriceData();
        }

        private void LoadPriceData()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    _allPrices = db.Products
                        .AsNoTracking()
                        .OrderBy(p => p.ProductName)
                        .Select(p => new PriceItemViewModel
                        {
                            ProductID = p.ProductID,
                            ProductCode = p.ProductCode,
                            ProductName = p.ProductName,
                            CostPrice = p.CostPrice,
                            CostPriceText = $"{p.CostPrice:N0} VNĐ",
                            ProfitRate = p.ProfitRate,
                            ProfitRateText = $"{p.ProfitRate:0.##}%",
                            SellingPrice = p.SellingPrice,
                            SellingPriceText = $"{p.SellingPrice:N0} VNĐ"
                        }).ToList();

                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi load giá sản phẩm: " + ex.Message);
            }
        }

        private void ApplyFilter()
        {
            if (PriceListView == null) return;

            string keyword = TxtSearch?.Text.Trim().ToLower() ?? string.Empty;
            if (string.IsNullOrEmpty(keyword))
            {
                PriceListView.ItemsSource = _allPrices;
            }
            else
            {
                PriceListView.ItemsSource = _allPrices
                    .Where(p => p.ProductCode.ToLower().Contains(keyword) || p.ProductName.ToLower().Contains(keyword))
                    .ToList();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void BtnEditPrice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                using (var db = new ShopDbContext())
                {
                    var product = db.Products.FirstOrDefault(p => p.ProductID == productId);
                    if (product == null) return;

                    var inputTextBox = new TextBox
                    {
                        Text = product.ProfitRate.ToString("0.##"),
                        PlaceholderText = "Nhập % lợi nhuận (VD: 25)...",
                        Height = 40,
                        CornerRadius = new CornerRadius(8)
                    };

                    var txtWarning = new TextBlock
                    {
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 38, 38)), // Đỏ #DC2626
                        FontSize = 12,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                        Visibility = Visibility.Collapsed
                    };

                    // Cảnh báo tức thì ngay khi người dùng gõ dấu âm "-"
                    inputTextBox.TextChanged += (s, args) =>
                    {
                        string raw = inputTextBox.Text.Trim();
                        if (raw.StartsWith("-") || (decimal.TryParse(raw, out decimal val) && val < 0))
                        {
                            txtWarning.Text = "Cảnh báo: Tỉ lệ lợi nhuận không được phép là số âm! Vui lòng nhập từ 0% trở lên.";
                            txtWarning.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            txtWarning.Visibility = Visibility.Collapsed;
                        }
                    };

                    var stackPanel = new StackPanel { Spacing = 10, Width = 340 };
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = $"Sản phẩm: {product.ProductName}",
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 27, 36, 50))
                    });
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = $"Giá vốn hiện tại: {product.CostPrice:N0} VNĐ",
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                        FontSize = 13
                    });
                    stackPanel.Children.Add(inputTextBox);
                    stackPanel.Children.Add(txtWarning);

                    var dialog = new ContentDialog
                    {
                        Title = "Cập Nhật Tỉ Lệ Lợi Nhuận",
                        Content = stackPanel,
                        PrimaryButtonText = "Lưu thay đổi",
                        CloseButtonText = "Hủy",
                        XamlRoot = this.XamlRoot,
                        DefaultButton = ContentDialogButton.Primary,
                        RequestedTheme = ElementTheme.Light
                    };

                    dialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Color.FromArgb(255, 5, 150, 105));
                    dialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

                    // Chặn đóng hộp thoại và báo lỗi nếu nhập lợi nhuận âm hoặc sai định dạng
                    dialog.PrimaryButtonClick += (s, args) =>
                    {
                        string raw = inputTextBox.Text.Trim();
                        if (string.IsNullOrEmpty(raw) || !decimal.TryParse(raw, out decimal newRate))
                        {
                            args.Cancel = true;
                            txtWarning.Text = "Vui lòng nhập một con số hợp lệ cho tỉ lệ lợi nhuận!";
                            txtWarning.Visibility = Visibility.Visible;
                            return;
                        }

                        if (newRate < 0)
                        {
                            args.Cancel = true;
                            txtWarning.Text = "Cảnh báo: Tỉ lệ lợi nhuận không được phép âm! Vui lòng nhập giá trị >= 0%.";
                            txtWarning.Visibility = Visibility.Visible;
                            return;
                        }

                        product.ProfitRate = newRate;
                        db.SaveChanges();
                    };

                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        LoadPriceData();
                    }
                }
            }
        }
    }

    public class PriceItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public string CostPriceText { get; set; } = string.Empty;
        public decimal ProfitRate { get; set; }
        public string ProfitRateText { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public string SellingPriceText { get; set; } = string.Empty;
    }
}