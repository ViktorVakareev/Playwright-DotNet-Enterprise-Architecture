using System;
using Bogus;

namespace WorldBank.Automation.Tests.Data
{
    // 1. Models (Records are the perfect immutable structure for test data)
    public record WireTransfer(decimal Amount, string Currency, string BeneficiaryName, string Iban, string SwiftCode, string PurposeOfTransfer);
    public record KycProfile(string SsnLastFour, string EmploymentStatus, decimal AnnualIncome, string PassportNumber);
    public record LoanApplication(string LoanType, decimal RequestedAmount, int TermMonths);

    public static class BankingDataFactory
    {
        // =====================================================================
        // CACHED FAKER DEFINITIONS (Performance Optimization)
        // =====================================================================

        private static readonly Faker<WireTransfer> ValidWireTransferFaker = new Faker<WireTransfer>("en")
            .RuleFor(w => w.Amount, f => Math.Round(f.Finance.Amount(100, 50000), 2)) // Lock to 2 decimals
            .RuleFor(w => w.Currency, f => f.Finance.Currency().Code)
            .RuleFor(w => w.BeneficiaryName, f => f.Name.FullName())
            .RuleFor(w => w.Iban, f => f.Finance.Iban())
            .RuleFor(w => w.SwiftCode, f => f.Finance.Bic())
            .RuleFor(w => w.PurposeOfTransfer, f => f.Commerce.ProductName());

        private static readonly Faker<KycProfile> ValidKycFaker = new Faker<KycProfile>("en")
            .RuleFor(k => k.SsnLastFour, f => f.Random.Number(1000, 9999).ToString())
            .RuleFor(k => k.EmploymentStatus, f => f.PickRandom("Employed", "Self-Employed", "Retired"))
            .RuleFor(k => k.AnnualIncome, f => Math.Round(f.Finance.Amount(40000, 250000), 2))
            .RuleFor(k => k.PassportNumber, f => f.Random.AlphaNumeric(9).ToUpper());

        private static readonly Faker<LoanApplication> ValidLoanFaker = new Faker<LoanApplication>("en")
            .RuleFor(l => l.LoanType, f => f.PickRandom("Mortgage", "Auto", "Personal"))
            .RuleFor(l => l.RequestedAmount, f => Math.Round(f.Finance.Amount(10000, 500000), 2))
            .RuleFor(l => l.TermMonths, f => f.PickRandom(12, 24, 36, 60, 120, 360));


        // =====================================================================
        // HAPPY PATH GENERATORS
        // =====================================================================

        public static WireTransfer CreateValidWireTransfer() => ValidWireTransferFaker.Generate();
        public static KycProfile CreateValidKycProfile() => ValidKycFaker.Generate();
        public static LoanApplication CreateValidLoanApplication() => ValidLoanFaker.Generate();


        // =====================================================================
        // NEGATIVE PATH / EDGE CASE GENERATORS (Using Record Mutation)
        // =====================================================================

        /// <summary>
        /// Generates a valid wire transfer, but strips out the SWIFT code for validation testing.
        /// </summary>
        public static WireTransfer CreateWireTransfer_MissingSwift()
        {
            var validData = ValidWireTransferFaker.Generate();
            return validData with { SwiftCode = string.Empty }; // C# Record Mutation
        }

        /// <summary>
        /// Generates a valid wire transfer, but injects a negative amount.
        /// </summary>
        public static WireTransfer CreateWireTransfer_NegativeAmount()
        {
            var validData = ValidWireTransferFaker.Generate();
            return validData with { Amount = -500.00m };
        }

        /// <summary>
        /// Generates a valid KYC profile, but uses an invalid SSN length.
        /// </summary>
        public static KycProfile CreateKycProfile_InvalidSsn()
        {
            var validData = ValidKycFaker.Generate();
            return validData with { SsnLastFour = "99" }; // Too short
        }

        /// <summary>
        /// Generates a valid wire transfer, but injects a zero amount.
        /// </summary>
        public static WireTransfer CreateWireTransfer_ZeroAmount()
        {
            var validData = ValidWireTransferFaker.Generate();
            return validData with { Amount = 0.00m };
        }
    }
}