pipeline {
    agent any

    options {
        ansiColor('xterm')
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    // 1. The Schedule (Evaluated outside of runtime parameters)
    // H 0 * * * = Runs automatically once a day around midnight
    triggers {
        cron('H 0 * * *')
    }

    // 2. The Dynamic Pipeline Parameters
    parameters {
        string(name: 'TARGET_BRANCH', defaultValue: 'main', description: 'Which Git branch should we execute?')
        
        choice(name: 'ENVIRONMENT', choices: ['Sandbox', 'QA', 'Pre-Prod'], description: 'Target environment for test execution')
        
        choice(name: 'TEST_SUITE', choices: ['All', 'Smoke', 'Authentication', 'Transfers'], description: 'Select the specific test category to run')
        
        booleanParam(name: 'RUN_AI_TRIAGE', defaultValue: true, description: 'Enable local Llama 3 analysis on failure?')
    }

    environment {
        ALLURE_RESULTS_DIR = "${WORKSPACE}/allure-results"
        
        // Pass Jenkins parameters down to the .NET environment variables
        TEST_ENV = "${params.ENVIRONMENT}"
        AI_TRIAGE_ENABLED = "${params.RUN_AI_TRIAGE}"
    }

    stages {
        stage('Checkout Code') {
            steps {
                echo "Fetching branch: ${params.TARGET_BRANCH}..."
                // Tells Jenkins to specifically pull the branch requested in the parameter
                git branch: "${params.TARGET_BRANCH}", url: 'https://github.com/ViktorVakareev/Playwright-DotNet-Enterprise-Architecture.git'
            }
        }

        stage('Clean & Restore') {
            steps {
                bat 'dotnet restore WorldBank.Automation.sln'
            }
        }

        stage('Compile Solution') {
            steps {
                bat 'dotnet build WorldBank.Automation.sln --configuration Release --no-restore'
            }
        }

        stage('Provision Playwright Engines') {
            steps {
                powershell '''
                $env:PLAYWRIGHT_BROWSERS_PATH="0"
                pwsh bin/Release/net10.0/playwright.ps1 install chromium --with-deps
                '''
            }
        }

        stage('Execute Automated Quality Gates') {
            steps {
                script {
                    echo "Executing ${params.TEST_SUITE} suite against ${params.ENVIRONMENT} environment."
                    
                    // Construct the dynamic test command based on parameters
                    def testCommand = 'dotnet test WorldBank.Automation.sln --configuration Release --no-build'
                    
                    if (params.TEST_SUITE != 'All') {
                        // Uses NUnit's filter feature to only run specific TestCategories
                        testCommand += " --filter TestCategory=${params.TEST_SUITE}"
                    }

                    // Run the constructed command
                    catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                        bat testCommand
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