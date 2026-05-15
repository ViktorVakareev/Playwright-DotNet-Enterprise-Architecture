using Bogus;

namespace WorldBank.Automation.Tests.Data
{
    // Expanded the record to explicitly support the Authentication and RBAC tests we wrote earlier
    public record UserProfile(string Id, string Username, string Email, string Password, string Role);

    public static class DataFactory
    {
        // =====================================================================
        // CACHED FAKER DEFINITIONS (Performance Optimization & CI/CD Stability)
        // =====================================================================

        // Locking to the "en" locale ensures predictable character sets across Linux/Windows Jenkins agents
        private static readonly Faker<UserProfile> ValidUserFaker = new Faker<UserProfile>("en")
            .RuleFor(u => u.Id, f => $"WB-{f.Random.Number(1000, 9999)}")
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            // Notice how Email dynamically uses the generated Username to maintain realistic data consistency!
            .RuleFor(u => u.Email, (f, u) => $"{u.Username}@worldbank.internal".ToLower())
            .RuleFor(u => u.Password, f => f.Internet.Password(12, false, "", "Valid123!")) // Guarantees password complexity rules pass
            .RuleFor(u => u.Role, f => "Standard");


        // =====================================================================
        // HAPPY PATH GENERATORS
        // =====================================================================

        public static UserProfile CreateValidUser() => ValidUserFaker.Generate();


        // =====================================================================
        // NEGATIVE PATH / EDGE CASE GENERATORS (Using Record Mutation)
        // =====================================================================

        /// <summary>
        /// Promotes a standard user to an Admin role with the correct internal email structure.
        /// </summary>
        public static UserProfile CreateAdminUser()
        {
            var validData = ValidUserFaker.Generate();
            // C# Record Mutation: Keeps the generated ID and Password, but alters the Role and Email
            return validData with
            {
                Role = "Admin",
                Email = $"admin.{validData.Username}@worldbank.internal"
            };
        }

        /// <summary>
        /// Injects a classic Zero-Trust SQL payload into the username field.
        /// </summary>
        public static UserProfile CreateUser_SqlInjection()
        {
            var validData = ValidUserFaker.Generate();
            return validData with { Username = "admin@worldbank.internal' OR '1'='1" };
        }

        /// <summary>
        /// Injects a malicious Cross-Site Scripting (XSS) payload into the username field.
        /// </summary>
        public static UserProfile CreateUser_XssPayload()
        {
            var validData = ValidUserFaker.Generate();
            return validData with { Username = "<script>alert('Hacked')</script>admin@worldbank.internal" };
        }

        /// <summary>
        /// Generates a user with an excessively long name to test UI buffer or boundary rendering.
        /// </summary>
        public static UserProfile CreateUser_ExtremeNameLength()
        {
            var validData = ValidUserFaker.Generate();
            var longName = new string('A', 255);
            return validData with { Username = longName, Email = $"{longName}@worldbank.internal" };
        }
    }
}