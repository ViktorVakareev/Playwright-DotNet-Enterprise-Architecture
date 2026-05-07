using Bogus;

namespace WorldBank.Automation.Tests.Data;

public static class BankingDataFactory
{
    // 1. Generate International Wire Transfer Data
    public static WireTransfer CreateWireTransfer() => new Faker<WireTransfer>()
        .RuleFor(w => w.Amount, f => f.Finance.Amount(100, 50000))
        .RuleFor(w => w.Currency, f => f.Finance.Currency().Code)
        .RuleFor(w => w.BeneficiaryName, f => f.Name.FullName())
        .RuleFor(w => w.Iban, f => f.Finance.Iban())
        .RuleFor(w => w.SwiftCode, f => f.Finance.Bic())
        .RuleFor(w => w.PurposeOfTransfer, f => f.Commerce.ProductName())
        .Generate();

    // 2. Generate KYC (Know Your Customer) Compliance Data
    public static KycProfile CreateKycProfile() => new Faker<KycProfile>()
        .RuleFor(k => k.SsnLastFour, f => f.Random.Number(1000, 9999).ToString())
        .RuleFor(k => k.EmploymentStatus, f => f.PickRandom("Employed", "Self-Employed", "Retired"))
        .RuleFor(k => k.AnnualIncome, f => f.Finance.Amount(40000, 250000))
        .RuleFor(k => k.PassportNumber, f => f.Random.AlphaNumeric(9).ToUpper())
        .Generate();

    // 3. Generate Loan Application Data
    public static LoanApplication CreateLoanApplication() => new Faker<LoanApplication>()
        .RuleFor(l => l.LoanType, f => f.PickRandom("Mortgage", "Auto", "Personal"))
        .RuleFor(l => l.RequestedAmount, f => f.Finance.Amount(10000, 500000))
        .RuleFor(l => l.TermMonths, f => f.PickRandom(12, 24, 36, 60, 120, 360))
        .Generate();
}

public record WireTransfer(decimal Amount, string Currency, string BeneficiaryName, string Iban, string SwiftCode, string PurposeOfTransfer);
public record KycProfile(string SsnLastFour, string EmploymentStatus, decimal AnnualIncome, string PassportNumber);
public record LoanApplication(string LoanType, decimal RequestedAmount, int TermMonths);