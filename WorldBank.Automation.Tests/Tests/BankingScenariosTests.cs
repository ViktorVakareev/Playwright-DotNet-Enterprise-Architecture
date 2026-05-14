using Microsoft.Playwright;
using NUnit.Framework;
using WorldBank.Automation.Tests.Infrastructure;
using System.Text.RegularExpressions;

namespace WorldBank.Automation.Tests.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Transfers")]
public class BankingScenariosTests : AiTriage
{
    [SetUp]
    public async Task SetupNavigation()
    {
        // We load the dashboard directly, simulating a standard user session
        await Page.GotoAsync("/dashboard.html?role=standard");
    }

    // 1. Core Page Verification
    [Test]
    public async Task Dashboard_PageLoad_ShouldShowCorePanels()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "World Bank Secure Dashboard" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Compliance: KYC Update" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Initiate Wire Transfer" })).ToBeVisibleAsync();
    }

    // 2. Happy Path: International Wire Transfer
    [Test]
    public async Task Transfer_InternationalWire_ValidInputs_ShouldSucceed()
    {
        await Page.GetByPlaceholder("Recipient IBAN").FillAsync("GB82WEST12345698765432");
        await Page.GetByPlaceholder("SWIFT/BIC Code").FillAsync("WESTGB2L");
        await Page.GetByPlaceholder("Amount (USD)").FillAsync("50000");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Send Funds" }).ClickAsync();

        var status = Page.Locator("#transfer-status");
        await Expect(status).ToHaveTextAsync("Wire Transfer Successfully Initiated.");
        // Asserting a specific CSS property computed from our JS logic
        await Expect(status).ToHaveCSSAsync("color", "rgb(0, 128, 0)"); // Green
    }

    // 3. Negative Path: Missing SWIFT Code
    [Test]
    public async Task Transfer_InternationalWire_MissingSwift_ShouldFailValidation()
    {
        await Page.GetByPlaceholder("Recipient IBAN").FillAsync("GB82WEST12345698765432");
        // Intentionally leaving SWIFT blank
        await Page.GetByPlaceholder("Amount (USD)").FillAsync("5000");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Send Funds" }).ClickAsync();

        var status = Page.Locator("#transfer-status");
        await Expect(status).ToHaveTextAsync("Validation Error: Both SWIFT and IBAN are required.");
        await Expect(status).ToHaveCSSAsync("color", "rgb(255, 0, 0)"); // Red
    }

    // 4. Negative Path: Missing IBAN
    [Test]
    public async Task Transfer_InternationalWire_MissingIban_ShouldFailValidation()
    {
        // Intentionally leaving IBAN blank
        await Page.GetByPlaceholder("SWIFT/BIC Code").FillAsync("WESTGB2L");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Send Funds" }).ClickAsync();

        var status = Page.Locator("#transfer-status");
        await Expect(status).ToHaveTextAsync("Validation Error: Both SWIFT and IBAN are required.");
    }

    // 5. Form State Mutation: KYC Update
    [Test]
    public async Task Compliance_UpdateKycProfile_ShouldReflectNewIncome()
    {
        var updatedIncome = "125000";

        await Page.GetByPlaceholder("Annual Income").FillAsync(updatedIncome);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Profile" }).ClickAsync();

        var status = Page.Locator("#kyc-status");
        await Expect(status).ToHaveTextAsync($"Profile updated. New Income: ${updatedIncome}");
    }

    // 6. Form State Resilience: Empty KYC Update
    [Test]
    public async Task Compliance_UpdateKycProfile_EmptySubmission_ShouldNotUpdate()
    {
        // Submitting without filling the income
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Profile" }).ClickAsync();

        var status = Page.Locator("#kyc-status");
        // Ensure the JS caught the empty state and didn't print "Income: $undefined"
        await Expect(status).ToBeEmptyAsync();
    }

    // 7. Role-Based Access Control: Standard User
    [Test]
    public async Task RBAC_StandardUser_ShouldNotSeeAdminControls()
    {
        // We are already on ?role=standard from Setup
        var adminPanel = Page.Locator("#admin-controls");

        // Assert the panel is completely hidden from the user
        await Expect(adminPanel).ToBeHiddenAsync();
    }

    // 8. Role-Based Access Control: Admin User
    [Test]
    public async Task RBAC_AdminUser_ShouldSeeAdminControls()
    {
        // Override the setup routing for this specific test
        await Page.GotoAsync("/dashboard.html?role=admin");

        var adminPanel = Page.Locator("#admin-controls");

        // Assert the JS properly unhid the panel
        await Expect(adminPanel).ToBeVisibleAsync();
        await Expect(adminPanel.GetByRole(AriaRole.Button, new() { Name = "Audit Logs" })).ToBeVisibleAsync();
    }

    // 9. Routing Verification
    [Test]
    [Category("Debug")]
    public async Task Navigation_Logout_ShouldRouteToLogin()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Out" }).ClickAsync();

        // Ensure the JS redirected the browser correctly
        await Expect(Page).ToHaveURLAsync(new Regex(".*login.html"));
    }

    // 10. Type Constraint Validation
    [Test]
    public async Task UI_NumberInputs_ShouldRejectLetters()
    {
        var amountInput = Page.GetByPlaceholder("Amount (USD)");

        // Attempt to type alphabetical characters into a type="number" field
        await amountInput.FillAsync("One Thousand");

        // The browser should reject the input, leaving the field empty
        await Expect(amountInput).ToHaveValueAsync("");
    }
}