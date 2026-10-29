using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using QuanLyBanHoa.Desktop.Data;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        // --- XỬ LÝ HIỆU ỨNG NHẢY CHỮ (FLOATING LABEL) ---

        private void UsernameBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            TxtUsername.Focus(FocusState.Programmatic);
        }

        private void PasswordBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            TxtPassword.Focus(FocusState.Programmatic);
        }

        private void TxtUsername_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateUsernameLabel(true);
        }

        private void TxtUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateUsernameLabel(false);
        }

        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUsernameLabel(TxtUsername.FocusState != FocusState.Unfocused);
        }

        private void UpdateUsernameLabel(bool isFocused)
        {
            if (isFocused || !string.IsNullOrEmpty(TxtUsername.Text))
            {
                // Nhảy lên trên như Hình 2
                TransUsername.TranslateY = 0;
                LblUsername.FontSize = 12;
                LblUsername.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                // Nằm ở vị trí mặc định khi trống
                TransUsername.TranslateY = 20;
                LblUsername.FontSize = 14;
                LblUsername.FontWeight = FontWeights.Normal;
            }
        }

        private void TxtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePasswordLabel(true);
        }

        private void TxtPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePasswordLabel(false);
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordLabel(TxtPassword.FocusState != FocusState.Unfocused);
        }

        private void UpdatePasswordLabel(bool isFocused)
        {
            if (isFocused || !string.IsNullOrEmpty(TxtPassword.Password))
            {
                // Nhảy lên trên như Hình 2
                TransPassword.TranslateY = 0;
                LblPassword.FontSize = 12;
                LblPassword.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                // Nằm ở vị trí mặc định khi trống
                TransPassword.TranslateY = 20;
                LblPassword.FontSize = 14;
                LblPassword.FontWeight = FontWeights.Normal;
            }
        }

        private void Input_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                BtnLogin_Click(this, new RoutedEventArgs());
            }
        }

        // --- XỬ LÝ ĐĂNG NHẬP CSDL ---

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập đầy đủ tên người dùng và mật khẩu!");
                return;
            }

            try
            {
                using (var context = new ShopDbContext())
                {
                    var account = context.Accounts
                        .FirstOrDefault(a => a.Username == username);

                    if (account == null || !account.Status)
                    {
                        ShowError("Tài khoản không tồn tại hoặc đã bị khóa!");
                        return;
                    }

                    if (account.Role != "QuanTriVien")
                    {
                        ShowError("Bạn không có quyền truy cập ứng dụng quản trị!");
                        return;
                    }

                    if (VerifyPassword(password, account.PasswordHash))
                    {
                        MainWindow.Current?.UnlockAdminSession(account.Username);
                    }
                    else
                    {
                        ShowError("Mật khẩu không chính xác!");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi kết nối CSDL: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            // Cho phép đăng nhập nhanh bằng 123456
            if (inputPassword == "123456") return true;

            if (storedHash.StartsWith("pbkdf2-sha256$"))
            {
                var parts = storedHash.Split('$');
                if (parts.Length == 4 && int.TryParse(parts[1], out int iterations))
                {
                    byte[] salt = Convert.FromBase64String(parts[2]);
                    byte[] expectedHash = Convert.FromBase64String(parts[3]);
                    byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                        inputPassword,
                        salt,
                        iterations,
                        HashAlgorithmName.SHA256,
                        expectedHash.Length);

                    return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
                }
            }

            return storedHash == inputPassword;
        }
    }
}