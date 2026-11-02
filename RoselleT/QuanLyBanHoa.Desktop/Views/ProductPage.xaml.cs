using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using QuanLyBanHoa.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class ProductPage : Page
    {
        private List<ProductViewModel> _allProducts = new();
        private List<ProductType> _productTypes = new();

        public ProductPage()
        {
            this.InitializeComponent();
            LoadProductTypesFilter();
            LoadProducts();
        }

        private void LoadProductTypesFilter()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    _productTypes = db.ProductTypes.Where(t => t.Status).ToList();
                    var filterList = new List<ProductType>
                    {
                        new ProductType { ProductTypeID = 0, ProductTypeName = "Tất cả loại hoa" }
                    };
                    filterList.AddRange(_productTypes);
                    CmbFilterType.ItemsSource = filterList;
                    CmbFilterType.SelectedIndex = 0;
                }
            }
            catch (Exception) { }
        }

        private void LoadProducts()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var products = db.Products.ToList();
                    var types = db.ProductTypes.ToDictionary(t => t.ProductTypeID, t => t.ProductTypeName);

                    // Tìm những sản phẩm đã có giao dịch để không cho xóa (buộc phải dùng nút khóa)
                    List<int> usedProductIds = new List<int>();
                    try
                    {
                        // Kiểm tra nếu DbSet OrderDetails và GoodsReceiptDetails đã có trong DbContext
                        usedProductIds.AddRange(db.OrderDetails.Select(o => o.ProductID).Distinct().ToList());
                        usedProductIds.AddRange(db.GoodsReceiptDetails.Select(g => g.ProductID).Distinct().ToList());
                    }
                    catch { } // Bỏ qua nếu chưa cài đặt DbSet cho các bảng này

                    _allProducts = products.Select(p =>
                    {
                        bool canDelete = !usedProductIds.Contains(p.ProductID);

                        return new ProductViewModel
                        {
                            ProductID = p.ProductID,
                            ProductCode = p.ProductCode,
                            ProductName = p.ProductName,
                            ProductTypeID = p.ProductTypeID,
                            TypeName = types.ContainsKey(p.ProductTypeID) ? types[p.ProductTypeID] : "Khác",

                            // Xử lý đường dẫn ảnh 
                            ImagePath = string.IsNullOrWhiteSpace(p.Image)
                                ? "ms-appx:///images/default.png"
                                : (p.Image.StartsWith("http") || p.Image.Contains(":\\") ? p.Image : $"ms-appx:///{p.Image.TrimStart('/', '\\')}"),

                            CostPriceText = $"{p.CostPrice:N0} đ",
                            SellingPriceText = $"{p.SellingPrice:N0} đ",
                            StockQuantity = p.StockQuantity,
                            StatusText = p.Status ? "Kinh doanh" : "Ngừng bán",
                            StatusBg = new SolidColorBrush(p.Status ? Color.FromArgb(255, 236, 253, 245) : Color.FromArgb(255, 254, 242, 242)),
                            StatusFg = new SolidColorBrush(p.Status ? Color.FromArgb(255, 5, 150, 105) : Color.FromArgb(255, 220, 38, 38)),
                            StockBg = new SolidColorBrush(p.StockQuantity <= p.MinStockLevel ? Color.FromArgb(255, 254, 242, 242) : Color.FromArgb(255, 248, 250, 252)),
                            StockFg = new SolidColorBrush(p.StockQuantity <= p.MinStockLevel ? Color.FromArgb(255, 220, 38, 38) : Color.FromArgb(255, 27, 36, 50)),

                            // Ẩn/Hiện nút Khóa và Xóa
                            LockVisibility = canDelete ? Visibility.Collapsed : Visibility.Visible,
                            DeleteVisibility = canDelete ? Visibility.Visible : Visibility.Collapsed,

                            LockIcon = p.Status ? "\uE785" : "\uE72E",
                            ToggleBg = p.Status ? new SolidColorBrush(Color.FromArgb(255, 252, 211, 77)) : new SolidColorBrush(Color.FromArgb(255, 229, 231, 235))
                        };
                    }).OrderByDescending(p => p.ProductID).ToList();

                    ApplyFilter();
                }
            }
            catch (Exception) { }
        }

        private void ApplyFilter()
        {
            string keyword = TxtSearch.Text.Trim().ToLower();
            int selectedTypeId = (CmbFilterType.SelectedItem as ProductType)?.ProductTypeID ?? 0;
            var query = _allProducts.AsEnumerable();

            if (selectedTypeId > 0) query = query.Where(p => p.ProductTypeID == selectedTypeId);
            if (!string.IsNullOrEmpty(keyword)) query = query.Where(p => p.ProductName.ToLower().Contains(keyword) || p.ProductCode.ToLower().Contains(keyword));

            ProductListView.ItemsSource = query.ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void CmbFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = string.Empty;
            CmbFilterType.SelectedIndex = 0;
            LoadProducts();
        }

        private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductEditDialog(null, _productTypes) { XamlRoot = this.XamlRoot };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary) LoadProducts();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                using (var db = new ShopDbContext())
                {
                    var product = db.Products.FirstOrDefault(p => p.ProductID == productId);
                    if (product != null)
                    {
                        var dialog = new ProductEditDialog(product, _productTypes) { XamlRoot = this.XamlRoot };
                        if (await dialog.ShowAsync() == ContentDialogResult.Primary) LoadProducts();
                    }
                }
            }
        }

        private async void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                using (var db = new ShopDbContext())
                {
                    var product = db.Products.FirstOrDefault(p => p.ProductID == productId);
                    if (product != null)
                    {
                        string actionText = product.Status ? "ngừng bán" : "mở bán lại";
                        var dialog = new ContentDialog
                        {
                            Title = "Cảnh báo",
                            Content = $"Bạn có chắc chắn muốn {actionText} sản phẩm này không?",
                            PrimaryButtonText = "Có",
                            CloseButtonText = "Không",
                            XamlRoot = this.XamlRoot,
                            DefaultButton = ContentDialogButton.Primary
                        };
                        dialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Colors.Red);
                        dialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

                        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            product.Status = !product.Status;
                            db.SaveChanges();
                            LoadProducts();
                        }
                    }
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                var dialog = new ContentDialog
                {
                    Title = "Xác nhận xóa",
                    Content = "Bạn có chắc chắn muốn xóa vĩnh viễn sản phẩm này khỏi hệ thống?",
                    PrimaryButtonText = "Xóa",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };
                dialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Colors.Red);
                dialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using (var db = new ShopDbContext())
                        {
                            var product = db.Products.FirstOrDefault(p => p.ProductID == productId);
                            if (product != null)
                            {
                                db.Products.Remove(product);
                                db.SaveChanges();
                                LoadProducts();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        var errDialog = new ContentDialog
                        {
                            Title = "Không thể xóa",
                            Content = "Sản phẩm này đã phát sinh dữ liệu (Đơn hàng/Nhập kho).\n\nVui lòng làm mới trang và sử dụng chức năng 'Ngừng bán' để ẩn sản phẩm đi.",
                            CloseButtonText = "Đã hiểu",
                            XamlRoot = this.XamlRoot
                        };
                        await errDialog.ShowAsync();
                    }
                }
            }
        }
    }

    public class ProductViewModel
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ProductTypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string CostPriceText { get; set; } = string.Empty;
        public string SellingPriceText { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public SolidColorBrush StatusBg { get; set; } = new(Colors.Transparent);
        public SolidColorBrush StatusFg { get; set; } = new(Colors.Black);
        public SolidColorBrush StockBg { get; set; } = new(Colors.Transparent);
        public SolidColorBrush StockFg { get; set; } = new(Colors.Black);

        // Điều khiển hiển thị Nút Khóa / Xóa
        public Visibility LockVisibility { get; set; } = Visibility.Visible;
        public Visibility DeleteVisibility { get; set; } = Visibility.Collapsed;

        public string LockIcon { get; set; } = string.Empty;
        public SolidColorBrush ToggleBg { get; set; } = new(Colors.Transparent);
    }
}