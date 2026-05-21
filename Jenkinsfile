pipeline {
    agent any

    options {
        ansiColor('xterm')
        timeout(time: 30, unit: 'MINUTES')
        // Keeps a max of 10 builds, but aggressively deletes artifacts (like .webm videos) after 2 days.
        buildDiscarder(logRotator(numToKeepStr: '10', artifactDaysToKeepStr: '2'))
        disableConcurrentBuilds()
    }

    parameters {
        string(name: 'branch', defaultValue: 'main', description: 'The branch to checkout')
        choice(name: 'TARGET_ENV', choices: ['dev', 'test', 'prod'], description: 'Select the target cloud environment for test execution')
        string(name: 'TEST_FILTER', defaultValue: '', description: 'Filter tests. Leave blank to run all tests')
        choice(name: 'browser', choices: ['ChromeHeadless', 'Chromium', 'Firefox', 'WebKit', 'Edge'], description: 'The browser')
        booleanParam(name: 'retryFailed', defaultValue: false, description: 'Whether retry of the failed tests should be used.')
        booleanParam(name: 'usePrebuilt', defaultValue: false, description: 'Skip build step (main branch only)')
        string(name: 'qTestFolderUrl', defaultValue: '', description: 'qTest Folder Url')
        booleanParam(name: 'RUN_AI_TRIAGE', defaultValue: true, description: 'Enable local Llama 3 analysis on failure?')
    }

    environment {
        TARGET_ENV = "${params.TARGET_ENV}"
        ALLURE_RESULTS_DIR = "bin/Release/net10.0/allure-results"
        AI_TRIAGE_ENABLED = "${params.RUN_AI_TRIAGE}"
        OLLAMA_API_URL = "http://host.docker.internal:11434"
        PLAYWRIGHT_BROWSER = "${params.browser}"
        RETRY_FAILED = "${params.retryFailed}"
        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "1"
        DOTNET_ROOT = "${HOME}/.dotnet"
        PATH = "${HOME}/.dotnet:${HOME}/.dotnet/tools:${env.PATH}"
    }

    stages {
        stage('Execute Health Check') {
            steps {
                script {
                    def targetUrl = "https://viktorvakareev.github.io/Playwright-DotNet-Enterprise-Architecture/WorldBankMockApp/${params.TARGET_ENV}/"
                    echo "Pinging Health Check Endpoint at: ${targetUrl}"

                    def statusCode = sh(
                        script: "curl -s -L -o /dev/null -w \"%{http_code}\" ${targetUrl} || echo '000'",
                        returnStdout: true
                    ).trim()

                    if (statusCode == "200") {
                        echo "✅ App is UP and Healthy! (Status: 200)"
                    } else {
                        error("❌ Health check failed! Received HTTP Status: ${statusCode} for ${targetUrl}")
                    }
                }
            }
        }

        stage('Checkout Code') {
            steps {
                echo "Fetching branch: ${params.branch}..."
                git branch: "${params.branch}", url: 'https://github.com/ViktorVakareev/Playwright-DotNet-Enterprise-Architecture.git'
            }
        }

        stage('Clean, Restore & Compile') {
            when {
                anyOf {
                    expression { params.branch != 'main' }
                    expression { params.branch == 'main' && !params.usePrebuilt }
                }
            }
            steps {
                sh '''
                echo "--- Restoring and Building ---"
                dotnet restore src/
                dotnet build src/ --configuration Release --no-restore

                echo "--- Installing PowerShell Core (pwsh) ---"
                dotnet tool update --global PowerShell
                '''
            }
        }

        stage('Execute Automated Quality Gates') {
            environment {
                REPORTPORTAL_SERVER_AUTHENTICATION_UUID = credentials('RP_API_KEY')
            }
            steps {
                script {
                    echo "--- Checking Outbound Network & DNS ---"
                    sh "curl -I https://viktorvakareev.github.io || echo 'WARNING: Cannot reach GitHub Pages!'"

                    sh "ls -la src/bin/Release/net10.0/ReportPortal.config.json || echo 'CRITICAL: Config file missing!'"

                    def filterFlag = ""
                    if (params.TEST_FILTER) {
                        filterFlag = "--filter \"${params.TEST_FILTER}\""
                    }

                    echo "====================================================="
                    echo "🚀 INITIATING PLAYWRIGHT SUITE"
                    echo "🌍 TARGET ENVIRONMENT: ${env.TARGET_ENV.toUpperCase()}"
                    echo "🔍 TEST FILTER: ${params.TEST_FILTER ?: 'ALL'}"
                    echo "====================================================="

                    catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                        sh """
                        dotnet test src/ \
                            --configuration Release \
                            --no-build \
                            ${filterFlag} \
                            --logger 'trx;LogFileName=TestResults.trx' \
                            --logger 'junit;LogFilePath=junit-results.xml' \
                            --results-directory ./TestResults \
                            -- NUnit.NumberOfTestWorkers=4
                        """
                    }
                }
            }
        }
    }

    post {
        always {
            echo "Pipeline execution complete for environment: ${env.TARGET_ENV}"
            echo 'Archiving Playwright Traces and AI Triage Reports...'
            archiveArtifacts artifacts: '**/playwright-traces/*.zip, **/AiTriage_Summary.md, **/TestResults/*.trx', allowEmptyArchive: true
            allure includeProperties: false, results: [[path: "${env.ALLURE_RESULTS_DIR}"]]
            
            // Scan for the XML result files and publish them
            junit testResults: '**/TestResults/*.xml', allowEmptyResults: true, keepLongStdio: true

            script {
                if (params.qTestFolderUrl != '') {
                    echo "Triggering qTest upload to: ${params.qTestFolderUrl}"
                    sh 'echo "qTest upload script executed."'
                } else {
                    echo "qTest Folder URL is empty. Skipping qTest publish."
                }
            }
        }
    }
}