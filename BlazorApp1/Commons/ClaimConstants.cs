using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorApp1.Commons
{
    public static class ClaimConstants
    {

        public const string AppRole = "AppRole";

        public const string RoleDeveloper = "DEMO_Developer";

        public const string RoleAdministrator = "DEMO_Administrator";
    }
   
    public static class PolicyConstants
    {

        public const string AdministratorAndDeveloper = "AdminOrDeveloper";

        public const string Developer = "DeveloperOnly";

        public const string Administrator = "AdminOnly";
    
    }
}
