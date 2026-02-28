pipeline{
    agent any

    parameters {
        booleanParam(name: 'DeployTest', defaultValue: true, description: 'Deploy to test server')
        booleanParam(name: 'DeployLive', defaultValue: false, description: 'Deploy to live server')
    }

    stages {
        stage('Build BackupService Docker Image') {
            when {
                changeset "AmiaReforged.BackupService/**"
            }
            steps {
                script {
                    echo 'Changes detected in BackupService, building Docker image...'

                    dir('AmiaReforged.BackupService') {
                        // Read version from version.txt
                        def version = readFile('version.txt').trim()
                        echo "Building BackupService version ${version}"

                        // Build Docker image with version tag
                        sh "docker build -t amia-backup-service:${version} -t amia-backup-service:latest ."

                        echo "Docker image built successfully: amia-backup-service:${version}"
                    }
                }
            }
        }
        stage('Build AdminPanel Docker Image') {
            when {
                changeset "AmiaReforged.AdminPanel/**"
            }
            steps {
                script {
                    echo 'Changes detected in AdminPanel, building Docker image...'

                    // Read version from version.txt
                    def version = readFile('AmiaReforged.AdminPanel/version.txt').trim()
                    echo "Building AdminPanel version ${version}"

                    // Build Docker image with version tag (Dockerfile expects repo root as context)
                    sh "docker build -f AmiaReforged.AdminPanel/Dockerfile -t amia-admin-panel:${version} -t amia-admin-panel:latest ."

                    echo "Docker image built successfully: amia-admin-panel:${version}"
                }
            }
        }
        stage('Deploy Test') {
            when {
                    expression {
                        return params.DeployTest == true
                    }
            }
            steps {
                script {
                    if (!env.TEST_SERVER_BASE?.trim()) {
                        error "TEST_SERVER_BASE environment variable is required but was not set."
                    }
                }
                echo 'Deploying....'
				sh 'chmod +x stop-test.sh'
				withEnv(["AMIA_SERVER_DIR=${env.TEST_SERVER_BASE}/..".toString()]) {
				    sh 'bash stop-test.sh'
				}

                sh "dotnet publish AmiaReforged.Core --output ${env.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Core/"
                sh "dotnet publish AmiaReforged.System --output ${env.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.System/"
                sh "dotnet publish AmiaReforged.Classes --output ${env.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Classes/"
                sh "dotnet publish AmiaReforged.Races --output ${env.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Races/"
                sh "dotnet publish AmiaReforged.DMS --output ${env.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.DMS/"
                sh "dotnet publish AmiaReforged.PwEngine --output ${env.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.PwEngine/"

				sh 'chmod +x start-test.sh'
				withEnv(["AMIA_SERVER_DIR=${env.TEST_SERVER_BASE}/..".toString()]) {
				    sh 'bash start-test.sh'
				}
            }
        }
		stage('Deploy Live') {
            when {
                    expression {
                        return params.DeployLive == true
                    }
            }
            steps {
                script {
                    if (!env.LIVE_SERVER_BASE?.trim()) {
                        error "LIVE_SERVER_BASE environment variable is required but was not set."
                    }
                }
                echo 'Deploying....'
				sh 'chmod +x stop-live.sh'
				withEnv(["AMIA_SERVER_DIR=${env.LIVE_SERVER_BASE}/..".toString()]) {
				    sh 'bash stop-live.sh'
				}

                sh "dotnet publish AmiaReforged.Core --output ${env.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Core/"
                sh "dotnet publish AmiaReforged.System --output ${env.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.System/"
                sh "dotnet publish AmiaReforged.Classes --output ${env.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Classes/"
                sh "dotnet publish AmiaReforged.Races --output ${env.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Races/"
                sh "dotnet publish AmiaReforged.DMS --output ${env.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.DMS/"
                sh "dotnet publish AmiaReforged.PwEngine --output ${env.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.PwEngine/"

				sh 'chmod +x start-live.sh'
				withEnv(["AMIA_SERVER_DIR=${env.LIVE_SERVER_BASE}/..".toString()]) {
				    sh 'bash start-live.sh'
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
