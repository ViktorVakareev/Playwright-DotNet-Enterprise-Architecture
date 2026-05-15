pipeline {
    agent any

    options {
        ansiColor('xterm')
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
    }

    parameters {
        string(name: 'branch', defaultValue: 'main', description: 'The branch to checkout')
        
        // NEW: Environment selection dropdown
        choice(name: 'TARGET_ENV', choices: ['dev', 'test', 'prod'], description: 'Select the target cloud environment for test execution')
        
        string(name: 'TEST_FILTER', defaultValue: '', description: 'Filter tests. Leave blank to run all tests')
        choice(name: 'browser', choices: ['ChromeHeadless', 'Chromium', 'Firefox', 'WebKit', 'Edge'], description: 'The browser')
        booleanParam(name: 'retryFailed', defaultValue: false, description: 'Whether retry of the failed tests should be used.')
        booleanParam(name: 'usePrebuilt', defaultValue: false, description: 'Skip build step (main branch only)')
        string(name: 'qTestFolderUrl', defaultValue: '', description: 'qTest Folder Url')
        booleanParam(name: 'RUN_AI_TRIAGE', defaultValue: true, description: 'Enable local Llama 3 analysis on failure?')
    }

    environment {
        // Expose TARGET_ENV so Playwright AppConfig.cs can read it
        TARGET_ENV = "${params.TARGET_ENV}"
        
        // Use a consistent directory for results
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
                # Playwright strictly requires pwsh to run its native setup scripts
                dotnet tool update --global PowerShell
                
                echo "--- Installing Browser Binaries (Official API) ---"
                # Execute the officially generated PowerShell script
                pwsh src/bin/Release/net10.0/playwright.ps1 install chromium
                '''
            }
        }

        stage('Execute Automated Quality Gates') {
            environment {
                REPORTPORTAL_SERVER_AUTHENTICATION_UUID = credentials('RP_API_KEY')
            }
            steps {
                script {
                    // 1. Verify ReportPortal configuration
                    sh "ls -la bin/Release/net10.0/ReportPortal.config.json || echo 'CRITICAL: Config file missing!'"
                    
                    // 2. Setup dynamic filtering based on your parameters
                    def filterFlag = params.TEST_FILTER ? "--filter \"${params.TEST_FILTER}\"" : ""
                    
                    echo "====================================================="
                    echo "🚀 INITIATING PLAYWRIGHT SUITE"
                    echo "🌍 TARGET ENVIRONMENT: ${env.TARGET_ENV.toUpperCase()}"
                    echo "🔍 TEST FILTER: ${params.TEST_FILTER ?: 'ALL'}"
                    echo "====================================================="
                    
                    // 3. Execute the test suite directly against GitHub Pages
                    catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                        sh """
                        SLN_FILE=\$(find . -name "*.sln" | head -n 1)
                        dotnet test "\$SLN_FILE" \
                            --configuration Release \
                            --no-build \
                            ${filterFlag} \
                            --logger "trx;LogFileName=TestResults.trx" \
                            --results-directory ./TestResults
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
} // End of Pipeline