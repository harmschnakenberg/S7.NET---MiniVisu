using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace S7.NET.web
{
    partial class Html
    {
        public static List<User> Users = new List<User>();

        internal static List<User> LoadUsersFromFile()
        {
            List<User> users = new List<User>();

            string path = Path.Combine(appPath, "csv", "Credentials.csv");
            if (!File.Exists(path)) return users;

            string[] lines = System.IO.File.ReadAllLines(path);

            foreach (string line in lines)
            {
                string[] items = line.Split(';');
                if (items.Length > 2)
                {
                    User user = new User
                    {
                        Name = items[0],
                        AccessLevel = int.Parse(items[1]),
                        EncyptedPassword = items[2]
                    };
                    users.Add(user);
                }
            }

            return users;
        }

        internal static void SafeUsersToFile(List<User> users)
        {
            try
            {
                string path = Path.Combine(appPath, "csv", "Credentials.csv");

                if (File.Exists(path))
                    File.Delete(path);

                StringBuilder sb = new StringBuilder();

                foreach (var user in users)
                {
                    sb.AppendLine($"{user.Name};{user.AccessLevel};{user.EncyptedPassword}");
                }

                File.WriteAllText(path, sb.ToString());
            }
            catch (IOException io_ex)
            {
                Console.WriteLine(io_ex.Message);
            }
        }

        internal static string CheckCredentials(string name, string password)
        {
            if (Users.Count == 0)
                Users = LoadUsersFromFile();

            try
            {
                #region Benutzer-Passwort-Kombination finden
                string guid = string.Empty;
                string encryped_pw = Encrypt(password);
                string path = Path.Combine(appPath, "csv", "Credentials.csv");
                User logedInUser = new User();

                foreach (var user in Users)
                    if (user.Name == name && user.EncyptedPassword == encryped_pw)
                    {
                        guid = Guid.NewGuid().ToString("N");
                        logedInUser = user;
                        break;
                    }
                        #endregion

                #region Liste eingeloggter Benutzer aktualisieren
                if (guid.Length > 0)
                {
                    while (Server.LogedInHash.Count > 10) //Max. 10 Benutzer gleichzetig eingelogged
                    {
                        Server.LogedInHash.Remove(Server.LogedInHash.Keys.GetEnumerator().Current);
                    }

                    Server.LogedInHash.Add(guid, logedInUser);

                    return guid;
                }
                #endregion
            }
            catch (Exception)
            {
                throw;
                // Was tun?
            }

            return string.Empty;
        }

        internal static bool CreateUser(string name, int level, string password)
        {
            if (Users.Count == 0)
                Users = LoadUsersFromFile();

            foreach (User user in Users)
            {
                if (user.Name == name)
                    return false;
            }

            User newUser = new User
            {
                Name = name,
                AccessLevel = level,
                EncyptedPassword = Encrypt(password)
            };

            Users.Add(newUser);

            SafeUsersToFile(Users);
            return true;
        }

        internal static bool DeleteUser(string oldUserName)
        {
            if (Users.Count == 0)
                Users = LoadUsersFromFile();
            
            for (int i = 0; i < Users.Count; i++)
            {
                if (Users[i].Name == oldUserName)
                {
                    Users.RemoveAt(i);
                    return true;
                }
            }

            SafeUsersToFile(Users);
            return false;
        }

        private static string Encrypt(string password)
        {
            if (password == null) return password;

            byte[] data = System.Text.Encoding.UTF8.GetBytes(password);
            data = new System.Security.Cryptography.SHA256Managed().ComputeHash(data);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        internal static string GetHtmlUserTable(int userAccessLevel)
        {
            if (Users.Count == 0)
                Users = LoadUsersFromFile();

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<table class='w3-table-all w3-light-grey'>");
                sb.AppendLine("<tr><th>Edit</th><th>Benutzername</th><th>Berechtigung</th></tr>");

                foreach (var user in Users)
                {
                    if (user.AccessLevel >= userAccessLevel) //nur Benutzer die max userAccessLevel haben
                    {
                        sb.Append($"<tr><td onclick='getAccount(this.parentNode)' class='w3-button material-icons' style='width:50px;'>edit</td>");
                        sb.Append($"<td>{user.Name}</td><td>{user.AccessLevel}</td></tr>");
                    }
                }

                sb.AppendLine("</table>");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
                //return false;
            }
        }


    }
}
