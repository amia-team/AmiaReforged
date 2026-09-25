pipeline {
    agent any

    parameters {
        booleanParam(
            name: 'DeployTest',
            defaultValue: true,
            description: 'Deploy to test server'
        )

        booleanParam(
            name: 'DeployLive',
            defaultValue: false,
            description: 'Deploy to live server'
        )

        booleanParam(
            name: 'RestartServer',
            defaultValue: true,
            description: 'Stop and restart the server around deploys (disable for hot-reload sessions)'
        )
    }

    stages {
        stage('Deploy Test') {
            when {
                expression {
                    params.DeployTest
                }
            }

            steps {
                script {
                    if (!env.TEST_SERVER_BASE?.trim()) {
                        error 'TEST_SERVER_BASE environment variable is required but was not set.'
                    }

                    if (params.RestartServer) {
                        withEnv(["AMIA_SERVER_DIR=${env.TEST_SERVER_BASE}/.."]) {
                            sh 'bash stop-test.sh'
                        }
                    } else {
                        echo 'Skipping server stop (RestartServer is false)'
                    }
                }

                sh """
                    dotnet publish AmiaReforged.Core/AmiaReforged.Core.csproj \
                        -c Release \
                        -o "${TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Core"

                    dotnet publish AmiaReforged.System/AmiaReforged.System.csproj \
                        -c Release \
                        -o "${TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.System"

                    dotnet publish AmiaReforged.Classes/AmiaReforged.Classes.csproj \
                        -c Release \
                        -o "${TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Classes"

                    dotnet publish AmiaReforged.Races/AmiaReforged.Races.csproj \
                        -c Release \
                        -o "${TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Races"

                    dotnet publish AmiaReforged.DMS/AmiaReforged.DMS.csproj \
                        -c Release \
                        -o "${TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.DMS"

                    dotnet publish AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
                        -c Release \
                        -o "${TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.PwEngine"
                """

                script {
                    if (params.RestartServer) {
                        withEnv(["AMIA_SERVER_DIR=${env.TEST_SERVER_BASE}/.."]) {
                            sh 'bash start-test.sh'
                        }
                    } else {
                        echo 'Skipping server start (RestartServer is false)'
                    }
                }
            }
        }

        stage('Deploy Live') {
            when {
                expression {
                    params.DeployLive
                }
            }

            steps {
                script {
                    if (!env.LIVE_SERVER_BASE?.trim()) {
                        error 'LIVE_SERVER_BASE environment variable is required but was not set.'
                    }

                    if (params.RestartServer) {
                        withEnv(["AMIA_SERVER_DIR=${env.LIVE_SERVER_BASE}/.."]) {
                            sh 'bash stop-live.sh'
                        }
                    } else {
                        echo 'Skipping server stop (RestartServer is false)'
                    }
                }

                sh """
                    dotnet publish AmiaReforged.Core/AmiaReforged.Core.csproj \
                        -c Release \
                        -o "${LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Core"

                    dotnet publish AmiaReforged.System/AmiaReforged.System.csproj \
                        -c Release \
                        -o "${LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.System"

                    dotnet publish AmiaReforged.Classes/AmiaReforged.Classes.csproj \
                        -c Release \
                        -o "${LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Classes"

                    dotnet publish AmiaReforged.Races/AmiaReforged.Races.csproj \
                        -c Release \
                        -o "${LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Races"

                    dotnet publish AmiaReforged.DMS/AmiaReforged.DMS.csproj \
                        -c Release \
                        -o "${LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.DMS"

                    dotnet publish AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
                        -c Release \
                        -o "${LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.PwEngine"
                """

                script {
                    if (params.RestartServer) {
                        withEnv(["AMIA_SERVER_DIR=${env.LIVE_SERVER_BASE}/.."]) {
                            sh 'bash start-live.sh'
                        }
                    } else {
                        echo 'Skipping server start (RestartServer is false)'
                    }
                }
            }
        }
    }

    post {
        success {
            echo 'Build success'
        }
    }
}
