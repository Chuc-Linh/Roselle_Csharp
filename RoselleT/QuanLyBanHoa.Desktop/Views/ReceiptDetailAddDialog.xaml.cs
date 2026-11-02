using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuanLyBanHoa.Desktop.Data;
using QuanLyBanHoa.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class ReceiptDetailAddDialog : ContentDialog
    {
        private int _receiptId;
        private GoodsReceiptDetail? _editingDetail;
        private List<Product> _allProducts = new();

        public ReceiptDetailAddDialog(int receiptId) : this(receiptId, null)
        {
        }

        public ReceiptDetailAddDialog(int receiptId, GoodsReceiptDetail? detail)
        {
            this.InitializeComponent();
            _receiptId = receiptId;
            _editingDetail = detail;

            using (var db = new ShopDbContext())
            {
                _allProducts = db.Products
                    .Where(p => p.Status || (_editingDetail != null && p.ProductID == _editingDetail.ProductID))
                    .OrderBy(p => p.ProductName)
                    .ToList();

                CmbProducts.ItemsSource = _allProducts;

                if (_editingDetail != null)
                {
                    this.Title = "Cập Nhật Mặt Hàng Nhập Kho";
                    this.PrimaryButtonText = "Lưu thay đổi";
                    SearchPanel.Visibility = Visibility.Collapsed;
                    CmbProducts.SelectedValue = _editingDetail.ProductID;
                    CmbProducts.IsEnabled = false;
                    TxtQuantity.Text = _editingDetail.Quantity.ToString();
                    TxtImportPrice.Text = _editingDetail.ImportPrice.ToString("N0");
                }
                else
                {
                    this.Title = "Thêm Mặt Hàng Nhập Kho";
                    this.PrimaryButtonText = "Thêm";
                    if (_allProducts.Count > 0) CmbProducts.SelectedIndex = 0;
                }
            }
        }

        private void TxtSearchProduct_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = TxtSearchProduct.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(keyword)
                ? _allProducts
                : _allProducts.Where(p => p.ProductName.ToLower().Contains(keyword) || p.ProductCode.ToLower().Contains(keyword)).ToList();

            CmbProducts.ItemsSource = filtered;
            if (filtered.Count > 0)
            {
                CmbProducts.SelectedIndex = 0;
            }
            else
            {
                TxtProductHint.Text = "Không tìm thấy sản phẩm phù hợp.";
            }
        }

        private void CmbProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbProducts.SelectedItem is Product prod)
            {
                TxtProductHint.Text = $"Mã SP: {prod.ProductCode} | Tồn hiện tại: {prod.StockQuantity} {prod.Unit} | Giá vốn: {prod.CostPrice:N0} đ";
                if (_editingDetail == null)
                {
                    TxtImportPrice.Text = prod.CostPrice.ToString("N0");
                }
            }
        }

        private void TxtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
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
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            if (CmbProducts.SelectedItem is not Product selectedProd)
            {
                ShowError("Vui lòng chọn sản phẩm!");
                return;
            }

            if (!int.TryParse(TxtQuantity.Text.Trim(), out int qty) || qty <= 0)
            {
                ShowError("Số lượng nhập phải là số nguyên lớn hơn 0!");
                return;
            }

            string rawPrice = TxtImportPrice.Text.Replace(",", "").Replace(".", "").Trim();
            if (!decimal.TryParse(rawPrice, out decimal price) || price < 0)
            {
                ShowError("Giá nhập không hợp lệ!");
                return;
            }

            try
            {
                using (var db = new ShopDbContext())
                {
                    if (_editingDetail == null)
                    {
                        var existing = db.GoodsReceiptDetails.FirstOrDefault(d => d.ReceiptID == _receiptId && d.ProductID == selectedProd.ProductID);
                        if (existing != null)
                        {
                            existing.Quantity += qty;
                            existing.ImportPrice = price;
                        }
                        else
                        {
                            var detail = new GoodsReceiptDetail
                            {
                                ReceiptID = _receiptId,
                                ProductID = selectedProd.ProductID,
                                Quantity = qty,
                                ImportPrice = price
                            };
                            db.GoodsReceiptDetails.Add(detail);
                        }
                    }
                    else
                    {
                        var existing = db.GoodsReceiptDetails.FirstOrDefault(d => d.ReceiptID == _receiptId && d.ProductID == _editingDetail.ProductID);
                        if (existing != null)
                        {
                            existing.Quantity = qty;
                            existing.ImportPrice = price;
                        }
                    }

                    db.SaveChanges();
                    args.Cancel = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi: " + ex.Message);
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}