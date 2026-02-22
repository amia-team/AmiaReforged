pipeline{
    agent any

    parameters {
        string(name: 'TEST_SERVER_BASE', description: 'Base path for test server (e.g. /home/amia/amia_server/test_server)')
        string(name: 'LIVE_SERVER_BASE', description: 'Base path for live server (e.g. /home/amia/amia_server/server)')
        string(name: 'resources_dest_test', description: 'Destination for WorldEngine resources (test)')
        string(name: 'resources_dest_prod', description: 'Destination for WorldEngine resources (prod)')
        booleanParam(name: 'DeployTest', defaultValue: false, description: 'Deploy to test server')
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
                    if (!params.TEST_SERVER_BASE?.trim()) {
                        error "TEST_SERVER_BASE parameter is required but was not set."
                    }
                }
                echo 'Deploying....'
				sh "chown -R jenkins.jenkins ${WORKSPACE}"
				sh 'chmod +x stop-test.sh'
				sh 'bash stop-test.sh'

                sh "dotnet publish AmiaReforged.Core --output ${params.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Core/"
                sh "dotnet publish AmiaReforged.System --output ${params.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.System/"
                sh "dotnet publish AmiaReforged.Classes --output ${params.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Classes/"
                sh "dotnet publish AmiaReforged.Races --output ${params.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.Races/"
                sh "dotnet publish AmiaReforged.DMS --output ${params.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.DMS/"
                sh "dotnet publish AmiaReforged.PwEngine --output ${params.TEST_SERVER_BASE}/anvil/Plugins/AmiaReforged.PwEngine/"

                script {
                    if (params.resources_dest_test?.trim()) {
                        sh "rsync -av --delete AmiaReforged.PwEngine/Resources/WorldEngine ${params.resources_dest_test}/"
                    } else {
                        echo 'WARNING: resources_dest_test parameter is empty. Skipping WorldEngine resource deployment.'
                    }
                }

				sh "chown -R jenkins.jenkins ${WORKSPACE}"
				sh 'chmod +x start-test.sh'
				sh 'bash start-test.sh'
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
                    if (!params.LIVE_SERVER_BASE?.trim()) {
                        error "LIVE_SERVER_BASE parameter is required but was not set."
                    }
                }
                echo 'Deploying....'
				sh "chown -R jenkins.jenkins ${WORKSPACE}"
				sh 'chmod +x stop-live.sh'
				sh 'bash stop-live.sh'

                sh "dotnet publish AmiaReforged.Core --output ${params.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Core/"
                sh "dotnet publish AmiaReforged.System --output ${params.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.System/"
                sh "dotnet publish AmiaReforged.Classes --output ${params.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Classes/"
                sh "dotnet publish AmiaReforged.Races --output ${params.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.Races/"
                sh "dotnet publish AmiaReforged.DMS --output ${params.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.DMS/"
                sh "dotnet publish AmiaReforged.PwEngine --output ${params.LIVE_SERVER_BASE}/anvil/Plugins/AmiaReforged.PwEngine/"

                script {
                    if (params.resources_dest_prod?.trim()) {
                        sh "rsync -av --delete AmiaReforged.PwEngine/Resources/WorldEngine ${params.resources_dest_prod}/"
                    } else {
                        echo 'WARNING: resources_dest_prod parameter is empty. Skipping WorldEngine resource deployment.'
                    }
                }

				sh "chown -R jenkins.jenkins ${WORKSPACE}"
				sh 'chmod +x start-live.sh'
				sh 'bash start-live.sh'
            }
        }
    }
    post {
        success {
            echo 'Build success'
        }
    }
}
