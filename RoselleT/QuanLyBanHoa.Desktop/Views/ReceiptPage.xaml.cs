using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using QuanLyBanHoa.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class ReceiptPage : Page
    {
        private int? _selectedReceiptId = null;
        private List<ReceiptViewModel> _allReceipts = new();

        public ReceiptPage()
        {
            this.InitializeComponent();
            LoadReceipts();
        }

        // HÀM DÙNG CHUNG ĐỂ TÁCH GHI CHÚ GỐC VÀ NGÀY CHỐT RIÊNG BIỆT (KHÔNG CẦN SỬA DB)
        public static void ExtractCleanNoteAndCompletedDate(GoodsReceipt r, out string cleanNote, out string completedDateText)
        {
            string raw = r.Note ?? string.Empty;
            bool isCompleted = r.Status == "Đã hoàn thành" || r.Status == "Hoàn thành";

            var match = Regex.Match(raw, @"\[Chốt ngày:\s*([^\]]+)\]");
            if (match.Success)
            {
                string fullStamp = match.Groups[1].Value.Trim();
                // Chỉ lấy phần dd/MM/yyyy cho gọn đẹp trên cột
                completedDateText = fullStamp.Length >= 10 ? fullStamp.Substring(0, 10) : fullStamp;
                cleanNote = raw.Replace(match.Value, "").Trim();
            }
            else
            {
                cleanNote = raw.Trim();
                completedDateText = isCompleted ? r.ImportDate.ToString("dd/MM/yyyy") : "Chưa chốt";
            }
        }

        public static string BuildNoteWithCompletedDate(string? cleanNote, DateTime completedDate)
        {
            string baseNote = (cleanNote ?? string.Empty).Trim();
            baseNote = Regex.Replace(baseNote, @"\[Chốt ngày:\s*([^\]]+)\]", "").Trim();
            return $"{baseNote} [Chốt ngày: {completedDate:dd/MM/yyyy}]".Trim();
        }

        private void LoadReceipts()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var data = db.GoodsReceipts.OrderByDescending(r => r.ReceiptID).ToList();
                    var allDetails = db.GoodsReceiptDetails.ToList();

                    var totalByReceipt = allDetails
                        .GroupBy(d => d.ReceiptID)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity * x.ImportPrice));

                    _allReceipts = data.Select(r =>
                    {
                        decimal total = totalByReceipt.ContainsKey(r.ReceiptID) ? totalByReceipt[r.ReceiptID] : 0;
                        bool isCompleted = r.Status == "Đã hoàn thành" || r.Status == "Hoàn thành";

                        ExtractCleanNoteAndCompletedDate(r, out string cleanNote, out string completedDateText);

                        return new ReceiptViewModel
                        {
                            ReceiptID = r.ReceiptID,
                            ReceiptCode = $"#PN{r.ReceiptID:D3}",
                            ImportDateText = r.ImportDate.ToString("dd/MM/yyyy"),      // Ngày tạo riêng
                            CompletedDateText = completedDateText,                    // Ngày chốt riêng
                            CompletedDateFg = new SolidColorBrush(isCompleted
                                ? Color.FromArgb(255, 5, 150, 105)                    // Xanh lá khi đã chốt
                                : Color.FromArgb(255, 148, 163, 184)),                // Xám nhạt khi "Chưa chốt"
                            Note = string.IsNullOrWhiteSpace(cleanNote) ? "—" : cleanNote,
                            TotalAmount = total,
                            TotalAmountText = $"{total:N0} đ",
                            Status = r.Status,
                            StatusBg = new SolidColorBrush(isCompleted ? Color.FromArgb(255, 236, 253, 245) : Color.FromArgb(255, 254, 243, 199)),
                            StatusFg = new SolidColorBrush(isCompleted ? Color.FromArgb(255, 5, 150, 105) : Color.FromArgb(255, 180, 83, 9)),
                            ActionVisibility = !isCompleted ? Visibility.Visible : Visibility.Collapsed
                        };
                    }).ToList();

                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải phiếu nhập: " + ex.Message, isError: true);
            }
        }

        private void ApplyFilter()
        {
            if (ReceiptListView == null) return;

            string keyword = TxtSearch?.Text.Trim().ToLower() ?? string.Empty;
            string selectedStatus = (CmbFilterStatus?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả trạng thái";

            var query = _allReceipts.AsEnumerable();

            if (selectedStatus != "Tất cả trạng thái")
            {
                query = query.Where(r => r.Status == selectedStatus);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.ReceiptCode.ToLower().Contains(keyword) ||
                                         r.Note.ToLower().Contains(keyword));
            }

            var filteredList = query.ToList();
            ReceiptListView.ItemsSource = filteredList;

            if (_selectedReceiptId.HasValue)
            {
                var target = filteredList.FirstOrDefault(r => r.ReceiptID == _selectedReceiptId.Value);
                if (target != null)
                {
                    ReceiptListView.SelectedItem = target;
                }
                else
                {
                    ClearDetailPanel();
                }
            }
        }

        private void ClearDetailPanel()
        {
            _selectedReceiptId = null;
            TxtDetailTitle.Text = "Chi tiết phiếu nhập";
            TxtDetailSub.Text = "Chọn một phiếu nhập bên trái để xem chi tiết";
            BtnCompleteReceipt.Visibility = Visibility.Collapsed;
            BtnAddDetail.Visibility = Visibility.Collapsed;
            DetailListView.ItemsSource = null;
            TxtTotalReceiptAmount.Text = "0 đ";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void CmbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = string.Empty;
            CmbFilterStatus.SelectedIndex = 0;
            TxtMessage.Visibility = Visibility.Collapsed;
            LoadReceipts();
        }

        private void ReceiptListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReceiptListView.SelectedItem is ReceiptViewModel selected)
            {
                _selectedReceiptId = selected.ReceiptID;
                TxtDetailTitle.Text = $"Chi tiết Phiếu Nhập {selected.ReceiptCode}";
                TxtDetailSub.Text = $"Ngày tạo: {selected.ImportDateText}   |   Ngày chốt: {selected.CompletedDateText}   |   Ghi chú: {selected.Note}";

                bool isPending = selected.Status != "Đã hoàn thành";
                BtnCompleteReceipt.Visibility = isPending ? Visibility.Visible : Visibility.Collapsed;
                BtnAddDetail.Visibility = isPending ? Visibility.Visible : Visibility.Collapsed;

                LoadReceiptDetails(selected.ReceiptID, isPending);
            }
        }

        private void LoadReceiptDetails(int receiptId, bool isEditable)
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var details = db.GoodsReceiptDetails.Where(d => d.ReceiptID == receiptId).ToList();
                    var products = db.Products.ToDictionary(p => p.ProductID, p => p.ProductName);

                    var viewList = details.Select(d => new ReceiptDetailViewModel
                    {
                        ProductID = d.ProductID,
                        ProductName = products.ContainsKey(d.ProductID) ? products[d.ProductID] : $"SP #{d.ProductID}",
                        Quantity = d.Quantity,
                        ImportPrice = d.ImportPrice,
                        ImportPriceText = $"{d.ImportPrice:N0} đ",
                        AmountText = $"{d.Quantity * d.ImportPrice:N0} đ",
                        ActionVisibility = isEditable ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();

                    DetailListView.ItemsSource = viewList;

                    decimal total = details.Sum(d => d.Quantity * d.ImportPrice);
                    TxtTotalReceiptAmount.Text = $"{total:N0} đ";
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải chi tiết: " + ex.Message, isError: true);
            }
        }

        private async void BtnAddReceipt_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ReceiptAddDialog(null) { XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
            if (dialog.IsSaved)
            {
                _selectedReceiptId = dialog.SavedReceiptId;
                LoadReceipts();
                ShowMessage("Đã lưu phiếu nhập hàng thành công!", isError: false);
            }
        }

        private async void BtnEditReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                using (var db = new ShopDbContext())
                {
                    var receipt = db.GoodsReceipts.FirstOrDefault(r => r.ReceiptID == receiptId);
                    if (receipt != null && receipt.Status != "Đã hoàn thành")
                    {
                        var dialog = new ReceiptAddDialog(receipt) { XamlRoot = this.XamlRoot };
                        await dialog.ShowAsync();
                        if (dialog.IsSaved)
                        {
                            _selectedReceiptId = receiptId;
                            LoadReceipts();
                            ShowMessage("Đã cập nhật phiếu nhập hàng thành công!", isError: false);
                        }
                    }
                }
            }
        }

        private async void BtnDeleteReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Xác nhận xóa phiếu nhập",
                    Content = $"Bạn có chắc chắn muốn xóa phiếu nhập #PN{receiptId:D3} (chưa hoàn thành) cùng toàn bộ mặt hàng bên trong không?",
                    PrimaryButtonText = "Xóa",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                    RequestedTheme = ElementTheme.Light
                };
                confirmDialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Colors.Red);
                confirmDialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using (var db = new ShopDbContext())
                        {
                            var receipt = db.GoodsReceipts.FirstOrDefault(r => r.ReceiptID == receiptId);
                            if (receipt != null && receipt.Status != "Đã hoàn thành")
                            {
                                var details = db.GoodsReceiptDetails.Where(d => d.ReceiptID == receiptId).ToList();
                                if (details.Any())
                                {
                                    db.GoodsReceiptDetails.RemoveRange(details);
                                }
                                db.GoodsReceipts.Remove(receipt);
                                db.SaveChanges();

                                if (_selectedReceiptId == receiptId)
                                {
                                    ClearDetailPanel();
                                }
                                LoadReceipts();
                                ShowMessage("Đã xóa phiếu nhập nháp thành công.", isError: false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessage("Lỗi xóa phiếu nhập: " + ex.Message, isError: true);
                    }
                }
            }
        }

        private async void BtnAddDetail_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedReceiptId.HasValue) return;

            var dialog = new ReceiptDetailAddDialog(_selectedReceiptId.Value, null) { XamlRoot = this.XamlRoot };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                LoadReceipts();
                ShowMessage("Đã thêm mặt hàng vào phiếu nhập.", isError: false);
            }
        }

        private async void BtnEditDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId && _selectedReceiptId.HasValue)
            {
                using (var db = new ShopDbContext())
                {
                    var detail = db.GoodsReceiptDetails.FirstOrDefault(d => d.ReceiptID == _selectedReceiptId.Value && d.ProductID == productId);
                    if (detail != null)
                    {
                        var dialog = new ReceiptDetailAddDialog(_selectedReceiptId.Value, detail) { XamlRoot = this.XamlRoot };
                        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            LoadReceipts();
                            ShowMessage("Đã cập nhật mặt hàng trong phiếu nhập.", isError: false);
                        }
                    }
                }
            }
        }

        private async void BtnDeleteDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId && _selectedReceiptId.HasValue)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Xác nhận xóa mặt hàng",
                    Content = "Bạn có chắc chắn muốn xóa mặt hàng này khỏi phiếu nhập không?",
                    PrimaryButtonText = "Xóa",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                    RequestedTheme = ElementTheme.Light
                };
                confirmDialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Colors.Red);
                confirmDialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using (var db = new ShopDbContext())
                        {
                            var item = db.GoodsReceiptDetails.FirstOrDefault(d => d.ReceiptID == _selectedReceiptId.Value && d.ProductID == productId);
                            if (item != null)
                            {
                                db.GoodsReceiptDetails.Remove(item);
                                db.SaveChanges();
                                LoadReceipts();
                                ShowMessage("Đã xóa mặt hàng khỏi phiếu nhập.", isError: false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessage("Lỗi xóa mặt hàng: " + ex.Message, isError: true);
                    }
                }
            }
        }

        // KHI ẤN NÚT "HOÀN THÀNH & CỘNG KHO" -> TỰ ĐỘNG TẠO NGÀY CHỐT RIÊNG (HÔM NAY) VÀ GIỮ NGUYÊN NGÀY TẠO CŨ
        private async void BtnCompleteReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedReceiptId.HasValue) return;

            using (var db = new ShopDbContext())
            {
                bool hasDetails = db.GoodsReceiptDetails.Any(d => d.ReceiptID == _selectedReceiptId.Value);
                if (!hasDetails)
                {
                    ShowMessage("Không thể hoàn thành phiếu nhập trống! Vui lòng thêm ít nhất 1 mặt hàng.", isError: true);
                    return;
                }
            }

            string todayStr = DateTime.Now.ToString("dd/MM/yyyy");
            var confirmDialog = new ContentDialog
            {
                Title = "Xác nhận Hoàn thành & Cộng kho",
                Content = $"Khi hoàn thành phiếu nhập:\n• Ngày chốt phiếu sẽ được ghi nhận là: {todayStr}\n• Hệ thống tự động cộng tồn kho, tính lại giá vốn bình quân và khóa phiếu nhập này.\n\nBạn có chắc chắn muốn hoàn thành?",
                PrimaryButtonText = "Hoàn thành",
                CloseButtonText = "Hủy",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = ElementTheme.Light
            };
            confirmDialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Color.FromArgb(255, 5, 150, 105));
            confirmDialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    using (var db = new ShopDbContext())
                    {
                        var receipt = db.GoodsReceipts.FirstOrDefault(r => r.ReceiptID == _selectedReceiptId.Value);
                        if (receipt != null)
                        {
                            ExtractCleanNoteAndCompletedDate(receipt, out string cleanNote, out _);
                            receipt.Note = BuildNoteWithCompletedDate(cleanNote, DateTime.Now);
                            receipt.Status = "Đã hoàn thành";
                            db.SaveChanges();

                            LoadReceipts();
                            ShowMessage($"Đã chốt phiếu nhập thành công (Ngày chốt: {todayStr})! Hệ thống đã tự động cộng kho.", isError: false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Lỗi hoàn thành phiếu: " + ex.Message, isError: true);
                }
            }
        }

        private void ShowMessage(string msg, bool isError)
        {
            TxtMessage.Text = msg;
            TxtMessage.Foreground = new SolidColorBrush(isError
                ? Color.FromArgb(255, 220, 38, 38)
                : Color.FromArgb(255, 5, 150, 105));
            TxtMessage.Visibility = Visibility.Visible;
        }
    }

    public class ReceiptViewModel
    {
        public int ReceiptID { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public string ImportDateText { get; set; } = string.Empty;       // Ngày tạo riêng
        public string CompletedDateText { get; set; } = string.Empty;    // Ngày chốt riêng
        public SolidColorBrush CompletedDateFg { get; set; } = new(Colors.Gray);
        public string Note { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string TotalAmountText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public SolidColorBrush StatusBg { get; set; } = new(Colors.Transparent);
        public SolidColorBrush StatusFg { get; set; } = new(Colors.Black);
        public Visibility ActionVisibility { get; set; } = Visibility.Collapsed;
    }

    public class ReceiptDetailViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal ImportPrice { get; set; }
        public string ImportPriceText { get; set; } = string.Empty;
        public string AmountText { get; set; } = string.Empty;
        public Visibility ActionVisibility { get; set; } = Visibility.Collapsed;
    }
}