using Microsoft.EntityFrameworkCore;
using KeremProject1backend.Models.DBs;

namespace KeremProject1backend.InternalServices
{
    public class ServiceParameters
    {
        public int MaxSessionNumber { get; set; }
        public int SessionTimoutSecond { get; set; }
        public int ActivityThresholdSecond { get; set; }
        public int MaxConcurrentSessionNumber { get; set; }
    }

    public static class ServiceRegistration
    {
        public static ServiceParameters? ServiceParameters { get; set; }

    }
}
