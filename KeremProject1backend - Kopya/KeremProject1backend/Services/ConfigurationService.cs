using System.Reflection;
using KeremProject1backend.Models.DBs;

namespace KeremProject1backend.Services
{
    public class ConfigurationService
    {
        public UIConfigurationDataClass UIConfigurationData { get; } = new UIConfigurationDataClass();

        public ConfigurationService()
        {
            InitializeEnums();
            LoadStaticConfig();
        }

        private void InitializeEnums()
        {
            // UserRoleinAuthorization enum'unu ekleyelim
            var enumItem = new EnumUIItem
            {
                Name = "UserRoleinAuthorization",
                Pairs = Enum.GetValues(typeof(UserRoleinAuthorization))
                           .Cast<UserRoleinAuthorization>()
                           .Select(e => new EnumUIItemParam
                           {
                               Key = e.ToString(),
                               Value = (int)e
                           }).ToList()
            };

            UIConfigurationData.AppEnums.Add(enumItem);
        }

        private void LoadStaticConfig()
        {
            // Diğer sabit yapılandırmaları buraya ekleyin
            UIConfigurationData.UsersConfig = new
            {
                MaxLoginAttempts = 5,
                PasswordRequirements = new { MinLength = 8 }
            };
        }

        // İsterseniz JSON dosyasından yükleme ekleyin
        public void LoadFromJson(string jsonPath)
        {
            // JSON'dan yükleme mantığı
        }
    }
}