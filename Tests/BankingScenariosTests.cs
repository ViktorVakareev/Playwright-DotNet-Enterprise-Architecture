using Microsoft.Playwright;
using NUnit.Framework;
using WorldBank.Automation.Tests.Data;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests;

[Parallelizable(ParallelScope.All)] // Maximum parallelization for speed
[TestFixture]
public class BankingScenariosTests : AiTriage
{
    [SetUp]
    public async Task SetupNavigation()
    {
        // Assuming user is already authenticated via StorageState in a real framework
        await Page.GotoAsync("https://sandbox.worldbank.internal/dashboard");
    }

    // 1. Domestic Fund Transfer
    [Test]
    public async Task Transfer_Domestic_ShouldDeductFromAvailableBalance()
    {
        var amount = new Bogus.Faker().Finance.Amount(10, 500);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Transfers" }).ClickAsync();
        await Page.GetByLabel("Recipient Account").FillAsync("8899223344");
        await Page.GetByLabel("Amount").FillAsync(amount.ToString());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Execute Transfer" }).ClickAsync();

        var successMessage = Page.GetByRole(AriaRole.Alert);
        await Expect(successMessage).ToContainTextAsync($"Successfully transferred ${amount}");
    }

    // 2. International SWIFT Transfer (Using Dynamic Data)
    [Test]
    public async Task Transfer_InternationalWire_ShouldRequireSwiftAndIban()
    {
        var wireData = BankingDataFactory.CreateWireTransfer();

        await Page.GetByRole(AriaRole.Link, new() { Name = "International Wire" }).ClickAsync();
        await Page.GetByLabel("Beneficiary Name").FillAsync(wireData.BeneficiaryName);
        await Page.GetByLabel("IBAN").FillAsync(wireData.Iban);
        await Page.GetByLabel("SWIFT/BIC").FillAsync(wireData.SwiftCode);
        await Page.GetByLabel("Amount").FillAsync(wireData.Amount.ToString());

        await Page.GetByRole(AriaRole.Button, new() { Name = "Verify Wire" }).ClickAsync();

        // Asserting against dynamically generated data
        await Expect(Page.Locator(".confirmation-iban")).ToHaveTextAsync(wireData.Iban);
    }

    // 3. KYC Profile Update (Compliance Scenario)
    [Test]
    public async Task Compliance_UpdateKycProfile_ShouldReflectNewIncome()
    {
        var kycData = BankingDataFactory.CreateKycProfile();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Profile & Settings" }).ClickAsync();
        await Page.GetByRole(AriaRole.Tab, new() { Name = "KYC Documents" }).ClickAsync();

        // Handling dropdowns dynamically
        await Page.GetByLabel("Employment Status").SelectOptionAsync(new[] { kycData.EmploymentStatus });
        await Page.GetByLabel("Annual Income").FillAsync(kycData.AnnualIncome.ToString());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Compliance Data" }).ClickAsync();

        await Expect(Page.GetByText("KYC Profile Updated Successfully")).ToBeVisibleAsync();
    }

    // 4. Loan Origination (Testing business logic via UI)
    [Test]
    public async Task Loans_HighAmount_ShouldTriggerManualUnderwritingReview()
    {
        var loanData = BankingDataFactory.CreateLoanApplication();
        // Force a high amount to trigger a specific business rule
        var highAmount = loanData.RequestedAmount + 1000000;

        await Page.GetByRole(AriaRole.Link, new() { Name = "Apply for Loan" }).ClickAsync();
        await Page.GetByLabel("Loan Type").SelectOptionAsync(loanData.LoanType);
        await Page.GetByLabel("Requested Amount").FillAsync(highAmount.ToString());
        await Page.GetByLabel("Term (Months)").FillAsync(loanData.TermMonths.ToString());

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit Application" }).ClickAsync();

        // Assert that the system correctly routed the high-risk application
        var statusBadge = Page.GetByTestId("application-status");
        await Expect(statusBadge).ToHaveTextAsync("Pending Manual Review");
    }

    // 5. Transaction Dispute (Chargeback)
    [Test]
    public async Task Disputes_FileChargeback_ShouldGenerateCaseId()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Recent Transactions" }).ClickAsync();

