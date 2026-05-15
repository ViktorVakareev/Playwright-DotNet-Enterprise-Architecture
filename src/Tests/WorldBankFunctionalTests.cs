using System.Text.RegularExpressions;
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
            await Page.GotoAsync(DashboardUrl);
            await Expect(Page).ToHaveTitleAsync("Dashboard - WorldBank Mock");
        }

        [Test]
        public async Task Nav_AppTitle_IsVisible()
        {
            await Page.GotoAsync(DashboardUrl);
            await Expect(Page.GetByTestId("app-title")).ToHaveTextAsync("WorldBank Enterprise");
        }

        [Test]
        public async Task Nav_DarkModeToggle_ChangesThemeAttribute()
        {
            await Page.GotoAsync(DashboardUrl);
            await Page.GetByTestId("btn-dark-mode").ClickAsync();

            // Check if the HTML tag has the dark theme data attribute
            var htmlLocator = Page.Locator("html");
            await Expect(htmlLocator).ToHaveAttributeAsync("data-theme", "dark");
        }

        [Test]
        public async Task Nav_WireTransferButton_NavigatesToTransferPage()
        {
            await Page.GotoAsync(DashboardUrl);
            await Page.GetByTestId("btn-nav-transfer").ClickAsync();

            // Using Regex to handle dynamic environment base URLs flexibly
            await Expect(Page).ToHaveURLAsync(new Regex(".*transfer\\.html"));
        }

        #endregion

        #region Group 2: Dashboard Grid & Search

        [Test]
        public async Task Grid_LedgerTable_RendersDefaultRows()
        {
            await Page.GotoAsync(DashboardUrl);
            var rows = Page.Locator(".ledger-row");
            await Expect(rows).ToHaveCountAsync(3);
        }

        [Test]
        public async Task Grid_Search_FiltersVisibleRows()
        {
            await Page.GotoAsync(DashboardUrl);
            await Page.GetByTestId("search-ledger").FillAsync("tech llc");

            // Upgraded to Playwright's native :visible pseudo-selector
            var visibleRows = Page.Locator(".ledger-row:visible");
            await Expect(visibleRows).ToHaveCountAsync(1);
            await Expect(visibleRows).ToContainTextAsync("Tech LLC");
        }

        [Test]
        public async Task Grid_Search_NoResultsShowsErrorMessage()
        {
            await Page.GotoAsync(DashboardUrl);
            await Page.GetByTestId("search-ledger").FillAsync("bitcoin");

            await Expect(Page.GetByTestId("no-results-msg")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Grid_Search_ClearingFilterRestoresAllRows()
        {
            await Page.GotoAsync(DashboardUrl);
            await Page.GetByTestId("search-ledger").FillAsync("cloud");
            await Page.GetByTestId("search-ledger").ClearAsync();

            var visibleRows = Page.Locator(".ledger-row:visible");
            await Expect(visibleRows).ToHaveCountAsync(3);
        }

        [Test]
        public async Task Grid_NotificationModal_OpensAndCloses()
        {
            await Page.GotoAsync(DashboardUrl);
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
            await Page.GotoAsync(TransferUrl);
            await Page.GetByTestId("acc-number").FillAsync("1234567890");
            await Page.GetByTestId("btn-next-1").ClickAsync();

            await Expect(Page.GetByTestId("recipient-error")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("step-2-form")).ToBeHiddenAsync();
        }

        [Test]
        public async Task Form_Step1_AccountNumberTooShort_ShowsError()
        {
            await Page.GotoAsync(TransferUrl);
            await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync("12345"); // Only 5 digits
            await Page.GetByTestId("btn-next-1").ClickAsync();

            await Expect(Page.GetByTestId("acc-error")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step2_NegativeAmount_ShowsError()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();

            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.GetByTestId("transfer-amount").FillAsync("-500");
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("amount-error")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step2_ZeroAmount_ShowsError()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();

            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.GetByTestId("transfer-amount").FillAsync("0");
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("amount-error")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step2_PastDate_ShowsError()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();

            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            await Page.GetByTestId("transfer-date").FillAsync(yesterday);
            await Page.GetByTestId("transfer-amount").FillAsync("1000");
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("date-error")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step2_MissingDate_ShowsError()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();

            await Page.GetByTestId("transfer-amount").FillAsync("1000");
            await Page.GetByTestId("btn-next-2").ClickAsync();

            await Expect(Page.GetByTestId("date-error")).ToBeVisibleAsync();
        }

        #endregion

        #region Group 4: Transfer Form - Happy Path & Stepper Logic

        [Test]
        public async Task Stepper_NavigatesToStep2_OnValidStep1()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();

            await Expect(Page.GetByTestId("step-2-form")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("step-2-ind")).ToHaveClassAsync(new Regex("active"));
        }

        [Test]
        public async Task Stepper_BackButton_ReturnsToStep1()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();

            await Page.GetByTestId("btn-back-2").ClickAsync();
            await Expect(Page.GetByTestId("step-1-form")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Form_Step3_ReviewCard_MatchesEnteredData()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();
            await FillValidStep2();

            await Expect(Page.GetByTestId("review-recipient")).ToHaveTextAsync("Acme Corp (US)");
            await Expect(Page.GetByTestId("review-acc")).ToHaveTextAsync("1234567890");
            await Expect(Page.GetByTestId("review-amount")).ToHaveTextAsync("5000.00");
        }

        [Test]
        public async Task Form_EndToEnd_HappyPath_CompletesTransfer()
        {
            await Page.GotoAsync(TransferUrl);
            await FillValidStep1();
            await FillValidStep2();

            await Page.GetByTestId("btn-submit-transfer").ClickAsync();

            await Expect(Page.GetByTestId("success-msg")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("btn-submit-transfer")).ToBeDisabledAsync();
        }

        #endregion

        // Helper methods to keep tests clean
        private async Task FillValidStep1()
        {
            await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync("1234567890");
            await Page.GetByTestId("btn-next-1").ClickAsync();
        }

        private async Task FillValidStep2()
        {
            await Page.GetByTestId("transfer-date").FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.GetByTestId("transfer-amount").FillAsync("5000");
            await Page.GetByTestId("btn-next-2").ClickAsync();
        }
    }
}