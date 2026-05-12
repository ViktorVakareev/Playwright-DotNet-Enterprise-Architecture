pipeline {
    agent any

    options {
        ansiColor('xterm')
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    parameters {
        string(name: 'branch', defaultValue: 'main', description: 'The branch to checkout')
        string(name: 'inputTestFilter', defaultValue: 'TestCategory=Debug', description: 'The test filter to execute. User input is only applied for OnDemand jobs.')
        choice(name: 'browser', choices: ['ChromeHeadless', 'Chromium', 'Firefox', 'WebKit', 'Edge'], description: 'The browser')
        booleanParam(name: 'retryFailed', defaultValue: false, description: 'Whether retry of the failed tests should be used.')
        booleanParam(name: 'usePrebuilt', defaultValue: false, description: 'Whether the pipeline should skip the build step (only master branch)')
        string(name: 'qTestFolderUrl', defaultValue: '', description: 'qTest Folder Url')
        booleanParam(name: 'RUN_AI_TRIAGE', defaultValue: true, description: 'Enable local Llama 3 analysis on failure?')
    }

    environment {
        // Core Config
        ALLURE_RESULTS_DIR = "${WORKSPACE}/allure-results"
        AI_TRIAGE_ENABLED = "${params.RUN_AI_TRIAGE}"
        OLLAMA_API_URL = "http://host.docker.internal:11434"
        
        // Pass the UI parameters down to the C# code
        PLAYWRIGHT_BROWSER = "${params.browser}"
        RETRY_FAILED = "${params.retryFailed}"
        
        // .NET Config
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
            // SKIP THIS STAGE if usePrebuilt is checked
            when {
                expression { return !params.usePrebuilt }
            }
            steps {
                sh '''
                SLN_FILE=$(find . -name "*.sln" | head -n 1)
                echo "Building solution: $SLN_FILE"
                dotnet restore "$SLN_FILE"
                dotnet build "$SLN_FILE" --configuration Release --no-restore
                '''
            }
        }

        stage('Execute Automated Quality Gates') {
            // 1. Inject the ReportPortal API Key securely into the execution environment
            environment {
                REPORTPORTAL_SERVER_AUTHENTICATION_UUID = credentials('RP_API_KEY')
            }
            steps {
                script {
                    echo "Executing tests with filter: ${params.inputTestFilter} on ${params.browser}"
                    echo "Streaming live telemetry to ReportPortal..."
                    
                    catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                        // 2. We pass the filter directly from the UI.
                        // 3. We add the TRX logger. qTest relies heavily on .trx files for .NET test parsing.
                        // 4. The ReportPortal NUnit agent automatically reads the environment variable and streams results in real-time.
                        sh """
                        SLN_FILE=\$(find . -name "*.sln" | head -n 1)
                        
                        dotnet test "\$SLN_FILE" \
                            --configuration Release \
                            --no-build \
                            --filter "${params.inputTestFilter}" \
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
            echo 'Archiving Playwright Traces and AI Triage Reports...'
            archiveArtifacts artifacts: '**/playwright-traces/*.zip, **/AiTriage_Summary.md, **/TestResults/*.trx', allowEmptyArchive: true
            
            // Allure Integration
            allure includeProperties: false, jdk: '', results: [[path: 'bin/Release/net10.0/allure-results']]
            
            // qTest Integration Trigger
            script {
                if (params.qTestFolderUrl != '') {
                    echo "Triggering qTest upload to: ${params.qTestFolderUrl}"
                    
                    // Option A: If using the official Tricentis qTest Jenkins Plugin
                    // qtestPublisher buildNumber: "${env.BUILD_NUMBER}", projectId: '12345', testResultFormat: 'TRX', resultPattern: '**/TestResults/*.trx'
                    
                    // Option B: API Push (Enterprise Standard for custom folder URLs)
                    sh '''
                    # Example of parsing the .trx file and pushing to qTest API
                    # curl -X POST "https://your-domain.qtestnet.com/api/v3/projects/..." -H "Authorization: Bearer $QTEST_TOKEN" -d @./TestResults/TestResults.trx
                    echo "qTest upload script executed."
                    '''
                } else {
                    echo "qTest Folder URL is empty. Skipping qTest publish."
                }
            }
        }
    }
}