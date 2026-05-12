pipeline {
    agent any

    options {
        ansiColor('xterm')
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    parameters {
        string(name: 'branch', defaultValue: 'main', description: 'The branch to checkout')
        string(name: 'inputTestFilter', defaultValue: '', description: 'NUnit filter (e.g., Category=Smoke). Leave blank to run all tests.')
        choice(name: 'browser', choices: ['ChromeHeadless', 'Chromium', 'Firefox', 'WebKit', 'Edge'], description: 'The browser')
        booleanParam(name: 'retryFailed', defaultValue: false, description: 'Whether retry of the failed tests should be used.')
        booleanParam(name: 'usePrebuilt', defaultValue: false, description: 'Skip build step ('main' branch only)')
        string(name: 'qTestFolderUrl', defaultValue: '', description: 'qTest Folder Url')
        booleanParam(name: 'RUN_AI_TRIAGE', defaultValue: true, description: 'Enable local Llama 3 analysis on failure?')
    }

    environment {
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
                    // Rule 1: Always build if NOT on main
                    expression { params.branch != 'main' }
                    // Rule 2: Build on main ONLY if 'usePrebuilt' is false
                    expression { params.branch == 'main' && !params.usePrebuilt }
                }
            }
            steps {
                sh '''
                SLN_FILE=$(find . -name "*.sln" | head -n 1)
                dotnet restore "$SLN_FILE"
                dotnet build "$SLN_FILE" --configuration Release --no-restore
                '''
            }
        }        

        stage('Execute Automated Quality Gates') {
            environment {
                REPORTPORTAL_SERVER_AUTHENTICATION_UUID = credentials('RP_API_KEY')
            }
            steps {
                script {
                    def filterFlag = params.inputTestFilter ? "--filter \"${params.inputTestFilter}\"" : ""
                    echo "Executing tests. Filter: ${params.inputTestFilter ?: 'ALL'}"
                    
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
    } // End of Stages

    post {
        always {
            echo 'Archiving Playwright Traces and AI Triage Reports...'
            archiveArtifacts artifacts: '**/playwright-traces/*.zip, **/AiTriage_Summary.md, **/TestResults/*.trx', allowEmptyArchive: true
            
            // Use the environment variable for Allure consistency
            allure includeProperties: false, jdk: '', results: [[path: "${env.ALLURE_RESULTS_DIR}"]]
            
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