        // Select the first transaction row dynamically
        var firstTransactionRow = Page.Locator("table tbody tr").First;
        await firstTransactionRow.GetByRole(AriaRole.Button, new() { Name = "Dispute" }).ClickAsync();

        await Page.GetByLabel("Reason for Dispute").SelectOptionAsync("Fraudulent Charge");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit Dispute" }).ClickAsync();

        // Regex assertion to ensure a Case ID format (e.g., CASE-12345) is generated
        var caseIdText = Page.Locator(".case-id-display");
        await Expect(caseIdText).ToHaveTextAsync(new System.Text.RegularExpressions.Regex(@"CASE-\d{5}"));
    }

    // 6. Network Interception (Mocking a 500 Server Error from the Core Banking API)
    [Test]
    public async Task Resilience_CoreBankingOffline_ShouldDisplayGracefulError()
    {
        // Intercept the API call and force a 500 error to test frontend resilience
        await Page.RouteAsync("**/api/v1/accounts/balance", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions { Status = 500 });
        });

        await Page.ReloadAsync();

        await Expect(Page.GetByText("Core banking services are temporarily unavailable.")).ToBeVisibleAsync();
    }

    // 7. Add New Beneficiary/Payee
    [Test]
    public async Task Payee_AddNew_ShouldAppearInQuickTransferList()
    {
        var payeeName = new Bogus.Faker().Company.CompanyName();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Manage Payees" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Payee" }).ClickAsync();
        await Page.GetByLabel("Company/Name").FillAsync(payeeName);
        await Page.GetByLabel("Account Number").FillAsync(new Bogus.Faker().Finance.Account());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Payee" }).ClickAsync();

        // Navigate to transfers to verify payee exists
        await Page.GetByRole(AriaRole.Link, new() { Name = "Quick Transfer" }).ClickAsync();
        var dropdown = Page.GetByLabel("Select Payee");
        await Expect(dropdown).ToContainTextAsync(payeeName);
    }

    // 8. Currency FX Rate Calculation
    [Test]
    public async Task FX_CurrencyConversion_ShouldCalculateExchangeRate()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Currency Exchange" }).ClickAsync();
        await Page.GetByLabel("From").SelectOptionAsync("USD");
        await Page.GetByLabel("To").SelectOptionAsync("EUR");
        await Page.GetByLabel("Amount to Convert").FillAsync("1000");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Calculate" }).ClickAsync();

        // The exact rate changes, so we assert the result box becomes visible and is not empty
        var resultBox = Page.GetByTestId("fx-result");
        await Expect(resultBox).ToBeVisibleAsync();
        await Expect(resultBox).Not.ToBeEmptyAsync();
    }

    // 9. Download Account Statement (Handling Files in Playwright)
    [Test]
    public async Task Statements_ExportPdf_ShouldDownloadSuccessfully()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Statements" }).ClickAsync();

        // Wait for the download event to trigger
        var downloadTask = Page.WaitForDownloadAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Export PDF" }).ClickAsync();
        var download = await downloadTask;

        // Verify the file name and that it actually downloaded
        Assert.That(download.SuggestedFilename, Does.EndWith(".pdf"));
        Assert.That(download.Url, Is.Not.Null);
    }

    // 10. Schedule Recurring Payment
    [Test]
    public async Task Payments_ScheduleRecurring_ShouldSetNextExecutionDate()
    {
        var amount = new Bogus.Faker().Finance.Amount();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Bill Pay" }).ClickAsync();
        await Page.GetByLabel("Biller").SelectOptionAsync("Electric Utility");
        await Page.GetByLabel("Amount").FillAsync(amount.ToString());

        // Check a radio button or checkbox
        await Page.GetByLabel("Make this a recurring payment").CheckAsync();
        await Page.GetByLabel("Frequency").SelectOptionAsync("Monthly");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Schedule Payment" }).ClickAsync();

        // Assert that the next payment date is exactly one month from today
        var expectedNextDate = DateTime.Now.AddMonths(1).ToString("MMM dd, yyyy");
        await Expect(Page.Locator(".next-payment-date")).ToHaveTextAsync(expectedNextDate);
    }
}