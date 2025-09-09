namespace KeremProject1backend.Models.DBs
{
    public class Config
    {
        public static UIConfigurationDataClass UIConfigurationData = InitializeConfig();

        private static UIConfigurationDataClass InitializeConfig()
        {
            var configData = new UIConfigurationDataClass();

            // UserRoleinAuthorization Enum'unu ekleyelim
            configData.AppEnums.Add(new EnumUIItem
            {
                Name = "UserRoleinAuthorization",
                Pairs = Enum.GetValues(typeof(UserRoleinAuthorization))
                           .Cast<UserRoleinAuthorization>()
                           .Select(e => new EnumUIItemParam
                           {
                               Key = e.ToString(),
                               Value = (int)e
                           }).ToList()
            });

            // YENİ: OrderStatus Enum'unu ekle
            configData.AppEnums.Add(new EnumUIItem
            {
                Name = "OrderStatus",
                Pairs = Enum.GetValues(typeof(OrderStatus))
                           .Cast<OrderStatus>()
                           .Select(e => new EnumUIItemParam { Key = e.ToString(), Value = (int)e }).ToList()
            });

            // Diğer sabit konfigürasyonlar
            configData.UsersConfig = new
            {
                MaxLoginAttempts = 5,
                PasswordRequirements = new { MinLength = 8 }
            };

            return configData;
        }
    }

    public class UIConfigurationDataClass
    {
        public object UsersConfig { get; set; } = new object();
        public List<EnumUIItem> AppEnums { get; set; } = new List<EnumUIItem>();
    }

    public class EnumUIItem
    {
        public string? Name { get; set; }
        public List<EnumUIItemParam> Pairs { get; set; } = new List<EnumUIItemParam>();
    }

    public class EnumUIItemParam
    {
        public string? Key { get; set; }
        public int Value { get; set; }
    }
}
