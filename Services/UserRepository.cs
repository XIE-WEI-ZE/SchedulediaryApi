using SchedulediaryApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SchedulediaryApi.Services
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public void Register(User user)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                INSERT INTO Users 
                (Name, Account, Email, PasswordHash, Salt, Gender, Birthday, CreatedAt, AvatarPath)
                VALUES 
                (@Name, @Account, @Email, @PasswordHash, @Salt, @Gender, @Birthday, @CreatedAt, @AvatarPath)
            ", conn);

            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = user.Name;
            cmd.Parameters.Add("@Account", SqlDbType.NVarChar, 50).Value = user.Account;
            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = user.Email;
            cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 100).Value = user.PasswordHash;
            cmd.Parameters.Add("@Salt", SqlDbType.NVarChar, 100).Value = string.Empty; // 設為空字串
            cmd.Parameters.Add("@Gender", SqlDbType.NVarChar, 10).Value = (object?)user.Gender ?? DBNull.Value;
            cmd.Parameters.Add("@Birthday", SqlDbType.Date).Value = (object?)user.Birthday ?? DBNull.Value;
            cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = user.CreatedAt;
            cmd.Parameters.Add("@AvatarPath", SqlDbType.NVarChar, 200).Value = (object?)user.AvatarPath ?? DBNull.Value;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public User? GetUserByAccount(string account)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT * FROM Users WHERE Account = @Account", conn);
            cmd.Parameters.Add("@Account", SqlDbType.NVarChar, 50).Value = account;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = (int)reader["UserId"],
                    Name = reader["Name"].ToString() ?? "",
                    Account = reader["Account"].ToString() ?? "",
                    Email = reader["Email"].ToString() ?? "",
                    PasswordHash = reader["PasswordHash"].ToString() ?? "",
                    Gender = reader["Gender"] as string,
                    Birthday = reader["Birthday"] == DBNull.Value ? null : (DateTime?)reader["Birthday"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    AvatarPath = reader["AvatarPath"] as string
                };
            }
            return null;
        }

        public bool IsAccountExist(string account)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE Account = @Account", conn);
            cmd.Parameters.Add("@Account", SqlDbType.NVarChar, 50).Value = account;

            conn.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }

        public bool IsUserIdExist(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE UserId = @UserId", conn);
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            conn.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }
    }
}