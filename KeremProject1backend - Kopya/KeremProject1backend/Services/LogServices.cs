using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KeremProject1backend.Services
{
    public partial class DataLog
    {
        public int Id { get; set; }
        public string? TableName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public char? Action { get; set; }
        public int? OldModUser { get; set; }
        public DateTime? OldModTime { get; set; }
        public int ModUser { get; set; }
        public DateTime ModTime { get; set; }
    }

    static public class LogServices
    {
        static public List<DataLog> GetSystemLogs(GetSystemLogsRequest inp, UsersContext dbcx)
        {
            IQueryable<DataLog> query = dbcx.DataLog.AsNoTracking();
            if (inp != null)
            {
                if (inp.Id.HasValue && inp.Id.Value != 0)
                    query = query.Where(row => row.Id == inp.Id);
                if (!string.IsNullOrEmpty(inp.Action))
                    query = query.Where(row => row.Action.ToString() == inp.Action);
                if (!string.IsNullOrEmpty(inp.TableName))
                    query = query.Where(row => row.TableName == inp.TableName);
                if (inp.ModUser.HasValue && inp.ModUser.Value != 0)
                    query = query.Where(row => row.ModUser == inp.ModUser);
                if (inp.FromTime.HasValue && inp.FromTime.Value > new DateTime(2000, 1, 1) && inp.FromTime.Value < new DateTime(2100, 1, 1))
                    query = query.Where(row => row.ModTime >= inp.FromTime.Value);
                if (inp.ToTime.HasValue && inp.ToTime.Value < new DateTime(2100, 1, 1) && inp.ToTime.Value > new DateTime(2000, 1, 1))
                    query = query.Where(row => row.ModTime <= inp.ToTime.Value);
            }
            var resp = query.OrderByDescending(l => l.ModTime).ToList();
            return resp;
        }

        /// <summary>
        /// Veritabanı işlemlerini loglayan yardımcı metot.
        /// </summary>
        /// <param name="dbContext">Log eklenecek DbContext (GeneralContext veya UsersContext)</param>
        /// <param name="tableName">İşlem yapılan tablo adı</param>
        /// <param name="action">İşlem türü: C=Create, U=Update, D=Delete</param>
        /// <param name="userId">İşlemi yapan kullanıcının ID'si</param>
        /// <param name="oldValue">Eski değer (varsa), JSON formatında bir string</param>
        /// <param name="newValue">Yeni değer (varsa), JSON formatında bir string</param>
        /// <returns>Async Task</returns>
        public static async Task AddLogAsync(DbContext dbContext, string tableName, char action, int userId, object? oldValue = null, object? newValue = null)
        {
            string? oldValueJson = oldValue != null ? JsonSerializer.Serialize(oldValue) : null;
            string? newValueJson = newValue != null ? JsonSerializer.Serialize(newValue) : null;

            var log = new DataLog
            {
                TableName = tableName,
                Action = action,
                OldValue = oldValueJson,
                NewValue = newValueJson,
                ModUser = userId,
                ModTime = DateTime.UtcNow
            };

            dbContext.Set<DataLog>().Add(log);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Veritabanı işlemlerini loglayan senkron metot (senkron işlemler için)
        /// </summary>
        public static void AddLog(DbContext dbContext, string tableName, char action, int userId, object? oldValue = null, object? newValue = null)
        {
            string? oldValueJson = oldValue != null ? JsonSerializer.Serialize(oldValue) : null;
            string? newValueJson = newValue != null ? JsonSerializer.Serialize(newValue) : null;

            var log = new DataLog
            {
                TableName = tableName,
                Action = action,
                OldValue = oldValueJson,
                NewValue = newValueJson,
                ModUser = userId,
                ModTime = DateTime.UtcNow
            };

            dbContext.Set<DataLog>().Add(log);
            dbContext.SaveChanges();
        }
    }
}
