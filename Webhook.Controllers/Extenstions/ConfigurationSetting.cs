using Microsoft.Extensions.Options;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Webhook.Controllers.Extenstions
{
    public class ConfigurationSetting
    {
        static IServiceProvider services = null;

        /// <summary>
        /// Provides static access to the framework's services provider
        /// </summary>
        public static IServiceProvider Services
        {
            get { return services; }
            set
            {
                if (services != null)
                {
                    throw new Exception("Can't set once a value has already been set.");
                }
                services = value;
            }
        }

        /// <summary>
        /// Provides static access to the current HttpContext
        /// </summary>
        public static HttpContext HttpContext_Current
        {
            get
            {
                IHttpContextAccessor httpContextAccessor = services.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
                return httpContextAccessor?.HttpContext;
            }
        }

        public static IHostingEnvironment HostingEnvironment
        {
            get
            {
                return services.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment;
            }
        }

        
        public static REDIS REDISConfig
        {
            get
            {
                //This works to get file changes.
                var s = services.GetService(typeof(IOptionsMonitor<REDIS>)) as IOptionsMonitor<REDIS>;
                REDIS config = s.CurrentValue;

                return config;
            }
        }

        public class REDIS
        {
            public string CacheConnection { get; set; }
        }
    }
}
