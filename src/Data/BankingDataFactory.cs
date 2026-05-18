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
    .CustomInstantiator(f => new WireTransfer(
        Math.Round(f.Finance.Amount(100, 50000), 2),
        f.Finance.Currency().Code,
        f.Name.FullName(),
        f.Finance.Iban(),
        f.Finance.Bic(),
        f.Commerce.ProductName()
    ));

        private static readonly Faker<KycProfile> ValidKycFaker = new Faker<KycProfile>("en")
            .CustomInstantiator(f => new KycProfile(
                f.Random.Number(1000, 9999).ToString(),
                f.PickRandom("Employed", "Self-Employed", "Retired"),
                Math.Round(f.Finance.Amount(40000, 250000), 2),
                f.Random.AlphaNumeric(9).ToUpper()
            ));

        private static readonly Faker<LoanApplication> ValidLoanFaker = new Faker<LoanApplication>("en")
            .CustomInstantiator(f => new LoanApplication(
                f.PickRandom("Mortgage", "Auto", "Personal"),
                Math.Round(f.Finance.Amount(10000, 500000), 2),
                f.PickRandom(12, 24, 36, 60, 120, 360)
            ));


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

        /// <summary>
        /// Generates an astronomical wire transfer amount to trigger anti-money laundering (AML) tier compliance.
        /// </summary>
        public static WireTransfer CreateWireTransfer_AmlThresholdExceeded()
        {
            var validData = ValidWireTransferFaker.Generate();
            return validData with { Amount = 5000000.00m }; // $5M triggers high-risk AML flow
        }

        /// <summary>
        /// Generates a transfer object with an completely malformed, non-standard IBAN pattern.
        /// </summary>
        public static WireTransfer CreateWireTransfer_MalformedIban()
        {
            var validData = ValidWireTransferFaker.Generate();
            return validData with { Iban = "INVALID_IBAN_CHARS_12345!!!" };
        }

        /// <summary>
        /// Generates a transfer object with an invalid, non-standard SWIFT/BIC code format length.
        /// </summary>
        public static WireTransfer CreateWireTransfer_InvalidSwiftFormat()
        {
            var validData = ValidWireTransferFaker.Generate();
            return validData with { SwiftCode = "BADSWIFT" }; // Too short / wrong format
        }
    }
}