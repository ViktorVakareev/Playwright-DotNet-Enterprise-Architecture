pipeline {
    agent any

    options {
        ansiColor('xterm')
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    triggers {
        cron('H 0 * * *')
    }

    parameters {
        string(name: 'TARGET_BRANCH', defaultValue: 'main', description: 'Which Git branch should we execute?')
        choice(name: 'ENVIRONMENT', choices: ['Sandbox', 'QA', 'Pre-Prod'], description: 'Target environment for test execution')
        choice(name: 'TEST_SUITE', choices: ['All', 'Smoke', 'Authentication', 'Transfers'], description: 'Select the specific test category to run')
        booleanParam(name: 'RUN_AI_TRIAGE', defaultValue: true, description: 'Enable local Llama 3 analysis on failure?')
    }

    environment {
        ALLURE_RESULTS_DIR = "${WORKSPACE}/allure-results"
        TEST_ENV = "${params.ENVIRONMENT}"
        AI_TRIAGE_ENABLED = "${params.RUN_AI_TRIAGE}"
        
        // ADD THIS LINE:
        OLLAMA_API_URL = "http://host.docker.internal:11434"

        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "1"
        DOTNET_ROOT = "${HOME}/.dotnet"
        PATH = "${HOME}/.dotnet:${HOME}/.dotnet/tools:${env.PATH}"
        PLAYWRIGHT_BROWSERS_PATH = "0"
    }

    stages {
        stage('Checkout Code') {
            steps {
                echo "Fetching branch: ${params.TARGET_BRANCH}..."
                git branch: "${params.TARGET_BRANCH}", url: 'https://github.com/ViktorVakareev/Playwright-DotNet-Enterprise-Architecture.git'
            }
        }

        stage('Install .NET Core') {
            steps {
                sh '''
                echo "1. Downloading Microsoft official Linux .NET installer..."
                curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
                chmod +x ./dotnet-install.sh
                
                echo "2. Installing .NET SDK..."
                ./dotnet-install.sh --channel 10.0
                '''
            }
        }

        stage('Clean, Restore & Compile') {
            steps {
                // Using single quotes (''') means this runs exactly as-is in Linux
                sh '''
                echo "3. Locating and Building the .NET Solution..."
                
                # Dynamically find the .sln file wherever it lives in the repo
                SLN_FILE=$(find . -name "*.sln" | head -n 1)
                echo "Found solution at: $SLN_FILE"
                
                dotnet restore "$SLN_FILE"
                dotnet build "$SLN_FILE" --configuration Release --no-restore
                '''
            }
        }

        stage('Provision Playwright Engines') {
            steps {
                sh '''
                echo "4. Installing Playwright CLI & Browsers..."
                dotnet tool install --global Microsoft.Playwright.CLI || true
                playwright install chromium
                '''
            }
        }

        stage('Execute Automated Quality Gates') {
            steps {
                script {
                    echo "Executing ${params.TEST_SUITE} suite against ${params.ENVIRONMENT} environment."
                    
                    // Setup the NUnit filter dynamically
                    def testFilter = ""
                    if (params.TEST_SUITE != 'All') {
                        testFilter = "--filter \"TestCategory=${params.TEST_SUITE}\""
                    }

                    catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                        // Using double quotes (""") allows Groovy to inject the testFilter variable,
                        // but we must escape the bash variables with a backslash (\$)
                        sh """
                        SLN_FILE=\$(find . -name "*.sln" | head -n 1)
                        echo "Testing: \$SLN_FILE"
                        
                        dotnet test "\$SLN_FILE" --configuration Release --no-build ${testFilter}
                        """
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Generating Allure Quality Report...'
            allure includeProperties: false, jdk: '', results: [[path: 'bin/Release/net10.0/allure-results']]
            
            echo 'Archiving Playwright Traces and AI Triage Reports...'
            // Updated to look for our single summary file
            archiveArtifacts artifacts: '**/playwright-traces/*.zip, **/AiTriage_Summary.md', allowEmptyArchive: true
        }
    }
}