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
    public sealed partial class CustomerPage : Page
    {
        private List<UserManagementViewModel> _allUsers = new();

        public CustomerPage()
        {
            this.InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var query = from a in db.Accounts
                                join c in db.Customers on a.AccountID equals c.AccountID into acGroup
                                from c in acGroup.DefaultIfEmpty()
                                select new
                                {
                                    a.AccountID,
                                    a.Username,
                                    a.Role,
                                    a.Status,
                                    FullName = c != null ? c.FullName : "—",
                                    Email = c != null ? c.Email : "—",
                                    Phone = c != null ? c.PhoneNumber : "—",
                                    Address = c != null ? c.Address : "—"
                                };

                    _allUsers = query.ToList().Select(u => new UserManagementViewModel
                    {
                        AccountID = u.AccountID,
                        Username = u.Username,
                        FullName = u.FullName ?? "—",
                        Email = string.IsNullOrWhiteSpace(u.Email) ? "—" : u.Email,
                        Phone = string.IsNullOrWhiteSpace(u.Phone) ? "—" : u.Phone,
                        Address = string.IsNullOrWhiteSpace(u.Address) ? "—" : u.Address,
                        Role = u.Role,
                        IsActive = u.Status,
                        LockIcon = u.Status ? "\uE785" : "\uE72E",
                        ToggleBg = u.Status ? new SolidColorBrush(Color.FromArgb(255, 252, 211, 77)) : new SolidColorBrush(Color.FromArgb(255, 229, 231, 235))
                    }).ToList();

                    ApplyFilter();
                }
            }
            catch (Exception) { }
        }

        private void ApplyFilter()
        {
            if (CustomerListView == null) return;
            string keyword = TxtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(keyword))
                CustomerListView.ItemsSource = _allUsers.ToList();
            else
                CustomerListView.ItemsSource = _allUsers.Where(u => u.Username.ToLower().Contains(keyword) || u.FullName.ToLower().Contains(keyword)).ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = string.Empty;
            LoadUsers();
        }

        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerEditDialog(null) { XamlRoot = this.XamlRoot };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary) LoadUsers();
        }

        private async void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int accountId)
            {
                var dialog = new CustomerEditDialog(accountId) { XamlRoot = this.XamlRoot };
                await dialog.ShowAsync();
                LoadUsers();
            }
        }

        private async void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int accountId)
            {
                using (var db = new ShopDbContext())
                {
                    var account = db.Accounts.FirstOrDefault(u => u.AccountID == accountId);
                    if (account != null)
                    {
                        string actionText = account.Status ? "khóa" : "mở khóa";
                        var dialog = new ContentDialog
                        {
                            Title = "Cảnh báo",
                            Content = $"Bạn có chắc chắn muốn {actionText} người dùng này không?",
                            PrimaryButtonText = "Có",
                            CloseButtonText = "Không",
                            XamlRoot = this.XamlRoot,
                            DefaultButton = ContentDialogButton.Primary,
                            RequestedTheme = ElementTheme.Light
                        };
                        dialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Color.FromArgb(255, 220, 38, 38));
                        dialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

                        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            account.Status = !account.Status;
                            db.SaveChanges();
                            LoadUsers();
                        }
                    }
                }
            }
        }

        private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int accountId)
            {
                var dialog = new ContentDialog
                {
                    Title = "Khởi tạo lại mật khẩu",
                    Content = "Mật khẩu của người dùng này sẽ được đặt về 123456.\nBạn có chắc chắn không?",
                    PrimaryButtonText = "Đồng ý",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                    RequestedTheme = ElementTheme.Light
                };
                dialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Color.FromArgb(255, 220, 38, 38));
                dialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    using (var db = new ShopDbContext())
                    {
                        var account = db.Accounts.FirstOrDefault(u => u.AccountID == accountId);
                        if (account != null)
                        {
                            account.PasswordHash = "123456";
                            db.SaveChanges();
                        }
                    }
                }
            }
        }
    }

    public class UserManagementViewModel
    {
        public int AccountID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string LockIcon { get; set; } = string.Empty;
        public SolidColorBrush ToggleBg { get; set; } = new(Colors.Transparent);
    }
}