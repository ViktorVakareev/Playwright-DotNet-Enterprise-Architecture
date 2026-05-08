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
        
        // This is crucial: It adds the .NET tools to the Linux PATH
        PATH = "${tool 'dotnet-10'}:$PATH"
    }

    // THIS IS THE FIX: Tell Jenkins to provision the .NET SDK
    tools {
        dotnetsdk 'dotnet-10'
    }

    stages {
        stage('Checkout Code') {
            steps {
                echo "Fetching branch: ${params.TARGET_BRANCH}..."
                git branch: "${params.TARGET_BRANCH}", url: 'https://github.com/ViktorVakareev/Playwright-DotNet-Enterprise-Architecture.git'
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

        stage('Provision Playwright Engines') {
            steps {
                // Swapped pwsh for the cross-platform dotnet tool command
                sh '''
                export PLAYWRIGHT_BROWSERS_PATH="0"
                dotnet tool install --global Microsoft.Playwright.CLI
                export PATH="$PATH:$HOME/.dotnet/tools"
                playwright install chromium --with-deps
                '''
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