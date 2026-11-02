using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using QuanLyBanHoa.Desktop.Data;
using QuanLyBanHoa.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class ProductEditDialog : ContentDialog
    {
        private Product? _editingProduct;
        private List<ProductType> _types;
        private bool _isInternalTextChange = false;

        public ProductEditDialog(Product? product, List<ProductType> types)
        {
            this.InitializeComponent();
            _editingProduct = product;
            _types = types;

            CmbProductType.ItemsSource = _types;

            // Gắn sự kiện chặn nhập số âm trực tiếp cho ô Mức tồn tối thiểu
            TxtMinStock.TextChanged += TxtMinStock_TextChanged;

            if (_editingProduct != null)
            {
                // --- TRƯỜNG HỢP SỬA SẢN PHẨM: KHÓA CỨNG GIÁ ---
                this.Title = "Cập Nhật Sản Phẩm";
                TxtProductCode.Text = _editingProduct.ProductCode;
                TxtProductName.Text = _editingProduct.ProductName;
                CmbProductType.SelectedValue = _editingProduct.ProductTypeID;

                TxtCostPrice.Text = _editingProduct.CostPrice.ToString("N0");
                TxtCostPrice.IsReadOnly = true;
                TxtCostPrice.Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));

                TxtProfitRate.Text = _editingProduct.ProfitRate.ToString("0.##");
                TxtProfitRate.IsReadOnly = true;
                TxtProfitRate.Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));

                TxtStockQuantity.Text = _editingProduct.StockQuantity.ToString("N0");

                foreach (ComboBoxItem item in CmbUnit.Items)
                {
                    if (item.Content?.ToString() == _editingProduct.Unit)
                    {
                        CmbUnit.SelectedItem = item;
                        break;
                    }
                }
                if (CmbUnit.SelectedItem == null && CmbUnit.Items.Count > 0) CmbUnit.SelectedIndex = 0;

                TxtMinStock.Text = _editingProduct.MinStockLevel.ToString();
                TxtColor.Text = _editingProduct.Color ?? string.Empty;
                TxtSupplier.Text = _editingProduct.SupplierName ?? string.Empty;
                TxtDescription.Text = _editingProduct.Description ?? string.Empty;
                TglStatus.IsOn = _editingProduct.Status;

                TxtImage.Text = _editingProduct.Image ?? string.Empty;
                UpdateCalculatedSellingPrice();
            }
            else
            {
                // --- TRƯỜNG HỢP THÊM MỚI: CHO PHÉP NHẬP GIÁ BAN ĐẦU ---
                this.Title = "Thêm Sản Phẩm Mới";
                TxtStockQuantity.Text = "0 (Cập nhật khi nhập kho)";
                TxtCostPrice.IsReadOnly = false;
                TxtCostPrice.Background = new SolidColorBrush(Colors.White);
                TxtProfitRate.IsReadOnly = false;
                TxtProfitRate.Background = new SolidColorBrush(Colors.White);

                if (_types.Count > 0) CmbProductType.SelectedIndex = 0;
                if (CmbUnit.Items.Count > 0) CmbUnit.SelectedIndex = 0;
                UpdateCalculatedSellingPrice();
            }
        }

        // CHẶN NHẬP SỐ ÂM VÀ KÝ TỰ LẠ Ở Ô MỨC TỒN TỐI THIỂU
        private void TxtMinStock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalTextChange) return;

            string raw = TxtMinStock.Text.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            if (raw.Contains("-"))
            {
                _isInternalTextChange = true;
                string cleanDigits = new string(raw.Where(char.IsDigit).ToArray());
                TxtMinStock.Text = string.IsNullOrEmpty(cleanDigits) ? "0" : cleanDigits;
                TxtMinStock.SelectionStart = TxtMinStock.Text.Length;
                _isInternalTextChange = false;

                ShowError("Cảnh báo: Mức tồn tối thiểu không được phép nhập số âm!");
                return;
            }

            if (raw.Any(c => !char.IsDigit(c)))
            {
                _isInternalTextChange = true;
                TxtMinStock.Text = new string(raw.Where(char.IsDigit).ToArray());
                TxtMinStock.SelectionStart = TxtMinStock.Text.Length;
                _isInternalTextChange = false;

                ShowError("Mức tồn tối thiểu chỉ được phép nhập số nguyên dương!");
                return;
            }

            if (TxtError.Visibility == Visibility.Visible && TxtError.Text.Contains("Mức tồn"))
            {
                TxtError.Visibility = Visibility.Collapsed;
            }
        }

        // TỰ ĐỘNG ĐỊNH DẠNG GIÁ VỐN & CHẶN SỐ ÂM
        private void TxtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text.Contains("-"))
                {
                    ShowError("Cảnh báo: Giá vốn không được phép nhập số âm!");
                }

                string rawText = new string(textBox.Text.Where(char.IsDigit).ToArray());

                if (decimal.TryParse(rawText, out decimal value))
                {
                    textBox.TextChanged -= TxtPrice_TextChanged;
                    textBox.Text = value.ToString("N0");
                    textBox.SelectionStart = textBox.Text.Length;
                    textBox.TextChanged += TxtPrice_TextChanged;
                }
                else if (string.IsNullOrEmpty(rawText))
                {
                    textBox.TextChanged -= TxtPrice_TextChanged;
                    textBox.Text = "";
                    textBox.TextChanged += TxtPrice_TextChanged;
                }

                UpdateCalculatedSellingPrice();
            }
        }

        // KHI SỬA TỈ LỆ LỢI NHUẬN -> CẢNH BÁO NẾU NHẬP SỐ ÂM & TÍNH LẠI GIÁ BÁN
        private void TxtProfitRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtProfitRate != null && TxtProfitRate.Text.Trim().StartsWith("-"))
            {
                ShowError("Cảnh báo: Tỉ lệ lợi nhuận không được phép là số âm!");
            }
            UpdateCalculatedSellingPrice();
        }

        private void UpdateCalculatedSellingPrice()
        {
            if (TxtSellingPrice == null || TxtCostPrice == null || TxtProfitRate == null) return;

            string rawCost = TxtCostPrice.Text.Replace(",", "").Replace(".", "").Trim();
            decimal.TryParse(rawCost, out decimal costPrice);
            decimal.TryParse(TxtProfitRate.Text.Trim(), out decimal profitRate);

            if (costPrice >= 0 && profitRate >= 0)
            {
                decimal sellingPrice = costPrice * (1 + (profitRate / 100m));
                TxtSellingPrice.Text = $"{sellingPrice:N0} VNĐ";
            }
            else
            {
                TxtSellingPrice.Text = "0 VNĐ";
            }
        }

        private async void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    TxtImage.Text = file.Path;
                }
            }
            catch (Exception ex)
            {
                ShowError("Không thể mở cửa sổ chọn ảnh: " + ex.Message);
            }
        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            TxtImage.Text = "images/default.png";
        }

        private void TxtImage_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string path = TxtImage.Text.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    ImgPreview.Source = new BitmapImage(new Uri("ms-appx:///images/default.png"));
                }
                else if (path.StartsWith("http") || path.Contains(":\\"))
                {
                    ImgPreview.Source = new BitmapImage(new Uri(path));
                }
                else
                {
                    ImgPreview.Source = new BitmapImage(new Uri($"ms-appx:///{path.TrimStart('/', '\\')}"));
                }
            }
            catch { ImgPreview.Source = null; }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            string code = TxtProductCode.Text.Trim();
            string name = TxtProductName.Text.Trim();
            if (CmbProductType.SelectedItem is not ProductType selectedType)
            {
                ShowError("Vui lòng chọn loại sản phẩm!");
                return;
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            {
                ShowError("Vui lòng nhập đầy đủ mã và tên sản phẩm!");
                return;
            }

            if (TxtMinStock.Text.Trim().StartsWith("-") || !int.TryParse(TxtMinStock.Text.Trim(), out int minStock) || minStock < 0)
            {
                ShowError("Cảnh báo: Mức tồn tối thiểu không hợp lệ (không được nhập số âm)!");
                return;
            }

            string selectedUnit = (CmbUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Bó";

            try
            {
                using (var db = new ShopDbContext())
                {
                    bool isCodeDuplicate = db.Products.Any(p => p.ProductCode == code && (_editingProduct == null || p.ProductID != _editingProduct.ProductID));
                    if (isCodeDuplicate)
                    {
                        ShowError("Mã sản phẩm này đã tồn tại trong hệ thống!");
                        return;
                    }

                    string? imagePath = string.IsNullOrWhiteSpace(TxtImage.Text) ? null : TxtImage.Text.Trim();

                    if (_editingProduct == null)
                    {
                        string rawCostPrice = TxtCostPrice.Text.Replace(",", "").Replace(".", "");
                        if (!decimal.TryParse(rawCostPrice, out decimal costPrice) || costPrice < 0)
                        {
                            ShowError("Giá vốn không hợp lệ (không được nhập số âm)!");
                            return;
                        }

                        if (!decimal.TryParse(TxtProfitRate.Text.Trim(), out decimal profitRate) || profitRate < 0)
                        {
                            ShowError("Cảnh báo: Tỉ lệ lợi nhuận không được phép nhập số âm!");
                            return;
                        }

                        var newProduct = new Product
                        {
                            ProductCode = code,
                            ProductName = name,
                            ProductTypeID = selectedType.ProductTypeID,
                            CostPrice = costPrice,
                            ProfitRate = profitRate,
                            Unit = selectedUnit,
                            MinStockLevel = minStock,
                            Color = string.IsNullOrWhiteSpace(TxtColor.Text) ? null : TxtColor.Text.Trim(),
                            SupplierName = string.IsNullOrWhiteSpace(TxtSupplier.Text) ? null : TxtSupplier.Text.Trim(),
                            Image = imagePath,
                            StockQuantity = 0,
                            Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim(),
                            Status = TglStatus.IsOn
                        };
                        db.Products.Add(newProduct);
                    }
                    else
                    {
                        var existing = db.Products.First(p => p.ProductID == _editingProduct.ProductID);
                        existing.ProductCode = code;
                        existing.ProductName = name;
                        existing.ProductTypeID = selectedType.ProductTypeID;
                        existing.Unit = selectedUnit;
                        existing.MinStockLevel = minStock;
                        existing.Color = string.IsNullOrWhiteSpace(TxtColor.Text) ? null : TxtColor.Text.Trim();
                        existing.SupplierName = string.IsNullOrWhiteSpace(TxtSupplier.Text) ? null : TxtSupplier.Text.Trim();
                        existing.Image = imagePath;
                        existing.Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                        existing.Status = TglStatus.IsOn;
                    }

                    db.SaveChanges();
                    args.Cancel = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi lưu CSDL: " + ex.Message);
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}