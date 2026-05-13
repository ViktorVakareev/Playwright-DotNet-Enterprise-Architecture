using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace WorldBank.Automation.Tests
{
    [TestFixture]
    // ParallelScope.All tells NUnit to run every single [TestCase] simultaneously!
    [Parallelizable(ParallelScope.All)]
    public class WorldBankDataDrivenTests : PageTest
    {
        private readonly string _transferUrl = "http://localhost:8081/transfer.html";

        // =========================================================================
        // SCENARIO 1: STEP 1 ACCOUNT & RECIPIENT VALIDATIONS
        // =========================================================================

        // --- THE DATA ---
        // --- THE DATA ---
        [TestCase("acme", "1234567890", true, "", TestName = "Valid Account - US Recipient")]
        [TestCase("global", "0987654321", true, "", TestName = "Valid Account - UK Recipient")]
        [TestCase("", "1234567890", false, "recipient-error", TestName = "Error - Missing Recipient")]
        [TestCase("acme", "123", false, "acc-error", TestName = "Error - Account Too Short")]
        [TestCase("global", "", false, "acc-error", TestName = "Error - Missing Account")]
        [TestCase("acme", "ABCDEFGHIJ", false, "acc-error", TestName = "Error - Letters Instead of Numbers")]

        // --- THE LOGIC ---
        public async Task Transfer_Step1_DataDrivenValidations(string recipient, string account, bool expectedSuccess, string errorTestId)
        {
            await Page.GotoAsync(_transferUrl);

            // 1. Fill the form using the provided data
            if (!string.IsNullOrEmpty(recipient))
            {
                await Page.GetByTestId("recipient-select").SelectOptionAsync(recipient);
            }
            await Page.GetByTestId("acc-number").FillAsync(account);

            // 2. Click Next
            await Page.GetByTestId("btn-next-1").ClickAsync();

            // 3. Assert based on the expected outcome
            if (expectedSuccess)
            {
                // If valid, we should successfully reach Step 2
                await Expect(Page.GetByTestId("step-2-form")).ToBeVisibleAsync();
            }
            else
            {
                // If invalid, Step 1 should stay visible and the specific error should appear
                await Expect(Page.GetByTestId(errorTestId)).ToBeVisibleAsync();
                await Expect(Page.GetByTestId("step-2-form")).Not.ToBeVisibleAsync();
            }
        }


        // =========================================================================
        // SCENARIO 2: STEP 2 FINANCIAL & DATE VALIDATIONS
        // =========================================================================

        // --- THE DATA --- (daysOffset: 0 = today, 1 = tomorrow, -1 = yesterday)
        // --- THE DATA --- (daysOffset: 0 = today, 1 = tomorrow, -1 = yesterday)
        [TestCase("500", 0, true, "", TestName = "Valid Financials - Today")]
        [TestCase("10000.50", 5, true, "", TestName = "Valid Financials - Future Date")]
        [TestCase("0", 0, false, "amount-error", TestName = "Error - Zero Amount")]
        [TestCase("-50", 1, false, "amount-error", TestName = "Error - Negative Amount")]
        [TestCase("100", -1, false, "date-error", TestName = "Error - Past Date")]
        [TestCase("", 0, false, "amount-error", TestName = "Error - Missing Amount")]

        // --- THE LOGIC ---
        public async Task Transfer_Step2_DataDrivenValidations(string amount, int daysOffset, bool expectedSuccess, string errorTestId)
        {
            await Page.GotoAsync(_transferUrl);

            // Setup: Quickly bypass Step 1 with valid static data
            await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync("1234567890");
            await Page.GetByTestId("btn-next-1").ClickAsync();

            // 1. Calculate the dynamic date based on the daysOffset parameter
            string testDate = DateTime.Now.AddDays(daysOffset).ToString("yyyy-MM-dd");

            // 2. Fill the Step 2 form using the provided data
            await Page.GetByTestId("transfer-date").FillAsync(testDate);
            await Page.GetByTestId("transfer-amount").FillAsync(amount);

            // 3. Click Next
            await Page.GetByTestId("btn-next-2").ClickAsync();

            // 4. Assert based on the expected outcome
            if (expectedSuccess)
            {
                await Expect(Page.GetByTestId("step-3-form")).ToBeVisibleAsync();

                // Extra assertion: Verify the amount formatted correctly on the review screen!
                await Expect(Page.GetByTestId("review-amount")).ToHaveTextAsync(decimal.Parse(amount).ToString("F2"));
            }
            else
            {
                await Expect(Page.GetByTestId(errorTestId)).ToBeVisibleAsync();
                await Expect(Page.GetByTestId("step-3-form")).Not.ToBeVisibleAsync();
            }
        }
    }
}