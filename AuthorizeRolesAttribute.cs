using Microsoft.AspNetCore.Authorization;

namespace FoysCoreAPITemplate
{
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles) : base()
        {
            Roles = string.Join(",", roles);
        }
    }

    public static class Role
    {
        public const string ADMIN = "Admin";
        public const string HELLOWORLD = "HelloWorld";
    }
}
