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
    public sealed partial class ProductTypePage : Page
    {
        private int? _selectedTypeId = null;
        private List<ProductTypeViewModel> _allTypes = new();

        public ProductTypePage()
        {
            this.InitializeComponent();
            LoadProductTypes();
        }

        private void LoadProductTypes()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var types = db.ProductTypes.ToList();
                    var productCounts = db.Products
                        .GroupBy(p => p.ProductTypeID)
                        .ToDictionary(g => g.Key, g => g.Count());

                    _allTypes = types.Select(t => new ProductTypeViewModel
                    {
                        ProductTypeID = t.ProductTypeID,
                        ProductTypeName = t.ProductTypeName,
                        Description = string.IsNullOrWhiteSpace(t.Description) ? "—" : t.Description,
                        RawDescription = t.Description ?? string.Empty,
                        Status = t.Status,
                        ProductCount = productCounts.ContainsKey(t.ProductTypeID) ? productCounts[t.ProductTypeID] : 0,
                        StatusText = t.Status ? "Hiển thị" : "Đang ẩn",
                        StatusBg = new SolidColorBrush(t.Status ? Color.FromArgb(255, 236, 253, 245) : Color.FromArgb(255, 254, 242, 242)),
                        StatusFg = new SolidColorBrush(t.Status ? Color.FromArgb(255, 5, 150, 105) : Color.FromArgb(255, 220, 38, 38))
                    }).OrderBy(t => t.ProductTypeID).ToList();

                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải dữ liệu: " + ex.Message, isError: true);
            }
        }

        private void ApplyFilter()
        {
            string keyword = TxtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                TypeListView.ItemsSource = _allTypes;
            }
            else
            {
                TypeListView.ItemsSource = _allTypes
                    .Where(t => t.ProductTypeName.ToLower().Contains(keyword) ||
                                t.Description.ToLower().Contains(keyword))
                    .ToList();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = string.Empty;
            ResetForm();
            LoadProductTypes();
        }

        private void TypeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeListView.SelectedItem is ProductTypeViewModel selected)
            {
                _selectedTypeId = selected.ProductTypeID;
                TxtFormTitle.Text = "Cập Nhật Loại Hoa";
                TxtTypeId.Text = selected.ProductTypeID.ToString();
                TxtTypeName.Text = selected.ProductTypeName;
                TxtDescription.Text = selected.RawDescription;
                TglStatus.IsOn = selected.Status;
                BtnSave.Content = "Lưu cập nhật";
                TxtMessage.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string typeName = TxtTypeName.Text.Trim();
            string description = TxtDescription.Text.Trim();
            bool status = TglStatus.IsOn;

            if (string.IsNullOrEmpty(typeName))
            {
                ShowMessage("Vui lòng nhập tên loại hoa!", isError: true);
                return;
            }

            try
            {
                using (var db = new ShopDbContext())
                {
                    // Kiểm tra trùng tên loại hoa (Ràng buộc UNIQUE trong SQL)
                    bool isDuplicate = db.ProductTypes.Any(t =>
                        t.ProductTypeName.ToLower() == typeName.ToLower() &&
                        t.ProductTypeID != (_selectedTypeId ?? 0));

                    if (isDuplicate)
                    {
                        ShowMessage("Tên loại hoa này đã tồn tại trong hệ thống!", isError: true);
                        return;
                    }

                    if (_selectedTypeId == null)
                    {
                        // Thêm mới
                        var newType = new ProductType
                        {
                            ProductTypeName = typeName,
                            Description = string.IsNullOrEmpty(description) ? null : description,
                            Status = status
                        };
                        db.ProductTypes.Add(newType);
                        db.SaveChanges();

                        ResetForm();
                        LoadProductTypes();
                        ShowMessage("Đã thêm loại hoa mới thành công!", isError: false);
                    }
                    else
                    {
                        // Cập nhật
                        var existing = db.ProductTypes.FirstOrDefault(t => t.ProductTypeID == _selectedTypeId.Value);
                        if (existing != null)
                        {
                            existing.ProductTypeName = typeName;
                            existing.Description = string.IsNullOrEmpty(description) ? null : description;
                            existing.Status = status;
                            db.SaveChanges();

                            LoadProductTypes();
                            ShowMessage("Đã cập nhật thông tin loại hoa thành công!", isError: false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi lưu dữ liệu: " + ex.Message, isError: true);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            _selectedTypeId = null;
            TypeListView.SelectedItem = null;
            TxtFormTitle.Text = "Thêm Loại Hoa Mới";
            TxtTypeId.Text = string.Empty;
            TxtTypeName.Text = string.Empty;
            TxtDescription.Text = string.Empty;
            TglStatus.IsOn = true;
            BtnSave.Content = "Thêm mới";
            TxtMessage.Visibility = Visibility.Collapsed;
        }

        private void ShowMessage(string message, bool isError)
        {
            TxtMessage.Text = message;
            TxtMessage.Foreground = new SolidColorBrush(isError
                ? Color.FromArgb(255, 220, 38, 38)
                : Color.FromArgb(255, 5, 150, 105));
            TxtMessage.Visibility = Visibility.Visible;
        }
    }

    public class ProductTypeViewModel
    {
        public int ProductTypeID { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RawDescription { get; set; } = string.Empty;
        public bool Status { get; set; }
        public int ProductCount { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public SolidColorBrush StatusBg { get; set; } = new(Colors.Transparent);
        public SolidColorBrush StatusFg { get; set; } = new(Colors.Black);
    }
}