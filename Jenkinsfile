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
        
        // Explicitly tell Linux where to find the .NET binaries we are about to install
        DOTNET_ROOT = "${HOME}/.dotnet"
        PATH = "${HOME}/.dotnet:${HOME}/.dotnet/tools:${env.PATH}"
    }

    stages {
        stage('Checkout Code') {
            steps {
                echo "Fetching branch: ${params.TARGET_BRANCH}..."
                git branch: "${params.TARGET_BRANCH}", url: 'https://github.com/ViktorVakareev/Playwright-DotNet-Enterprise-Architecture.git'
            }
        }

        stage('Provision Environment (OS-Agnostic)') {
            steps {
                sh '''
                echo "1. Downloading Microsoft official Linux .NET installer..."
                curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
                chmod +x ./dotnet-install.sh
                
                echo "2. Installing .NET SDK..."
                # Installs the latest .NET SDK directly into the Linux container
                ./dotnet-install.sh --channel 10.0
                
                echo "3. Installing Playwright CLI & Browsers..."
                export PLAYWRIGHT_BROWSERS_PATH="0"
                dotnet tool install --global Microsoft.Playwright.CLI
                playwright install chromium --with-deps
                '''
            }
        }

        stage('Clean & Restore') {
            steps {
                sh 'dotnet restore WorldBank.Automation.sln'
            }
        }

        stage('Compile Solution') {
            steps {
                sh 'dotnet build WorldBank.Automation.sln --configuration Release --no-restore'
            }
        }

        stage('Execute Automated Quality Gates') {
            steps {
                script {
                    echo "Executing ${params.TEST_SUITE} suite against ${params.ENVIRONMENT} environment."
                    
                    def testCommand = 'dotnet test WorldBank.Automation.sln --configuration Release --no-build'
                    
                    if (params.TEST_SUITE != 'All') {
                        testCommand += " --filter TestCategory=${params.TEST_SUITE}"
                    }

                    catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                        sh testCommand
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Generating Allure Quality Report...'
            allure includeProperties: false, jdk: '', results: [[path: 'WorldBank.Automation.Tests/bin/Release/net10.0/allure-results']]
            
            echo 'Archiving Playwright Traces...'
            archiveArtifacts artifacts: '**/playwright-traces/*.zip', allowEmptyArchive: true
        }
    }
}