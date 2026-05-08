pipeline {
    // Defines that this can run on any available Jenkins node
    agent any

    options {
        // Keeps the console output clean and colorized
        ansiColor('xterm')
        // Fails the build fast if it hangs
        timeout(time: 30, unit: 'MINUTES')
        // Only keep the last 10 builds to save disk space
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    environment {
        // Tells .NET to output Allure results to a specific folder
        ALLURE_RESULTS_DIR = "${WORKSPACE}/allure-results"
    }

    stages {
        stage('Clean & Restore') {
            steps {
                echo 'Restoring NuGet packages...'
                bat 'dotnet restore WorldBank.Automation.sln'
            }
        }

        stage('Compile Solution') {
            steps {
                echo 'Building solution in Release mode...'
                bat 'dotnet build WorldBank.Automation.sln --configuration Release --no-restore'
            }
        }

        stage('Provision Playwright Engines') {
            steps {
                echo 'Installing Playwright browsers...'
                // Using PowerShell to trigger the Playwright installation
                powershell '''
                $env:PLAYWRIGHT_BROWSERS_PATH="0"
                pwsh bin/Release/net10.0/playwright.ps1 install chromium --with-deps
                '''
            }
        }

        stage('Execute Automated Quality Gates') {
            steps {
                echo 'Running NUnit Test Suite...'
                // The test step is wrapped in a try/catch (or handled by post) 
                // so the pipeline continues even if tests fail
                catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                    bat 'dotnet test WorldBank.Automation.sln --configuration Release --no-build'
                }
            }
        }
    }

    post {
        always {
            echo 'Generating Allure Quality Report...'
            // This triggers the Allure plugin to read the JSON files and build the HTML report
            allure includeProperties: false, jdk: '', results: [[path: 'WorldBank.Automation.Tests/bin/Release/net10.0/allure-results']]
            
            echo 'Archiving Playwright Traces for AI Triage...'
            archiveArtifacts artifacts: '**/playwright-traces/*.zip', allowEmptyArchive: true
        }
        failure {
            echo 'Pipeline failed! Check the Allure report or downloaded traces for the local AI Triage output.'
        }
    }
}