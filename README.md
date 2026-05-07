# World Bank: Enterprise Playwright Automation Framework

## Overview
This repository contains a Staff-level, production-grade test automation framework built with **.NET 10**, **NUnit**, and **Playwright**. It is designed with strict DevSecOps principles, zero-trust security architecture, and AI-augmented failure triage to meet the rigorous compliance standards of financial institutions like the World Bank.

## 🏗️ Architectural Decisions (The "Why")

### 1. Playwright over Selenium
*   **Why:** Playwright offers out-of-the-box auto-waiting, network interception, and isolated browser contexts. We strictly use the **Locator API** (`GetByRole`, `GetByLabel`) and **Web-First Assertions**, eliminating the need for flaky `WebDriverWait` loops or custom synchronization logic.

### 2. Zero-Trust Secrets Management
*   **Why:** Hardcoding credentials or committing them to source control is a critical security vulnerability. 
*   **Implementation:** We utilize `.NET User Secrets` during local development to keep passwords and API tokens completely out of the repository. In CI/CD pipelines, these map directly to Azure Key Vault or GitHub Secrets.

### 3. Data Sovereignty via Synthetic Generation
*   **Why:** Using real production data (PII) for testing violates regulatory compliance (GDPR, KYC laws). Static test databases lead to test collisions and flakiness.
*   **Implementation:** We use the `Bogus` library in our `BankingDataFactory.cs` to generate highly realistic, relationally accurate banking data (IBANs, SWIFT codes, SSNs) dynamically at runtime. Tests are non-deterministic and leave no data residue.

### 4. Supply Chain Integrity
*   **Why:** Blindly pulling NuGet packages opens the framework to supply chain attacks.
*   **Implementation:** A `NuGet.Config` file restricts package sources, simulating a corporate JFrog Artifactory gateway.

---

## 🚀 Getting Started

### Prerequisites
1.  **Visual Studio 2026** (or Rider) with .NET 10 SDK.
2.  **Ollama** installed locally (for AI Triage).
3.  **PowerShell** (for Playwright binary installation).

### Installation Setup
1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/yourusername/WorldBank.Automation.git](https://github.com/yourusername/WorldBank.Automation.git)
    
Install Playwright Browsers:
Open the Developer PowerShell in Visual Studio and run:

PowerShell
pwsh bin/Debug/net10.0/playwright.ps1 install

3.  **Setup User Secrets:**
    Right-click the `WorldBank.Automation.Tests` project -> **Manage User Secrets**. Add your configuration:
    ```json
    {
      "TargetEnv": {
        "Url": "[https://sandbox.worldbank.internal](https://sandbox.worldbank.internal)"
      }
    }
    
Start Local AI:
Ensure Ollama is running your Llama 3 model in the background:

Bash
ollama run llama3


### Project Structure
*   `Infrastructure/` - Global setup, AI integrations, and HTTP clients.
*   `Data/` - Bogus factories for generating Synthetic Banking Data.
*   `Pages/` - Component-based Page Objects.
*   `Tests/` - NUnit test execution suites.

---