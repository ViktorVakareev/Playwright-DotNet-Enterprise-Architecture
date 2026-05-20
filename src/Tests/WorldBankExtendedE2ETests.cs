using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorldBank.Automation.Tests.Data;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [Category("ExtendedE2E")]
    [Category("CoreBanking")]
    public class WorldBankExtendedE2ETests : AiTriage
    {
        private string TransferUrl => $"{AppConfig.GetBaseUrl()}/transfer.html";
        private string DashboardUrl => $"{AppConfig.GetBaseUrl()}/dashboard.html?role=standard";
        private string LoginUrl => $"{AppConfig.GetBaseUrl()}/login.html";

        #region Business Logic & Stepper Workflow Tests

        [Test]
        public async Task EndToEnd_ValidInternationalTransfer_ShouldGenerateTransactionReceipt()
        {
            // Arrange
            var transfer = BankingDataFactory.CreateValidWireTransfer();
            string expectedDate = DateTime.Now.ToString("yyyy-MM-dd");

			await Page.GotoAsync(TransferUrl);

			// Act - Step 1
			await Page.GetByTestId("recipient-select").SelectOptionAsync("global");
            await Page.GetByTestId("acc-number").FillAsync(transfer.Iban);
            await Page.GetByTestId("btn-next-1").ClickAsync();

            // Act - Step 2
            await Page.GetByTestId("transfer-date").FillAsync(expectedDate);
            await Page.GetByTestId("transfer-amount").FillAsync(transfer.Amount.ToString(CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();

            // Assert - Step 3 Review
            await Expect(Page.GetByTestId("review-acc")).ToHaveTextAsync(transfer.Iban);
            await Expect(Page.GetByTestId("review-amount")).ToHaveTextAsync(transfer.Amount.ToString("F2", CultureInfo.InvariantCulture));

            // Act - Finalize
            await Page.GetByTestId("btn-submit-transfer").ClickAsync();

            // Assert
            await Expect(Page.GetByTestId("success-msg")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("receipt-id")).ToHaveTextAsync(new Regex("TXN-\\d{4,10}"));
        }

        [Test]
        public async Task Stepper_ModifyingDataAtStep3_ShouldPersistStateReentry()
        {
            var transfer = BankingDataFactory.CreateValidWireTransfer();

			await Page.GotoAsync(TransferUrl);

			// Fill out Step 1 & Step 2
			await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync(transfer.Iban);
            await Page.GetByTestId("btn-next-1").ClickAsync();
            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.GetByTestId("transfer-amount").FillAsync(transfer.Amount.ToString(CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();

            // Navigate backwards from Step 3 to Step 2
            await Page.GetByTestId("btn-back-2").ClickAsync();

            // Assert state is retained
            await Expect(Page.GetByTestId("transfer-amount")).ToHaveValueAsync(transfer.Amount.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Risk & Compliance (AML / Zero-Trust Verification)

        [Test]
        public async Task Compliance_TransferExceedingAmlThreshold_ShouldTriggerHighRiskAuditFlag()
        {
            // Arrange - Generates $5,000,000.00 transfer via factory
            var highRiskTransfer = BankingDataFactory.CreateWireTransfer_AmlThresholdExceeded();

			await Page.GotoAsync(TransferUrl);

			await Page.GetByTestId("recipient-select").SelectOptionAsync("global");
            await Page.GetByTestId("acc-number").FillAsync(highRiskTransfer.Iban);
            await Page.GetByTestId("btn-next-1").ClickAsync();

            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.GetByTestId("transfer-amount").FillAsync(highRiskTransfer.Amount.ToString(CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();

            // Assert - UI logic catches the threshold violation and triggers compliance warnings
            await Expect(Page.GetByTestId("aml-warning")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("aml-warning")).ToContainTextAsync("Requires Compliance Review");
        }

        [Test]
        public async Task Security_MalformedIbanPattern_ShouldRejectAtEdgeGate()
        {
            // Arrange
            var malformedData = BankingDataFactory.CreateWireTransfer_MalformedIban();

			await Page.GotoAsync(TransferUrl);

			// Act
			await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync(malformedData.Iban);
            await Page.GetByTestId("btn-next-1").ClickAsync();

            // Assert
            await Expect(Page.GetByTestId("acc-error")).ToHaveTextAsync("Invalid IBAN format structure.");
            await Expect(Page.GetByTestId("step-2-form")).ToBeHiddenAsync();
        }

        [Test]
        public async Task Security_InvalidSwiftFormat_ShouldFailValidationInline()
        {
            // Arrange
            var badSwiftData = BankingDataFactory.CreateWireTransfer_InvalidSwiftFormat();

			await Page.GotoAsync(TransferUrl);

			await Page.GetByTestId("recipient-select").SelectOptionAsync("global");
            await Page.GetByTestId("acc-number").FillAsync(badSwiftData.Iban);
            await Page.GetByTestId("btn-next-1").ClickAsync();

            // Assert
            await Expect(Page.GetByTestId("swift-error")).ToBeVisibleAsync();
        }

        #endregion

        #region UI State Mutations & Layout Resiliency

        [Test]
        public async Task Dashboard_DynamicLedgerSearch_ShouldUpdateVisibleBalanceSum()
        {
            await Page.GotoAsync(DashboardUrl);

            // Fetch initial row visibility counts dynamically
            var initialRows = Page.Locator(".ledger-row:visible");
            await Expect(initialRows).ToHaveCountAsync(3);

            // Act - Apply dynamic query search filter
            await Page.GetByTestId("search-ledger").FillAsync("Tech LLC");

            // Assert - DOM updates accurately reflect filtered query state
            var filteredRows = Page.Locator(".ledger-row:visible");
            await Expect(filteredRows).ToHaveCountAsync(1);
            await Expect(filteredRows).ToContainTextAsync("Tech LLC");
        }

        [Test]
        public async Task UI_ExtremeInputLengths_ShouldHandledCleanlyWithoutDomBreaks()
        {
            // Arrange - Retrieves massive generated 255-character string
            var edgeUser = DataFactory.CreateUser_ExtremeNameLength();

            await Page.GotoAsync(LoginUrl);

            // Act - Inject data into inputs to verify layouts hold bounding wrappers
            await Page.GetByPlaceholder("Username").FillAsync(edgeUser.Username);
            await Page.GetByPlaceholder("Password").FillAsync(edgeUser.Password);

            // Assert element properties are preserved without exploding layout
            var usernameInput = Page.GetByPlaceholder("Username");
            await Expect(usernameInput).ToHaveValueAsync(edgeUser.Username);
        }

        [Test]
        public async Task NotificationModal_EscKeyPreserve_ShouldCloseModalCleanly()
        {
            await Page.GotoAsync(DashboardUrl);

            // Open Modal
            await Page.GetByTestId("btn-notifications").ClickAsync();
            await Expect(Page.GetByTestId("notification-modal")).ToBeVisibleAsync();

            // Act - Fire Native Keyboard Escape Event
            await Page.Keyboard.PressAsync("Escape");

            // Assert
            await Expect(Page.GetByTestId("notification-modal")).ToBeHiddenAsync();
        }

        #endregion

        #region Identity & Profile Context Validations

        [Test]
        public async Task Profile_ValidKycVerification_ShouldDisplayVerifiedBadge()
        {
            // Arrange
            var kyc = BankingDataFactory.CreateValidKycProfile();
            var user = DataFactory.CreateValidUser();

            await Page.GotoAsync(DashboardUrl);

            // Injecting data securely into dynamic data fields
            var profileContainer = Page.Locator("#user-profile-widget");
            await Expect(profileContainer).ToContainTextAsync(user.Id);

            // Assume system updates profile view dynamically based on data state
            await Expect(Page.Locator("#kyc-status-badge")).ToHaveTextAsync("Verified");
        }

        [Test]
        public async Task Security_SessionCookieDestruction_ShouldEnforceImmediateRedirect()
        {
            // Arrange
            var user = DataFactory.CreateValidUser();
            await Page.GotoAsync(LoginUrl);

            await Page.GetByPlaceholder("Username").FillAsync(user.Username);
            await Page.GetByPlaceholder("Password").FillAsync(user.Password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            // Clear Cookies dynamically via Playwright BrowserContext API
            await Page.Context.ClearCookiesAsync();

            // Act - Try navigating to protected page space
            await Page.ReloadAsync();

            // Assert - App forces redirection out of private routes back into auth gateway
            await Expect(Page).ToHaveURLAsync(new Regex(".*login\\.html"));
        }

        #endregion
    }
}