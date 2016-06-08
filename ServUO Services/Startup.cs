using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ServUO_Services.Startup))]
namespace ServUO_Services
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}
