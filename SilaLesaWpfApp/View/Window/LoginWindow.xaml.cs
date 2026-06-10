using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using SilaLesaWpfApp.Model;

namespace SilaLesaWpfApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private static string RoleToRu(string role)
        {
            switch (role)
            {
                case "admin": return "Администратор";
                case "moderator": return "Модератор";
                case "user": return "Пользователь";
                default: return role;
            }
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbMessage.Text = "";
            try
            {
                var dt = Db.Query("SELECT RoleName FROM Roles ORDER BY RoleName");

                if (!dt.Columns.Contains("RoleRu"))
                    dt.Columns.Add("RoleRu", typeof(string));

                foreach (DataRow r in dt.Rows)
                    r["RoleRu"] = RoleToRu(r["RoleName"].ToString());

                cbRole.ItemsSource = dt.DefaultView;
                cbRole.DisplayMemberPath = "RoleRu";
                cbRole.SelectedValuePath = "RoleName";
                cbRole.SelectedIndex = dt.Rows.Count > 0 ? 0 : -1;
            }
            catch
            {
                // fallback (if DB not connected yet)
                var dt = new DataTable();
                dt.Columns.Add("RoleName", typeof(string));
                dt.Columns.Add("RoleRu", typeof(string));
                dt.Rows.Add("admin", RoleToRu("admin"));
                dt.Rows.Add("moderator", RoleToRu("moderator"));
                dt.Rows.Add("user", RoleToRu("user"));

                cbRole.ItemsSource = dt.DefaultView;
                cbRole.DisplayMemberPath = "RoleRu";
                cbRole.SelectedValuePath = "RoleName";
                cbRole.SelectedIndex = 0;

                tbMessage.Text = "База данных не подключена. Проверьте строку подключения в App.config.";
            }

            // Demo accounts (from SQL script):
            // admin / hash_admin_123
            // moderator / hash_moderator_123
            // user / hash_user_123
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            tbMessage.Text = "";

            var username = (tbUsername.Text ?? "").Trim();
            var password = pbPassword.Password ?? "";
            var role = cbRole.SelectedValue == null ? "" : cbRole.SelectedValue.ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                tbMessage.Text = "Введите логин, пароль и роль.";
                return;
            }

            try
            {
                var dt = Db.Query(@"
SELECT u.UserID, u.Username, r.RoleName
FROM AppUsers u
JOIN Roles r ON r.RoleID = u.RoleID
WHERE u.Username = @u
  AND u.PasswordHash = @p
  AND r.RoleName = @r
  AND u.IsActive = 1;",
                    new SqlParameter("@u", username),
                    new SqlParameter("@p", password),
                    new SqlParameter("@r", role));

                if (dt.Rows.Count == 0)
                {
                    tbMessage.Text = "Не удалось войти. Проверьте логин/пароль/роль.";
                    return;
                }

                Session.UserID = Convert.ToInt32(dt.Rows[0]["UserID"]);
                Session.Username = dt.Rows[0]["Username"].ToString();
                Session.Role = dt.Rows[0]["RoleName"].ToString();

                Window next;
                if (Session.Role == "admin")
                    next = new AdminWindow();
                else if (Session.Role == "moderator")
                    next = new ModeratorWindow();
                else
                    next = new UserWindow();

                next.Show();
                Close();
            }
            catch (Exception ex)
            {
                tbMessage.Text = "Ошибка базы данных: " + ex.Message;
            }
        }
    }
}
