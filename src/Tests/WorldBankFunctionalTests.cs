using System.Text.RegularExpressions;
using WorldBank.Automation.Tests.Data;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [Category("Functional")]
    public class WorldBankFunctionalTests : AiTriage // Inherits AI capabilities and Context initialization
    {
        // Computed properties dynamically resolve the environment URL at runtime
        private string DashboardUrl => $"{AppConfig.GetBaseUrl()}/dashboard.html?role=standard";
        private string TransferUrl => $"{AppConfig.GetBaseUrl()}/transfer.html";

        #region Group 1: Navigation & Rendering

        [Test]
        public async Task Nav_Dashboard_LoadsCorrectTitle()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Expect(Page).ToHaveTitleAsync("Dashboard - WorldBank Mock");
        }

        [Test]
        public async Task Nav_AppTitle_IsVisible()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Expect(Page.GetByTestId("app-title")).ToHaveTextAsync("WorldBank Enterprise");
        }

        [Test]
        public async Task Nav_DarkModeToggle_ChangesThemeAttribute()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Page.GetByTestId("btn-dark-mode").ClickAsync();

            // Check if the HTML tag has the dark theme data attribute
            var htmlLocator = Page.Locator("html");
            await Expect(htmlLocator).ToHaveAttributeAsync("data-theme", "dark");
        }

        [Test]
        public async Task Nav_WireTransferButton_NavigatesToTransferPage()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Page.GetByTestId("btn-nav-transfer").ClickAsync();

            // Using Regex to handle dynamic environment base URLs flexibly
            await Expect(Page).ToHaveURLAsync(new Regex(".*transfer\\.html"));
        }

        #endregion

        #region Group 2: Dashboard Grid & Search

        [Test]
        public async Task Grid_LedgerTable_RendersDefaultRows()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            var rows = Page.Locator(".ledger-row");
            await Expect(rows).ToHaveCountAsync(3);
        }

        [Test]
        public async Task Grid_Search_FiltersVisibleRows()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Page.GetByTestId("search-ledger").FillAsync("tech llc");

            // Upgraded to Playwright's native :visible pseudo-selector
            var visibleRows = Page.Locator(".ledger-row:visible");
            await Expect(visibleRows).ToHaveCountAsync(1);
            await Expect(visibleRows).ToContainTextAsync("Tech LLC");
        }

        [Test]
        public async Task Grid_Search_NoResultsShowsErrorMessage()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Page.GetByTestId("search-ledger").FillAsync("bitcoin");

            await Expect(Page.GetByTestId("no-results-msg")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Grid_Search_ClearingFilterRestoresAllRows()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Page.GetByTestId("search-ledger").FillAsync("cloud");
            await Page.GetByTestId("search-ledger").ClearAsync();

            var visibleRows = Page.Locator(".ledger-row:visible");
            await Expect(visibleRows).ToHaveCountAsync(3);
        }

        [Test]
        public async Task Grid_NotificationModal_OpensAndCloses()
        {
            await AuthenticateAndNavigateAsync(DashboardUrl);
            await Page.GetByTestId("btn-notifications").ClickAsync();
            await Expect(Page.GetByTestId("notification-modal")).ToBeVisibleAsync();

            await Page.GetByTestId("btn-close-modal").ClickAsync();
            await Expect(Page.GetByTestId("notification-modal")).ToBeHiddenAsync();
        }

        #endregion

        #region Group 3: Transfer Form - Validation Edge Cases

        [Test]
        public async Task Form_Step1_SubmitWithoutRecipient_ShowsError()
        {
            await AuthenticateAndNavigateAsync(TransferUrl);
            await Page.GetByTestId("acc-number").FillAsync("1234567890");
            await Page.GetByTestId("btn-next-1").ClickAsync();

            await Expect(Page.GetByTestId("recipient-error")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("step-2-form")).ToBeHiddenAsync();
        }

        [Test]
        public async Task Form_Step1_AccountNumberTooShort_ShowsError()
        {
            await AuthenticateAndNavigateAsync(TransferUrl);
            await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync("12345"); // Only 5 digits
            await Page.GetByTestId("btn-next-1").ClickAsync();

            await Expect(Page.GetByTestId("acc-error")).ToBeVisibleAsync();
        }

        #region Group 3: Transfer Form - Validation Edge Cases

        [Test]
        public async Task Form_Step2_NegativeAmount_ShowsError()
        {
            // Inject dynamic boundary data
            var testData = BankingDataFactory.CreateWireTransfer_NegativeAmount();

            await AuthenticateAndNavigateAsync(TransferUrl);
            await FillValidStep1(testData);

            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));

            // Apply the dynamic negative amount safely
            await Page.GetByTestId("transfer-amount").FillAsync(testData.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("amount-error")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step2_ZeroAmount_ShowsError()
        {
            // Inject dynamic boundary data
            var testData = BankingDataFactory.CreateWireTransfer_ZeroAmount();

            await AuthenticateAndNavigateAsync(TransferUrl);
            await FillValidStep1(testData);

            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.GetByTestId("transfer-amount").FillAsync(testData.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("amount-error")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step2_PastDate_ShowsError()
        {
            // We use valid financial data here, because we are testing the Date boundary
            var testData = BankingDataFactory.CreateValidWireTransfer();

            await AuthenticateAndNavigateAsync(TransferUrl);
            await FillValidStep1(testData);

            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            await Page.GetByTestId("transfer-date").FillAsync(yesterday);
            await Page.GetByTestId("transfer-amount").FillAsync(testData.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("date-error")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step2_MissingDate_ShowsError()
        {
            // Use valid financial data
            var testData = BankingDataFactory.CreateValidWireTransfer();

            await AuthenticateAndNavigateAsync(TransferUrl);
            await FillValidStep1(testData);

            // Intentionally skip the Date field
            await Page.GetByTestId("transfer-amount").FillAsync(testData.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("date-error")).ToBeVisibleAsync();
        }

        #endregion

        #endregion

        #region Group 4: Transfer Form - Happy Path & Stepper Logic

        [Test]
        public async Task Form_Step3_ReviewCard_MatchesEnteredData()
        {
            // 1. Generate clean, dynamic financial data
            var transferData = BankingDataFactory.CreateValidWireTransfer();

            await AuthenticateAndNavigateAsync(TransferUrl);

            // 2. Pass the data objects into the helpers
            await FillValidStep1(transferData);
            await FillValidStep2(transferData);

            // 3. Assert against the exact dynamic data we injected
            await Expect(Page.GetByTestId("review-acc")).ToHaveTextAsync(transferData.Iban);

            // Format to match the UI's expected 2-decimal display
            string expectedAmount = transferData.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            await Expect(Page.GetByTestId("review-amount")).ToHaveTextAsync(expectedAmount);
        }

        [Test]
        public async Task Form_EndToEnd_HappyPath_CompletesTransfer()
        {
            var transferData = BankingDataFactory.CreateValidWireTransfer();

            await AuthenticateAndNavigateAsync(TransferUrl);
            await FillValidStep1(transferData);
            await FillValidStep2(transferData);

            await Page.GetByTestId("btn-submit-transfer").ClickAsync();

            await Expect(Page.GetByTestId("success-msg")).ToBeVisibleAsync();
        }

        #endregion

        // Upgraded Helpers to accept dynamic Record data
        private async Task FillValidStep1(WireTransfer data)
        {
            await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync(data.Iban);
            await Page.GetByTestId("btn-next-1").ClickAsync();
        }

        private async Task FillValidStep2(WireTransfer data)
        {
            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            // Safe conversion for any locale
            await Page.GetByTestId("transfer-amount").FillAsync(data.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Page.GetByTestId("btn-next-2").ClickAsync();
        }
    }
}