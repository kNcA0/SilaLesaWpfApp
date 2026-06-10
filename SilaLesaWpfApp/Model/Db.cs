using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SilaLesaWpfApp.Model
{
    public static class Db
    {
        public static string ConnectionString
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["CampingBookingDB"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                    throw new Exception("Connection string 'CampingBookingDB' not found in App.config.");
                return cs.ConnectionString;
            }
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            var dt = new DataTable();
            using (var con = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        public static int Exec(string sql, params SqlParameter[] parameters)
        {
            using (var con = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var con = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                con.Open();
                return cmd.ExecuteScalar();
            }
        }
    }
}