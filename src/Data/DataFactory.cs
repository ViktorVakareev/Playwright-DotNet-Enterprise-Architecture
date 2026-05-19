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

        private static readonly Faker<UserProfile> ValidUserFaker = new Faker<UserProfile>("en")
    .CustomInstantiator(f =>
    {
        return new UserProfile(
            Id: $"WB-{f.Random.Number(1000, 9999)}",
            Username: "standarduser", // ⚠️ Must exactly match your mock app's expected username
            Email: "standarduser@worldbank.internal",
            Password: "password123",  // ⚠️ Must exactly match your mock app's expected password
            Role: "Standard"
        );
    });

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