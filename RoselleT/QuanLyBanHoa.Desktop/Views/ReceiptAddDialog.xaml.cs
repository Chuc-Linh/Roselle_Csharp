using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuanLyBanHoa.Desktop.Data;
using QuanLyBanHoa.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class ReceiptAddDialog : ContentDialog
    {
        private GoodsReceipt? _editingReceipt;
        private List<Product> _allProducts = new();
        private List<DraftReceiptItemViewModel> _items = new();
        private bool _isUpdatingText = false;

        public bool IsSaved { get; private set; } = false;
        public int? SavedReceiptId { get; private set; } = null;

        public ReceiptAddDialog() : this(null)
        {
        }

        public ReceiptAddDialog(GoodsReceipt? receipt)
        {
            this.InitializeComponent();
            _editingReceipt = receipt;

            LoadProductsFromDb();

            if (_editingReceipt != null)
            {
                TxtDialogTitle.Text = $"Cập Nhật Phiếu Nhập #PN{_editingReceipt.ReceiptID:D3}";
                DpImportDate.Date = new DateTimeOffset(_editingReceipt.ImportDate);

                ReceiptPage.ExtractCleanNoteAndCompletedDate(_editingReceipt, out string cleanNote, out string completedDateText);
                TxtNote.Text = cleanNote;
                TxtCompletedDateInfo.Text = completedDateText == "Chưa chốt" ? "Chưa chốt (Đang nháp)" : completedDateText;

                LoadExistingDetails(_editingReceipt.ReceiptID);
            }
            else
            {
                TxtDialogTitle.Text = "Tạo Phiếu Nhập Hàng";
                DpImportDate.Date = DateTimeOffset.Now;
                TxtCompletedDateInfo.Text = $"Chưa chốt (Sẽ chốt: {DateTime.Now:dd/MM/yyyy})";
                RefreshListUI();
            }
        }

        private void LoadProductsFromDb()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    _allProducts = db.Products.Where(p => p.Status).OrderBy(p => p.ProductName).ToList();
                }
            }
            catch (Exception) { }
        }

        private void LoadExistingDetails(int receiptId)
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var details = db.GoodsReceiptDetails.Where(d => d.ReceiptID == receiptId).ToList();
                    var prodDict = db.Products.ToDictionary(p => p.ProductID);

                    _items = details.Select(d =>
                    {
                        prodDict.TryGetValue(d.ProductID, out var prod);
                        return new DraftReceiptItemViewModel
                        {
                            ProductID = d.ProductID,
                            ProductName = prod != null ? prod.ProductName : $"SP #{d.ProductID}",
                            SubInfo = prod != null ? $"Mã: {prod.ProductCode} | Tồn: {prod.StockQuantity} {prod.Unit}" : string.Empty,
                            Quantity = d.Quantity,
                            QuantityText = d.Quantity.ToString(),
                            ImportPrice = d.ImportPrice,
                            ImportPriceText = d.ImportPrice.ToString("N0")
                        };
                    }).ToList();

                    RefreshListUI();
                }
            }
            catch (Exception) { }
        }

        private void AsbProductSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string keyword = sender.Text.Trim().ToLower();
                if (string.IsNullOrEmpty(keyword))
                {
                    sender.ItemsSource = _allProducts.Take(15).Select(p => new ProductSearchSuggestion(p)).ToList();
                }
                else
                {
                    var matched = _allProducts
                        .Where(p => p.ProductName.ToLower().Contains(keyword) || p.ProductCode.ToLower().Contains(keyword))
                        .Take(15)
                        .Select(p => new ProductSearchSuggestion(p))
                        .ToList();
                    sender.ItemsSource = matched;
                }
            }
        }

        private void AsbProductSearch_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is ProductSearchSuggestion suggestion)
            {
                var prod = suggestion.Product;
                var existing = _items.FirstOrDefault(i => i.ProductID == prod.ProductID);

                if (existing != null)
                {
                    existing.Quantity += 1;
                    existing.QuantityText = existing.Quantity.ToString();
                }
                else
                {
                    _items.Add(new DraftReceiptItemViewModel
                    {
                        ProductID = prod.ProductID,
                        ProductName = prod.ProductName,
                        SubInfo = $"Mã: {prod.ProductCode} | Tồn: {prod.StockQuantity} {prod.Unit}",
                        Quantity = 10,
                        QuantityText = "10",
                        ImportPrice = prod.CostPrice,
                        ImportPriceText = prod.CostPrice.ToString("N0")
                    });
                }

                sender.Text = string.Empty;
                sender.ItemsSource = null;
                TxtError.Visibility = Visibility.Collapsed;
                RefreshListUI();
            }
        }

        // SỬA SỐ LƯỢNG TRỰC TIẾP TRÊN DÒNG & CẢNH BÁO NẾU NHẬP SỐ ÂM
        private void TxtRowQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText) return;
            if (sender is TextBox tb && tb.Tag is int productId)
            {
                var item = _items.FirstOrDefault(x => x.ProductID == productId);
                if (item == null) return;

                if (tb.Text.Contains("-"))
                {
                    ShowError($"Cảnh báo: Số lượng của '{item.ProductName}' không được phép nhập số âm!");
                }

                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                int qty = int.TryParse(digits, out int parsed) ? parsed : 0;

                item.Quantity = qty;
                item.QuantityText = digits;

                if (tb.Text != digits)
                {
                    _isUpdatingText = true;
                    tb.Text = digits;
                    tb.SelectionStart = tb.Text.Length;
                    _isUpdatingText = false;
                }

                UpdateRowAmountOnUI(tb, item);
                UpdateGrandTotal();
            }
        }

        // SỬA GIÁ NHẬP TRỰC TIẾP TRÊN DÒNG & CẢNH BÁO NẾU NHẬP SỐ ÂM
        private void TxtRowPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText) return;
            if (sender is TextBox tb && tb.Tag is int productId)
            {
                var item = _items.FirstOrDefault(x => x.ProductID == productId);
                if (item == null) return;

                if (tb.Text.Contains("-"))
                {
                    ShowError($"Cảnh báo: Giá nhập của '{item.ProductName}' không được phép nhập số âm!");
                }

                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                decimal price = decimal.TryParse(digits, out decimal parsed) ? parsed : 0;

                item.ImportPrice = price;

                _isUpdatingText = true;
                if (!string.IsNullOrEmpty(digits))
                {
                    tb.Text = price.ToString("N0");
                    tb.SelectionStart = tb.Text.Length;
                }
                else
                {
                    tb.Text = string.Empty;
                }
                item.ImportPriceText = tb.Text;
                _isUpdatingText = false;

                UpdateRowAmountOnUI(tb, item);
                UpdateGrandTotal();
            }
        }

        private void UpdateRowAmountOnUI(TextBox sourceTextBox, DraftReceiptItemViewModel item)
        {
            if (sourceTextBox.Parent is Grid rowGrid)
            {
                foreach (var child in rowGrid.Children)
                {
                    if (child is TextBlock txt && Grid.GetColumn(txt) == 3)
                    {
                        txt.Text = item.AmountText;
                        break;
                    }
                }
            }
        }

        private void BtnRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                _items.RemoveAll(x => x.ProductID == productId);
                RefreshListUI();
            }
        }

        private void RefreshListUI()
        {
            bool hasItems = _items.Count > 0;
            EmptyStateBorder.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            DraftDetailListView.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;

            DraftDetailListView.ItemsSource = null;
            DraftDetailListView.ItemsSource = _items;

            UpdateGrandTotal();
        }

        private void UpdateGrandTotal()
        {
            decimal total = _items.Sum(x => x.Quantity * x.ImportPrice);
            TxtGrandTotal.Text = $"{total:N0} VNĐ";
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            SaveReceiptToDatabase(completeNow: false);
        }

        private void BtnCompleteReceipt_Click(object sender, RoutedEventArgs e)
        {
            SaveReceiptToDatabase(completeNow: true);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void SaveReceiptToDatabase(bool completeNow)
        {
            if (!DpImportDate.Date.HasValue)
            {
                ShowError("Vui lòng chọn ngày tạo phiếu nhập!");
                return;
            }

            if (completeNow && _items.Count == 0)
            {
                ShowError("Không thể chốt phiếu nhập trống! Vui lòng tìm và thêm ít nhất 1 sản phẩm.");
                return;
            }

            foreach (var item in _items)
            {
                if (item.Quantity <= 0)
                {
                    ShowError($"Cảnh báo: Số lượng của '{item.ProductName}' phải lớn hơn 0 (không được âm hoặc bằng 0)!");
                    return;
                }
                if (item.ImportPrice < 0)
                {
                    ShowError($"Cảnh báo: Giá nhập của '{item.ProductName}' không hợp lệ!");
                    return;
                }
            }

            try
            {
                using (var db = new ShopDbContext())
                {
                    GoodsReceipt targetReceipt;
                    string cleanNote = string.IsNullOrWhiteSpace(TxtNote.Text) ? string.Empty : TxtNote.Text.Trim();

                    // Bước 1: Tạo/Cập nhật phiếu ở trạng thái "Chưa hoàn thành" trước để nạp chi tiết
                    if (_editingReceipt == null)
                    {
                        targetReceipt = new GoodsReceipt
                        {
                            ImportDate = DpImportDate.Date.Value.DateTime.Date, // Ngày tạo riêng
                            Note = string.IsNullOrEmpty(cleanNote) ? null : cleanNote,
                            Status = "Chưa hoàn thành"
                        };
                        db.GoodsReceipts.Add(targetReceipt);
                        db.SaveChanges();
                    }
                    else
                    {
                        targetReceipt = db.GoodsReceipts.First(r => r.ReceiptID == _editingReceipt.ReceiptID);
                        targetReceipt.ImportDate = DpImportDate.Date.Value.DateTime.Date; // Ngày tạo riêng
                        targetReceipt.Note = string.IsNullOrEmpty(cleanNote) ? null : cleanNote;

                        var oldDetails = db.GoodsReceiptDetails.Where(d => d.ReceiptID == targetReceipt.ReceiptID).ToList();
                        if (oldDetails.Any())
                        {
                            db.GoodsReceiptDetails.RemoveRange(oldDetails);
                        }
                        db.SaveChanges();
                    }

                    // Bước 2: Lưu toàn bộ danh sách mặt hàng vào GOODS_RECEIPT_DETAIL
                    foreach (var item in _items)
                    {
                        db.GoodsReceiptDetails.Add(new GoodsReceiptDetail
                        {
                            ReceiptID = targetReceipt.ReceiptID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            ImportPrice = item.ImportPrice
                        });
                    }
                    db.SaveChanges();

                    // Bước 3: Nếu bấm "Chốt phiếu nhập" -> Đóng dấu Ngày chốt riêng (DateTime.Now) & đổi Status = "Đã hoàn thành"
                    if (completeNow)
                    {
                        targetReceipt.Note = ReceiptPage.BuildNoteWithCompletedDate(cleanNote, DateTime.Now);
                        targetReceipt.Status = "Đã hoàn thành";
                        db.SaveChanges();
                    }

                    IsSaved = true;
                    SavedReceiptId = targetReceipt.ReceiptID;
                    this.Hide();
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

    public class ProductSearchSuggestion
    {
        public Product Product { get; set; }
        public ProductSearchSuggestion(Product product) => Product = product;
        public override string ToString() => $"{Product.ProductCode} - {Product.ProductName} (Tồn: {Product.StockQuantity} {Product.Unit} | Giá vốn: {Product.CostPrice:N0} đ)";
    }

    public class DraftReceiptItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SubInfo { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string QuantityText { get; set; } = string.Empty;
        public decimal ImportPrice { get; set; }
        public string ImportPriceText { get; set; } = string.Empty;
        public string AmountText => $"{Quantity * ImportPrice:N0} đ";
    }
}